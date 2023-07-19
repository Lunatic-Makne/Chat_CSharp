using Protocol;
using Protocol.ClientToServer;
using Protocol.ServerToClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer.User
{
    public partial class User
    {
        public void ProcessSendChat(SendChat packet)
        {
            // [TODO] Need Chat Filter
            var channel = GetChannel();
            if (channel != null)
            {
                var user_info = new Protocol.SharedStruct.UserInfo { UserId = UserId, UserName = UserName };
                var reply = new ReceiveChat { UserInfo = user_info, Message = packet.message };
                channel.BroadcastPacket(reply);
            }
        }
    }
}
