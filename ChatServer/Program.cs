using ChatServer.Connection;
using NetworkCore;

namespace ChatServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (Config.Inst.Load() == false)
            {
                Console.WriteLine("[Chat Server] Load Config Failed.");
            }

            Protocol.ClientToServer.PacketHandler.Inst.Register();

            NetworkManager.Inst.ConnectionFactory = () => { return new UserConnection(); };

            var listen_result = NetworkManager.Inst.Listen(Config.Inst.Info.network.listen_port, Config.Inst.Info.network.listen_backlog);
            if (listen_result == false)
            {
                Console.WriteLine("[NetworkManager] Listen Failed.");
            }

            NetworkManager.Inst.Accept();

            while (true)
            {
                var inputed = Console.ReadKey();

                if (inputed.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine($"[Chat Server] Exit Server. Registerd Event Count[{NetworkManager.Inst.GetTotalTCPConnectionCount()}]");
                    break;
                }
            }

            NetworkManager.Inst.Close();
        }
    }
}