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

        void SendMoveChannelFailed()
        {
            var reply = new MoveChannelReply { Error = true, ChannelId = ChannelId };
            SendPacket(reply);
        }
        public void ProcessMoveChannel(MoveChannel packet)
        {
            do
            {
                if (packet.ChannelId == ChannelId)
                {
                    Console.WriteLine($"[MoveChannel] Duplicate Request. user[{UserId}:{UserName}] request_channel_id[{packet.ChannelId}]");
                    break;
                }

                if (LeaveChannel() == false)
                {
                    Console.WriteLine($"[MoveChannel] LeaveChannel failed. user[{UserId}:{UserName}] channel_id[{ChannelId}]");
                    break;
                }

                if (EnterChannel(packet.ChannelId) == false)
                {
                    Console.WriteLine($"[MoveChannel] EnterChannel failed. user[{UserId}:{UserName}] request_channel_id[{packet.ChannelId}]");
                    break;
                }

                var reply = new MoveChannelReply { Error = false, ChannelId = ChannelId };
                SendPacket(reply);

                return;
            } while (false);

            SendMoveChannelFailed();
            return;
        }
    }
}
