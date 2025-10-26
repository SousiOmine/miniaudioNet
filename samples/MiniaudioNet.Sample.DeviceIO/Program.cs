using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Miniaudio.Net;

var options = SampleOptions.Parse(args);
using var context = MiniaudioContext.Create(options.PreferredBackends);
using var resourceManager = MiniaudioResourceManager.Create(new MiniaudioResourceManagerOptions
{
    DecodedSampleRate = 48_000,
    DecodedChannels = 2,
    JobThreadCount = 2,
});

var engineOptions = new MiniaudioEngineOptions
{
    Context = context,
    ResourceManager = resourceManager,
    PlaybackDeviceId = options.PlaybackDeviceId,
    SampleRate = 48_000,
};

using var engine = MiniaudioEngine.Create(engineOptions);
using var sound = engine.CreateSound(options.AudioFile, SoundInitFlags.Stream | SoundInitFlags.Async | SoundInitFlags.Looping);
sound.Volume = 0.75f;
sound.Looping = true;
sound.ApplyFade(0f, sound.Volume, TimeSpan.FromSeconds(1.5));
sound.ScheduleStart(TimeSpan.FromSeconds(1));
sound.Ended += (_, __) => Console.WriteLine("[sound] Playback completed.");

MiniaudioCaptureDevice? capture = null;
DateTimeOffset lastMeterLog = DateTimeOffset.MinValue;

if (options.EnableMonitor)
{
    var captureOptions = new MiniaudioCaptureDeviceOptions
    {
        Context = context,
        CaptureDeviceId = options.CaptureDeviceId,
        SampleRate = options.MonitorSampleRate,
        Channels = 1,
    };

    capture = MiniaudioCaptureDevice.Create(captureOptions);
    capture.PcmCaptured += (_, e) =>
    {
        var now = DateTimeOffset.UtcNow;
        if (now - lastMeterLog < TimeSpan.FromMilliseconds(250))
        {
            return;
        }

        lastMeterLog = now;
        var rms = CalculateRms(e.Samples);
        var db = 20 * Math.Log10(Math.Max(rms, 1e-6));
        Console.WriteLine($"[monitor] RMS: {db:0.0} dBFS");
    };

    capture.Start();
}

Console.WriteLine("Streaming '{0}'. Press Enter to schedule a 2s fade-out.", options.AudioFile);
sound.Start();
Console.ReadLine();

sound.ScheduleStop(TimeSpan.Zero, TimeSpan.FromSeconds(2));
await WaitForSoundAsync(sound);

if (capture is not null)
{
    capture.Stop();
    capture.Dispose();
}

Console.WriteLine("Done.");

static async Task WaitForSoundAsync(MiniaudioSound sound)
{
    while (sound.State is SoundState.Playing or SoundState.Starting)
    {
        await Task.Delay(50);
    }
}

static double CalculateRms(float[] samples)
{
    if (samples.Length == 0)
    {
        return 0;
    }

    double sum = 0;
    for (var i = 0; i < samples.Length; i++)
    {
        sum += samples[i] * samples[i];
    }

    return Math.Sqrt(sum / samples.Length);
}

internal sealed record SampleOptions(
    string AudioFile,
    string? PlaybackDeviceId,
    string? CaptureDeviceId,
    bool EnableMonitor,
    uint MonitorSampleRate,
    IReadOnlyList<MiniaudioBackend> PreferredBackends)
{
    public static SampleOptions Parse(string[] args)
    {
        string? file = null;
        string? playbackDeviceId = null;
        string? captureDeviceId = null;
        var preferredBackends = new List<MiniaudioBackend>();
        var monitor = false;
        uint monitorRate = 16_000;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--file":
                    file = RequireValue(args, ref i, "--file");
                    break;
                case "--device-id":
                    playbackDeviceId = RequireValue(args, ref i, "--device-id");
                    break;
                case "--capture-device-id":
                    captureDeviceId = RequireValue(args, ref i, "--capture-device-id");
                    break;
                case "--backend":
                    var backendName = RequireValue(args, ref i, "--backend");
                    if (!Enum.TryParse<MiniaudioBackend>(backendName, true, out var backend))
                    {
                        throw new ArgumentException($"Unknown backend '{backendName}'.");
                    }

                    preferredBackends.Add(backend);
                    break;
                case "--monitor":
                    monitor = true;
                    break;
                case "--monitor-rate":
                    var rateValue = RequireValue(args, ref i, "--monitor-rate");
                    if (!uint.TryParse(rateValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out monitorRate) || monitorRate == 0)
                    {
                        throw new ArgumentException("--monitor-rate expects a positive integer");
                    }

                    break;
                case "--help":
                case "-h":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
                default:
                    file ??= args[i];
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(file))
        {
            PrintUsage();
            throw new ArgumentException("You must specify an audio file path.");
        }

        return new SampleOptions(file, playbackDeviceId, captureDeviceId, monitor, monitorRate, preferredBackends);
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Option '{option}' expects a value.");
        }

        index += 1;
        return args[index];
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            "Usage: dotnet run --project samples/MiniaudioNet.Sample.DeviceIO -- [options] --file <audio>\n" +
            "Options:\n" +
            "  --file <path>               Required audio file to stream.\n" +
            "  --device-id <HEX>           Playback device (see other samples for listing).\n" +
            "  --capture-device-id <HEX>   Optional capture device for monitoring.\n" +
            "  --backend <Name>            Adds a preferred backend (Wasapi, CoreAudio, etc.).\n" +
            "  --monitor                   Enables microphone level monitoring.\n" +
            "  --monitor-rate <Hz>         Sample rate for monitoring (default 16000).\n" +
            "  --help                      Prints this help.");
    }
}
