using System;
using Microsoft.Win32.SafeHandles;

namespace Miniaudio.Net.Interop;

internal sealed class EngineHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private EngineHandle()
        : base(true)
    {
    }

    internal static EngineHandle FromIntPtr(IntPtr handle)
    {
        var safeHandle = new EngineHandle();
        safeHandle.SetHandle(handle);
        return safeHandle;
    }

    protected override bool ReleaseHandle()
    {
        NativeMethods.EngineDestroy(handle);
        return true;
    }
}

internal sealed class SoundHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private SoundHandle()
        : base(true)
    {
    }

    internal static SoundHandle FromIntPtr(IntPtr handle)
    {
        var safeHandle = new SoundHandle();
        safeHandle.SetHandle(handle);
        return safeHandle;
    }

    protected override bool ReleaseHandle()
    {
        NativeMethods.SoundDestroy(handle);
        return true;
    }
}
