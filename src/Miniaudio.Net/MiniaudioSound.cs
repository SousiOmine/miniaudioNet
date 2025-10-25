using System;
using Miniaudio.Net.Interop;

namespace Miniaudio.Net;

public sealed class MiniaudioSound : IDisposable
{
    private SoundHandle? _handle;
    private readonly MiniaudioEngine _engine;

    internal MiniaudioSound(MiniaudioEngine engine, SoundHandle handle, string sourcePath)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        SourcePath = sourcePath;
    }

    public string SourcePath { get; }

    public MiniaudioEngine Engine => _engine;

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

    private double ConvertFramesToSeconds(ulong frames)
    {
        var rate = SampleRate;
        if (rate == 0)
        {
            return 0d;
        }

        return frames / (double)rate;
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
            throw new ObjectDisposedException(nameof(MiniaudioSound));
        }
    }
}
