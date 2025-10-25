using System;

namespace Miniaudio.Net;

[Flags]
public enum SoundInitFlags : uint
{
    None = 0,
    Stream = 0x0000_0001,
    Decode = 0x0000_0002,
    Async = 0x0000_0004,
    WaitInit = 0x0000_0008,
    UnknownLength = 0x0000_0010,
    Looping = 0x0000_0020,
    NoDefaultAttachment = 0x0000_1000,
    NoPitch = 0x0000_2000,
    NoSpatialization = 0x0000_4000,
}
