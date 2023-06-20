using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatClient
{
    internal class Proram
    {
        static void Main(string[] args)
        {
            if (Config.Inst.Load() == false)
            {
                Console.WriteLine("Load Config Failed.");
            }

            var ep = new IPEndPoint(IPAddress.Parse(Config.Inst.Info.target_ip), Config.Inst.Info.target_port);
            using (var client_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                client_socket.Connect(ep);

                new Task(() =>
                {
                    try
                    {
                        while (true)
                        {
                            var binary = new Byte[1024];
                            client_socket.Receive(binary);
                            var data = Encoding.ASCII.GetString(binary).Trim('\0');
                            if (String.IsNullOrWhiteSpace(data)) { continue; }

                            Console.WriteLine(data);
                        }
                    }
                    catch (SocketException ex) 
                    {
                        Console.WriteLine(ex);
                    }

                }).Start();

                while (true)
                {
                    var message = Console.ReadLine();
                    client_socket.Send(Encoding.ASCII.GetBytes(message + "\r\n"));
                    if ("EXIT".Equals(message, StringComparison.OrdinalIgnoreCase)) { break; }
                }

                Console.WriteLine("Disconnected.");
            }
        }
    }
}