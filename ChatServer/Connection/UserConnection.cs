using NetworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChatServer.Connection
{
    public class UserConnection : TCPConnection
    {
        public override int OnRecv(ArraySegment<byte> buffer)
        {
            // [TODO] Temp
            var message = Encoding.UTF8.GetString(buffer);
            Console.WriteLine($"[C2S] {message}");

            return buffer.Count;
        }

        public override void OnSend(int byte_transferred)
        {
            Console.WriteLine($"Transferred : [{byte_transferred}]");
        }

        public override void OnConnected(EndPoint ep)
        {
            Console.WriteLine($"Connected. Addr[{RemoteAddr}]");

            SendTemp("Welcome!");
        }

        public override void OnDisconnected(EndPoint ep)
        {
            Console.WriteLine($"Disconnected. Addr[{RemoteAddr}]");
            NetworkManager.Inst.UnregisterTCPConnection(ConnectionID);
        }
    }
}
