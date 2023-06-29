using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkCore
{
    public class SendBufferHelper
    {
        public static ThreadLocal<SendBuffer> CurrentBuffer = new ThreadLocal<SendBuffer>(() => { return null; });

        public static readonly int CHUNK_SIZE = 8 * 1024 * 100; // 8K * 100

        public static ArraySegment<byte>? Open(int reserve_size)
        {
            if (CurrentBuffer.Value == null)
                CurrentBuffer.Value = new SendBuffer(CHUNK_SIZE);

            if (CurrentBuffer.Value.FreeSize < reserve_size)
                CurrentBuffer.Value = new SendBuffer(CHUNK_SIZE);

            return CurrentBuffer.Value.Open(reserve_size);
        }

        public static ArraySegment<byte>? Close(int used_size)
        {
            return CurrentBuffer.Value?.Close(used_size);
        }
    }

    public class SendBuffer
    {
        private byte[] _Buffer;
        private int _UsedSize = 0;

        public int FreeSize { get { return _Buffer.Length - _UsedSize; } }

        public SendBuffer(int chunk_size)
        {
            _Buffer = new byte[chunk_size];
        }

        public ArraySegment<byte>? Open(int reserve_size)
        {
            if (reserve_size > FreeSize) { return null; }

            return new ArraySegment<byte>(_Buffer, _UsedSize, reserve_size);
        }

        public ArraySegment<byte> Close(int used_size) 
        {
            var before_used_size = _UsedSize;
            _UsedSize += used_size;

            return new ArraySegment<byte>(_Buffer, before_used_size, used_size);
        }
    }
}
