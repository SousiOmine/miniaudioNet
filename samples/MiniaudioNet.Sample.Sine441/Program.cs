using System;
using System.Threading.Tasks;
using Miniaudio.Net;

const double Frequency = 441.0;
const int SampleRate = 44_100;
const int Channels = 1;
const double DurationSeconds = 3.0;
const float EngineVolume = 0.8f;
const float SoundVolume = 0.9f;
const double WaveAmplitude = 0.6;

var pcmFrames = GenerateSineWaveFrames(Frequency, SampleRate, Channels, DurationSeconds, WaveAmplitude);

using var engine = MiniaudioEngine.Create();
engine.Volume = EngineVolume;

using var sound = engine.CreateSoundFromPcmFrames(pcmFrames, Channels, SampleRate);
sound.Volume = SoundVolume;

Console.WriteLine($"Playing {Frequency} Hz sine wave for {DurationSeconds:0.#} seconds (sample rate: {SampleRate} Hz)...");
sound.Start();

await WaitForSoundToFinishAsync(sound);
Console.WriteLine("Done.");

static async Task WaitForSoundToFinishAsync(MiniaudioSound sound)
{
    while (sound.State is SoundState.Playing or SoundState.Starting)
    {
        await Task.Delay(25);
    }
}

static float[] GenerateSineWaveFrames(double frequency, int sampleRate, int channels, double durationSeconds, double amplitude)
{
    var totalFrames = (int)(sampleRate * durationSeconds);
    var totalSamples = totalFrames * channels;
    var buffer = new float[totalSamples];
    var angularStep = 2 * Math.PI * frequency / sampleRate;

    for (var frame = 0; frame < totalFrames; frame++)
    {
        var sampleValue = (float)(Math.Sin(angularStep * frame) * amplitude);
        var start = frame * channels;
        buffer[start] = sampleValue;
        for (var channel = 1; channel < channels; channel++)
        {
            buffer[start + channel] = sampleValue;
        }
    }

    return buffer;
}
