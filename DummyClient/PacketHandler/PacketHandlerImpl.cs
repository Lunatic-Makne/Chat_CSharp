using NetworkCore;
using Protocol.ClientToServer;
using DummyClient;

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
            }
            else
            {
                Console.WriteLine($"[S2C][LoginReply] packet: [{packet}]");
            }

            Thread.Sleep(1000);

            connection.CloseConnection();
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
                Console.WriteLine($"[S2C][CreateUserReply] packet: [{packet}]");
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
    }
}
