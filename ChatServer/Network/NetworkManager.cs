using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics.CodeAnalysis;

namespace ChatServer.Network
{
    class NetworkManager
    {
        private NetworkManager() { }
        private static readonly Lazy<NetworkManager> _inst = new Lazy<NetworkManager>(() => new NetworkManager());
        public static NetworkManager Inst { get { return _inst.Value; } }

        [AllowNull]
        private Socket ListenSocket { get; set; }

        public bool Initialize()
        {
            ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ep = new IPEndPoint(IPAddress.Any, Config.Inst.Info.network.listen_port);
            ListenSocket.Bind(ep);

            ListenSocket.Listen(Config.Inst.Info.network.listen_backlog);

            Console.WriteLine($"[NetworkManager] Start Listen. port[{ep.Port}]");

            return true;
        }

        public void Run()
        {
            ListenSocket.AcceptAsync(new AcceptEvent(ListenSocket));

            while (true)
            {
                var inputed = Console.ReadKey();
                
                if (inputed.Key == ConsoleKey.Escape)
                {
                    Console.WriteLine($"Exit Server. Registerd Event Count[{SocketEventDic.Count}]");
                    break;
                }
            }
        }

        #region Socket Event Manage
        private long _CurrentSocketEventID = 0;
        public long GenSocketEventID()
        {
            return Interlocked.Increment(ref _CurrentSocketEventID);
        }

        private Dictionary<long, ReceiveEvent> SocketEventDic = new Dictionary<long, ReceiveEvent>();

        public bool RegisterSocketEvent(long eventID, ReceiveEvent conn)
        {
            if (SocketEventDic.ContainsKey(eventID))
            {
                return false;
            }
            else
            {
                SocketEventDic.Add(eventID, conn);
            }

            return true;
        }

        public bool UnregisterConnection(long eventID)
        {
            if (SocketEventDic.ContainsKey(eventID) == false)
            {
                return false;
            }

            SocketEventDic.Remove(eventID);
            return true;
        }

        public ReceiveEvent? GetConnection(long eventID)
        {
            if (SocketEventDic.ContainsKey(eventID) == false)
            {
                return null;
            }

            return SocketEventDic[eventID];
        }
        #endregion Socket Event Manage


    }
}
