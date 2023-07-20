using ChatClient;
using DummyClient.Connection;
using NetworkCore;
using Protocol.ClientToServer;
using System.Net;


namespace DummyClient
{
    public class UserInfo
    {
        private UserInfo() { }
        private static readonly Lazy<UserInfo> _Inst = new Lazy<UserInfo>(() => new UserInfo());
        public static UserInfo Inst { get { return _Inst.Value; } }

        public string Name { get; set; } = "";
        public string Password { get; set; } = "";

        public ServerConnection Connection { get; set; }
    }

    internal class DummyClient_Program
    {
        static bool Command(string command)
        {
            switch(command.ToLower())
            {
                case "movechannel":
                    {
                        Console.Write("Channel: ");
                        var ch_string = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(ch_string))
                        {
                            return true;
                        }

                        var ch = Convert.ToInt64(ch_string);
                        
                        if (UserInfo.Inst.Connection != null)
                        {
                            var req = new MoveChannel();
                            req.ChannelId = ch;
                            UserInfo.Inst.Connection.SendPacket(req);
                        }
                    }
                    return true;
                default:
                    return false;
            }
        }
        static void Main(string[] args)
        {
            Console.Write("Write your name: ");
            UserInfo.Inst.Name = Console.ReadLine();

            Console.WriteLine("Write password: ");
            UserInfo.Inst.Password = Console.ReadLine();

            if (Config.Inst.Load() == false)
            {
                Console.WriteLine("[DummyClient] Load Config failed.");
                return;
            }

            Protocol.ServerToClient.PacketHandler.Inst.Register();

            NetworkManager.Inst.ConnectionFactory = () => { return new ServerConnection(); };

            // DNS
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var address_list = host.AddressList;
            if (address_list.Length < 1) { return; }
            var ep = new IPEndPoint(address_list[0], Config.Inst.Info.target_port);

            try
            {
                if (NetworkManager.Inst.Connect(ep))
                {
                    while (true)
                    {
                        Console.WriteLine("Write Message: ");
                        var message = Console.ReadLine();
                        if (message != null) 
                        {
                            if (message.ToLower() == "exit")
                            {
                                Console.WriteLine($"Goodbye.");
                                break;
                            }

                            if (Command(message)) { continue; }

                            if (UserInfo.Inst.Connection != null)
                            {
                                var packet = new SendChat { message = message };
                                UserInfo.Inst.Connection.SendPacket(packet);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}