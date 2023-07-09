using NetworkCore;
using Protocol;
using Protocol.ClientToServer;
using Protocol.ServerToClient;
using Protocol.SharedStruct;
using System.Net;
using System.Text;

namespace ChatServer.Connection
{
    public class PacketHandler
    {
        public static bool Dispatch(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null) {  return false; }
                        
            int offset = 0;
            var size = BitConverter.ToInt16(buffer.Array, buffer.Offset + offset);
            offset += sizeof(short);
            var id = BitConverter.ToInt64(buffer.Array, buffer.Offset + offset);
            offset += sizeof(long);

            if (offset + size > buffer.Array.Length) {  return false; }

            switch ((PacketId)id)
            {
                case PacketId._HI_:
                    var packet = new Hi();
                    packet.Read(new ArraySegment<byte>(buffer.Array, buffer.Offset + offset, buffer.Count - offset));

                    Console.WriteLine($"[C2S][HI] name: [{packet.Name}]");
                    break;
                default:
                    Console.WriteLine($"[PacketHandler] Unregistered PacketId[{id}].");
                    return false;
            }

            return true;
        }
    }

    public class UserConnection : PacketHandleConnection
    {
        public override void OnReceivePacket(ArraySegment<byte> buffer)
        {
            if (PacketHandler.Dispatch(buffer) ==false)
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

            var packet = new Welcome { UserId = Random.Shared.Next() };
            packet.UserList.Add(new UserInfo { UserId = Random.Shared.Next() });
            packet.UserList.Add(new UserInfo { UserId = Random.Shared.Next() });
            packet.UserList.Add(new UserInfo { UserId = Random.Shared.Next() });
            SendPacket(packet);

            foreach(var item in packet.UserList)
            {
                Console.WriteLine($"TEST: {item.UserId}");
            }
        }

        public override void OnDisconnected(EndPoint ep)
        {
            Console.WriteLine($"Disconnected. Addr[{RemoteAddr}]");
            NetworkManager.Inst.UnregisterTCPConnection(ConnectionID);
        }
    }
}
