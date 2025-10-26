using System;
using Miniaudio.Net.Interop;

namespace Miniaudio.Net;

public sealed class MiniaudioResourceManagerOptions
{
    public MiniaudioSampleFormat DecodedFormat { get; init; } = MiniaudioSampleFormat.Unknown;

    public uint? DecodedChannels { get; init; }

    public uint? DecodedSampleRate { get; init; }

    public uint? JobThreadCount { get; init; }

    public ResourceManagerFlags Flags { get; init; } = ResourceManagerFlags.None;

    internal bool HasOverrides =>
        DecodedFormat != MiniaudioSampleFormat.Unknown ||
        DecodedChannels.HasValue ||
        DecodedSampleRate.HasValue ||
        JobThreadCount.HasValue ||
        Flags != ResourceManagerFlags.None;

    internal void Validate()
    {
        if (DecodedChannels is { } channels && channels == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(DecodedChannels), "Channel count must be greater than 0.");
        }

        if (DecodedSampleRate is { } rate && rate == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(DecodedSampleRate), "Sample rate must be greater than 0.");
        }

        if (JobThreadCount is { } threads && threads == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(JobThreadCount), "Job thread count must be greater than 0.");
        }
    }

    internal NativeMethods.ResourceManagerConfig ToNativeConfig()
    {
        return new NativeMethods.ResourceManagerConfig
        {
            Flags = (uint)Flags,
            DecodedFormat = (uint)DecodedFormat,
            DecodedChannels = DecodedChannels ?? 0,
            DecodedSampleRate = DecodedSampleRate ?? 0,
            JobThreadCount = JobThreadCount ?? 0,
        };
    }

    internal MiniaudioResourceManagerOptions Snapshot()
    {
        return new MiniaudioResourceManagerOptions
        {
            DecodedFormat = DecodedFormat,
            DecodedChannels = DecodedChannels,
            DecodedSampleRate = DecodedSampleRate,
            JobThreadCount = JobThreadCount,
            Flags = Flags,
        };
    }
}
