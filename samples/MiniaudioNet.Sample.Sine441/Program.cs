using System;
using System.IO;
using System.Threading.Tasks;
using Miniaudio.Net;

const double Frequency = 441.0;
const int SampleRate = 44_100;
const int Channels = 1;
const double DurationSeconds = 3.0;
const float EngineVolume = 0.8f;
const float SoundVolume = 0.9f;

var tempFile = Path.Combine(Path.GetTempPath(), "miniaudionet_sine441.wav");
GenerateSineWaveWav(tempFile, Frequency, SampleRate, Channels, DurationSeconds);

using var engine = MiniaudioEngine.Create();
engine.Volume = EngineVolume;

using var sound = engine.CreateSound(tempFile, SoundInitFlags.Decode | SoundInitFlags.Async);
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

static void GenerateSineWaveWav(string path, double frequency, int sampleRate, int channels, double durationSeconds)
{
    var totalSamples = (int)(sampleRate * durationSeconds);
    var bytesPerSample = sizeof(short);
    var blockAlign = channels * bytesPerSample;
    var byteRate = sampleRate * blockAlign;
    var dataChunkSize = totalSamples * blockAlign;
    var riffChunkSize = 36 + dataChunkSize;

    using var stream = File.Create(path);
    using var writer = new BinaryWriter(stream);

    // RIFF header
    writer.Write("RIFF"u8);
    writer.Write(riffChunkSize);
    writer.Write("WAVE"u8);

    // fmt chunk
    writer.Write("fmt "u8);
    writer.Write(16); // PCM chunk size
    writer.Write((short)1); // AudioFormat = PCM
    writer.Write((short)channels);
    writer.Write(sampleRate);
    writer.Write(byteRate);
    writer.Write((short)blockAlign);
    writer.Write((short)(bytesPerSample * 8));

    // data chunk
    writer.Write("data"u8);
    writer.Write(dataChunkSize);

    var amplitude = short.MaxValue * 0.6;
    for (var i = 0; i < totalSamples; i++)
    {
        var sampleValue = (short)(Math.Sin(2 * Math.PI * frequency * i / sampleRate) * amplitude);
        for (var channel = 0; channel < channels; channel++)
        {
            writer.Write(sampleValue);
        }
    }
}
