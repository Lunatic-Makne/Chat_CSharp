using ChatServer.User;
using NetworkCore;
using Protocol;
using Protocol.ClientToServer;
using Protocol.ServerToClient;
using Protocol.SharedStruct;
using System.Net;
using System.Text;

namespace ChatServer.Connection
{
    public class UserConnection : PacketHandleConnection
    {
        public override void OnReceivePacket(ArraySegment<byte> buffer)
        {
            if (Protocol.ClientToServer.PacketHandler.Inst.Dispatch(this, buffer) == false)
            {
                CloseConnection();
            }
        }

        public override void OnSend(int byte_transferred)
        {
            Console.WriteLine($"Transferred : [{byte_transferred}]");
        }

        public override void OnConnected(EndPoint ep)
        {
            Console.WriteLine($"Connected. Addr[{RemoteAddr}]");
        }

        public override void OnDisconnected(EndPoint ep)
        {
            Console.WriteLine($"Disconnected. Addr[{RemoteAddr}]");
            UserManager.Inst.RemoveUser(ConnectionID);
            NetworkManager.Inst.UnregisterTCPConnection(ConnectionID);
        }
    }
}
