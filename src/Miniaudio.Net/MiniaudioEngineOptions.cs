using System;

namespace Miniaudio.Net;

public sealed class MiniaudioEngineOptions
{
    public MiniaudioContext? Context { get; init; }

    public MiniaudioResourceManager? ResourceManager { get; init; }

    public string? PlaybackDeviceId { get; init; }

    public uint? SampleRate { get; init; }

    public uint? Channels { get; init; }

    public uint? PeriodSizeInFrames { get; init; }

    public uint? PeriodSizeInMilliseconds { get; init; }

    public bool NoAutoStart { get; init; }

    public bool NoDevice { get; init; }

    internal void Validate()
    {
        if (NoDevice)
        {
            if (!SampleRate.HasValue || SampleRate.Value == 0)
            {
                throw new ArgumentException("SampleRate must be specified when NoDevice is enabled.", nameof(SampleRate));
            }

            if (!Channels.HasValue || Channels.Value == 0)
            {
                throw new ArgumentException("Channels must be specified when NoDevice is enabled.", nameof(Channels));
            }
        }

        if (PlaybackDeviceId is { Length: 0 })
        {
            throw new ArgumentException("PlaybackDeviceId cannot be an empty string.", nameof(PlaybackDeviceId));
        }
    }

    internal string? NormalizeDeviceId()
    {
        return string.IsNullOrWhiteSpace(PlaybackDeviceId) ? null : PlaybackDeviceId.Trim();
    }
}
