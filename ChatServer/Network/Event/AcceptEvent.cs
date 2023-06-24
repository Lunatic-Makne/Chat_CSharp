using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer.Network
{
    public class AcceptEvent : SocketAsyncEventArgs
    {
        private Socket _ListenSocket;
        private Action<Socket?> _OnAccept;

        public AcceptEvent(Socket socket, Action<Socket?> on_accept) 
        {
            _ListenSocket = socket;
            _OnAccept = on_accept;
            base.UserToken = socket;
            base.Completed += Accepted;
        }

        public void Accepted(object? sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                _OnAccept.Invoke(e.AcceptSocket);
                e.AcceptSocket = null;
            }
            else
            {
                Console.WriteLine(e.SocketError.ToString());
            }

            var pending = _ListenSocket.AcceptAsync(e);
            if (pending == false)
            {
                Accepted(sender, e);
            }
        }
    }
}
