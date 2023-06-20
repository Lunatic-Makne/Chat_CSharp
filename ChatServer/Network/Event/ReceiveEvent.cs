using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatServer.Network
{
    public class ReceiveEvent : SocketAsyncEventArgs
    {
        private long SocketEventID = 0;
        private Socket ConnSocket;
        private StringBuilder MemBuilder = new StringBuilder();
        private IPEndPoint RemoteAddr = new IPEndPoint(0, 0);

        private static readonly int MAX_BUFFER_SIZE = sizeof(char) * 8 * 1024;
        public ReceiveEvent(Socket socket, long eventID)
        {
            this.SocketEventID = eventID;
            this.ConnSocket = socket;
            base.SetBuffer(new byte[MAX_BUFFER_SIZE], 0, MAX_BUFFER_SIZE);
            base.UserToken = socket;
            base.Completed += this.EventCompleted;
            var ep = socket.RemoteEndPoint as IPEndPoint;
            if (ep != null) { this.RemoteAddr = ep; }

            if (NetworkManager.Inst.RegisterSocketEvent(eventID, this))
            {
                this.ConnSocket.ReceiveAsync(this);
                Console.WriteLine($"Connected. Addr[{this.RemoteAddr}]");
            }
            else
            {
                Console.WriteLine($"Register Connection Failed. Addr[{this.RemoteAddr}], ConnID[{eventID}]");
                ConnSocket.Disconnect(false);
                ConnSocket.Close();
            }
        }

        private void EventCompleted(object? sender, SocketAsyncEventArgs e)
        {
            if (ConnSocket.Connected && base.BytesTransferred > 0)
            {
                var data = e.Buffer;
                if (data != null)
                {
                    var message = Encoding.ASCII.GetString(data);
                    Console.WriteLine($"Received message. [{message}]");
                }

                SetBuffer(new byte[MAX_BUFFER_SIZE], 0, MAX_BUFFER_SIZE);
                ConnSocket.ReceiveAsync(this);
            }
            else
            {
                Console.WriteLine($"Disconnected. Addr[{RemoteAddr}]");
                NetworkManager.Inst.UnregisterConnection(SocketEventID);
                ConnSocket.Close();
            }
        }
    }
}
