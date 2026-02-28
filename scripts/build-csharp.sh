#!/bin/bash
set -e

echo "Building Pocket TTS C# bindings for all platforms..."

# Create runtime directories
mkdir -p runtimes/win-x64/native
mkdir -p runtimes/linux-x64/native
mkdir -p runtimes/osx-x64/native
mkdir -p runtimes/osx-arm64/native

# Build Linux x64
echo "Building for Linux x64..."
cargo build --release --package pocket-tts-csharp --target x86_64-unknown-linux-gnu
cp target/x86_64-unknown-linux-gnu/release/libpocket_tts_csharp.so runtimes/linux-x64/native/libpocket_tts.so

# Build Windows x64 (requires mingw-w64)
echo "Building for Windows x64..."
if command -v x86_64-w64-mingw32-gcc &> /dev/null; then
    cargo build --release --package pocket-tts-csharp --target x86_64-pc-windows-gnu
    cp target/x86_64-pc-windows-gnu/release/pocket_tts_csharp.dll runtimes/win-x64/native/pocket_tts.dll
else
    echo "Warning: mingw-w64 not found. Skipping Windows build."
    echo "To build for Windows, install: sudo apt-get install gcc-mingw-w64-x86-64"
fi

# Build macOS (requires macOS or cross-compilation toolchain)
echo "Building for macOS..."
if [[ "$OSTYPE" == "darwin"* ]]; then
    # Native macOS build
    cargo build --release --package pocket-tts-csharp --target x86_64-apple-darwin
    cp target/x86_64-apple-darwin/release/libpocket_tts_csharp.dylib runtimes/osx-x64/native/libpocket_tts.dylib
    
    cargo build --release --package pocket-tts-csharp --target aarch64-apple-darwin
    cp target/aarch64-apple-darwin/release/libpocket_tts_csharp.dylib runtimes/osx-arm64/native/libpocket_tts.dylib
else
    echo "Warning: Not on macOS. Skipping macOS builds."
    echo "macOS builds require macOS or OSXCross toolchain."
fi

echo "Native library build complete!"
echo "Built libraries are in: runtimes/"
