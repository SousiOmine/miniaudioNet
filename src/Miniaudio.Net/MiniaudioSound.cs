using System;
using System.Runtime.InteropServices;
using Miniaudio.Net.Interop;

namespace Miniaudio.Net;

public class MiniaudioSound : IDisposable
{
    private SoundHandle? _handle;
    private readonly MiniaudioEngine _engine;
    private event EventHandler? _ended;
    private NativeMethods.SoundEndCallback? _endCallback;
    private GCHandle _endCallbackHandle;
    private bool _endCallbackHandleAllocated;

    internal MiniaudioSound(MiniaudioEngine engine, SoundHandle handle, string sourcePath)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        SourcePath = sourcePath;
    }

    public string SourcePath { get; }

    public MiniaudioEngine Engine => _engine;

    internal SoundHandle DangerousHandle
    {
        get
        {
            ThrowIfDisposed();
            return _handle!;
        }
    }

    public bool Looping
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.SoundIsLooping(_handle!) != 0;
        }
        set
        {
            ThrowIfDisposed();
            NativeMethods.SoundSetLooping(_handle!, value ? 1 : 0).EnsureSuccess(nameof(Looping));
        }
    }

    public float Volume
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.SoundGetVolume(_handle!);
        }
        set
        {
            ThrowIfDisposed();
            NativeMethods.SoundSetVolume(_handle!, value).EnsureSuccess(nameof(Volume));
        }
    }

    public float Pitch
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.SoundGetPitch(_handle!);
        }
        set
        {
            ThrowIfDisposed();
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Pitch must be greater than 0.");
            }

            NativeMethods.SoundSetPitch(_handle!, value).EnsureSuccess(nameof(Pitch));
        }
    }

    public float Pan
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.SoundGetPan(_handle!);
        }
        set
        {
            ThrowIfDisposed();
            NativeMethods.SoundSetPan(_handle!, value).EnsureSuccess(nameof(Pan));
        }
    }

    public (float X, float Y, float Z) Position
    {
        get
        {
            ThrowIfDisposed();
            NativeMethods.SoundGetPosition(_handle!, out var x, out var y, out var z).EnsureSuccess(nameof(Position));
            return (x, y, z);
        }
        set
        {
            ThrowIfDisposed();
            NativeMethods.SoundSetPosition(_handle!, value.X, value.Y, value.Z).EnsureSuccess(nameof(Position));
        }
    }

    public (float X, float Y, float Z) Direction
    {
        get
        {
            ThrowIfDisposed();
            NativeMethods.SoundGetDirection(_handle!, out var x, out var y, out var z).EnsureSuccess(nameof(Direction));
            return (x, y, z);
        }
        set
        {
            ThrowIfDisposed();
            NativeMethods.SoundSetDirection(_handle!, value.X, value.Y, value.Z).EnsureSuccess(nameof(Direction));
        }
    }

    public SoundPositioning Positioning
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.SoundGetPositioning(_handle!);
        }
        set
        {
            ThrowIfDisposed();
            NativeMethods.SoundSetPositioning(_handle!, value).EnsureSuccess(nameof(Positioning));
        }
    }

    public SoundState State
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.SoundGetState(_handle!);
        }
    }

    public ulong LengthInFrames
    {
        get
        {
            ThrowIfDisposed();
            NativeMethods.SoundGetLength(_handle!, out var frames).EnsureSuccess(nameof(LengthInFrames));
            return frames;
        }
    }

    public ulong CursorInFrames
    {
        get
        {
            ThrowIfDisposed();
            NativeMethods.SoundGetCursor(_handle!, out var frames).EnsureSuccess(nameof(CursorInFrames));
            return frames;
        }
    }

    public uint SampleRate
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.SoundGetSampleRate(_handle!);
        }
    }

    public double LengthInSeconds => ConvertFramesToSeconds(LengthInFrames);

    public double CursorInSeconds => ConvertFramesToSeconds(CursorInFrames);

    public double Progress => LengthInFrames == 0 ? 0d : (double)CursorInFrames / LengthInFrames;

    public event EventHandler? Ended
    {
        add
        {
            ThrowIfDisposed();
            _ended += value;
            EnsureEndCallbackRegistered();
        }
        remove
        {
            ThrowIfDisposed();
            _ended -= value;
            if (_ended is null)
            {
                DisableEndCallback();
            }
        }
    }

    public void Start()
    {
        ThrowIfDisposed();
        NativeMethods.SoundStart(_handle!).EnsureSuccess(nameof(Start));
    }

    public void Stop()
    {
        ThrowIfDisposed();
        NativeMethods.SoundStop(_handle!).EnsureSuccess(nameof(Stop));
    }

    public void SeekToFrame(ulong frameIndex)
    {
        ThrowIfDisposed();
        NativeMethods.SoundSeek(_handle!, frameIndex).EnsureSuccess(nameof(SeekToFrame));
    }

    public void SeekToStart() => SeekToFrame(0);

    public void ApplyFade(float fromVolume, float toVolume, ulong lengthInFrames)
    {
        ThrowIfDisposed();
        NativeMethods.SoundSetFadeInFrames(_handle!, fromVolume, toVolume, lengthInFrames).EnsureSuccess(nameof(ApplyFade));
    }

    public void ApplyFade(float fromVolume, float toVolume, TimeSpan duration)
    {
        ApplyFade(fromVolume, toVolume, ConvertTimeSpanToFrames(duration));
    }

    public void ApplyFade(float fromVolume, float toVolume, TimeSpan startDelay, TimeSpan duration)
    {
        ThrowIfDisposed();
        var fadeLength = ConvertTimeSpanToFrames(duration);
        var startFrame = _engine.GetAbsoluteTimeInFrames(startDelay);
        NativeMethods.SoundSetFadeStartInFrames(_handle!, fromVolume, toVolume, fadeLength, startFrame).EnsureSuccess(nameof(ApplyFade));
    }

    public void ScheduleStart(ulong absoluteFrameIndex)
    {
        ThrowIfDisposed();
        NativeMethods.SoundSetStartTimeInFrames(_handle!, absoluteFrameIndex).EnsureSuccess(nameof(ScheduleStart));
    }

    public void ScheduleStart(TimeSpan delay)
    {
        ScheduleStart(_engine.GetAbsoluteTimeInFrames(delay));
    }

    public void ScheduleStop(ulong absoluteFrameIndex)
    {
        ThrowIfDisposed();
        NativeMethods.SoundSetStopTimeInFrames(_handle!, absoluteFrameIndex).EnsureSuccess(nameof(ScheduleStop));
    }

    public void ScheduleStop(TimeSpan delay)
    {
        ScheduleStop(_engine.GetAbsoluteTimeInFrames(delay));
    }

    public void ScheduleStop(ulong absoluteFrameIndex, ulong fadeLengthInFrames)
    {
        ThrowIfDisposed();
        NativeMethods.SoundSetStopTimeWithFadeInFrames(_handle!, absoluteFrameIndex, fadeLengthInFrames).EnsureSuccess(nameof(ScheduleStop));
    }

    public void ScheduleStop(TimeSpan delay, TimeSpan fadeLength)
    {
        ScheduleStop(_engine.GetAbsoluteTimeInFrames(delay), ConvertTimeSpanToFrames(fadeLength));
    }

    private double ConvertFramesToSeconds(ulong frames)
    {
        var rate = SampleRate;
        if (rate == 0)
        {
            return 0d;
        }

        return frames / (double)rate;
    }

    private ulong ConvertTimeSpanToFrames(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return 0;
        }

        var rate = SampleRate;
        if (rate == 0)
        {
            return 0;
        }

        var frames = duration.TotalSeconds * rate;
        return (ulong)Math.Max(0, Math.Round(frames, MidpointRounding.AwayFromZero));
    }

    private void EnsureEndCallbackRegistered()
    {
        if (_endCallback is not null)
        {
            return;
        }

        _endCallback = HandleNativeSoundEnded;
        _endCallbackHandle = GCHandle.Alloc(this, GCHandleType.Normal);
        _endCallbackHandleAllocated = true;
        NativeMethods.SoundSetEndCallback(_handle!, _endCallback, GCHandle.ToIntPtr(_endCallbackHandle)).EnsureSuccess(nameof(Ended));
    }

    private void DisableEndCallback()
    {
        if (_endCallback is null)
        {
            return;
        }

        NativeMethods.SoundSetEndCallback(_handle!, null, IntPtr.Zero).EnsureSuccess(nameof(Ended));
        _endCallback = null;
        if (_endCallbackHandleAllocated)
        {
            _endCallbackHandle.Free();
            _endCallbackHandleAllocated = false;
        }
    }

    private static void HandleNativeSoundEnded(SoundHandle soundHandle, IntPtr userData)
    {
        if (userData == IntPtr.Zero)
        {
            return;
        }

        var handle = GCHandle.FromIntPtr(userData);
        if (handle.Target is not MiniaudioSound sound)
        {
            return;
        }

        sound.RaiseEnded();
    }

    private void RaiseEnded()
    {
        var handlers = _ended;
        if (handlers is null)
        {
            return;
        }

        try
        {
            handlers.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // Intentionally ignore user exceptions to avoid terminating the audio thread.
        }
    }

    public void Dispose()
    {
        if (_handle is null)
        {
            return;
        }

        DisableEndCallback();
        _handle.Dispose();
        _handle = null;
        GC.SuppressFinalize(this);
    }

    protected void ThrowIfDisposed()
    {
        if (_handle is null || _handle.IsClosed)
        {
            throw new ObjectDisposedException(nameof(MiniaudioSound));
        }
    }
}
