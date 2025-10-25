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
