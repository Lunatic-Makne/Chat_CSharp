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

            bool result = true;
            var span = new Span<byte>(buffer.Array, buffer.Offset, buffer.Count);

            var name_length = Encoding.Unicode.GetBytes(Name, 0, Name.Length, buffer.Array, buffer.Offset + offset + sizeof(int));
            result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), name_length);
            offset += sizeof(int);
            offset += name_length;

            if (result == false) { return 0; }

            return offset;
        }

        public override bool Read(ArraySegment<byte> buffer)
        {
            if (buffer.Array == null) { return false; }

            var readonly_span = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);

            int offset = 0;
            var length = BitConverter.ToInt32(readonly_span.Slice(offset, readonly_span.Length - offset));
            offset += sizeof(int);

            Name = Encoding.Unicode.GetString(readonly_span.Slice(offset, readonly_span.Length - offset));
            offset += length;

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
            if (offset == 0) { return 0; }

            bool result = true;
            var span = new Span<byte>(buffer.Array, buffer.Offset, buffer.Count);

            result &= BitConverter.TryWriteBytes(span.Slice(offset, span.Length - offset), UserId);
            offset += sizeof(long);

            if (result == false) { return 0; }

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
