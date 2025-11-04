using System;
using Miniaudio.Net.Interop;

namespace Miniaudio.Net;

public sealed class MiniaudioStreamingSound : MiniaudioSound
{
    private readonly uint _channels;
    private readonly uint _capacityInFrames;

    internal MiniaudioStreamingSound(MiniaudioEngine engine, SoundHandle handle, uint channels, uint sampleRate, uint capacityInFrames)
        : base(engine, handle, "pcm:stream")
    {
        if (channels == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "Channel count must be greater than 0.");
        }

        if (sampleRate == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than 0.");
        }

        _channels = channels;
        _capacityInFrames = capacityInFrames;
    }

    public uint Channels => _channels;

    public uint BufferCapacityInFrames => _capacityInFrames;

    public ulong QueuedFrames
    {
        get
        {
            ThrowIfDisposed();
            NativeMethods.SoundStreamGetQueuedFrames(DangerousHandle, out var frames).EnsureSuccess(nameof(QueuedFrames));
            return frames;
        }
    }

    public ulong AvailableFramesToWrite
    {
        get
        {
            ThrowIfDisposed();
            NativeMethods.SoundStreamGetAvailableWrite(DangerousHandle, out var frames).EnsureSuccess(nameof(AvailableFramesToWrite));
            return frames;
        }
    }

    public ulong AppendPcmFrames(ReadOnlySpan<float> interleavedFrames)
    {
        ThrowIfDisposed();

        if (interleavedFrames.IsEmpty)
        {
            return 0;
        }

        if (interleavedFrames.Length % _channels != 0)
        {
            throw new ArgumentException("PCM data length must be divisible by the number of channels.", nameof(interleavedFrames));
        }

        var frameCount = (ulong)(interleavedFrames.Length / _channels);
        NativeMethods.SoundStreamAppendPcmFrames(DangerousHandle, interleavedFrames, frameCount, out var written).EnsureSuccess(nameof(AppendPcmFrames));
        return written;
    }

    public void SignalEndOfStream()
    {
        ThrowIfDisposed();
        NativeMethods.SoundStreamMarkEnd(DangerousHandle).EnsureSuccess(nameof(SignalEndOfStream));
    }

    public void ClearEndOfStream()
    {
        ThrowIfDisposed();
        NativeMethods.SoundStreamClearEnd(DangerousHandle).EnsureSuccess(nameof(ClearEndOfStream));
    }

    public bool IsEndOfStreamSignaled
    {
        get
        {
            ThrowIfDisposed();
            return NativeMethods.SoundStreamIsEnd(DangerousHandle) != 0;
        }
    }

    public void ResetBuffer()
    {
        ThrowIfDisposed();
        NativeMethods.SoundStreamReset(DangerousHandle).EnsureSuccess(nameof(ResetBuffer));
    }
}
