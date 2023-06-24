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
        private Queue<byte[]> _SendQueue = new Queue<byte[]>();
        private List<ArraySegment<byte>> _SendPendingList = new List<ArraySegment<byte>>();
        private object _SendLock = new object();

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
                        if (receive_event.Buffer != null)
                        {
                            OnRecv(new ArraySegment<byte>(receive_event.Buffer, receive_event.Offset, receive_event.BytesTransferred));
                        }

                        RegisterRecv();

                        return;
                    }
                }
            } while (false);
            
            CloseConnection();
        }

        public abstract void OnRecv(ArraySegment<byte> buffer);
        public abstract void OnSend(int byte_transferred);
        public abstract void OnConnected(EndPoint ep);
        public abstract void OnDisconnected(EndPoint ep);

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
            Send(buffer);
        }

        public void Send(byte[] send_buffer)
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
                _SendPendingList.Add(new ArraySegment<byte>(buffer, 0, buffer.Length));
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
}
