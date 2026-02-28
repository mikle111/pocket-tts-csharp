# Building Pocket TTS C# Wrapper

Complete guide to building the C# wrapper and creating the NuGet package.

## Quick Start

### 1. Build Native Libraries

#### Using Docker (Recommended for Linux + Windows builds)

```bash
# Build Linux and Windows libraries in Docker
./scripts/build-csharp-docker.sh
```

This will create:
- `runtimes/linux-x64/native/libpocket_tts.so`
- `runtimes/win-x64/native/pocket_tts.dll`

#### Native Builds

**On Linux:**
```bash
# Install cross-compilation tools
sudo apt-get update
sudo apt-get install gcc-mingw-w64-x86-64

# Add Rust targets
rustup target add x86_64-unknown-linux-gnu
rustup target add x86_64-pc-windows-gnu

# Build
./scripts/build-csharp.sh
```

**On Windows:**
```powershell
# Add Rust target
rustup target add x86_64-pc-windows-msvc

# Build
.\scripts\build-csharp.ps1
```

**On macOS:**
```bash
# Add Rust targets
rustup target add x86_64-apple-darwin
rustup target add aarch64-apple-darwin

# Build
cargo build --release --package pocket-tts-csharp --target x86_64-apple-darwin
cargo build --release --package pocket-tts-csharp --target aarch64-apple-darwin

# Copy to runtimes directory
mkdir -p runtimes/osx-x64/native runtimes/osx-arm64/native
cp target/x86_64-apple-darwin/release/libpocket_tts_csharp.dylib runtimes/osx-x64/native/libpocket_tts.dylib
cp target/aarch64-apple-darwin/release/libpocket_tts_csharp.dylib runtimes/osx-arm64/native/libpocket_tts.dylib
```

### 2. Create NuGet Package

After building native libraries for all target platforms:

**Linux/macOS:**
```bash
./scripts/pack-nuget.sh
```

**Windows:**
```powershell
.\scripts\pack-nuget.ps1
```

The package will be created in `nuget/PocketTTS.0.6.2.nupkg`
