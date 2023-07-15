using ChatClient;
using DummyClient.Connection;
using NetworkCore;
using System.Net;


namespace DummyClient
{
    public class UserInfo
    {
        private UserInfo() { }
        private static readonly Lazy<UserInfo> _Inst = new Lazy<UserInfo>(() => new UserInfo());
        public static UserInfo Inst { get { return _Inst.Value; } }

        public string Name { get; set; } = new string("");
    }

    internal class DummyClient_Program
    {

        static void Main(string[] args)
        {
            Console.Write("Write your name: ");
            UserInfo.Inst.Name = Console.ReadLine();

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

            while (true)
            {
                try
                {
                    if (NetworkManager.Inst.Connect(ep) == false)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                Thread.Sleep(5000);
            }
        }
    }
}