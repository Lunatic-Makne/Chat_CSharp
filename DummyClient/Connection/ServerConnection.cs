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
    public class ServerConnection : PacketHandleConnection
    {

        public override void OnConnected(EndPoint ep)
        {
            Console.WriteLine($"Connect to [{ep}]");

            PacketHandler.SendCreateUser(this);

            UserInfo.Inst.Connection = this;
        }

        public override void OnDisconnected(EndPoint ep)
        {
            Console.WriteLine($"Disconnected. Addr[{ep}]");
        }

        public override void OnReceivePacket(ArraySegment<byte> buffer)
        {
            if (Protocol.ServerToClient.PacketHandler.Inst.Dispatch(this, buffer) == false)
            {
                CloseConnection();
            }
        }

        public override void OnSend(int byte_transferred)
        {
            Console.WriteLine($"Transferred : [{byte_transferred}]");
        }
    }
}