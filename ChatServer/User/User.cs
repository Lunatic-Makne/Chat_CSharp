using ChatServer.Channel;
using ChatServer.Connection;
using Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer.User
{

    public partial class User
    {
        UserConnection _Connection;
        UserAuthentication _AuthInfo;

        long _ChannelId = ChannelManager.INVALID_CHANNEL_ID;

        public User(UserConnection connection, UserAuthentication auth)
        {
            _Connection = connection;
            _AuthInfo = auth;
        }

        public long UserId { get { return _AuthInfo.UserId; } }
        public string UserName { get { return _AuthInfo.UserName; } }
        
        public long ChannelId { get { return _ChannelId; } set { _ChannelId = value; } }

        public void SendPacket(IPacket packet)
        {
            _Connection.SendPacket(packet);
        }

        public void CloseConneciton()
        {
            _Connection.CloseConnection();
        }

        public bool EnterChannel(long channel_id)
        {
            if (ChannelManager.Inst.EnterChannel(channel_id, this) == false) { return false; }

            var channel = ChannelManager.Inst.Find(channel_id);
            if (channel == null) { return false; };

            ChannelId = channel_id;

            {
                var reply = new Protocol.ServerToClient.ChannelUserList();
                reply.ChannelId = channel_id;
                channel.GetUserList(reply.UserList);
                SendPacket(reply);
            }

            {
                var reply = new Protocol.ServerToClient.EnterChannel();
                reply.ChannelId = channel_id;
                reply.Entered = new Protocol.SharedStruct.UserInfo { UserId = UserId, UserName = UserName };
                channel.BroadcastPacket(reply);
            }

            return true;
        }

        public bool LeaveChannel()
        {
            var channel = ChannelManager.Inst.Find(ChannelId);
            if (channel == null) { return false; };

            if (ChannelManager.Inst.LeaveChannel(ChannelId, UserId) == false) { return false; }

            ChannelId = ChannelManager.INVALID_CHANNEL_ID;

            {
                var reply = new Protocol.ServerToClient.LeaveChannel();
                reply.ChannelId = channel.ChannelId;
                reply.Leaved = new Protocol.SharedStruct.UserInfo { UserId = UserId, UserName = UserName };
                channel.BroadcastPacket(reply);
            }

            return true;
        }

        public Channel.Channel? GetChannel()
        {
            return ChannelManager.Inst.Find(ChannelId);
        }
    }
}
