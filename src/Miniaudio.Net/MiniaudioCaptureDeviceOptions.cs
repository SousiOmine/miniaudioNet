using System;

namespace Miniaudio.Net;

public sealed class MiniaudioCaptureDeviceOptions
{
    public MiniaudioContext? Context { get; init; }

    public string? CaptureDeviceId { get; init; }

    public uint SampleRate { get; init; } = 48_000;

    public uint Channels { get; init; } = 1;

    internal void Validate()
    {
        if (SampleRate == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SampleRate), "Sample rate must be greater than 0.");
        }

        if (Channels == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Channels), "Channel count must be greater than 0.");
        }
    }

    internal MiniaudioCaptureDeviceOptions Snapshot()
    {
        return new MiniaudioCaptureDeviceOptions
        {
            Context = Context,
            CaptureDeviceId = CaptureDeviceId,
            SampleRate = SampleRate,
            Channels = Channels,
        };
    }
}
