using System;
using System.Runtime.InteropServices;
using Miniaudio.Net.Interop;

namespace Miniaudio.Net;

public sealed class MiniaudioCaptureDevice : IDisposable
{
    private CaptureDeviceHandle? _handle;
    private readonly MiniaudioContext? _context;
    private readonly MiniaudioCaptureDeviceOptions _options;
    private readonly NativeMethods.CaptureDeviceDataCallback _callback;
    private GCHandle _selfHandle;
    private bool _selfHandleAllocated;
    private event EventHandler<MiniaudioCaptureDataEventArgs>? _pcmCaptured;

    private MiniaudioCaptureDevice(MiniaudioCaptureDeviceOptions options)
    {
        _options = options.Snapshot();
        _context = options.Context;
        _callback = OnNativeData;
        _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        _selfHandleAllocated = true;

        var userData = GCHandle.ToIntPtr(_selfHandle);
        var handle = NativeMethods.CaptureDeviceCreate(
            options.Context?.DangerousHandle,
            string.IsNullOrWhiteSpace(options.CaptureDeviceId) ? null : options.CaptureDeviceId,
            options.SampleRate,
            options.Channels,
            _callback,
            userData);

        if (handle is null || handle.IsInvalid)
        {
            _selfHandle.Free();
            _selfHandleAllocated = false;
            throw new InvalidOperationException("Failed to initialize capture device. Confirm that the selected device exists and that native binaries are available.");
        }

        _handle = handle;
    }

    public static MiniaudioCaptureDevice Create(MiniaudioCaptureDeviceOptions? options = null)
    {
        options ??= new MiniaudioCaptureDeviceOptions();
        options.Validate();
        return new MiniaudioCaptureDevice(options);
    }

    public MiniaudioCaptureDeviceOptions Options => _options;

    public event EventHandler<MiniaudioCaptureDataEventArgs>? PcmCaptured
    {
        add => _pcmCaptured += value;
        remove => _pcmCaptured -= value;
    }

    public void Start()
    {
        ThrowIfDisposed();
        NativeMethods.CaptureDeviceStart(_handle!).EnsureSuccess(nameof(Start));
    }

    public void Stop()
    {
        ThrowIfDisposed();
        NativeMethods.CaptureDeviceStop(_handle!).EnsureSuccess(nameof(Stop));
    }

    private void ThrowIfDisposed()
    {
        if (_handle is null || _handle.IsClosed)
        {
            throw new ObjectDisposedException(nameof(MiniaudioCaptureDevice));
        }
    }

    private unsafe void OnNativeData(IntPtr samples, uint frameCount, uint channelCount, IntPtr userData)
    {
        var handlers = _pcmCaptured;
        if (handlers is null || samples == IntPtr.Zero || channelCount == 0)
        {
            return;
        }

        var sampleCount = checked((int)(frameCount * channelCount));
        var buffer = new float[sampleCount];
        var source = (float*)samples.ToPointer();
        for (var i = 0; i < sampleCount; i++)
        {
            buffer[i] = source[i];
        }

        var args = new MiniaudioCaptureDataEventArgs(buffer, channelCount);
        try
        {
            handlers.Invoke(this, args);
        }
        catch
        {
            // Swallow exceptions to avoid terminating the audio thread.
        }
    }

    public void Dispose()
    {
        if (_handle is null)
        {
            return;
        }

        _handle.Dispose();
        _handle = null;

        if (_selfHandleAllocated)
        {
            _selfHandle.Free();
            _selfHandleAllocated = false;
        }

        GC.SuppressFinalize(this);
    }
}
