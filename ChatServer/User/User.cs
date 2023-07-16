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

    public class User
    {
        UserConnection _Connection;
        UserAuthentication _AuthInfo;
        public User(UserConnection connection, UserAuthentication auth)
        {
            _Connection = connection;
            _AuthInfo = auth;
        }

        public long UserId { get { return _AuthInfo.UserId; } }
        public string UserName { get { return _AuthInfo.UserName; } }

        public void SendPacket(IPacket packet)
        {
            _Connection.SendPacket(packet);
        }
    }
}
