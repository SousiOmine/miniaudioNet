using NUnit.Framework;
using Miniaudio.Net;
using System;

namespace Miniaudio.Net.Tests;

[TestFixture]
public class MiniaudioResourceManagerOptionsTests
{
    [Test]
    public void Validate_DefaultOptions_DoesNotThrow()
    {
        var options = new MiniaudioResourceManagerOptions();

        Assert.DoesNotThrow(() => options.Validate());
    }

    [Test]
    public void Validate_ZeroDecodedChannels_ThrowsArgumentOutOfRangeException()
    {
        var options = new MiniaudioResourceManagerOptions
        {
            DecodedChannels = 0,
        };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());

        Assert.That(ex?.Message, Does.Contain("Channel count"));
    }

    [Test]
    public void Validate_ZeroDecodedSampleRate_ThrowsArgumentOutOfRangeException()
    {
        var options = new MiniaudioResourceManagerOptions
        {
            DecodedSampleRate = 0,
        };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());

        Assert.That(ex?.Message, Does.Contain("Sample rate"));
    }

    [Test]
    public void Validate_ZeroJobThreadCount_ThrowsArgumentOutOfRangeException()
    {
        var options = new MiniaudioResourceManagerOptions
        {
            JobThreadCount = 0,
        };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());

        Assert.That(ex?.Message, Does.Contain("Job thread count"));
    }

    [Test]
    public void Validate_ValidOptions_DoesNotThrow()
    {
        var options = new MiniaudioResourceManagerOptions
        {
            DecodedChannels = 2,
            DecodedSampleRate = 48000,
            JobThreadCount = 4,
            DecodedFormat = MiniaudioSampleFormat.F32,
            Flags = ResourceManagerFlags.NonBlocking,
        };

        Assert.DoesNotThrow(() => options.Validate());
    }

    [Test]
    public void HasOverrides_DefaultOptions_ReturnsFalse()
    {
        var options = new MiniaudioResourceManagerOptions();

        Assert.That(options.HasOverrides, Is.False);
    }

    [Test]
    public void HasOverrides_WithDecodedFormat_ReturnsTrue()
    {
        var options = new MiniaudioResourceManagerOptions
        {
            DecodedFormat = MiniaudioSampleFormat.S16,
        };

        Assert.That(options.HasOverrides, Is.True);
    }

    [Test]
    public void HasOverrides_WithDecodedChannels_ReturnsTrue()
    {
        var options = new MiniaudioResourceManagerOptions
        {
            DecodedChannels = 2,
        };

        Assert.That(options.HasOverrides, Is.True);
    }

    [Test]
    public void HasOverrides_WithDecodedSampleRate_ReturnsTrue()
    {
        var options = new MiniaudioResourceManagerOptions
        {
            DecodedSampleRate = 44100,
        };

        Assert.That(options.HasOverrides, Is.True);
    }

    [Test]
    public void HasOverrides_WithJobThreadCount_ReturnsTrue()
    {
        var options = new MiniaudioResourceManagerOptions
        {
            JobThreadCount = 2,
        };

        Assert.That(options.HasOverrides, Is.True);
    }

    [Test]
    public void HasOverrides_WithFlags_ReturnsTrue()
    {
        var options = new MiniaudioResourceManagerOptions
        {
            Flags = ResourceManagerFlags.NonBlocking,
        };

        Assert.That(options.HasOverrides, Is.True);
    }

    [Test]
    public void Snapshot_CopiesAllProperties()
    {
        var options = new MiniaudioResourceManagerOptions
        {
            DecodedFormat = MiniaudioSampleFormat.S24,
            DecodedChannels = 4,
            DecodedSampleRate = 96000,
            JobThreadCount = 8,
            Flags = ResourceManagerFlags.NonBlocking,
        };

        var snapshot = options.Snapshot();

        Assert.That(snapshot, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(snapshot.DecodedFormat, Is.EqualTo(MiniaudioSampleFormat.S24));
            Assert.That(snapshot.DecodedChannels, Is.EqualTo(4u));
            Assert.That(snapshot.DecodedSampleRate, Is.EqualTo(96000u));
            Assert.That(snapshot.JobThreadCount, Is.EqualTo(8u));
            Assert.That(snapshot.Flags, Is.EqualTo(ResourceManagerFlags.NonBlocking));
        });
    }

    [Test]
    public void Snapshot_CreatesIndependentCopy()
    {
        var options = new MiniaudioResourceManagerOptions
        {
            DecodedChannels = 2,
        };

        var snapshot = options.Snapshot();

        Assert.That(snapshot, Is.Not.SameAs(options));
    }

    [Test]
    public void Properties_DefaultValues()
    {
        var options = new MiniaudioResourceManagerOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.DecodedFormat, Is.EqualTo(MiniaudioSampleFormat.Unknown));
            Assert.That(options.DecodedChannels, Is.Null);
            Assert.That(options.DecodedSampleRate, Is.Null);
            Assert.That(options.JobThreadCount, Is.Null);
            Assert.That(options.Flags, Is.EqualTo(ResourceManagerFlags.None));
        });
    }

    [Test]
    public void ToNativeConfig_MapsPropertiesCorrectly()
    {
        var options = new MiniaudioResourceManagerOptions
        {
            DecodedFormat = MiniaudioSampleFormat.F32,
            DecodedChannels = 2,
            DecodedSampleRate = 44100,
            JobThreadCount = 4,
            Flags = ResourceManagerFlags.NonBlocking,
        };

        var config = options.ToNativeConfig();

        Assert.Multiple(() =>
        {
            Assert.That(config.DecodedFormat, Is.EqualTo((uint)MiniaudioSampleFormat.F32));
            Assert.That(config.DecodedChannels, Is.EqualTo(2u));
            Assert.That(config.DecodedSampleRate, Is.EqualTo(44100u));
            Assert.That(config.JobThreadCount, Is.EqualTo(4u));
            Assert.That(config.Flags, Is.EqualTo((uint)ResourceManagerFlags.NonBlocking));
        });
    }

    [Test]
    public void ToNativeConfig_NullValues_MapToZero()
    {
        var options = new MiniaudioResourceManagerOptions();

        var config = options.ToNativeConfig();

        Assert.Multiple(() =>
        {
            Assert.That(config.DecodedChannels, Is.EqualTo(0u));
            Assert.That(config.DecodedSampleRate, Is.EqualTo(0u));
            Assert.That(config.JobThreadCount, Is.EqualTo(0u));
        });
    }
}
