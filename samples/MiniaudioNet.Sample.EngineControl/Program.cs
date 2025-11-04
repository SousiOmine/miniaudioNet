using System.Globalization;
using System.Numerics;
using Miniaudio.Net;

if (!EngineControlOptions.TryParse(args, out var options, out var error))
{
    Console.WriteLine(error);
    EngineControlOptions.PrintUsage();
    return;
}

if (options.ShowHelp)
{
    EngineControlOptions.PrintUsage();
    return;
}

if (string.IsNullOrWhiteSpace(options.AudioPath))
{
    Console.WriteLine("音声ファイルを指定してください (--file <path>)。");
    EngineControlOptions.PrintUsage();
    return;
}

if (!File.Exists(options.AudioPath))
{
    Console.WriteLine($"指定されたファイルが見つかりません: {options.AudioPath}");
    return;
}

using var engine = options.ManualStart
    ? MiniaudioEngine.Create(new MiniaudioEngineOptions { NoAutoStart = true })
    : MiniaudioEngine.Create();

engine.SetTimeInFrames(0);
Console.WriteLine($"Engine 初期化完了: SampleRate={engine.SampleRate}Hz, Channels={engine.Channels}, Listeners={engine.ListenerCount}");
Console.WriteLine($"現在のエンジン時間: {engine.Time.TotalMilliseconds:0}ms (frames={engine.TimeInPcmFrames})");

if (options.Volume is { } engineVolume)
{
    engine.Volume = engineVolume;
    Console.WriteLine($"Engine.Volume = {engineVolume:0.00}");
}

if (options.GainDb is { } gainDb)
{
    engine.GainDb = gainDb;
    Console.WriteLine($"Engine.GainDb = {gainDb:0.0} dB");
}

const uint listenerIndex = 0;
engine.SetListenerPosition(listenerIndex, 0f, 0f, options.ListenerZ);
engine.SetListenerDirection(listenerIndex, new Vector3(0f, 0f, -1f));
engine.SetListenerWorldUp(listenerIndex, Vector3.UnitY);
engine.SetListenerVelocity(listenerIndex, Vector3.Zero);
engine.SetListenerCone(listenerIndex, MathF.PI / 6f, MathF.PI / 3f, 0.25f);
engine.SetListenerEnabled(listenerIndex, !options.DisableListener);

var cone = engine.GetListenerCone(listenerIndex);
Console.WriteLine($"Listener {listenerIndex}: enabled={engine.IsListenerEnabled(listenerIndex)}, position={engine.GetListenerPosition(listenerIndex)}, " +
                  $"cone(inner={RadiansToDegrees(cone.InnerAngleRadians):0.0}°, outer={RadiansToDegrees(cone.OuterAngleRadians):0.0}°, gain={cone.OuterGain:0.00})");
Console.WriteLine($"原点に最も近いリスナー index={engine.FindClosestListener(Vector3.Zero)}");

if (options.ManualStart)
{
    Console.WriteLine("NoAutoStart が有効なため、Engine.Start() を呼び出します。");
    engine.Start();
}

using var sound = engine.CreateSound(options.AudioPath, SoundInitFlags.Decode | SoundInitFlags.Async);
sound.Volume = options.SoundVolume ?? 0.9f;

//sound.Positioning = SoundPositioning.Relative; // 2Dモードで再生するならこっち
engine.SetListenerPosition(listenerIndex, 0f, 0f, 1f);

sound.Position = (0f, 0f, 0f);

if (options.DelayMs > 0)
{
    var scheduledFrames = engine.GetAbsoluteTimeInFrames(TimeSpan.FromMilliseconds(options.DelayMs));
    sound.ScheduleStart(scheduledFrames);
    sound.Start();
    Console.WriteLine($"{options.DelayMs}ms 後 (frame={scheduledFrames}) に再生をスケジュールしました。");
}
else
{
    sound.Start();
    Console.WriteLine("サウンドの再生を即時開始しました。");
}

await MonitorPlaybackAsync(engine, sound);

engine.SetListenerEnabled(listenerIndex, false);
engine.Stop();
Console.WriteLine("サンプルを終了しました。");

static double RadiansToDegrees(float radians) => radians * 180.0 / Math.PI;

static async Task MonitorPlaybackAsync(MiniaudioEngine engine, MiniaudioSound sound)
{
    while (true)
    {
        var state = sound.State;
        Console.WriteLine($"[Engine] time={engine.Time.TotalMilliseconds:0}ms frames={engine.TimeInPcmFrames} gain={engine.GainDb:0.0}dB | [Sound] state={state} cursor={sound.CursorInSeconds:0.00}s");

        if (state == SoundState.Stopped)
        {
            break;
        }

        await Task.Delay(200);
    }
}

internal sealed class EngineControlOptions
{
    private EngineControlOptions(string? audioPath, float? volume, float? gainDb, float? soundVolume, int delayMs, bool manualStart, bool disableListener, float listenerZ, bool showHelp)
    {
        AudioPath = audioPath;
        Volume = volume;
        GainDb = gainDb;
        SoundVolume = soundVolume;
        DelayMs = delayMs;
        ManualStart = manualStart;
        DisableListener = disableListener;
        ListenerZ = listenerZ;
        ShowHelp = showHelp;
    }

    public string? AudioPath { get; }
    public float? Volume { get; }
    public float? GainDb { get; }
    public float? SoundVolume { get; }
    public int DelayMs { get; }
    public bool ManualStart { get; }
    public bool DisableListener { get; }
    public float ListenerZ { get; }
    public bool ShowHelp { get; }

    public static bool TryParse(string[] args, out EngineControlOptions options, out string errorMessage)
    {
        options = new EngineControlOptions(null, null, null, null, 0, false, false, -2.5f, showHelp: false);
        errorMessage = string.Empty;

        if (args.Length == 0)
        {
            options = new EngineControlOptions(null, null, null, null, 0, false, false, -2.5f, showHelp: true);
            return true;
        }

        string? audioPath = null;
        float? engineVolume = null;
        float? gainDb = null;
        float? soundVolume = null;
        int delayMs = 0;
        bool manualStart = false;
        bool disableListener = false;
        float listenerZ = -2.5f;
        bool showHelp = false;

        var culture = CultureInfo.InvariantCulture;

        try
        {
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "--file" or "-f":
                        audioPath = RequireValue(args, ref i, "--file");
                        break;
                    case "--volume":
                        engineVolume = ParseFloat(RequireValue(args, ref i, "--volume"), culture);
                        break;
                    case "--sound-volume":
                        soundVolume = ParseFloat(RequireValue(args, ref i, "--sound-volume"), culture);
                        break;
                    case "--gain-db":
                        gainDb = ParseFloat(RequireValue(args, ref i, "--gain-db"), culture);
                        break;
                    case "--delay-ms":
                        delayMs = ParseInt(RequireValue(args, ref i, "--delay-ms"), culture);
                        if (delayMs < 0)
                        {
                            throw new ArgumentException("--delay-ms は 0 以上で指定してください。");
                        }
                        break;
                    case "--manual-start":
                        manualStart = true;
                        break;
                    case "--disable-listener":
                        disableListener = true;
                        break;
                    case "--listener-z":
                        listenerZ = ParseFloat(RequireValue(args, ref i, "--listener-z"), culture);
                        break;
                    case "--help" or "-h":
                        showHelp = true;
                        break;
                    default:
                        audioPath ??= arg;
                        break;
                }
            }
        }
        catch (ArgumentException ex)
        {
            errorMessage = ex.Message;
            return false;
        }

        options = new EngineControlOptions(audioPath, engineVolume, gainDb, soundVolume, delayMs, manualStart, disableListener, listenerZ, showHelp);
        return true;
    }

    public static void PrintUsage()
    {
        Console.WriteLine(@"Usage:
  dotnet run --project samples/MiniaudioNet.Sample.EngineControl -- --file <path> [options]

Options:
  --file, -f <path>           再生する音声ファイル (必須)
  --volume <value>            エンジンのマスターボリューム (0.0-1.0)
  --sound-volume <value>      サウンド個別のボリューム (0.0-1.0)
  --gain-db <value>           エンジンのマスターゲイン (dB)
  --manual-start              NoAutoStart でエンジンを生成し、Engine.Start() を手動実行
  --delay-ms <value>          エンジン時間を使った遅延再生 (ミリ秒)
  --listener-z <value>        リスナーの Z 座標 (初期値 -2.5)
  --disable-listener          再生前にリスナーを無効化
  --help, -h                  ヘルプを表示
");
    }

    private static string RequireValue(string[] args, ref int index, string name)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{name} オプションには値が必要です。");
        }

        return args[++index];
    }

    private static float ParseFloat(string value, CultureInfo culture)
    {
        if (!float.TryParse(value, NumberStyles.Float, culture, out var result))
        {
            throw new ArgumentException($"値 '{value}' は浮動小数点数として解釈できません。");
        }

        return result;
    }

    private static int ParseInt(string value, CultureInfo culture)
    {
        if (!int.TryParse(value, NumberStyles.Integer, culture, out var result))
        {
            throw new ArgumentException($"値 '{value}' は整数として解釈できません。");
        }

        return result;
    }
}
