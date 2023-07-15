using NetworkCore;

namespace Protocol.ServerToClient
{
	using OnReceiveFunc = Action<PacketHandleConnection, ArraySegment<byte>>;
	using PacketHandleFunc = Action<PacketHandleConnection, IPacket>;
	partial class PacketHandler
	{
		private static readonly Lazy<PacketHandler> _Inst = new Lazy<PacketHandler>(() => new PacketHandler());
		public static PacketHandler Inst { get { return _Inst.Value; } }
		private PacketHandler() { }
		void MakePacket<T>(PacketHandleConnection connection, ArraySegment<byte> buffer) where T : IPacket, new()
		{
			T packet = new T();
			packet.Read(buffer);
			PacketHandleFunc func = null;
			if (_PacketHandlerDic.TryGetValue(packet.Id, out func)) func.Invoke(connection, packet);
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
			OnReceiveFunc func = null;
			if (_OnReceiveHandlerDic.TryGetValue(id, out func)) func.Invoke(connection, new ArraySegment<byte>(buffer.Array, buffer.Offset + offset, buffer.Count - offset));
			return true;
		}
	}
	partial class PacketHandler
	{
		private Dictionary<long, OnReceiveFunc> _OnReceiveHandlerDic = new Dictionary<long, OnReceiveFunc>();
		private Dictionary<long, PacketHandleFunc> _PacketHandlerDic = new Dictionary<long, PacketHandleFunc>();
		public void Register()
		{
			_OnReceiveHandlerDic.Add((long)PacketId._LOGINREPLY_, MakePacket<LoginReply>);
			_PacketHandlerDic.Add((long)PacketId._LOGINREPLY_, LoginReplyHandler);
		}
	}
}
