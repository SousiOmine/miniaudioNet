using NUnit.Framework;
using Miniaudio.Net;
using System;

namespace Miniaudio.Net.Tests.Integration;

/// <summary>
/// MiniaudioResourceManagerのインテグレーションテスト。
/// これらのテストはネイティブライブラリが必要です。
/// </summary>
[TestFixture]
[Category("Integration")]
public class MiniaudioResourceManagerIntegrationTests
{
    [Test]
    public void Create_DefaultOptions_ReturnsValidManager()
    {
        using var manager = MiniaudioResourceManager.Create();

        Assert.That(manager, Is.Not.Null);
    }

    [Test]
    public void Create_WithOptions_ReturnsValidManager()
    {
        var options = new MiniaudioResourceManagerOptions
        {
            DecodedFormat = MiniaudioSampleFormat.F32,
            DecodedChannels = 2,
            DecodedSampleRate = 48000,
            JobThreadCount = 2,
        };

        using var manager = MiniaudioResourceManager.Create(options);

        Assert.That(manager, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(manager.Options.DecodedFormat, Is.EqualTo(MiniaudioSampleFormat.F32));
            Assert.That(manager.Options.DecodedChannels, Is.EqualTo(2u));
            Assert.That(manager.Options.DecodedSampleRate, Is.EqualTo(48000u));
            Assert.That(manager.Options.JobThreadCount, Is.EqualTo(2u));
        });
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var manager = MiniaudioResourceManager.Create();

        Assert.DoesNotThrow(() =>
        {
            manager.Dispose();
            manager.Dispose();
        });
    }



    [Test]
    public void Options_ReturnsSnapshot()
    {
        var options = new MiniaudioResourceManagerOptions
        {
            DecodedChannels = 4,
        };

        using var manager = MiniaudioResourceManager.Create(options);
        var retrievedOptions = manager.Options;

        Assert.That(retrievedOptions.DecodedChannels, Is.EqualTo(4u));
    }
}
