using System;
using Miniaudio.Net.Interop;

namespace Miniaudio.Net;

public sealed class MiniaudioEngine : IDisposable
{
    private EngineHandle? _handle;
    private readonly MiniaudioContext? _context;
    private readonly MiniaudioResourceManager? _resourceManager;

    private MiniaudioEngine(EngineHandle handle, MiniaudioContext? context = null, MiniaudioResourceManager? resourceManager = null)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        _context = context;
        _resourceManager = resourceManager;
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

    public static MiniaudioEngine Create(MiniaudioEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var handle = NativeMethods.EngineCreateWithOptions(
            options.Context?.DangerousHandle,
            options.ResourceManager?.DangerousHandle,
            options.NormalizeDeviceId(),
            options.SampleRate ?? 0,
            options.Channels ?? 0,
            options.PeriodSizeInFrames ?? 0,
            options.PeriodSizeInMilliseconds ?? 0,
            options.NoAutoStart,
            options.NoDevice);

        if (handle is null || handle.IsInvalid)
        {
            throw new InvalidOperationException("Failed to initialize miniaudio engine with the provided options. Confirm that the native miniaudionet library is built and discoverable.");
        }

        return new MiniaudioEngine(handle, options.Context, options.ResourceManager);
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

    public MiniaudioContext? Context => _context;

    public MiniaudioResourceManager? ResourceManager => _resourceManager;

    public ulong GetAbsoluteTimeInFrames(TimeSpan offset)
    {
        ThrowIfDisposed();
        var clampedSeconds = Math.Max(0d, offset.TotalSeconds);
        var additionalFrames = (ulong)Math.Round(clampedSeconds * SampleRate, MidpointRounding.AwayFromZero);
        return TimeInPcmFrames + additionalFrames;
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

    public MiniaudioSound CreateSoundFromPcmFrames(ReadOnlySpan<float> interleavedFrames, uint channels, uint sampleRate, SoundInitFlags flags = SoundInitFlags.None)
    {
        ThrowIfDisposed();

        if (channels == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "Channel count must be greater than 0.");
        }

        if (sampleRate == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than 0.");
        }

        if (interleavedFrames.IsEmpty)
        {
            throw new ArgumentException("PCM data cannot be empty.", nameof(interleavedFrames));
        }

        if (interleavedFrames.Length % channels != 0)
        {
            throw new ArgumentException("PCM data length must be divisible by the number of channels.", nameof(interleavedFrames));
        }

        var frameCount = (ulong)(interleavedFrames.Length / channels);
        var soundHandle = NativeMethods.SoundCreateFromPcmFrames(_handle!, interleavedFrames, frameCount, channels, sampleRate, (uint)flags);
        if (soundHandle is null || soundHandle.IsInvalid)
        {
            throw new InvalidOperationException("Failed to create sound from PCM frames. Ensure that the native library was built with PCM buffer support.");
        }

        return new MiniaudioSound(this, soundHandle, "pcm:memory");
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

    public void SetListenerDirection(uint index, float x, float y, float z)
    {
        ThrowIfDisposed();
        NativeMethods.EngineSetListenerDirection(_handle!, index, x, y, z).EnsureSuccess(nameof(SetListenerDirection));
    }

    public void SetListenerWorldUp(uint index, float x, float y, float z)
    {
        ThrowIfDisposed();
        NativeMethods.EngineSetListenerWorldUp(_handle!, index, x, y, z).EnsureSuccess(nameof(SetListenerWorldUp));
    }

    public void SetListenerVelocity(uint index, float x, float y, float z)
    {
        ThrowIfDisposed();
        NativeMethods.EngineSetListenerVelocity(_handle!, index, x, y, z).EnsureSuccess(nameof(SetListenerVelocity));
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
