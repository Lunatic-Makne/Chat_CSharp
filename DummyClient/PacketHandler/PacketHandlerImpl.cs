using NetworkCore;
using Protocol.ClientToServer;
using DummyClient;
using System.Net.Sockets;

namespace Protocol.ServerToClient
{
    partial class PacketHandler
    {
        void LoginReplyHandler(PacketHandleConnection connection, IPacket data)
        {
            var packet = (LoginReply)data;

            if (packet.Error)
            {
                Console.WriteLine($"[LoginReply] ErrorMessage: {packet.ErrorMessage}");
                connection.CloseConnection();
                return;
            }
            else
            {
                Console.WriteLine($"[S2C][LoginReply] success.");
            }
        }

        void CreateUserReplyHandler(PacketHandleConnection connection, IPacket data)
        {
            var packet = (CreateUserReply)data;
            if (packet.Error)
            {
                Console.WriteLine($"[CreateUserReply] ErrorMessage: {packet.ErrorMessage}");
            }
            else
            {
                Console.WriteLine($"[S2C][CreateUserReply] sucess.");
            }

            SendLogin(connection);
        }

        public static void SendCreateUser(PacketHandleConnection connection)
        {
            var packet = new CreateUser { UserName = UserInfo.Inst.Name, Password = UserInfo.Inst.Password };
            connection.SendPacket(packet);
        }

        public static void SendLogin(PacketHandleConnection connection)
        {
            var packet = new Login { Name = UserInfo.Inst.Name , Password = UserInfo.Inst.Password };
            connection.SendPacket(packet);
        }

        void EnterChannelHandler(PacketHandleConnection connection, IPacket data)
        {
            var packet = (EnterChannel)data;
            Console.WriteLine($"[EnterChannel] channel_id[{packet.ChannelId}] entered_user[{packet.Entered.UserId}:{packet.Entered.UserName}]");
        }

        void LeaveChannelHandler(PacketHandleConnection connection, IPacket data)
        {
            var packet = (LeaveChannel)data;
            Console.WriteLine($"[LeaveChannel] channel_id[{packet.ChannelId}] entered_user[{packet.Leaved.UserId}:{packet.Leaved.UserName}]");
        }

        void ChannelUserListHandler(PacketHandleConnection connection, IPacket data)
        {
            var packet = (ChannelUserList)data;
            foreach( var user_info in packet.UserList )
            {
                Console.WriteLine($"[ChannelUserList] User[{user_info.UserId}:{user_info.UserName}]");
            }
        }
    }
}
