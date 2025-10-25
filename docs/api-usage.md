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

`--backend` を複数指定すると、その順序でバックエンドをトライしながらコンテキストを初期化します。`--device-id` は `--list` で表示された 16 進文字列を貼り付けてください。

## リソース解放

すべての `MiniaudioEngine` / `MiniaudioSound` / `MiniaudioContext` は `IDisposable` です。長時間実行するアプリケーションやテストでは `using` ステートメントでスコープを限定し、確実に `Dispose()` が呼ばれるようにしてください。
