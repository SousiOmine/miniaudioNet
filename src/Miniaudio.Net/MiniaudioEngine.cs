using System;
using Miniaudio.Net.Interop;

namespace Miniaudio.Net;

public sealed class MiniaudioEngine : IDisposable
{
    private EngineHandle? _handle;

    private MiniaudioEngine(EngineHandle handle)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    public static MiniaudioEngine Create()
    {
        var handle = NativeMethods.EngineCreate();
        if (handle is null || handle.IsInvalid)
        {
            throw new InvalidOperationException("Failed to initialize miniaudio engine. Confirm that the native miniaudionet library is built and discoverable.");
        }

        return new MiniaudioEngine(handle);
    }

    public float Volume
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.EngineGetVolume(_handle!);
        }
        set
        {
            ThrowIfDisposed();
            NativeMethods.EngineSetVolume(_handle!, value).EnsureSuccess(nameof(Volume));
        }
    }

    public uint SampleRate
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.EngineGetSampleRate(_handle!);
        }
    }

    public ulong TimeInPcmFrames
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.EngineGetTimeInPcmFrames(_handle!);
        }
    }

    public MiniaudioSound CreateSound(string filePath, SoundInitFlags flags = SoundInitFlags.None)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var soundHandle = NativeMethods.SoundCreateFromFile(_handle!, filePath, (uint)flags);
        if (soundHandle is null || soundHandle.IsInvalid)
        {
            throw new InvalidOperationException($"Failed to create sound for '{filePath}'. Verify that the file exists and the native library was compiled with decoder support.");
        }

        return new MiniaudioSound(this, soundHandle, filePath);
    }

    public void Play(string filePath)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        NativeMethods.EnginePlaySound(_handle!, filePath).EnsureSuccess(nameof(Play));
    }

    public void Stop()
    {
        ThrowIfDisposed();
        NativeMethods.EngineStop(_handle!).EnsureSuccess(nameof(Stop));
    }

    public void SetListenerPosition(uint index, float x, float y, float z)
    {
        ThrowIfDisposed();
        NativeMethods.EngineSetListenerPosition(_handle!, index, x, y, z).EnsureSuccess(nameof(SetListenerPosition));
    }

    internal EngineHandle DangerousHandle
    {
        get
        {
            ThrowIfDisposed();
            return _handle!;
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
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (_handle is null || _handle.IsClosed)
        {
            throw new ObjectDisposedException(nameof(MiniaudioEngine));
        }
    }
}
