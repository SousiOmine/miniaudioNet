using System;

namespace Miniaudio.Net;

[Flags]
public enum ResourceManagerFlags : uint
{
    None = 0,
    NonBlocking = 0x0000_0001,
    NoThreading = 0x0000_0002,
}
