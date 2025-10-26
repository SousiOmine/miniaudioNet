using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Miniaudio.Net;

const double Frequency = 441.0;
const int Channels = 1;
const double DurationSeconds = 3.0;
const float EngineVolume = 0.8f;
const float SoundVolume = 0.9f;
const double WaveAmplitude = 0.6;
const int FallbackSampleRate = 48_000;

try
{
    var options = SampleOptions.Parse(args);
    using var context = MiniaudioContext.Create(options.PreferredBackends);
    IReadOnlyList<MiniaudioDeviceInfo>? cachedPlaybackDevices = null;

    IReadOnlyList<MiniaudioDeviceInfo> LoadPlaybackDevices()
        => cachedPlaybackDevices ??= context.EnumerateDevices(MiniaudioDeviceKind.Playback);

    if (options.ListDevices)
    {
        var devices = LoadPlaybackDevices();
        PrintDevices(devices);
        return;
    }

    var playbackDevices = LoadPlaybackDevices();
    var selectedDeviceId = options.ResolvePlaybackDeviceId(playbackDevices);
    using var engine = CreateEngine(context, selectedDeviceId, options.SampleRate);
    engine.Volume = EngineVolume;

    var playbackSampleRate = GetEffectiveSampleRate(engine.SampleRate, options.SampleRate);
    var pcmFrames = GenerateSineWaveFrames(Frequency, playbackSampleRate, Channels, DurationSeconds, WaveAmplitude);

    using var sound = engine.CreateSoundFromPcmFrames(pcmFrames, Channels, (uint)playbackSampleRate);
    sound.Volume = SoundVolume;

    AnnounceDevice(playbackDevices, selectedDeviceId);
    Console.WriteLine($"Playing {Frequency} Hz sine wave for {DurationSeconds:0.#} seconds (sample rate: {playbackSampleRate} Hz)...");
    sound.Start();

    await WaitForSoundToFinishAsync(sound);
    Console.WriteLine("Done.");
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error:");
    Console.Error.WriteLine(ex);
    Environment.ExitCode = 1;
}

static void AnnounceDevice(IReadOnlyList<MiniaudioDeviceInfo> devices, string? deviceId)
{
    if (string.IsNullOrWhiteSpace(deviceId))
    {
        Console.WriteLine("Using system default playback device.");
        return;
    }

    var device = devices.FirstOrDefault(d => string.Equals(d.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
    if (device is null)
    {
        Console.WriteLine("Using explicit playback device (not found in current enumeration).");
        return;
    }

    var label = device.IsDefault ? "default" : "explicit";
    Console.WriteLine($"Using playback device '{device.Name}' ({label}).");
}

static void PrintDevices(IReadOnlyList<MiniaudioDeviceInfo> devices)
{
    if (devices.Count == 0)
    {
        Console.WriteLine("No playback devices were reported by the active miniaudio backends.");
        return;
    }

    Console.WriteLine("Available playback devices (use --device-index N or --device-id <HEX> to select):\n");
    for (var i = 0; i < devices.Count; i++)
    {
        var device = devices[i];
        var defaultLabel = device.IsDefault ? " [default]" : string.Empty;
        Console.WriteLine($"[{i}] {device.Name}{defaultLabel}");
        PrintWrapped($"DeviceId: {device.DeviceId}", indent: 6, width: 90);
        Console.WriteLine();
    }
}

static void PrintWrapped(string text, int indent, int width)
{
    var indentString = new string(' ', indent);
    var contentWidth = Math.Max(1, width - indent);
    var remaining = text;
    while (remaining.Length > contentWidth)
    {
        Console.WriteLine(indentString + remaining[..contentWidth]);
        remaining = remaining[contentWidth..];
    }

    if (remaining.Length > 0)
    {
        Console.WriteLine(indentString + remaining);
    }
}

static async Task WaitForSoundToFinishAsync(MiniaudioSound sound)
{
    while (sound.State is SoundState.Playing or SoundState.Starting)
    {
        await Task.Delay(25);
    }
}

static MiniaudioEngine CreateEngine(MiniaudioContext context, string? playbackDeviceId, int? sampleRateOverride)
{
    if (sampleRateOverride.HasValue && sampleRateOverride.Value <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(sampleRateOverride), "Sample rate must be greater than 0.");
    }

    var engineOptions = new MiniaudioEngineOptions
    {
        Context = context,
        PlaybackDeviceId = playbackDeviceId,
        SampleRate = sampleRateOverride.HasValue ? (uint)sampleRateOverride.Value : null,
    };

    try
    {
        return MiniaudioEngine.Create(engineOptions);
    }
    catch (InvalidOperationException ex) when (!sampleRateOverride.HasValue)
    {
        Console.Error.WriteLine("Warning: Failed to initialize engine with the device default. Retrying with 48 kHz...");

        var fallbackOptions = new MiniaudioEngineOptions
        {
            Context = context,
            PlaybackDeviceId = playbackDeviceId,
            SampleRate = FallbackSampleRate,
        };

        try
        {
            return MiniaudioEngine.Create(fallbackOptions);
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Failed to initialize miniaudio engine. Verify that native binaries are available and the selected playback device is usable.",
                ex);
        }
    }
}

static int GetEffectiveSampleRate(uint engineSampleRate, int? overrideSampleRate)
{
    if (overrideSampleRate.HasValue)
    {
        return overrideSampleRate.Value;
    }

    if (engineSampleRate > 0)
    {
        return (int)engineSampleRate;
    }

    return FallbackSampleRate;
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

internal sealed record SampleOptions(
    bool ListDevices,
    string? DeviceId,
    int? DeviceIndex,
    IReadOnlyList<MiniaudioBackend> PreferredBackends,
    int? SampleRate)
{
    public static SampleOptions Parse(string[] args)
    {
        var listDevices = false;
        string? deviceId = null;
        int? deviceIndex = null;
        var preferredBackends = new List<MiniaudioBackend>();
        int? sampleRate = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--list":
                case "--list-devices":
                    listDevices = true;
                    break;
                case "--device-id":
                    deviceId = RequireValue(args, ref i, "--device-id");
                    break;
                case "--device-index":
                    var indexValue = RequireValue(args, ref i, "--device-index");
                    if (!int.TryParse(indexValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedIndex) || parsedIndex < 0)
                    {
                        throw new ArgumentException("--device-index expects a non-negative integer.");
                    }

                    deviceIndex = parsedIndex;
                    break;
                case "--backend":
                    var backendName = RequireValue(args, ref i, "--backend");
                    if (!Enum.TryParse<MiniaudioBackend>(backendName, ignoreCase: true, out var backend))
                    {
                        throw new ArgumentException($"Unknown backend '{backendName}'.");
                    }

                    preferredBackends.Add(backend);
                    break;
                case "--sample-rate":
                    var sampleRateValue = RequireValue(args, ref i, "--sample-rate");
                    if (!int.TryParse(sampleRateValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSampleRate) || parsedSampleRate <= 0)
                    {
                        throw new ArgumentException("--sample-rate expects a positive integer.");
                    }

                    sampleRate = parsedSampleRate;
                    break;
                case "--help":
                case "-h":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{args[i]}'. Use --help to see the supported switches.");
            }
        }

        return new SampleOptions(listDevices, deviceId, deviceIndex, preferredBackends, sampleRate);
    }

    public string? ResolvePlaybackDeviceId(IReadOnlyList<MiniaudioDeviceInfo> devices)
    {
        if (!string.IsNullOrWhiteSpace(DeviceId))
        {
            return DeviceId;
        }

        if (DeviceIndex.HasValue)
        {
            if (DeviceIndex.Value < 0 || DeviceIndex.Value >= devices.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(DeviceIndex), $"Device index {DeviceIndex.Value} is out of range. Listed devices: {devices.Count}.");
            }

            return devices[DeviceIndex.Value].DeviceId;
        }

        return null;
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
            "Usage: dotnet run --project samples/MiniaudioNet.Sample.Sine441 -- [options]\n" +
            "Options:\n" +
            "  --list, --list-devices          Lists playback devices and exits.\n" +
            "  --device-index <N>              Selects the Nth playback device reported by --list.\n" +
            "  --device-id <HEX>               Selects a playback device by ID (see --list for values).\n" +
            "  --backend <Name>                Adds a preferred backend (e.g., Wasapi, CoreAudio). Multiple allowed.\n" +
            "  --sample-rate <Hz>              Forces a playback sample rate (falls back to device default otherwise).\n" +
            "  --help                          Prints this help.");
    }
}
