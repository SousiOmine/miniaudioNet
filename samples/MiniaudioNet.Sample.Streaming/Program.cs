using System;
using System.Threading;
using System.Threading.Tasks;
using Miniaudio.Net;

const uint channels = 2;
const uint sampleRate = 48000;
const uint framesPerChunk = sampleRate / 10; // 100 ms worth of audio per push.
const uint defaultBufferCapacityInFrames = sampleRate * 2; // 2 seconds of audio.

var frequency = args.Length > 0 && double.TryParse(args[0], out var parsedFrequency) && parsedFrequency > 0
    ? parsedFrequency
    : 440d;

var durationSeconds = args.Length > 1 && int.TryParse(args[1], out var parsedDuration) && parsedDuration > 0
    ? parsedDuration
    : 5;

Console.WriteLine($"Generating a {frequency:0.##} Hz sine wave for {durationSeconds} seconds via streaming...");

using var engine = MiniaudioEngine.Create();
engine.Volume = 0.8f;

using var streamingSound = engine.CreateStreamingSound(channels, sampleRate, defaultBufferCapacityInFrames, SoundInitFlags.Async);
streamingSound.Volume = 0.6f;

var buffer = new float[framesPerChunk * channels];
var totalFramesToGenerate = (ulong)durationSeconds * sampleRate;
var totalGeneratedFrames = 0UL;
var phase = 0d;
var phaseIncrement = 2 * Math.PI * frequency / sampleRate;

streamingSound.Start();

while (totalGeneratedFrames < totalFramesToGenerate)
{
    phase = GenerateStereoSine(buffer, framesPerChunk, channels, phase, phaseIncrement);

    var framesRemaining = (ulong)framesPerChunk;
    var offsetFrames = 0UL;

    while (framesRemaining > 0)
    {
        var framesToSend = (int)Math.Min(framesRemaining, (ulong)framesPerChunk - offsetFrames);
        var span = buffer.AsSpan((int)(offsetFrames * channels), framesToSend * (int)channels);
        var written = streamingSound.AppendPcmFrames(span);

        if (written == 0)
        {
            Thread.Sleep(5);
            continue;
        }

        framesRemaining -= written;
        offsetFrames += written;
    }

    totalGeneratedFrames += framesPerChunk;
    Console.Write($"\rQueued {(double)totalGeneratedFrames / sampleRate:0.00}s / {durationSeconds}s");
}

Console.WriteLine("\nFinishing stream...");
streamingSound.SignalEndOfStream();

WaitForSoundToFinishAsync(streamingSound).GetAwaiter().GetResult();
Console.WriteLine("Playback complete.");

static double GenerateStereoSine(float[] destination, uint frameCount, uint channelCount, double phase, double phaseIncrement)
{
    for (var frame = 0; frame < frameCount; frame++)
    {
        var sample = (float)Math.Sin(phase);
        for (var c = 0; c < channelCount; c++)
        {
            destination[frame * channelCount + c] = sample;
        }

        phase += phaseIncrement;
        if (phase >= Math.PI * 2)
        {
            phase -= Math.PI * 2;
        }
    }

    return phase;
}

static async Task WaitForSoundToFinishAsync(MiniaudioSound sound)
{
    while (sound.State is SoundState.Playing or SoundState.Starting)
    {
        await Task.Delay(50);
    }
}
