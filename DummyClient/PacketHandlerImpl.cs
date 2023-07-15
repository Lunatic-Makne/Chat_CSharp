using NetworkCore;

namespace Protocol.ServerToClient
{
    partial class PacketHandler
    {
        void LoginReplyHandler(PacketHandleConnection connection, IPacket data)
        {
            var packet = (LoginReply)data;

            Console.WriteLine($"[S2C][WELCOME] UserId: [{packet.UserId}]");
            foreach (var info in packet.UserList)
            {
                Console.WriteLine($"[UserList] UserId: [{info.UserId}]");
            }
        }
    }
}
