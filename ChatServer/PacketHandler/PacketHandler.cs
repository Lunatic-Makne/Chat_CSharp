using NetworkCore;
using Protocol;
using Protocol.ClientToServer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    using PacketHandleFunc = Action<PacketHandleConnection, ArraySegment<byte>>;

    partial class PacketHandler
    {
        private static readonly Lazy<PacketHandler> _Inst = new Lazy<PacketHandler>(() => new PacketHandler());
        public static PacketHandler Inst { get { return _Inst.Value; } }

        private PacketHandler() { }

        
        private Dictionary<long, PacketHandleFunc> _PacketFactory = new Dictionary<long, PacketHandleFunc>();

        private void Register()
        {

        }


        public bool Dispatch(PacketHandleConnection connection, ArraySegment<byte> buffer)
        {
            if (buffer.Array == null) { return false; }

            int offset = 0;
            var size = BitConverter.ToInt16(buffer.Array, buffer.Offset + offset);
            offset += sizeof(short);
            var id = BitConverter.ToInt64(buffer.Array, buffer.Offset + offset);
            offset += sizeof(long);

            if (offset + size > buffer.Array.Length) { return false; }

            switch ((Protocol.ClientToServer.PacketId)id)
            {
                case Protocol.ClientToServer.PacketId._LOGIN_:
                    var packet = new Login();
                    packet.Read(new ArraySegment<byte>(buffer.Array, buffer.Offset + offset, buffer.Count - offset));

                    Console.WriteLine($"[C2S][HI] name: [{packet.Name}]");
                    break;
                default:
                    Console.WriteLine($"[PacketHandler] Unregistered PacketId[{id}].");
                    return false;
            }

            return true;
        }
    }
}