using NUnit.Framework;
using Miniaudio.Net;
using System;

namespace Miniaudio.Net.Tests.Integration;

/// <summary>
/// MiniaudioSoundのインテグレーションテスト。
/// これらのテストはネイティブライブラリが必要です。
/// </summary>
[TestFixture]
[Category("Integration")]
public class MiniaudioSoundIntegrationTests
{
    private MiniaudioEngine _engine = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new MiniaudioEngineOptions
        {
            NoDevice = true,
            SampleRate = 48000,
            Channels = 2,
        };
        _engine = MiniaudioEngine.Create(options);
    }

    [TearDown]
    public void TearDown()
    {
        _engine?.Dispose();
    }

    [Test]
    public void CreateSoundFromPcmFrames_ReturnsValidSound()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);

        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        Assert.That(sound, Is.Not.Null);
    }

    [Test]
    public void Volume_GetSet_WorksCorrectly()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        sound.Volume = 0.75f;

        Assert.That(sound.Volume, Is.EqualTo(0.75f).Within(0.01f));
    }

    [Test]
    public void Pitch_GetSet_WorksCorrectly()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        sound.Pitch = 1.5f;

        Assert.That(sound.Pitch, Is.EqualTo(1.5f).Within(0.01f));
    }

    [Test]
    public void Looping_GetSet_WorksCorrectly()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        sound.Looping = true;

        Assert.That(sound.Looping, Is.True);
    }

    [Test]
    public void Pan_GetSet_WorksCorrectly()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        sound.Pan = -0.5f;

        Assert.That(sound.Pan, Is.EqualTo(-0.5f).Within(0.01f));
    }

    [Test]
    public void State_InitialState_IsStopped()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        Assert.That(sound.State, Is.EqualTo(SoundState.Stopped));
    }

    [Test]
    public void Start_ChangesState()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        sound.Start();

        Assert.That(sound.State, Is.EqualTo(SoundState.Playing).Or.EqualTo(SoundState.Starting));
    }

    [Test]
    public void Stop_AfterStart_ChangesState()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        sound.Start();
        sound.Stop();

        Assert.That(sound.State, Is.EqualTo(SoundState.Stopped).Or.EqualTo(SoundState.Stopping));
    }

    [Test]
    public void SeekToFrame_DoesNotThrow()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        Assert.DoesNotThrow(() => sound.SeekToFrame(100));
    }

    [Test]
    public void SeekToStart_DoesNotThrow()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        Assert.DoesNotThrow(() => sound.SeekToStart());
    }

    [Test]
    public void LengthInFrames_ReturnsValidValue()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        var length = sound.LengthInFrames;

        Assert.That(length, Is.GreaterThan(0UL));
    }

    [Test]
    public void CursorInFrames_ReturnsValue()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        var position = sound.CursorInFrames;

        Assert.That(position, Is.GreaterThanOrEqualTo(0UL));
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        Assert.DoesNotThrow(() =>
        {
            sound.Dispose();
            sound.Dispose();
        });
    }

    [Test]
    public void ApplyFade_WithValidParameters_DoesNotThrow()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        Assert.DoesNotThrow(() => sound.ApplyFade(0f, 1f, 1000));
    }

    [Test]
    public void ScheduleStart_DoesNotThrow()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);

        Assert.DoesNotThrow(() => sound.ScheduleStart(48000));
    }

    [Test]
    public void ScheduleStop_DoesNotThrow()
    {
        var pcmData = GenerateSineWave(440, 48000, 2, 1.0);
        using var sound = _engine.CreateSoundFromPcmFrames(pcmData, 2, 48000);
        sound.Start();

        Assert.DoesNotThrow(() => sound.ScheduleStop(96000));
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
