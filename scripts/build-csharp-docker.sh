#!/bin/bash
set -e

echo "Building Pocket TTS C# native libraries using Docker..."

# Build the Docker image with cross-compilation support
docker build -f Dockerfile.csharp -t pocket-tts-csharp-builder .

# Create a temporary container to extract the artifacts
container_id=$(docker create pocket-tts-csharp-builder)

# Create runtime directories
mkdir -p runtimes/win-x64/native
mkdir -p runtimes/linux-x64/native

# Extract Linux library
echo "Extracting Linux x64 library..."
docker cp $container_id:/linux-x64/libpocket_tts.so runtimes/linux-x64/native/

# Extract Windows library
echo "Extracting Windows x64 library..."
docker cp $container_id:/win-x64/pocket_tts.dll runtimes/win-x64/native/

# Clean up
docker rm $container_id

echo "Docker build complete!"
echo "Native libraries extracted to: runtimes/"
echo ""
echo "Note: For macOS libraries, you need to build on macOS:"
echo "  cargo build --release --package pocket-tts-csharp --target x86_64-apple-darwin"
echo "  cargo build --release --package pocket-tts-csharp --target aarch64-apple-darwin"
