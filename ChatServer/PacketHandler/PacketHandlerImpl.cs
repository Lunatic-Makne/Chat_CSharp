using NetworkCore;
using Protocol.ServerToClient;
using Protocol.SharedStruct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protocol.ClientToServer
{
    partial class PacketHandler
    {
        void LoginHandler(PacketHandleConnection connection, IPacket data)
        {
            {
                var packet = (Login)data;

                Console.WriteLine($"[C2S][HI] name: [{packet.Name}]");
            }

            {
                var packet = new LoginReply { UserId = Random.Shared.Next() };
                packet.UserList.Add(new UserInfo { UserId = Random.Shared.Next() });
                packet.UserList.Add(new UserInfo { UserId = Random.Shared.Next() });
                packet.UserList.Add(new UserInfo { UserId = Random.Shared.Next() });
                connection.SendPacket(packet);

                foreach (var item in packet.UserList)
                {
                    Console.WriteLine($"TEST: {item.UserId}");
                }
            }
        }
    }
}
