using NetworkCore;
using System.Text;

namespace Protocol
{
	namespace ClientToServer
	{
		public enum PacketId : long
		{
			_Unknown_ = 0
			, _LOGIN_
			, _CREATEUSER_
			, _MAX_
		}
	}
	namespace ServerToClient
	{
		public enum PacketId : long
		{
			_Unknown_ = 0
			, _LOGINREPLY_
			, _CREATEUSERREPLY_
			, _MAX_
		}
	}
	
	public abstract class IPacket
	{
	    public short Size = 0;
	    public long Id = 0;
	
	    public IPacket(long id)
	    {
	        Id = id;
	
	        Size += sizeof(short);
	        Size += sizeof(long);
	    }
	
	    public ArraySegment<byte>? Write(int send_buffer_size)
	    {
	        var openSegment = SendBufferHelper.Open(send_buffer_size);
	        if (openSegment == null || openSegment.HasValue == false) { return null; }
	        if (openSegment.Value.Array == null) { return null; }
	
	        var buffer = openSegment.Value;
	        Size = (short)WriteImpl(buffer);
	        if (Size <= 0) { return null; }
	
	        var size = BitConverter.GetBytes(Size);
	        Array.Copy(size, 0, buffer.Array, buffer.Offset, sizeof(short));
	
	        return SendBufferHelper.Close(Size);
	    }
	
	    protected virtual int WriteImpl(ArraySegment<byte> buffer)
	    {
	        if (buffer.Array == null) { return 0; }
	
	        int offset = 0;
	        // reserve byte for packet size
	        offset += sizeof(short);
	
	        var span = new Span<byte>(buffer.Array, buffer.Offset, buffer.Count);
	
	        bool result = BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), (long) Id);
	        offset += sizeof(long);
	
	        if (result == false) { return 0; }
	
	        return offset;
	    }
	
	    public abstract bool Read(ArraySegment<byte> buffer);
	}
	namespace SharedStruct
	{
		public struct UserInfo
		{
			public long UserId;
			public string UserName = "";
			
			public UserInfo()
			{
			}
			public bool Write(Span<byte> span, ref int offset)
			{
				bool result = true;
				result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), UserId);
				offset += sizeof(long);
				var username_length = Encoding.Unicode.GetBytes(UserName, span.Slice(offset + sizeof(int), span.Length - offset - sizeof(int)));
				result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), username_length);
				offset += sizeof(int); offset += username_length;
				return result;
			}
			public void Read(ReadOnlySpan<byte> readonly_span, ref int offset)
			{
				UserId = BitConverter.ToInt64(readonly_span.Slice(offset, readonly_span.Length - offset));
				offset += sizeof(long);
				var username_length = BitConverter.ToInt32(readonly_span.Slice(offset, readonly_span.Length - offset));
				offset += sizeof(int);
				UserName = Encoding.Unicode.GetString(readonly_span.Slice(offset, username_length));
				offset += username_length;
			}
		}
	}
	namespace ClientToServer
	{
		public class Login : IPacket
		{
			public string Name = "";
			public string Password = "";
			
			public Login() : base((long)PacketId._LOGIN_)
			{
			}
			protected override int WriteImpl(ArraySegment<byte> buffer)
			{
				if (buffer.Array == null) { return 0; }
				
				var offset = base.WriteImpl(buffer);
				if (offset == 0) { return 0; }
				
				bool result = true;
				var span = new Span<byte>(buffer.Array, buffer.Offset, buffer.Count);
				var name_length = Encoding.Unicode.GetBytes(Name, span.Slice(offset + sizeof(int), span.Length - offset - sizeof(int)));
				result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), name_length);
				offset += sizeof(int); offset += name_length;
				var password_length = Encoding.Unicode.GetBytes(Password, span.Slice(offset + sizeof(int), span.Length - offset - sizeof(int)));
				result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), password_length);
				offset += sizeof(int); offset += password_length;
				
				if (result == false) { return 0; }
				
				return offset;
			}
			public override bool Read(ArraySegment<byte> buffer)
			{
				if (buffer.Array == null) { return false; }
				
				var readonly_span = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);
				int offset = 0;
				var name_length = BitConverter.ToInt32(readonly_span.Slice(offset, readonly_span.Length - offset));
				offset += sizeof(int);
				Name = Encoding.Unicode.GetString(readonly_span.Slice(offset, name_length));
				offset += name_length;
				var password_length = BitConverter.ToInt32(readonly_span.Slice(offset, readonly_span.Length - offset));
				offset += sizeof(int);
				Password = Encoding.Unicode.GetString(readonly_span.Slice(offset, password_length));
				offset += password_length;
				return true;
			}
		}
		public class CreateUser : IPacket
		{
			public string UserName = "";
			public string Password = "";
			
			public CreateUser() : base((long)PacketId._CREATEUSER_)
			{
			}
			protected override int WriteImpl(ArraySegment<byte> buffer)
			{
				if (buffer.Array == null) { return 0; }
				
				var offset = base.WriteImpl(buffer);
				if (offset == 0) { return 0; }
				
				bool result = true;
				var span = new Span<byte>(buffer.Array, buffer.Offset, buffer.Count);
				var username_length = Encoding.Unicode.GetBytes(UserName, span.Slice(offset + sizeof(int), span.Length - offset - sizeof(int)));
				result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), username_length);
				offset += sizeof(int); offset += username_length;
				var password_length = Encoding.Unicode.GetBytes(Password, span.Slice(offset + sizeof(int), span.Length - offset - sizeof(int)));
				result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), password_length);
				offset += sizeof(int); offset += password_length;
				
				if (result == false) { return 0; }
				
				return offset;
			}
			public override bool Read(ArraySegment<byte> buffer)
			{
				if (buffer.Array == null) { return false; }
				
				var readonly_span = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);
				int offset = 0;
				var username_length = BitConverter.ToInt32(readonly_span.Slice(offset, readonly_span.Length - offset));
				offset += sizeof(int);
				UserName = Encoding.Unicode.GetString(readonly_span.Slice(offset, username_length));
				offset += username_length;
				var password_length = BitConverter.ToInt32(readonly_span.Slice(offset, readonly_span.Length - offset));
				offset += sizeof(int);
				Password = Encoding.Unicode.GetString(readonly_span.Slice(offset, password_length));
				offset += password_length;
				return true;
			}
		}
	}
	namespace ServerToClient
	{
		public class LoginReply : IPacket
		{
			public bool Error;
			public string ErrorMessage = "";
			public long UserId;
			public List<SharedStruct.UserInfo> UserList = new List<SharedStruct.UserInfo>();
			
			public LoginReply() : base((long)PacketId._LOGINREPLY_)
			{
			}
			protected override int WriteImpl(ArraySegment<byte> buffer)
			{
				if (buffer.Array == null) { return 0; }
				
				var offset = base.WriteImpl(buffer);
				if (offset == 0) { return 0; }
				
				bool result = true;
				var span = new Span<byte>(buffer.Array, buffer.Offset, buffer.Count);
				result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), Error);
				offset += sizeof(bool);
				var errormessage_length = Encoding.Unicode.GetBytes(ErrorMessage, span.Slice(offset + sizeof(int), span.Length - offset - sizeof(int)));
				result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), errormessage_length);
				offset += sizeof(int); offset += errormessage_length;
				result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), UserId);
				offset += sizeof(long);
				result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), UserList.Count);
				offset += sizeof(int);
				foreach( var element in UserList )
				{
					result &= element.Write(span, ref offset);
				}
				
				if (result == false) { return 0; }
				
				return offset;
			}
			public override bool Read(ArraySegment<byte> buffer)
			{
				if (buffer.Array == null) { return false; }
				
				var readonly_span = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);
				int offset = 0;
				Error = BitConverter.ToBoolean(readonly_span.Slice(offset, readonly_span.Length - offset));
				offset += sizeof(bool);
				var errormessage_length = BitConverter.ToInt32(readonly_span.Slice(offset, readonly_span.Length - offset));
				offset += sizeof(int);
				ErrorMessage = Encoding.Unicode.GetString(readonly_span.Slice(offset, errormessage_length));
				offset += errormessage_length;
				UserId = BitConverter.ToInt64(readonly_span.Slice(offset, readonly_span.Length - offset));
				offset += sizeof(long);
				var UserList_list_count = BitConverter.ToInt32(readonly_span.Slice(offset, readonly_span.Length - offset));
				offset += sizeof(int);
				for(int i = 0; i < UserList_list_count; ++i)
				{
					var element = new SharedStruct.UserInfo();
					element.Read(readonly_span, ref offset);
					UserList.Add(element);
				}
				return true;
			}
		}
		public class CreateUserReply : IPacket
		{
			public bool Error;
			public string ErrorMessage = "";
			public SharedStruct.UserInfo UserInfo = new SharedStruct.UserInfo();
			
			public CreateUserReply() : base((long)PacketId._CREATEUSERREPLY_)
			{
			}
			protected override int WriteImpl(ArraySegment<byte> buffer)
			{
				if (buffer.Array == null) { return 0; }
				
				var offset = base.WriteImpl(buffer);
				if (offset == 0) { return 0; }
				
				bool result = true;
				var span = new Span<byte>(buffer.Array, buffer.Offset, buffer.Count);
				result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), Error);
				offset += sizeof(bool);
				var errormessage_length = Encoding.Unicode.GetBytes(ErrorMessage, span.Slice(offset + sizeof(int), span.Length - offset - sizeof(int)));
				result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), errormessage_length);
				offset += sizeof(int); offset += errormessage_length;
				result &= UserInfo.Write(span, ref offset);
				
				if (result == false) { return 0; }
				
				return offset;
			}
			public override bool Read(ArraySegment<byte> buffer)
			{
				if (buffer.Array == null) { return false; }
				
				var readonly_span = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);
				int offset = 0;
				Error = BitConverter.ToBoolean(readonly_span.Slice(offset, readonly_span.Length - offset));
				offset += sizeof(bool);
				var errormessage_length = BitConverter.ToInt32(readonly_span.Slice(offset, readonly_span.Length - offset));
				offset += sizeof(int);
				ErrorMessage = Encoding.Unicode.GetString(readonly_span.Slice(offset, errormessage_length));
				offset += errormessage_length;
				UserInfo.Read(readonly_span, ref offset);
				return true;
			}
		}
	}
}