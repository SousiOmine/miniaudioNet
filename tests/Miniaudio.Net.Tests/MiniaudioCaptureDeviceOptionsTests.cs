using NUnit.Framework;
using Miniaudio.Net;
using System;

namespace Miniaudio.Net.Tests;

[TestFixture]
public class MiniaudioCaptureDeviceOptionsTests
{
    [Test]
    public void Validate_DefaultOptions_DoesNotThrow()
    {
        var options = new MiniaudioCaptureDeviceOptions();

        Assert.DoesNotThrow(() => options.Validate());
    }

    [Test]
    public void Validate_ZeroSampleRate_ThrowsArgumentOutOfRangeException()
    {
        var options = new MiniaudioCaptureDeviceOptions
        {
            SampleRate = 0,
        };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());

        Assert.That(ex?.Message, Does.Contain("Sample rate"));
    }

    [Test]
    public void Validate_ZeroChannels_ThrowsArgumentOutOfRangeException()
    {
        var options = new MiniaudioCaptureDeviceOptions
        {
            SampleRate = 48000,
            Channels = 0,
        };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());

        Assert.That(ex?.Message, Does.Contain("Channel count"));
    }

    [Test]
    public void Snapshot_CopiesAllProperties()
    {
        var options = new MiniaudioCaptureDeviceOptions
        {
            CaptureDeviceId = "test-device",
            SampleRate = 44100,
            Channels = 2,
        };

        var snapshot = options.Snapshot();

        Assert.That(snapshot, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(snapshot.CaptureDeviceId, Is.EqualTo("test-device"));
            Assert.That(snapshot.SampleRate, Is.EqualTo(44100u));
            Assert.That(snapshot.Channels, Is.EqualTo(2u));
        });
    }

    [Test]
    public void Snapshot_CreatesIndependentCopy()
    {
        var options = new MiniaudioCaptureDeviceOptions
        {
            CaptureDeviceId = "original-device",
            SampleRate = 48000,
            Channels = 1,
        };

        var snapshot = options.Snapshot();

        Assert.That(snapshot, Is.Not.SameAs(options));
    }

    [Test]
    public void Properties_DefaultValues()
    {
        var options = new MiniaudioCaptureDeviceOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Context, Is.Null);
            Assert.That(options.CaptureDeviceId, Is.Null);
            Assert.That(options.SampleRate, Is.EqualTo(48_000u));
            Assert.That(options.Channels, Is.EqualTo(1u));
        });
    }

    [Test]
    public void Validate_CustomValidValues_DoesNotThrow()
    {
        var options = new MiniaudioCaptureDeviceOptions
        {
            SampleRate = 96000,
            Channels = 8,
            CaptureDeviceId = "custom-device",
        };

        Assert.DoesNotThrow(() => options.Validate());
    }
}
