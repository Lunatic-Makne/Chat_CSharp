using Microsoft.Extensions.Logging;
using Protocol;
using Protocol.SharedStruct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ChatServer.Channel
{
    public class Channel
    {
        private long _Id;
        public Channel(long id)
        {
            this._Id = id;
        }

        public long ChannelId { get { return _Id; } }

        private object _UserLock = new object();
        private Dictionary<long, User.User> _UserDic = new Dictionary<long, User.User>();


        public bool AddUser(User.User new_user)
        {
            lock (_UserLock)
            {
                if (_UserDic.ContainsKey(new_user.UserId)) { return false; }

                _UserDic.Add(new_user.UserId, new_user);
            }

            return true;
        }

        public bool RemoveUser(long userId)
        {
            lock (_UserLock)
            {
                if (_UserDic.ContainsKey(userId) == false) { return false; }

                _UserDic.Remove(userId);

                if (_UserDic.Count == 0 ) { ChannelManager.Inst.RemoveChannel(_Id); }
            }

            return true;
        }

        private void _BroadcastPacket(IPacket packet)
        {
            foreach (var user in _UserDic.Values)
            {
                user.SendPacket(packet);
            }
        }

        public void BroadcastPacket(IPacket packet)
        {
            lock (_UserLock)
            {
                _BroadcastPacket(packet);
            }
        }

        public void GetUserList(List<UserInfo> user_list)
        {
            lock (_UserLock)
            {
                foreach (var user in _UserDic.Values)
                {
                    user_list.Add(new UserInfo { UserId = user.UserId, UserName = user.UserName });
                }
            }
        }
    }

    public class ChannelManager
    {
        private ChannelManager() { }
        private static Lazy<ChannelManager> _Inst = new Lazy<ChannelManager>(() => new ChannelManager());
        public static ChannelManager Inst { get { return _Inst.Value; } }

        public static readonly long INVALID_CHANNEL_ID = -1;

        private object _ChannelLock = new object();
        private Dictionary<long, Channel> _ChannelDic = new Dictionary<long, Channel>();

        private Channel FindOrAdd(long id)
        {
            lock (_ChannelLock)
            {
                Channel? ch = null;
                if (_ChannelDic.ContainsKey(id) == false)
                {
                    ch = new Channel(id);
                    _ChannelDic.Add(id, ch);
                }
                else
                {
                    ch = _ChannelDic[id];
                }

                return ch;
            }
        }

        public Channel? Find(long id)
        {
            lock (_ChannelLock)
            {
                if (_ChannelDic.ContainsKey(id) == false) { return null; }

                return _ChannelDic[id];
            }
        }

        public bool RemoveChannel(long id)
        {
            lock (_ChannelLock)
            {
                if (_ChannelDic.Remove(id) == false)
                {
                    Console.WriteLine($"[ChannelManager] Remove channel failed. id[{id}]");
                    return false;
                }
            }
            return true;
        }

        public bool EnterChannel(long channel_id, User.User new_user)
        {
            var channel = FindOrAdd(channel_id);
            if (channel == null) { return false; }

            return channel.AddUser(new_user);
        }

        public bool LeaveChannel(long channel_id, long user_id)
        {
            var channel = Find(channel_id);
            if (channel == null) { return false; }

            return channel.RemoveUser(user_id);
        }
    }
}
