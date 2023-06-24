using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer.Network
{
    public class SendEvent : SocketAsyncEventArgs
    {
        private TCPConnection _Connection;

        public SendEvent(TCPConnection connection)
        {
            _Connection = connection;

            base.UserToken = _Connection;
            base.Completed += this.EventCompleted;
        }

        private void EventCompleted(object? sender, SocketAsyncEventArgs e)
        {
            _Connection.ProcessSend(this);
        }
    }
}
