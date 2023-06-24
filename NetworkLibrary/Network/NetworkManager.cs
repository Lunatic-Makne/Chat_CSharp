using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics.CodeAnalysis;
using NetworkCore.Event;

namespace NetworkCore
{
    public class NetworkManager
    {
        private NetworkManager() { }
        private static readonly Lazy<NetworkManager> _inst = new Lazy<NetworkManager>(() => new NetworkManager());
        public static NetworkManager Inst { get { return _inst.Value; } }

        [AllowNull]
        private Socket _ListenSocket;

        [AllowNull]
        Func<TCPConnection> _ConnectionFactory;
        public bool Listen(Func<TCPConnection> factory, short listen_port, int listen_backlog)
        {
            _ConnectionFactory = factory;

            try
            {
                // DNS
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var address_list = host.AddressList;
                if (address_list.Length < 1) { return false; }
                var ep = new IPEndPoint(host.AddressList[0], listen_port);

                // Bind
                _ListenSocket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _ListenSocket.Bind(ep);

                // Listen Start
                _ListenSocket.Listen(listen_backlog);

                Console.WriteLine($"[NetworkManager] Start Listen. port[{ep.Port}]");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return true;
        }

        public bool Connect(IPEndPoint ep, Func<TCPConnection> factory, Action<Socket?> on_success)
        {
            _ConnectionFactory = factory;

            var connect_event = new ConnectEvent(ep, on_success);
            return true;
        }
        
        public void OnAccept(Socket? socket)
        {
            if (_ConnectionFactory == null)
            {
                Console.WriteLine($"[{GetType().Name}] OnAccept failed. _ConnectionFactory is null.");
                return;
            }
            try
            {
                if (socket != null)
                {
                    var conn_id = GenSocketEventID();
                    var conn = _ConnectionFactory.Invoke();
                    if (conn == null) { return; }

                    conn.SetSocket(socket);

                    if (RegisterTCPConnection(conn_id, conn) == false)
                    {
                        Console.WriteLine($"Register Connection Failed. Addr[{conn.RemoteAddr}], ConnID[{conn_id}]");
                        conn.CloseConnection();
                    }
                    else
                    {
                        conn.Start();
                    }
                }
                else
                {
                    Console.WriteLine("[NetworkManager] OnAccept failed. socket is null.");
                }
            }
            catch (Exception ex) { Console.WriteLine( ex.ToString()); }
        }

        public void Run()
        {
            var accept_event = new AcceptEvent(_ListenSocket, OnAccept);
            var pending = _ListenSocket.AcceptAsync(accept_event);
            if (pending == false)
            {
                accept_event.Accepted(null, accept_event);
            }
        }

        public void Close()
        {
        }

        #region Socket Event Manage
        private long _CurrentSocketEventID = 0;
        public long GenSocketEventID()
        {
            return Interlocked.Increment(ref _CurrentSocketEventID);
        }

        private object _ConnectionLock = new object();
        private Dictionary<long, TCPConnection> ConnectionDic = new Dictionary<long, TCPConnection>();

        public bool RegisterTCPConnection(long conn_id, TCPConnection conn)
        {
            lock (_ConnectionLock)
            {
                if (ConnectionDic.ContainsKey(conn_id))
                {
                    return false;
                }
                else
                {
                    ConnectionDic.Add(conn_id, conn);
                }
            }

            return true;
        }

        public bool UnregisterTCPConnection(long conn_id)
        {
            lock (_ConnectionLock)
            {
                if (ConnectionDic.ContainsKey(conn_id) == false)
                {
                    return false;
                }

                ConnectionDic.Remove(conn_id);
            }
            
            return true;
        }

        public TCPConnection? GetTCPConnection(long conn_id)
        {
            lock (_ConnectionLock)
            {
                if (ConnectionDic.ContainsKey(conn_id) == false)
                {
                    return null;
                }

                return ConnectionDic[conn_id];
            }
        }

        public int GetTotalTCPConnectionCount() { return ConnectionDic.Count; }
        #endregion Socket Event Manage


    }
}
