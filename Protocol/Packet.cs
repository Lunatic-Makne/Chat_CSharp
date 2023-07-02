using System;
using System.Collections.Generic;
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

    public class IPacket
    {
        public short Size = 0;
        public PacketId Id = PacketId._Unknown_;
    }


}
