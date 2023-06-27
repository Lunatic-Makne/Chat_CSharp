using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkCore.Event
{
    public class ReceiveEvent : SocketAsyncEventArgs
    {
        private TCPConnection _Connection;

        public ReceiveEvent(TCPConnection connection)
        {
            _Connection = connection;
            base.UserToken = _Connection;
            base.Completed += this.EventCompleted;
        }

        private void EventCompleted(object? sender, SocketAsyncEventArgs e)
        {
            _Connection.ProcessReceive(this);
        }
    }
}
