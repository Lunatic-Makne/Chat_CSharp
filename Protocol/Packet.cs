using NetworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Sockets;
using System.Text;

namespace Protocol
{
    public enum PacketId : long
    {
        _Unknown_ = 0,
        _HI_,
        _WELCOME_,
        _MAX_
    };

    public abstract class IPacket
    {
        public short Size = 0;
        public PacketId Id = PacketId._Unknown_;

        public IPacket(PacketId id)
        {
            Id = id;

            Size = sizeof(short);
            Size += sizeof(PacketId);
        }

        public ArraySegment<byte>? Write(int send_buffer_size)
        {
            var openSegment = SendBufferHelper.Open(send_buffer_size);
            if (openSegment == null || openSegment.HasValue == false) { return null; }
            if (openSegment.Value.Array == null) { return null; }

            var buffer = openSegment.Value;
            Size = (short)WriteImpl(buffer);
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

            var id = BitConverter.GetBytes((long)Id);
            Array.Copy(id, 0, buffer.Array, buffer.Offset + offset, sizeof(long));
            offset += sizeof(long);

            return offset;
        }

        public abstract bool Read(ArraySegment<byte> buffer);
    }

    public class  Hi : IPacket
    {
        public string Name { get; set; } = new string("");

        public Hi()
            : base(PacketId._HI_)
        {
        }

        protected override int WriteImpl(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null) { return 0; }

            var offset = base.WriteImpl(buffer);
            if (offset == 0) { return 0; }

            var name = Encoding.UTF8.GetBytes(Name);
            bool result = true;
            result &= BitConverter.TryWriteBytes(new Span<byte>(buffer.Array, buffer.Offset + offset, buffer.Count - offset), name.Length);
            offset += sizeof(int);

            Array.Copy(name, 0, buffer.Array, buffer.Offset + offset, name.Length);
            offset += name.Length;

            return offset;
        }

        public override bool Read(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null) { return false; }

            int offset = 0;
            var length = BitConverter.ToInt32(new ReadOnlySpan<byte>(buffer.Array, buffer.Offset + offset, buffer.Count - offset));
            offset += sizeof(int);

            var byte_buffer = new byte[length];
            Array.Copy(buffer.Array, buffer.Offset + offset, byte_buffer, 0, length);
            Name = Encoding.UTF8.GetString(byte_buffer);

            return true;
        }
    }

    public class Welcome : IPacket
    {
        public long UserId { get; set; } = 0;

        public Welcome()
            : base(PacketId._WELCOME_)
        {
        }

        protected override int WriteImpl(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null) { return 0; }

            var offset = base.WriteImpl(buffer);
            var user_id = BitConverter.GetBytes(UserId);
            Array.Copy(user_id, 0, buffer.Array, buffer.Offset + offset, user_id.Length);
            offset += sizeof(long);

            return offset;
        }

        public override bool Read(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null) { return false; }

            int offset = 0;
            UserId = BitConverter.ToInt64(new ReadOnlySpan<byte>(buffer.Array, buffer.Offset + offset, buffer.Count - offset));
            offset += sizeof(long);

            return true;
        }
    }
}
