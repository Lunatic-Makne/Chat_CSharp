using ChatClient;
using NetworkCore;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DummyClient
{
    class ClientConnection : PacketHandleConnection
    {
        public override void OnConnected(EndPoint ep)
        {
            Console.WriteLine($"Connect to [{ep}]");

            var packet = new Protocol.IPacket();
            packet.Size = sizeof(short); // size
            packet.Id = Protocol.PacketId._HI_;
            packet.Size += sizeof(long); // id

            SendPacket(packet);
        }

        public override void OnDisconnected(EndPoint ep)
        {
            Console.WriteLine($"Disconnected. Addr[{ep}]");
        }

        public override void OnReceivePacket(ArraySegment<byte> buffer)
        {
            if (buffer.Array != null)
            {
                var size = BitConverter.ToInt16(buffer.Array, buffer.Offset);
                var id = BitConverter.ToInt64(buffer.Array, buffer.Offset + sizeof(short));

                Console.WriteLine($"[S2C] Packet size: {size}, Id: {id}");
            }
        }

        public override void OnSend(int byte_transferred)
        {
            Console.WriteLine($"Transferred : [{byte_transferred}]");
        }
    }

    internal class DummyClient_Program
    {

        static void Main(string[] args)
        {
            if (Config.Inst.Load() == false)
            {
                Console.WriteLine("[DummyClient] Load Config failed.");
                return;
            }

            NetworkManager.Inst.ConnectionFactory = () => { return new ClientConnection(); };

            // DNS
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var address_list = host.AddressList;
            if (address_list.Length < 1) { return; }
            var ep = new IPEndPoint(address_list[0], Config.Inst.Info.target_port);

            while (true)
            {
                try
                {
                    NetworkManager.Inst.Connect(ep);
                    Thread.Sleep(500);

                    var conn_id = NetworkManager.Inst.GetCurrentConnectionID();
                    var conn = NetworkManager.Inst.GetTCPConnection(conn_id);
                    if (conn != null)
                    {
                        conn.CloseConnection();
                    }
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