using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer.Network
{
    public class TCPConnection
    {
        public long ConnectionID { get; private set; } = 0;
        public IPEndPoint RemoteAddr { get; private set; } = new IPEndPoint(0, 0);

        private Socket _ConnSocket;
        private int _Disconnected = 0;

        private SendEvent _SendEvent;
        private Queue<byte[]> _SendQueue = new Queue<byte[]>();
        private bool _SendPendingStatus = false;
        private object _SendLock = new object();

        public TCPConnection(Socket conn_socket, long connection_id)
        {
            ConnectionID = connection_id;
            _ConnSocket = conn_socket;
            _SendEvent = new SendEvent(this);

            var ep = conn_socket.RemoteEndPoint as IPEndPoint;
            if (ep != null) { this.RemoteAddr = ep; }
        }

        [AllowNull]
        public Action<TCPConnection> OnStart { get; set; }

        private void RegisterRecv(ReceiveEvent receive_event)
        {
            var pending = _ConnSocket.ReceiveAsync(receive_event);
            if (pending == false)
            {
                ProcessReceive(receive_event);
            }
        }
        public void Start()
        {
            Console.WriteLine($"Connected. Addr[{RemoteAddr}]");

            var receive_event = new ReceiveEvent(this);
            RegisterRecv(receive_event);

            if (OnStart != null) { OnStart(this); }
        }

        public void ProcessReceive(SocketAsyncEventArgs e)
        {
            var receive_event = e as ReceiveEvent;
            if (receive_event != null)
            {
                if (_ConnSocket.Connected && receive_event.BytesTransferred > 0)
                {
                    var data = receive_event.Buffer;
                    if (data != null)
                    {
                        // [TODO] Temp
                        var message = Encoding.UTF8.GetString(data);
                        Console.WriteLine($"[C2S] {message}");
                    }

                    receive_event.ClearBuffer();

                    RegisterRecv(receive_event);
                }
                else
                {
                    CloseConnection();
                }
            }
            else
            {
                CloseConnection();
            }
        }

        public delegate void CloseCallbackType();
        [AllowNull]
        public CloseCallbackType OnClose { get; set; }

        public void CloseConnection()
        {
            if (Interlocked.Exchange(ref _Disconnected, 1) == 1)
                return;

            try
            {
                _ConnSocket.Shutdown(SocketShutdown.Both);
                _ConnSocket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine($"Disconnected. Addr[{RemoteAddr}]");
            NetworkManager.Inst.UnregisterConnection(ConnectionID);

            if (OnClose != null) { OnClose(); }
        }

        

        // Temp
        public void SendTemp(string message)
        {
            var send_buff = Encoding.UTF8.GetBytes(message);
            _ConnSocket.SendAsync(send_buff);
        }

        
        public void Send(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            Send(buffer);
        }

        public void Send(byte[] send_buffer)
        {
            lock(_SendLock)
            {
                _SendQueue.Enqueue(send_buffer);
                if (_SendPendingStatus == false)
                {
                    RegisterSend();
                }
            }
        }

        void RegisterSend()
        {
            _SendPendingStatus = true;

            var buffer = _SendQueue.Dequeue();
            _SendEvent.SetBuffer(buffer, 0, buffer.Length);

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
                        if (event_args.BytesTransferred > 0 && event_args.SocketError == SocketError.Success)
                        {
                            if (_SendQueue.Count > 0)
                            {
                                RegisterSend();
                            }
                            else
                            {
                                _SendPendingStatus = false;
                            }

                            return;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                } while (false);
            }

            CloseConnection();
        }
    }
}
