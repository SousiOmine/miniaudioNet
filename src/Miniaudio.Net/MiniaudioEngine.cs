using System;
using System.Numerics;
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

    public float GainDb
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.EngineGetGainDb(_handle!);
        }
        set
        {
            ThrowIfDisposed();
            NativeMethods.EngineSetGainDb(_handle!, value).EnsureSuccess(nameof(GainDb));
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

    public uint Channels
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.EngineGetChannelCount(_handle!);
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

    public TimeSpan Time
    {
        get
        {
            ThrowIfDisposed();
            var milliseconds = NativeMethods.EngineGetTimeInMilliseconds(_handle!);
            return TimeSpan.FromMilliseconds(milliseconds);
        }
    }

    public uint ListenerCount
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.EngineGetListenerCount(_handle!);
        }
    }

    public MiniaudioContext? Context => _context;

    public MiniaudioResourceManager? ResourceManager => _resourceManager;

    public readonly struct ListenerCone
    {
        public ListenerCone(float innerAngleRadians, float outerAngleRadians, float outerGain)
        {
            InnerAngleRadians = innerAngleRadians;
            OuterAngleRadians = outerAngleRadians;
            OuterGain = outerGain;
        }

        public float InnerAngleRadians { get; }

        public float OuterAngleRadians { get; }

        public float OuterGain { get; }
    }

    public ulong GetAbsoluteTimeInFrames(TimeSpan offset)
    {
        ThrowIfDisposed();
        var clampedSeconds = Math.Max(0d, offset.TotalSeconds);
        var additionalFrames = (ulong)Math.Round(clampedSeconds * SampleRate, MidpointRounding.AwayFromZero);
        return TimeInPcmFrames + additionalFrames;
    }

    public void SetTimeInFrames(ulong absoluteFrameIndex)
    {
        ThrowIfDisposed();
        NativeMethods.EngineSetTimeInPcmFrames(_handle!, absoluteFrameIndex).EnsureSuccess(nameof(SetTimeInFrames));
    }

    public void SetTime(TimeSpan absoluteTime)
    {
        ThrowIfDisposed();
        if (absoluteTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteTime), "Time must be non-negative.");
        }

        var totalMilliseconds = Math.Round(absoluteTime.TotalMilliseconds, MidpointRounding.AwayFromZero);
        var clampedMilliseconds = Math.Clamp(totalMilliseconds, 0d, ulong.MaxValue);
        NativeMethods.EngineSetTimeInMilliseconds(_handle!, (ulong)clampedMilliseconds).EnsureSuccess(nameof(SetTime));
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

    public void Start()
    {
        ThrowIfDisposed();
        NativeMethods.EngineStart(_handle!).EnsureSuccess(nameof(Start));
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

    public void SetListenerPosition(uint index, Vector3 position) => SetListenerPosition(index, position.X, position.Y, position.Z);

    public Vector3 GetListenerPosition(uint index)
    {
        ThrowIfDisposed();
        NativeMethods.EngineGetListenerPosition(_handle!, index, out var x, out var y, out var z).EnsureSuccess(nameof(GetListenerPosition));
        return new Vector3(x, y, z);
    }

    public void SetListenerDirection(uint index, float x, float y, float z)
    {
        ThrowIfDisposed();
        NativeMethods.EngineSetListenerDirection(_handle!, index, x, y, z).EnsureSuccess(nameof(SetListenerDirection));
    }

    public void SetListenerDirection(uint index, Vector3 direction) => SetListenerDirection(index, direction.X, direction.Y, direction.Z);

    public Vector3 GetListenerDirection(uint index)
    {
        ThrowIfDisposed();
        NativeMethods.EngineGetListenerDirection(_handle!, index, out var x, out var y, out var z).EnsureSuccess(nameof(GetListenerDirection));
        return new Vector3(x, y, z);
    }

    public void SetListenerWorldUp(uint index, float x, float y, float z)
    {
        ThrowIfDisposed();
        NativeMethods.EngineSetListenerWorldUp(_handle!, index, x, y, z).EnsureSuccess(nameof(SetListenerWorldUp));
    }

    public void SetListenerWorldUp(uint index, Vector3 worldUp) => SetListenerWorldUp(index, worldUp.X, worldUp.Y, worldUp.Z);

    public Vector3 GetListenerWorldUp(uint index)
    {
        ThrowIfDisposed();
        NativeMethods.EngineGetListenerWorldUp(_handle!, index, out var x, out var y, out var z).EnsureSuccess(nameof(GetListenerWorldUp));
        return new Vector3(x, y, z);
    }

    public void SetListenerVelocity(uint index, float x, float y, float z)
    {
        ThrowIfDisposed();
        NativeMethods.EngineSetListenerVelocity(_handle!, index, x, y, z).EnsureSuccess(nameof(SetListenerVelocity));
    }

    public void SetListenerVelocity(uint index, Vector3 velocity) => SetListenerVelocity(index, velocity.X, velocity.Y, velocity.Z);

    public Vector3 GetListenerVelocity(uint index)
    {
        ThrowIfDisposed();
        NativeMethods.EngineGetListenerVelocity(_handle!, index, out var x, out var y, out var z).EnsureSuccess(nameof(GetListenerVelocity));
        return new Vector3(x, y, z);
    }

    public void SetListenerCone(uint index, float innerAngleInRadians, float outerAngleInRadians, float outerGain)
    {
        ThrowIfDisposed();
        NativeMethods.EngineSetListenerCone(_handle!, index, innerAngleInRadians, outerAngleInRadians, outerGain).EnsureSuccess(nameof(SetListenerCone));
    }

    public void SetListenerCone(uint index, ListenerCone cone) => SetListenerCone(index, cone.InnerAngleRadians, cone.OuterAngleRadians, cone.OuterGain);

    public ListenerCone GetListenerCone(uint index)
    {
        ThrowIfDisposed();
        NativeMethods.EngineGetListenerCone(_handle!, index, out var inner, out var outer, out var gain).EnsureSuccess(nameof(GetListenerCone));
        return new ListenerCone(inner, outer, gain);
    }

    public void SetListenerEnabled(uint index, bool isEnabled)
    {
        ThrowIfDisposed();
        NativeMethods.EngineSetListenerEnabled(_handle!, index, isEnabled ? 1 : 0).EnsureSuccess(nameof(SetListenerEnabled));
    }

    public bool IsListenerEnabled(uint index)
    {
        ThrowIfDisposed();
        return NativeMethods.EngineIsListenerEnabled(_handle!, index) != 0;
    }

    public uint FindClosestListener(float x, float y, float z)
    {
        ThrowIfDisposed();
        return NativeMethods.EngineFindClosestListener(_handle!, x, y, z);
    }

    public uint FindClosestListener(Vector3 position) => FindClosestListener(position.X, position.Y, position.Z);

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
