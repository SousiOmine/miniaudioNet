using NUnit.Framework;
using Miniaudio.Net;
using System;

namespace Miniaudio.Net.Tests.Integration;

/// <summary>
/// MiniaudioEngineのインテグレーションテスト。
/// これらのテストはネイティブライブラリが必要です。
/// </summary>
[TestFixture]
[Category("Integration")]
public class MiniaudioEngineIntegrationTests
{
    [Test]
    public void Create_NoDeviceMode_ReturnsValidEngine()
    {
        var options = new MiniaudioEngineOptions
        {
            NoDevice = true,
            SampleRate = 48000,
            Channels = 2,
        };

        using var engine = MiniaudioEngine.Create(options);

        Assert.That(engine, Is.Not.Null);
        Assert.That(engine.SampleRate, Is.EqualTo(48000u));
        Assert.That(engine.Channels, Is.EqualTo(2u));
    }

    [Test]
    public void Create_WithContext_ReturnsValidEngine()
    {
        using var context = MiniaudioContext.Create();
        var options = new MiniaudioEngineOptions
        {
            Context = context,
            NoDevice = true,
            SampleRate = 44100,
            Channels = 1,
        };

        using var engine = MiniaudioEngine.Create(options);

        Assert.That(engine, Is.Not.Null);
        Assert.That(engine.SampleRate, Is.EqualTo(44100u));
    }

    [Test]
    public void Volume_GetSet_WorksCorrectly()
    {
        var options = new MiniaudioEngineOptions
        {
            NoDevice = true,
            SampleRate = 48000,
            Channels = 2,
        };

        using var engine = MiniaudioEngine.Create(options);
        engine.Volume = 0.5f;

        Assert.That(engine.Volume, Is.EqualTo(0.5f).Within(0.01f));
    }

    [Test]
    public void ListenerCount_ReturnsNonNegativeValue()
    {
        var options = new MiniaudioEngineOptions
        {
            NoDevice = true,
            SampleRate = 48000,
            Channels = 2,
        };

        using var engine = MiniaudioEngine.Create(options);

        Assert.That(engine.ListenerCount, Is.GreaterThanOrEqualTo(0u));
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var options = new MiniaudioEngineOptions
        {
            NoDevice = true,
            SampleRate = 48000,
            Channels = 2,
        };

        var engine = MiniaudioEngine.Create(options);

        Assert.DoesNotThrow(() =>
        {
            engine.Dispose();
            engine.Dispose();
        });
    }

    [Test]
    public void CreateSoundFromPcmFrames_ValidData_ReturnsSound()
    {
        var options = new MiniaudioEngineOptions
        {
            NoDevice = true,
            SampleRate = 48000,
            Channels = 2,
        };

        using var engine = MiniaudioEngine.Create(options);

        var pcmData = GenerateSineWave(440, 48000, 1, 0.1);
        using var sound = engine.CreateSoundFromPcmFrames(pcmData, 1, 48000);

        Assert.That(sound, Is.Not.Null);
    }

    [Test]
    public void GetAbsoluteTimeInFrames_ReturnsValue()
    {
        var options = new MiniaudioEngineOptions
        {
            NoDevice = true,
            SampleRate = 48000,
            Channels = 2,
        };

        using var engine = MiniaudioEngine.Create(options);

        var frames = engine.GetAbsoluteTimeInFrames(TimeSpan.Zero);

        Assert.That(frames, Is.GreaterThanOrEqualTo(0UL));
    }

    [Test]
    public void SetTimeInFrames_DoesNotThrow()
    {
        var options = new MiniaudioEngineOptions
        {
            NoDevice = true,
            SampleRate = 48000,
            Channels = 2,
        };

        using var engine = MiniaudioEngine.Create(options);

        Assert.DoesNotThrow(() => engine.SetTimeInFrames(100));
    }

    [Test]
    public void SetTime_DoesNotThrow()
    {
        var options = new MiniaudioEngineOptions
        {
            NoDevice = true,
            SampleRate = 48000,
            Channels = 2,
        };

        using var engine = MiniaudioEngine.Create(options);

        Assert.DoesNotThrow(() => engine.SetTime(TimeSpan.FromSeconds(1)));
    }

    private static float[] GenerateSineWave(double frequency, int sampleRate, int channels, double durationSeconds)
    {
        var totalFrames = (int)(sampleRate * durationSeconds);
        var totalSamples = totalFrames * channels;
        var buffer = new float[totalSamples];
        var angularStep = 2 * Math.PI * frequency / sampleRate;

        for (var frame = 0; frame < totalFrames; frame++)
        {
            var sampleValue = (float)(Math.Sin(angularStep * frame) * 0.5);
            var start = frame * channels;
            for (var channel = 0; channel < channels; channel++)
            {
                buffer[start + channel] = sampleValue;
            }
        }

        return buffer;
    }
}
