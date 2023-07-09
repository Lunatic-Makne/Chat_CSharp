using NetworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Protocol;
using Protocol.ClientToServer;
using Protocol.ServerToClient;
using Protocol.SharedStruct;

namespace DummyClient.Connection
{
    class PacketHandler
    {
        public static bool Dispatch(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null) { return false; }

            int offset = 0;
            var size = BitConverter.ToInt16(buffer.Array, buffer.Offset + offset);
            offset += sizeof(short);
            var id = BitConverter.ToInt64(buffer.Array, buffer.Offset + offset);
            offset += sizeof(long);

            if (size > buffer.Array.Length) { return false; }

            switch ((PacketId)id)
            {
                case PacketId._WELCOME_:
                    var packet = new Welcome();
                    packet.Read(new ArraySegment<byte>(buffer.Array, buffer.Offset + offset, buffer.Count - offset));

                    Console.WriteLine($"[S2C][WELCOME] UserId: [{packet.UserId}]");
                    foreach(var info in packet.UserList)
                    {
                        Console.WriteLine($"[UserList] UserId: [{info.UserId}]");
                    }
                    break;
                default:
                    Console.WriteLine($"[PacketHandler] Unregistered PacketId[{id}].");
                    return false;
            }

            return true;
        }
    }
    class ServerConnection : PacketHandleConnection
    {

        public override void OnConnected(EndPoint ep)
        {
            Console.WriteLine($"Connect to [{ep}]");

            SendHi();

            Thread.Sleep(1000);

            CloseConnection();
        }

        public override void OnDisconnected(EndPoint ep)
        {
            Console.WriteLine($"Disconnected. Addr[{ep}]");
        }

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

        public void SendHi()
        {
            var packet = new Hi { Name = UserInfo.Inst.Name };
            SendPacket(packet);
        }
    }
}
