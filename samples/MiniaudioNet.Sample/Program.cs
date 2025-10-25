using System;
using System.Threading.Tasks;
using Miniaudio.Net;

var audioFile = args.Length > 0 ? args[0] : null;
if (string.IsNullOrWhiteSpace(audioFile))
{
    Console.WriteLine("Usage: dotnet run --project samples/MiniaudioNet.Sample -- <audio file path>");
    return;
}

using var engine = MiniaudioEngine.Create();
engine.Volume = 0.75f;

using var sound = engine.CreateSound(audioFile, SoundInitFlags.Decode | SoundInitFlags.Async);
sound.Volume = 0.9f;

Console.WriteLine($"Playing '{audioFile}' at sample rate {sound.SampleRate} Hz...");
sound.Start();

await WaitForSoundToFinishAsync(sound);
Console.WriteLine("Done.");

static async Task WaitForSoundToFinishAsync(MiniaudioSound sound)
{
    while (sound.State is SoundState.Playing or SoundState.Starting)
    {
        await Task.Delay(50);
    }
}
