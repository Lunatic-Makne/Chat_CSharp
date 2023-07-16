using ChatServer.Connection;
using NetworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Protocol.ClientToServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChatServer.User
{
    public struct UserAuthentication
    {
        public long UserId;
        public string UserName;
        public string Password;
    }

    partial class UserManager
    {
        private static Lazy<UserManager> _Inst = new Lazy<UserManager>(() => new UserManager());
        public static UserManager Inst { get { return _Inst.Value; } }
        private UserManager() { }

        // [TODO] DB 연동 전까진 파일로 유저 인증 정보 로드 하니 인증 정보를 캐싱
        private object _AuthLock = new object();
        private Dictionary<string, UserAuthentication> _AuthDic = new Dictionary<string, UserAuthentication>();
        private long _CurrentUserId = 0;
        public long PickUserId() { return Interlocked.Increment(ref _CurrentUserId); }

        private readonly string USER_INFO_TABLE = "USER_INFO";
        public void LoadFromDB()
        {
            var table_data = DBManager.Inst.Load(USER_INFO_TABLE);
            if (table_data != null)
            {
                var auth_token_list = table_data.ToList();
                if (auth_token_list != null)
                {
                    foreach (var auth_token in auth_token_list)
                    {
                        var auth = auth_token.ToObject<UserAuthentication>();
                        _AuthDic.Add(auth.UserName, auth);

                        if (_CurrentUserId < auth.UserId)
                        {
                            _CurrentUserId = auth.UserId;
                        }
                    }
                }
            }
        }

        public void SaveToDB()
        {
            var auth_list = _AuthDic.Values.ToList();
            var jobject = new JObject { { USER_INFO_TABLE, JArray.Parse(JsonConvert.SerializeObject(auth_list)) } };

            DBManager.Inst.Update(jobject);
        }

        public string SHA256Hash(string data)
        {
            SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.ASCII.GetBytes(data));
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in hash)
            {
                stringBuilder.AppendFormat("{0:x2}", b);
            }
            return stringBuilder.ToString();
        }

        
        bool LoginAuthentication(string user_name, string password)
        {
            lock (_AuthLock)
            {
                if (_AuthDic.ContainsKey(user_name) == false) { return false; }

                var auth_info = _AuthDic[user_name];
                if (auth_info.Password != SHA256Hash(password)) { return false; }
            }

            return true;
        }

        private Dictionary<long, User> _UserDic = new Dictionary<long, User>();

        bool CreateNewUser(PacketHandleConnection connection, string user_name, string password)
        {
            lock(_AuthLock)
            {
                if (_AuthDic.ContainsKey(user_name)) { return false; }

                var auth_info = new UserAuthentication { UserId = PickUserId(), UserName = user_name, Password = SHA256Hash(password) };
                _AuthDic.Add(user_name, auth_info);

                var reply = new Protocol.ServerToClient.CreateUserReply();
                reply.UserInfo = new Protocol.SharedStruct.UserInfo { UserId = auth_info.UserId, UserName = user_name };
                connection.SendPacket(reply);
            }

            return true;
        }

    }

    partial class UserManager
    {
        public void ProcessLogin(PacketHandleConnection connection, Protocol.ClientToServer.Login packet)
        {
            Console.WriteLine($"[C2S][Login] packet: [{packet}]");

            if (LoginAuthentication(packet.Name, packet.Password) == false)
            {
                Console.WriteLine($"[C2S][Login] Login Success. user[{packet.Name}]");

                var reply = new Protocol.ServerToClient.LoginReply();
                reply.Error = true;
                reply.ErrorMessage = $"Password miss match";
                connection.SendPacket(reply);

                return;
            }

            lock(_AuthLock)
            {
                UserAuthentication auth_info; 
                if (_AuthDic.TryGetValue(packet.Name, out auth_info) == false)
                {
                    Console.WriteLine($"[CS2][Login] Not found auth info. name[{packet.Name}]");

                    var reply = new Protocol.ServerToClient.LoginReply();
                    reply.Error = true;
                    reply.ErrorMessage = $"Not found auth info";
                    connection.SendPacket(reply);
                }
                var user = new User((UserConnection)connection, auth_info);
                _UserDic.Add(auth_info.UserId, user);

                {
                    var reply = new Protocol.ServerToClient.LoginReply();
                    reply.UserId = user.UserId;
                    foreach(var element in _UserDic.Values)
                    {
                        reply.UserList.Add(new Protocol.SharedStruct.UserInfo { UserId = element.UserId, UserName = element.UserName});
                    }
                    user.SendPacket(reply);
                }
            }
        }

        public void ProcessCreateUser(PacketHandleConnection conn, Protocol.ClientToServer.CreateUser packet)
        {
            Console.WriteLine($"[C2S][CreateUser] packet: [{packet}]");
            
            var result = CreateNewUser(conn, packet.UserName, packet.Password);
            if (result == false)
            {
                var reply = new Protocol.ServerToClient.CreateUserReply();
                reply.Error = result;
                reply.ErrorMessage = $"Create User failed.";
                conn.SendPacket(reply);
            }
        }
    }
}
