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
            var packet = data as Login;
            if (packet != null)
            {
                UserManager.Inst.ProcessLogin(connection, packet);
            }
            
        }

        void CreateUserHandler(PacketHandleConnection connection, IPacket data)
        {
            var packet = data as CreateUser;
            if (packet != null)
            {
                UserManager.Inst.ProcessCreateUser(connection, packet);
            }
        }

        void SendChatHandler(PacketHandleConnection connection, IPacket data)
        {
            var packet = data as SendChat;
            var user = UserManager.Inst.GetUser(connection);
            if (user != null && packet != null)
            {
                user.ProcessSendChat(packet);
            }
        }

        void MoveChannelHandler(PacketHandleConnection connection, IPacket data)
        {
            var packet = data as MoveChannel;
            var user = UserManager.Inst.GetUser(connection);
            if (user != null && packet != null)
            {
                user.ProcessMoveChannel(packet);
            }
        }
    }
}
