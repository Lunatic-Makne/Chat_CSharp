using NetworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChatServer.Connection
{
    public class UserConnection : PacketHandleConnection
    {
        public override void OnReceivePacket(ArraySegment<byte> buffer)
        {
            if (buffer.Array != null)
            {
                var size = BitConverter.ToInt16(buffer.Array, buffer.Offset);
                var id = BitConverter.ToInt64(buffer.Array, buffer.Offset + sizeof(short));

                Console.WriteLine($"[C2S] Packet size: {size}, Id: {id}");
            }
        }

        public override void OnSend(int byte_transferred)
        {
            Console.WriteLine($"Transferred : [{byte_transferred}]");
        }

        public override void OnConnected(EndPoint ep)
        {
            Console.WriteLine($"Connected. Addr[{RemoteAddr}]");

            var packet = new Protocol.IPacket();
            packet.Size = sizeof(short);
            packet.Id = Protocol.PacketId._WELCOME_;
            packet.Size += sizeof(Protocol.PacketId);
            SendPacket(packet);
        }

        public override void OnDisconnected(EndPoint ep)
        {
            Console.WriteLine($"Disconnected. Addr[{RemoteAddr}]");
            NetworkManager.Inst.UnregisterTCPConnection(ConnectionID);
        }
    }
}
