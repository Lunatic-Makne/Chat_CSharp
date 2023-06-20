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
        private Socket ListenSocket;
        public AcceptEvent(Socket socket) 
        {
            ListenSocket = socket;
            base.UserToken = socket;
            base.Completed += Accepted;
        }

        public void Accepted(object? sender, SocketAsyncEventArgs e)
        {
            if (e.AcceptSocket != null)
            {
                var connection = new ReceiveEvent(e.AcceptSocket, NetworkManager.Inst.GenSocketEventID());
                e.AcceptSocket = null;
            }

            ListenSocket.AcceptAsync(e);
        }
    }
}
