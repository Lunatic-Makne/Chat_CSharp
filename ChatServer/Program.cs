using ChatServer.Network;

namespace ChatServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (Config.Inst.Load() == false)
            {
                Console.WriteLine("Load Config Failed.");
            }

            var network_init_result = NetworkManager.Inst.Initialize();
            if (network_init_result.Result == false)
            {
                Console.WriteLine("Network Initialize Failed.");
            }
        }
    }
}