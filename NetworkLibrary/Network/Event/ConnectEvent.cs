using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkCore.Event
{
    public class ConnectEvent : SocketAsyncEventArgs
    {
        private Action<Socket?> _OnConnected;
        public ConnectEvent(IPEndPoint end_point, Action<Socket?> onConnected)
        {
            var socket = new Socket(end_point.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            base.Completed += OnConnected;
            base.RemoteEndPoint = end_point;
            base.UserToken = socket;
            this._OnConnected = onConnected;
        }

        public bool RegisterConnect()
        {
            var socket = base.UserToken as Socket;
            if (socket == null) { return false; }

            var pending = socket.ConnectAsync(this);
            if (pending == false)
            {
                OnConnected(null, this);
            }

            return true;
        }

        void OnConnected(object? sender, SocketAsyncEventArgs e) 
        {
            var connect_event = e as ConnectEvent;
            if (connect_event == null) { return; }

            if (connect_event.SocketError != SocketError.Success)
            {
                Console.WriteLine($"[ConnectEvent] Connect failed. Socket Error[{connect_event.SocketError}]");
                return;
            }

            _OnConnected.Invoke(UserToken as Socket);
        }
    }
}
