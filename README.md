# miniaudioNet

miniaudioNet は、シングルヘッダーの軽量オーディオライブラリ [miniaudio](https://github.com/mackron/miniaudio) を .NET から安全に扱うための極小ラッパーです。ネイティブ側で `ma_engine` / `ma_sound` を管理し、C# からは `MiniaudioEngine` と `MiniaudioSound` で再生・ボリューム制御・シーク・進捗監視を手軽に行えます。

## ディレクトリ構成

```
miniaudioNet/
├── native/                     # CMake ベースのネイティブブリッジ (miniaudionet)
│   ├── CMakeLists.txt          # ma_engine / ma_sound を DLL/SO/DYLIB 化
│   └── CMakePresets.json       # 各 OS / RID 向けのビルドプリセット
├── scripts/                    # ネイティブ資産を出力する補助スクリプト
├── src/Miniaudio.Net/          # .NET 8.0 ラッパー (NuGet パッケージ本体)
├── samples/MiniaudioNet.Sample # 簡単な再生コンソールアプリ
└── third_party/miniaudio/      # 付属 miniaudio ヘッダー
```

## 必要条件

- CMake 3.21 以上
- C/C++ コンパイラ (MSVC, clang, gcc など)
- .NET SDK 8.0 以上
- PowerShell 7

## ネイティブバイナリの取得

GitHub Actions (`.github/workflows/ci.yml`) では Windows / Linux / macOS 各 RID 向けに `miniaudionet` をビルドし、成果物を NuGet パッケージへ自動投入します。ローカルでも同じ出力を得たい場合は次のスクリプトを利用してください。

### PowerShell (Windows)

```powershell
pwsh ./scripts/build-native.ps1 -Rid win-x64   # 例: Windows x64 版
pwsh ./scripts/build-native.ps1 -Rid win-arm64 # ARM64 版も同じコマンドで生成
```

### Bash (Linux / macOS)

```bash
./scripts/build-native.sh --rid linux-x64
./scripts/build-native.sh --rid osx-arm64
```

各コマンドは CMakePresets を介して `build/native/<preset>/` にビルドし、`artifacts/native/<RID>/native/` に DLL/SO/DYLIB を配置します。`.csproj` はこのディレクトリを検出して `runtimes/<RID>/native/` として NuGet へ自動梱包し、同時に `bin/<Configuration>/<TFM>/runtimes/...` にコピーします。アプリ側ではパッケージを参照するだけでネイティブライブラリが解決されます。

## ラッパーとサンプルのビルド

```powershell
# ソリューション全体
dotnet build miniaudioNet.sln -c Release

# パッケージのみを Release で生成
dotnet pack src/Miniaudio.Net/Miniaudio.Net.csproj -c Release
```

`dotnet pack` は `artifacts/native/**` の内容を自動で取り込み、`artifacts/packages/` に `.nupkg` と `.snupkg` を出力します。生成されたパッケージをローカルフィードに追加すれば、`.NET` プロジェクトへ参照するだけで Windows / macOS / Linux のいずれでも即動作します。

サンプルの実行:

```powershell
dotnet run --project samples/MiniaudioNet.Sample -- <audio-file>
```

## GitHub Actions ワークフロー概要

`CI` ワークフローは以下を自動化します。

1. 行列 (`win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`) でネイティブブリッジをビルド。
2. 各 RID の成果物をアップロード。
3. 収集済みのバイナリを使って `dotnet build` / `dotnet pack` を実行し、NuGet パッケージを生成。

公開リリース時は `packages` アーティファクトから `.nupkg` を取得して NuGet.org や自前フィードへそのままプッシュできます。

## 今後のアイデア

- `ma_device` を利用したキャプチャ API
- HRTF / 3D サウンドのリスナー設定補助
- Linux / macOS / Windows 向けの自動テスト (Decoder モックなど)
- `dotnet pack` と合わせた `dotnet nuget push` 自動化
## デバイス / コンテキスト機能の使い方

- `MiniaudioContext.Create()` でバックエンドを優先順位付きで初期化し、`EnumerateDevices()` から取得した `DeviceId` を保持できます。
- `MiniaudioEngineOptions` に `Context` と `PlaybackDeviceId` を指定すると、任意のデバイスにエンジンを接続できます。`NoDevice` を指定する場合は `SampleRate` と `Channels` が必須です。
- `samples/MiniaudioNet.Sample.Sine441` は `--list` や `--device-index`、`--backend` オプションを備え、デバイス列挙と選択の挙動を確認できる最小サンプルになりました。
