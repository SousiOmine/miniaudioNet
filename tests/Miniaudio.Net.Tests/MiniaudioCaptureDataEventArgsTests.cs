using NUnit.Framework;
using Miniaudio.Net;
using System;

namespace Miniaudio.Net.Tests;

[TestFixture]
public class MiniaudioCaptureDataEventArgsTests
{
    [Test]
    public void Constructor_SetsProperties()
    {
        var samples = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
        var args = new MiniaudioCaptureDataEventArgs(samples, 2);

        Assert.Multiple(() =>
        {
            Assert.That(args.Samples, Is.EqualTo(samples));
            Assert.That(args.ChannelCount, Is.EqualTo(2u));
        });
    }

    [Test]
    public void Samples_ReturnsSameReference()
    {
        var samples = new float[] { 1.0f, 2.0f };
        var args = new MiniaudioCaptureDataEventArgs(samples, 1);

        Assert.That(args.Samples, Is.SameAs(samples));
    }

    [Test]
    public void FrameCount_CalculatedCorrectly_Mono()
    {
        var samples = new float[100];
        var args = new MiniaudioCaptureDataEventArgs(samples, 1);

        Assert.That(args.FrameCount, Is.EqualTo(100u));
    }

    [Test]
    public void FrameCount_CalculatedCorrectly_Stereo()
    {
        var samples = new float[100];
        var args = new MiniaudioCaptureDataEventArgs(samples, 2);

        Assert.That(args.FrameCount, Is.EqualTo(50u));
    }

    [Test]
    public void FrameCount_CalculatedCorrectly_Multichannel()
    {
        var samples = new float[120];
        var args = new MiniaudioCaptureDataEventArgs(samples, 6);

        Assert.That(args.FrameCount, Is.EqualTo(20u));
    }

    [Test]
    public void EmptySamples_FrameCountIsZero()
    {
        var samples = Array.Empty<float>();
        var args = new MiniaudioCaptureDataEventArgs(samples, 2);

        Assert.That(args.FrameCount, Is.EqualTo(0u));
    }

    [Test]
    public void SingleChannel_FrameCountEqualsSampleCount()
    {
        var samples = new float[] { 0.5f, -0.5f, 0.25f };
        var args = new MiniaudioCaptureDataEventArgs(samples, 1);

        Assert.Multiple(() =>
        {
            Assert.That(args.FrameCount, Is.EqualTo(3u));
            Assert.That(args.Samples.Length, Is.EqualTo(3));
        });
    }
}
