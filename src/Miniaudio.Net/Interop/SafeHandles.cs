using System;
using Microsoft.Win32.SafeHandles;

namespace Miniaudio.Net.Interop;

internal sealed class ContextHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private ContextHandle()
        : base(true)
    {
    }

    internal static ContextHandle FromIntPtr(IntPtr handle)
    {
        var safeHandle = new ContextHandle();
        safeHandle.SetHandle(handle);
        return safeHandle;
    }

    protected override bool ReleaseHandle()
    {
        NativeMethods.ContextDestroy(handle);
        return true;
    }
}

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

internal sealed class ResourceManagerHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private ResourceManagerHandle()
        : base(true)
    {
    }

    internal static ResourceManagerHandle FromIntPtr(IntPtr handle)
    {
        var safeHandle = new ResourceManagerHandle();
        safeHandle.SetHandle(handle);
        return safeHandle;
    }

    protected override bool ReleaseHandle()
    {
        NativeMethods.ResourceManagerDestroy(handle);
        return true;
    }
}

internal sealed class CaptureDeviceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private CaptureDeviceHandle()
        : base(true)
    {
    }

    internal static CaptureDeviceHandle FromIntPtr(IntPtr handle)
    {
        var safeHandle = new CaptureDeviceHandle();
        safeHandle.SetHandle(handle);
        return safeHandle;
    }

    protected override bool ReleaseHandle()
    {
        NativeMethods.CaptureDeviceDestroy(handle);
        return true;
    }
}
