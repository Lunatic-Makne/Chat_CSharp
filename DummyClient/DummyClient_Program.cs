using ChatClient;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DummyClient
{
    internal class DummyClient_Program
    {
        static void Main(string[] args)
        {
            if (Config.Inst.Load() == false)
            {
                Console.WriteLine("Load Config failed.");
                return;
            }

            // DNS
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var address_list = host.AddressList;
            if (address_list.Length < 1) { return; }
            var ep = new IPEndPoint(address_list[0], Config.Inst.Info.target_port);

            while (true)
            {
                try
                {
                    var socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(ep);

                    Console.WriteLine($"Connect to [{ep.ToString()}]");

                    byte[] send_buff = Encoding.UTF8.GetBytes("Hi!");
                    var send_bytes = socket.Send(send_buff);

                    byte[] recv_buff = new byte[8 * 1024];
                    var recv_bytes = socket.Receive(recv_buff);
                    var recv_message = Encoding.UTF8.GetString(recv_buff);
                    Console.WriteLine($"[S2C] : {recv_message}");

                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                Thread.Sleep(200);
            }
        }
    }
}