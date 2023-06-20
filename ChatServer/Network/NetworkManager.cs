using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ChatServer.Network
{
    class NetworkManager
    {
        private NetworkManager() { }
        private static readonly Lazy<NetworkManager> _inst = new Lazy<NetworkManager>(() => new NetworkManager());
        public static NetworkManager Inst { get { return _inst.Value; } }

        private Socket ListenSocket { get; set; }

        public async Task<bool> Initialize()
        {
            ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            using (ListenSocket)
            {
                var ep = new IPEndPoint(IPAddress.Any, Config.Inst.Info.network.listen_port);
                ListenSocket.Bind(ep);

                ListenSocket.Listen();

                Console.WriteLine($"[NetworkManager] Start Listen. port[{ep.Port}]");

                await MakeAcceptTask();                
            }

            return true;
        }

        public async Task MakeAcceptTask()
        {
            var task = new Task(() =>
            {
                try
                {
                    while (true)
                    {
                        var client = ListenSocket.Accept();
                        if (client == null) { break; }

                        var client_ep = client.RemoteEndPoint as IPEndPoint;
                        Console.WriteLine($"[NetworkManager] Accept Client. ep[{client_ep}]");

                        client.Send(Encoding.ASCII.GetBytes("Welcome.\r\n"));

                        var builder = new StringBuilder();

                        using (client)
                        {
                            while (true)
                            {
                                var binary = new Byte[1024];
                                client.Receive(binary);

                                var data = Encoding.ASCII.GetString(binary);
                                builder.Append(data.Trim('\0'));

                                if (builder.Length > 2 && builder[builder.Length - 2] == '\r' && builder[builder.Length - 1] == '\n')
                                {
                                    data = builder.ToString().Replace("\n", "").Replace("\r", "");
                                    if (String.IsNullOrWhiteSpace(data)) { continue; }

                                    if ("EXIT".Equals(data, StringComparison.OrdinalIgnoreCase)) { break; }

                                    Console.WriteLine("Message = " + data);
                                    builder.Clear();

                                    client.Send(Encoding.ASCII.GetBytes("ECHO : " + data + "\r\n"));
                                }
                            }

                            Console.WriteLine($"[NetworkManager] Disconnected. Client[{client_ep}");
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Console.Write(ex.ToString());
                }
            });

            task.Start();
            await task;
        }
    }
}
