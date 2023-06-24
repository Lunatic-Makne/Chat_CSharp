using System;
using System.Collections.Generic;
using System.Text;

namespace Protocol
{
    public interface IPacket
    {
        public long Size { get; set; } = 0;
        public long Id { get; private set; } = 0;

        public IPacket(long id)
        {
            Id = id;
        }
    }
}
