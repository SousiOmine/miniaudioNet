# miniaudioNet

miniaudioNet は、シングルヘッダーの軽量オーディオライブラリである [miniaudio](https://github.com/mackron/miniaudio) を .NET から扱うための極小ラッパーです。ネイティブ側にエンジンとサウンドの安全なハンドルを用意し、C# からは `MiniaudioEngine` と `MiniaudioSound` で音声再生やボリューム制御、シーク、進捗監視が行えます。

## ディレクトリ構成

```
miniaudioNet/
├── native/                 # miniaudionet.dll/.so を生成する CMake プロジェクト
│   └── manet_bridge.c      # ma_engine と ma_sound への薄いブリッジ
├── third_party/miniaudio/  # 公式 miniaudio ヘッダー (2025-10-25 時点の master)
├── src/Miniaudio.Net/      # C# ラッパー本体 (net8.0)
└── samples/MiniaudioNet.Sample/ # 簡易的な再生サンプル
```

## 必要条件

- CMake 3.21 以上
- C コンパイラ (MSVC / clang / gcc など)
- .NET SDK 8.0 以上

## ネイティブライブラリのビルド

1. `native` ディレクトリでビルドディレクトリを作成
   ```bash
   cmake -S native -B build/native -DCMAKE_BUILD_TYPE=Release
   cmake --build build/native --config Release
   ```
2. 生成された共有ライブラリ (`build/native/Release/miniaudionet.dll` など) を .NET 実行時に見つかる場所へ配置します。推奨: `src/Miniaudio.Net/runtimes/<RID>/native/` を作成しコピーするか、`PATH` / `LD_LIBRARY_PATH` に追加してください。

## .NET ライブラリとサンプルのビルド

```bash
powershell -Command "dotnet build miniaudioNet.sln"
```

ネイティブライブラリを配置済みであれば、サンプルは次のように実行できます。

```bash
powershell -Command "dotnet run --project samples/MiniaudioNet.Sample -- <audio-file>"
```

## 主な公開 API

- `MiniaudioEngine.Create()` / `Dispose()`
- `MiniaudioEngine.Play(string path)` : Fire-and-forget 再生
- `MiniaudioEngine.CreateSound(string path, SoundInitFlags flags)` : サウンドハンドル作成
- `MiniaudioSound.Start()/Stop()` / `SeekToFrame()`
- `MiniaudioSound.Volume`, `State`, `LengthInSeconds`, `Progress`

## 今後の拡張案

- miniaudio の `ma_device` やキャプチャ関連 API への対応
- HRTF/3D サウンド向けのリスナー設定補助 API
- Linux/macOS 用の CI ビルドとパッケージング (`dotnet pack`) サポート
- 追加のユニットテスト (ファイルをモックするデコーダー差し替えなど)
