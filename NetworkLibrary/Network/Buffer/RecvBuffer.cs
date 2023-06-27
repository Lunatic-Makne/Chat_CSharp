using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkCore
{
    public class RecvBuffer
    {
        private ArraySegment<byte> _Buffer;
        private int _ReadPos = 0;
        private int _RecvPos = 0;

        public RecvBuffer(int buffer_size)
        {
            _Buffer = new ArraySegment<byte>(new byte[buffer_size], 0, buffer_size);
        }

        public int DataSize { get { return _RecvPos - _ReadPos; } }
        public int FreeSize { get { return _Buffer.Count - _RecvPos; } }

        public ArraySegment<byte> ReadSegment
        {
            get { return new ArraySegment<byte>(_Buffer.Array, _Buffer.Offset + _ReadPos, DataSize); }
        }

        public ArraySegment<byte> WriteSegment
        {
            get { return new ArraySegment<byte>(_Buffer.Array, _Buffer.Offset + _RecvPos, FreeSize); }
        }

        public void Clear()
        {
            var before_data_size = DataSize;
            if (before_data_size == 0)
            {
                _ReadPos = _RecvPos = 0;
            }
            else
            {

                Array.Copy(_Buffer.Array, _Buffer.Offset + _ReadPos, _Buffer.Array, _Buffer.Offset, before_data_size);
                _ReadPos = 0;
                _RecvPos = before_data_size;
            }
        }

        public bool OnRead(int number_of_bytes)
        {
            if (number_of_bytes > DataSize)
            {
                return false;
            }

            _ReadPos += number_of_bytes;
            return true;
        }

        public bool OnRecv(int number_of_bytes)
        {
            if (number_of_bytes > FreeSize)
            {
                return false;
            }

            _RecvPos += number_of_bytes;
            return true;
        }
    }
}
