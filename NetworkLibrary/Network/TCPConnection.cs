using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NetworkCore.Event;

namespace NetworkCore
{
    public abstract class TCPConnection
    {
        public long ConnectionID { get; private set; } = 0;
        public IPEndPoint RemoteAddr { get; private set; } = new IPEndPoint(0, 0);

        [AllowNull]
        private Socket _ConnSocket;
        private int _Disconnected = 0;

        private SendEvent _SendEvent;
        private Queue<ArraySegment<byte>> _SendQueue = new Queue<ArraySegment<byte>>();
        private List<ArraySegment<byte>> _SendPendingList = new List<ArraySegment<byte>>();
        private object _SendLock = new object();

        protected static readonly int MAX_BUFFER_SIZE = 8 * 1024;
        private RecvBuffer _RecvBuffer = new RecvBuffer(MAX_BUFFER_SIZE);
        private ReceiveEvent _ReceiveEvent;

        public TCPConnection()
        {
            _SendEvent = new SendEvent(this);
            _ReceiveEvent = new ReceiveEvent(this);
        }

        public void SetSocket(Socket socket)
        {
            _ConnSocket = socket;
            if (_ConnSocket != null)
            {
                var ep = _ConnSocket.RemoteEndPoint as IPEndPoint;
                if (ep != null) { this.RemoteAddr = ep; }
            }
        }

        private void RegisterRecv()
        {
            if (_ConnSocket == null) { return; }

            _RecvBuffer.Clear();
            var segment = _RecvBuffer.WriteSegment;
            _ReceiveEvent.SetBuffer(segment.Array, segment.Offset, segment.Count);

            var pending = _ConnSocket.ReceiveAsync(_ReceiveEvent);
            if (pending == false)
            {
                ProcessReceive(_ReceiveEvent);
            }
        }

        public void Start()
        {
            RegisterRecv();
            OnConnected(RemoteAddr);
        }

        public void ProcessReceive(SocketAsyncEventArgs e)
        {
            do
            {
                var receive_event = e as ReceiveEvent;
                if (receive_event != null)
                {
                    if (receive_event.BytesTransferred > 0 && receive_event.SocketError == SocketError.Success)
                    {
                        try
                        {
                            // Move WritePos First
                            if (_RecvBuffer.OnRecv(receive_event.BytesTransferred) == false)
                            {
                                break;
                            }

                            var process_length = OnRecv(_RecvBuffer.ReadSegment);
                            if (process_length < 0 || process_length > _RecvBuffer.DataSize)
                            {
                                break;
                            }

                            if (_RecvBuffer.OnRead(process_length) == false)
                            {
                                break;
                            }

                            RegisterRecv();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[TCPConnection] ProcessReceive failed. exception : \n{ex}");
                        }

                        return;
                    }
                }
            } while (false);
            
            CloseConnection();
        }

        #region Connection Event Callback
        public abstract int OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int byte_transferred);
        public abstract void OnConnected(EndPoint ep);
        public abstract void OnDisconnected(EndPoint ep);
        #endregion // Connection Event Callback

        public void CloseConnection()
        {
            if (Interlocked.Exchange(ref _Disconnected, 1) == 1)
                return;

            try
            {
                if (_ConnSocket != null)
                {
                    _ConnSocket.Shutdown(SocketShutdown.Both);
                    _ConnSocket.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            OnDisconnected(RemoteAddr);
        }
                
        public void SendTemp(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var segment = SendBufferHelper.Open(buffer.Length);
            if (segment != null && segment.HasValue && segment.Value.Array != null)
            {
                Array.Copy(buffer, 0, segment.Value.Array, segment.Value.Offset, buffer.Length);
                SendBufferHelper.Close(buffer.Length);

                Send(segment.Value);
            }
        }

        public void Send(ArraySegment<byte> send_buffer)
        {
            lock(_SendLock)
            {
                _SendQueue.Enqueue(send_buffer);
                if (_SendPendingList.Count == 0)
                {
                    RegisterSend();
                }
            }
        }

        void RegisterSend()
        {
            while (_SendQueue.Count > 0)
            {
                var buffer = _SendQueue.Dequeue();
                _SendPendingList.Add(buffer);
            }

            _SendEvent.BufferList = _SendPendingList;

            var pending = _ConnSocket.SendAsync(_SendEvent);
            if (pending == false)
            {
                ProcessSend(_SendEvent);
            }
        }

        public void ProcessSend(SocketAsyncEventArgs event_args)
        {
            lock(_SendLock)
            {
                do
                {
                    var send_event = event_args as SendEvent;
                    if (send_event != null)
                    {
                        if (send_event.BytesTransferred > 0 && send_event.SocketError == SocketError.Success)
                        {
                            _SendEvent.BufferList = null;
                            _SendPendingList.Clear();

                            OnSend(send_event.BytesTransferred);

                            if (_SendQueue.Count > 0)
                            {
                                RegisterSend();
                            }

                            return;
                        }
                    }
                } while (false);
            }

            CloseConnection();
        }
    }

    public abstract class PacketHandleConnection : TCPConnection
    {
        public static readonly int HEADER_SIZE = 2 + 8; // size + id

        public sealed override int OnRecv(ArraySegment<byte> buffer)
        {
            int process_len = 0;

            while (true)
            {
                if (buffer.Array == null) { break; }
                if (buffer.Count < HEADER_SIZE) { break; }

                var packet_size = BitConverter.ToInt16(buffer.Array, buffer.Offset);
                if (packet_size == 0) {  break; }
                if (buffer.Count < packet_size) { break; }

                OnReceivePacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, packet_size));

                process_len += packet_size;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + packet_size, buffer.Count - packet_size);
            }

            return process_len;
        }

        public abstract void OnReceivePacket(ArraySegment<byte> buffer);

        public void SendPacket(Protocol.IPacket packet)
        {
            var openSegment = packet.Write(MAX_BUFFER_SIZE);
            if (openSegment.HasValue && openSegment.Value.Array != null)
            {
                Send(openSegment.Value);
            }
        }
    }
}
