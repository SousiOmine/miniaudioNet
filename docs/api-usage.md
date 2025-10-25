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

## 音量・パン・ループ設定

```csharp
sound.Volume = 0.5f;
sound.Pan = -0.2f; // 左寄り
sound.IsLooping = true;
```

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

## 拡張シナリオ

- **エフェクトチェーン**: `ma_node_graph` を活用すればカスタムエフェクトを構築できます (今後のラッパー拡張予定)。
- **キャプチャ**: `ma_device` を利用した録音 API は将来的に追加予定です。
- **3D サウンド**: HRTF やリスナー設定をサポートする拡張を検討しています。
