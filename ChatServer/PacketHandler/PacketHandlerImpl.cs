using ChatServer.User;
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
            UserManager.Inst.ProcessLogin(connection, (Login) data);
        }

        void CreateUserHandler(PacketHandleConnection connection, IPacket data)
        {
            UserManager.Inst.ProcessCreateUser(connection, (CreateUser) data);            
        }
    }
}
