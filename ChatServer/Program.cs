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
            if (network_init_result == false)
            {
                Console.WriteLine("Network Initialize Failed.");
            }

            NetworkManager.Inst.Run();

            while (true)
            {
                var inputed = Console.ReadKey();

                if (inputed.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine($"[NetworkManager] Exit Server. Registerd Event Count[{NetworkManager.Inst.GetTotalClientConnectionCount()}]");
                    break;
                }
            }

            NetworkManager.Inst.Close();
        }
    }
}