using ChatClient;
using NetworkCore;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DummyClient
{
    class ClientConnection : TCPConnection
    {
        public override void OnConnected(EndPoint ep)
        {
            
        }

        public override void OnDisconnected(EndPoint ep)
        {
            throw new NotImplementedException();
        }

        public override void OnRecv(ArraySegment<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public override void OnSend(int byte_transferred)
        {
            throw new NotImplementedException();
        }
    }

    internal class DummyClient_Program
    {
        public static void SocketConnected(Socket? socket) 
        {
            if (socket == null) { return; }

            Console.WriteLine($"Connect to [{socket.RemoteEndPoint}]");

            byte[] send_buff = Encoding.UTF8.GetBytes("Hi!");
            var send_bytes = socket.Send(send_buff);

            byte[] recv_buff = new byte[8 * 1024];
            var recv_bytes = socket.Receive(recv_buff);
            var recv_message = Encoding.UTF8.GetString(recv_buff);
            Console.WriteLine($"[S2C] : {recv_message}");

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        static void Main(string[] args)
        {
            if (Config.Inst.Load() == false)
            {
                Console.WriteLine("[DummyClient] Load Config failed.");
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
                    NetworkManager.Inst.Connect(ep, () => { return new ClientConnection(); }, SocketConnected);
                    Thread.Sleep(500);
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