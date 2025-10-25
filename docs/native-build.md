# ネイティブバイナリのビルド手順

miniaudioNet の .NET ラッパーは、内部で `miniaudionet` というネイティブブリッジライブラリを利用します。このドキュメントでは、各プラットフォーム向けにネイティブ資産をビルドし、NuGet パッケージやアプリで利用できるようにする方法を解説します。

## 前提条件

- CMake 3.21 以上
- 対象プラットフォームの C/C++ コンパイラ
  - Windows: MSVC (Visual Studio Build Tools) など
  - Linux: gcc / clang
  - macOS: Xcode Command Line Tools (clang)
- PowerShell 7 以上 (Windows) または Bash (Linux / macOS)

## ビルドスクリプトの利用

`/scripts` ディレクトリには PowerShell と Bash のヘルパースクリプトが用意されています。これらは `CMakePresets.json` に定義されたプリセットを利用し、成果物を `artifacts/native/<RID>/native/` にコピーします。

### Windows (PowerShell)

```powershell
pwsh ./scripts/build-native.ps1 -Rid win-x64
pwsh ./scripts/build-native.ps1 -Rid win-arm64
```

### Linux / macOS (Bash)

```bash
./scripts/build-native.sh --rid linux-x64
./scripts/build-native.sh --rid osx-arm64
```

### 生成物

各コマンドは次の構成で成果物を出力します。

```
artifacts/
└── native/
    └── <RID>/
        └── native/
            └── miniaudionet.(dll|so|dylib)
```

`.csproj` はビルド時にこのディレクトリを自動検出し、NuGet パッケージの `runtimes/<RID>/native/` に梱包します。同時に `dotnet build` の出力フォルダにもコピーされ、開発中の実行が容易になります。

## 手動で CMake を実行する

スクリプトを利用しない場合は、CMake コマンドを直接叩いてビルドできます。

```bash
cmake --preset <PresetName>
cmake --build --preset <PresetName>
```

プリセット名は `native/CMakePresets.json` を参照してください。例: `win-x64-release`, `linux-x64-release` など。

ビルド完了後、生成されたバイナリを `artifacts/native/<RID>/native/` にコピーする点を忘れないようにしてください。

## FAQ

- **Q. MSVC 以外のコンパイラは利用できますか？**
  - Windows の場合は MSVC を推奨しています。MinGW などでもビルド可能ですが、未検証です。
- **Q. ビルド済みバイナリを入手できますか？**
  - GitHub Actions の CI 成果物や公式リリースから取得できます。ローカルで再現したい場合は上記スクリプトを利用してください。
- **Q. 新しい RID を追加するには？**
  - `CMakePresets.json` に新しいプリセットを追加し、スクリプトで対応するケースを増やしてください。`.csproj` にも RID を追記するとパッケージングに反映されます。
