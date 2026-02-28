#!/bin/bash
set -e

echo "Packing PocketTTS NuGet package..."

# Navigate to C# project directory
cd csharp/PocketTTS

# Build the project
echo "Building C# project..."
dotnet build -c Release

# Pack the NuGet package
echo "Creating NuGet package..."
dotnet pack -c Release -o ../../nuget

echo "NuGet package created!"
echo "Package location: nuget/PocketTTS.*.nupkg"