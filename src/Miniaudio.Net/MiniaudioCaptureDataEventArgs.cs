using System;

namespace Miniaudio.Net;

public sealed class MiniaudioCaptureDataEventArgs : EventArgs
{
    public MiniaudioCaptureDataEventArgs(float[] samples, uint channelCount)
    {
        Samples = samples ?? throw new ArgumentNullException(nameof(samples));
        ChannelCount = channelCount;
    }

    public float[] Samples { get; }

    public uint ChannelCount { get; }

    public uint FrameCount => ChannelCount == 0 ? 0u : (uint)(Samples.Length / ChannelCount);
}
