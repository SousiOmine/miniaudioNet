param(
    [string]$Preset,
    [string]$Rid,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

function Resolve-PresetFromRid {
    param([string]$RuntimeId)

    $map = @{
        "win-x64"    = "windows-x64"
        "win-arm64"  = "windows-arm64"
        "linux-x64"  = "linux-x64"
        "linux-arm64"= "linux-arm64"
        "osx-x64"    = "macos-x64"
        "osx-arm64"  = "macos-arm64"
    }

    if (-not $RuntimeId) {
        throw "RID を指定するか、Preset を直接指定してください。"
    }

    if (-not $map.ContainsKey($RuntimeId)) {
        throw "未知の RID '$RuntimeId' です。対応表を scripts/build-native.ps1 内で更新してください。"
    }

    return $map[$RuntimeId]
}

if (-not $Preset) {
    $Preset = Resolve-PresetFromRid -RuntimeId $Rid
}

if (-not $Rid) {
    switch ($Preset) {
        "windows-x64"   { $Rid = "win-x64" }
        "windows-arm64" { $Rid = "win-arm64" }
        "linux-x64"     { $Rid = "linux-x64" }
        "linux-arm64"   { $Rid = "linux-arm64" }
        "macos-x64"     { $Rid = "osx-x64" }
        "macos-arm64"   { $Rid = "osx-arm64" }
        default { throw "Preset '$Preset' に対応する RID が未定義です。" }
    }
}

$configurationLower = $Configuration.ToLowerInvariant()
$buildPreset = "$Preset-$configurationLower"

$nativeDir = Join-Path $PSScriptRoot ".." | Resolve-Path
Push-Location (Join-Path $nativeDir "native")

try {
    Write-Host "Configuring '$Preset'..."
    cmake --preset $Preset | Out-Null

    Write-Host "Building preset '$buildPreset'..."
    cmake --build --preset $buildPreset | Out-Null

    Write-Host "Installing artifacts for '$Rid'..."
    cmake --install --preset $buildPreset | Out-Null

    $outputDir = Join-Path $nativeDir "artifacts/native/$Rid"
    Write-Host "Nativeバイナリを $outputDir に配置しました。"
}
finally {
    Pop-Location
}
