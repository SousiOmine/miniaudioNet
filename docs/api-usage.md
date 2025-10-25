# API 利用ガイド

miniaudioNet の主な API と、アプリケーションでの利用パターンを紹介します。

## エンジンの生成

`MiniaudioEngine.Create()` を利用すると、デフォルト設定でエンジンを構築できます。`EngineConfig` を渡すことで、チャネル数やサンプルレート、デバイス設定をカスタマイズできます。

```csharp
var config = EngineConfig.CreateDefault();
config.Channels = 2;
config.SampleRate = 48000;

using var engine = MiniaudioEngine.Create(config);
```

## サウンドの生成と再生

`engine.CreateSound()` は、ファイルパスやストリーム、メモリバッファから `MiniaudioSound` を生成します。

```csharp
using var sound = engine.CreateSound("bgm.ogg", SoundInitFlags.Decode | SoundInitFlags.Async);
sound.Volume = 0.8f;
sound.Start();
```

### ストリーミング再生

大容量ファイルを扱う場合は `SoundInitFlags.Async` を付与することで、バックグラウンドでの読み込みを行いながら再生できます。

## 音量・ピッチ・パン・ループ設定

`MiniaudioSound` は音量だけでなくピッチとパンもプロパティで調整できます。`Pitch` は 0 より大きい値を指定してください。

```csharp
sound.Volume = 0.5f;
sound.Pitch = 1.2f; // 20% 高いピッチ
sound.Pan = -0.2f;  // 左寄り
sound.IsLooping = true;
```

## 3D ポジショニング

`Position` と `Direction` プロパティで音源の位置と向きを指定できます。`Positioning` を `SoundPositioning.Relative` にするとリスナーに対する相対座標で解釈されます。

```csharp
sound.Position = (0f, 1.2f, -3f);
sound.Direction = (0f, 0f, 1f);
sound.Positioning = SoundPositioning.Relative;

engine.SetListenerPosition(0, 0f, 0f, 0f);
```

リスナーの移動や複数リスナーを扱いたい場合は `MiniaudioEngine.SetListenerPosition()` を活用してください。

## 再生状態の監視

```csharp
while (sound.State is SoundState.Playing or SoundState.Starting)
{
    await Task.Delay(100);
}
```

`MiniaudioEngine` には複数のサウンドを管理するためのヘルパーも備わっています。

```csharp
engine.StopAll();
engine.WaitForAllSoundsToStop();
```

## リソース管理

- すべての `MiniaudioEngine` / `MiniaudioSound` は `IDisposable` を実装しています。
- `using` ステートメントで包むか、明示的に `Dispose()` を呼び出してください。

## エラーハンドリング

- ファイルが見つからない場合やサポート外フォーマットの場合、`MiniaudioException` がスローされます。
- ネイティブライブラリがロードできない場合は `DllNotFoundException` が発生します。`runtimes/<RID>/native/` の配置や `PATH`/`LD_LIBRARY_PATH` を確認してください。