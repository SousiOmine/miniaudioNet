# API 使い方ガイド

miniaudioNet の API と、典型的なアプリケーションでの利用方法をまとめています。

## コンテキストとデバイス列挙

`MiniaudioContext` は miniaudio の `ma_context` を管理し、利用したいバックエンドの優先順位やデバイス列挙を制御できます。

```csharp
using var context = MiniaudioContext.Create(new[] { MiniaudioBackend.Wasapi, MiniaudioBackend.CoreAudio });
var playbackDevices = context.EnumerateDevices(MiniaudioDeviceKind.Playback);

foreach (var (device, index) in playbackDevices.Select((d, i) => (d, i)))
{
    Console.WriteLine($"[{index}] {device.Name}" + (device.IsDefault ? " [default]" : string.Empty));
}
```

列挙結果に含まれる `DeviceId` は 16 進文字列化した `ma_device_id` で、そのまま `MiniaudioEngineOptions.PlaybackDeviceId` に設定できます。インデックスで選択したい場合は、`DeviceId` を保持しておきエンジン作成時に渡してください。

## デバイスを指定したエンジン生成

`MiniaudioEngineOptions` を使うと、既存コンテキストの共有、デバイス ID の明示、デバイスレスモードなどを選択できます。`NoDevice` を有効にする場合はサンプルレートとチャンネル数の指定が必須です。

```csharp
var engineOptions = new MiniaudioEngineOptions
{
    Context = context,
    PlaybackDeviceId = playbackDevices[chosenIndex].DeviceId,
    SampleRate = 48_000,
};

using var engine = MiniaudioEngine.Create(engineOptions);
engine.Volume = 0.8f;
```

従来どおり `MiniaudioEngine.Create()` も利用できますが、明示的なデバイス制御は `MiniaudioEngineOptions` でのみ提供されます。

## サウンドの生成と再生

`MiniaudioEngine.CreateSound()` でファイルを、`CreateSoundFromPcmFrames()` でメモリ上の PCM をサウンドに変換できます。戻り値の `MiniaudioSound` からボリューム、ピッチ、パン、シーク位置などを制御します。

```csharp
using var sound = engine.CreateSound("bgm.ogg", SoundInitFlags.Decode | SoundInitFlags.Async);
sound.Volume = 0.8f;
sound.Pitch = 1.05f;
sound.Start();
```

任意の PCM を即時再生する場合は次のようにします。

```csharp
var frames = GeneratePcm();
using var pcmSound = engine.CreateSoundFromPcmFrames(frames, channels: 2, sampleRate: 48_000);
pcmSound.Start();
```

## 3D ポジショニングと進捗取得

`Position` と `Direction` を設定すると 3D 空間での位置を制御できます。`SoundState` や `CursorInFrames` を参照すると進捗監視やループ処理が簡単です。

```csharp
sound.Position = (0f, 1.2f, -3f);
sound.Direction = (0f, 0f, 1f);
sound.Positioning = SoundPositioning.Relative;

while (sound.State is SoundState.Playing or SoundState.Starting)
{
    Console.WriteLine($"{sound.CursorInSeconds:0.00}s / {sound.LengthInSeconds:0.00}s");
    await Task.Delay(100);
}
```

## サンプルアプリの使い方

`samples/MiniaudioNet.Sample.Sine441` には、今回追加したコンテキスト・デバイス API を利用する CLI が含まれています。

```powershell
# デバイス一覧を表示
pwsh dotnet run --project samples/MiniaudioNet.Sample.Sine441 -- --list

# インデックス 2 のデバイスでサイン波を再生
pwsh dotnet run --project samples/MiniaudioNet.Sample.Sine441 -- --device-index 2

# WASAPI を最優先にして列挙し、DeviceId で直接指定
pwsh dotnet run --project samples/MiniaudioNet.Sample.Sine441 -- --backend Wasapi --device-id <hex>
```

`--backend` を複数指定すると、その順序でバックエンドをトライしながらコンテキストを初期化します。`--device-id` は `--list` で表示された 16 進文字列を貼り付けてください。`--sample-rate` を指定すると任意のサンプルレートを強制でき、未指定の場合はデバイス既定値を採用しつつ、初期化に失敗した場合は自動的に 48 kHz へフォールバックします。

## リソース解放

すべての `MiniaudioEngine` / `MiniaudioSound` / `MiniaudioContext` は `IDisposable` です。長時間実行するアプリケーションやテストでは `using` ステートメントでスコープを限定し、確実に `Dispose()` が呼ばれるようにしてください。

## リソースマネージャーを利用したストリーミング

`MiniaudioResourceManager` は C# から `ma_resource_manager` を扱うためのラッパーです。デコード後のサンプルレートやチャンネル数を統一したい場合は `MiniaudioResourceManagerOptions` で設定し、`MiniaudioEngineOptions.ResourceManager` に渡してエンジンを生成します。これにより `SoundInitFlags.Stream | SoundInitFlags.Async` を使ったストリーミング再生でキャッシュやデコードスレッドを効率的に共有できます。

```csharp
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
    PlaybackDeviceId = playbackDeviceId,
};

using var engine = MiniaudioEngine.Create(engineOptions);
using var streamingSound = engine.CreateSound("bgm.flac", SoundInitFlags.Stream | SoundInitFlags.Async | SoundInitFlags.Looping);
streamingSound.Volume = 0.7f;
streamingSound.Start();
```

## フェード / スケジューラ / End イベント

`MiniaudioSound` では `Looping` プロパティでループ再生を制御できるほか、`ApplyFade` や `ScheduleStart` / `ScheduleStop` でフェードと再生タイミングを組み合わせることができます。`Ended` イベントにハンドラーを登録すれば、再生終了時に後片付けや次のサウンドの開始などを行えます。

```csharp
sound.Looping = true;
sound.ApplyFade(0f, 0.8f, TimeSpan.FromSeconds(1));
sound.ScheduleStart(TimeSpan.FromSeconds(2));
sound.ScheduleStop(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));
sound.Ended += (_, __) => Console.WriteLine("Playback completed.");
```

## キャプチャデバイス

`MiniaudioCaptureDevice` は入力デバイスからの PCM をイベントで受け取れます。`PcmCaptured` ではフレーム数・チャンネル数・サンプル配列が提供されるため、リアルタイムのレベルメーターなどを実装できます。

```csharp
using var capture = MiniaudioCaptureDevice.Create(new MiniaudioCaptureDeviceOptions
{
    Context = context,
    SampleRate = 16_000,
    Channels = 1,
});

capture.PcmCaptured += (_, e) =>
{
    double sum = 0;
    foreach (var sample in e.Samples)
    {
        sum += sample * sample;
    }

    var rms = Math.Sqrt(sum / e.Samples.Length);
    Console.WriteLine($"Mic RMS: {20 * Math.Log10(Math.Max(rms, 1e-6)):0.0} dBFS");
};

capture.Start();
```

## デバイス IO サンプル

```powershell
pwsh dotnet run --project samples/MiniaudioNet.Sample.DeviceIO -- --file ./bgm.flac --monitor --backend Wasapi
