using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkCore.Event
{
    public class ReceiveEvent : SocketAsyncEventArgs
    {
        private TCPConnection _Connection;

        private static readonly int MAX_BUFFER_SIZE = sizeof(char) * 8 * 1024;
        public ReceiveEvent(TCPConnection connection)
        {
            _Connection = connection;
            base.SetBuffer(new byte[MAX_BUFFER_SIZE], 0, MAX_BUFFER_SIZE);
            base.UserToken = _Connection;
            base.Completed += this.EventCompleted;
        }

        private void EventCompleted(object? sender, SocketAsyncEventArgs e)
        {
            _Connection.ProcessReceive(this);
        }
    }
}
