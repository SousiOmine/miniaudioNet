using NUnit.Framework;
using Miniaudio.Net;
using System;

namespace Miniaudio.Net.Tests;

[TestFixture]
public class MiniaudioEngineOptionsTests
{
    [Test]
    public void Validate_DefaultOptions_DoesNotThrow()
    {
        var options = new MiniaudioEngineOptions();

        Assert.DoesNotThrow(() => options.Validate());
    }

    [Test]
    public void Validate_NoDeviceWithoutSampleRate_ThrowsArgumentException()
    {
        var options = new MiniaudioEngineOptions
        {
            NoDevice = true,
            Channels = 2,
        };

        var ex = Assert.Throws<ArgumentException>(() => options.Validate());

        Assert.That(ex?.Message, Does.Contain("SampleRate"));
    }

    [Test]
    public void Validate_NoDeviceWithoutChannels_ThrowsArgumentException()
    {
        var options = new MiniaudioEngineOptions
        {
            NoDevice = true,
            SampleRate = 48000,
        };

        var ex = Assert.Throws<ArgumentException>(() => options.Validate());

        Assert.That(ex?.Message, Does.Contain("Channels"));
    }

    [Test]
    public void Validate_NoDeviceWithValidOptions_DoesNotThrow()
    {
        var options = new MiniaudioEngineOptions
        {
            NoDevice = true,
            SampleRate = 48000,
            Channels = 2,
        };

        Assert.DoesNotThrow(() => options.Validate());
    }

    [Test]
    public void Validate_EmptyPlaybackDeviceId_ThrowsArgumentException()
    {
        var options = new MiniaudioEngineOptions
        {
            PlaybackDeviceId = "",
        };

        var ex = Assert.Throws<ArgumentException>(() => options.Validate());

        Assert.That(ex?.Message, Does.Contain("PlaybackDeviceId"));
    }

    [Test]
    public void Validate_NullPlaybackDeviceId_DoesNotThrow()
    {
        var options = new MiniaudioEngineOptions
        {
            PlaybackDeviceId = null,
        };

        Assert.DoesNotThrow(() => options.Validate());
    }

    [Test]
    public void NormalizeDeviceId_NullValue_ReturnsNull()
    {
        var options = new MiniaudioEngineOptions
        {
            PlaybackDeviceId = null,
        };

        var result = options.NormalizeDeviceId();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void NormalizeDeviceId_WhitespaceValue_ReturnsNull()
    {
        var options = new MiniaudioEngineOptions
        {
            PlaybackDeviceId = "   ",
        };

        var result = options.NormalizeDeviceId();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void NormalizeDeviceId_ValidValue_ReturnsTrimmed()
    {
        var options = new MiniaudioEngineOptions
        {
            PlaybackDeviceId = "  device-id-123  ",
        };

        var result = options.NormalizeDeviceId();

        Assert.That(result, Is.EqualTo("device-id-123"));
    }

    [Test]
    public void Properties_InitialValues_AreDefaults()
    {
        var options = new MiniaudioEngineOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Context, Is.Null);
            Assert.That(options.ResourceManager, Is.Null);
            Assert.That(options.PlaybackDeviceId, Is.Null);
            Assert.That(options.SampleRate, Is.Null);
            Assert.That(options.Channels, Is.Null);
            Assert.That(options.PeriodSizeInFrames, Is.Null);
            Assert.That(options.PeriodSizeInMilliseconds, Is.Null);
            Assert.That(options.NoAutoStart, Is.False);
            Assert.That(options.NoDevice, Is.False);
        });
    }
}
