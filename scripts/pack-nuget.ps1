# PowerShell script to pack NuGet package
$ErrorActionPreference = "Stop"

Write-Host "Packing PocketTTS NuGet package..." -ForegroundColor Green

# Navigate to C# project directory
Set-Location "csharp\PocketTTS"

# Build the project
Write-Host "Building C# project..." -ForegroundColor Cyan
dotnet build -c Release

# Pack the NuGet package
Write-Host "Creating NuGet package..." -ForegroundColor Cyan
dotnet pack -c Release -o "..\..\nuget"

# Return to root directory
Set-Location "..\..\"

Write-Host "NuGet package created!" -ForegroundColor Green
Write-Host "Package location: nuget\PocketTTS.*.nupkg" -ForegroundColor Green