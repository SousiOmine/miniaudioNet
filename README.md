# miniaudioNet

[![NuGet](https://img.shields.io/nuget/v/Miniaudio.Net.svg)](https://www.nuget.org/packages/Miniaudio.Net/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Miniaudio.Net.svg)](https://www.nuget.org/packages/Miniaudio.Net/)
[![CI](https://github.com/SousiOmine/miniaudioNet/actions/workflows/ci.yml/badge.svg)](https://github.com/SousiOmine/miniaudioNet/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

miniaudioNetは、C言語で書かれたオーディオライブラリである[miniaudio](https://github.com/mackron/miniaudio)を.NETから安全に扱うためのラッパーライブラリです。

Windows、Linux、macOS（x64/ARM64）に対応しており、NuGetパッケージにはすべてのプラットフォーム向けのネイティブバイナリが含まれています。

## インストール

### NuGet パッケージマネージャー

```powershell
Install-Package Miniaudio.Net
```

### .NET CLI

```bash
dotnet add package Miniaudio.Net
```

### PackageReference

```xml
<PackageReference Include="Miniaudio.Net" Version="0.1.0" />
```

## クイックスタート

```csharp
using Miniaudio.Net;

// オーディオファイルを再生
using var context = new MiniaudioContext();
using var sound = context.CreateSound("path/to/audio.mp3");

sound.Start();

Console.WriteLine("再生中... Enterキーで終了");
Console.ReadLine();
```

## 対応プラットフォーム

| プラットフォーム | アーキテクチャ |
|------------------|----------------|
| Windows          | x64, ARM64     |
| Linux            | x64, ARM64     |
| macOS            | x64, ARM64     |

## 開発状況

> **注意**: 現状はCodexまかせで開発されています。

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

ストリーミング API の動作確認には次のコンソールアプリを利用できます。

```powershell
dotnet run --project samples/MiniaudioNet.Sample.Streaming -- [frequency-hz] [duration-seconds]
```

## GitHub Actions ワークフロー概要
1. 行列 (`win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`) でネイティブブリッジをビルド。
2. 各 RID の成果物をアップロード。
3. 収集済みのバイナリを使って `dotnet build` / `dotnet pack` を実行し、NuGet パッケージを生成。
