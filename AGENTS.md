miniaudioNet は、シングルヘッダーの軽量オーディオライブラリ [miniaudio](https://github.com/mackron/miniaudio) を .NET から安全に扱うためのラッパーです

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

# テスト
`dotnet test` で単体テストを実行できます。