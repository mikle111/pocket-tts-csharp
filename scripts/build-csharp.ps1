# PowerShell build script for Windows
$ErrorActionPreference = "Stop"

Write-Host "Building Pocket TTS C# bindings..." -ForegroundColor Green

# Create runtime directories
New-Item -ItemType Directory -Force -Path "runtimes\win-x64\native" | Out-Null
New-Item -ItemType Directory -Force -Path "runtimes\linux-x64\native" | Out-Null
#New-Item -ItemType Directory -Force -Path "runtimes\osx-x64\native" | Out-Null
#New-Item -ItemType Directory -Force -Path "runtimes\osx-arm64\native" | Out-Null

# Build Windows x64 (native)
Write-Host "Building for Windows x64..." -ForegroundColor Cyan
cargo build --release --package pocket-tts-csharp --target x86_64-pc-windows-msvc
Copy-Item "target\x86_64-pc-windows-msvc\release\pocket_tts_csharp.dll" "runtimes\win-x64\native\pocket_tts.dll" -Force

Write-Host "Native library build complete!" -ForegroundColor Green
Write-Host "Built libraries are in: runtimes\" -ForegroundColor Green
Write-Host ""
Write-Host "Note: For Linux and macOS builds, use WSL or a Linux/macOS machine." -ForegroundColor Yellow
