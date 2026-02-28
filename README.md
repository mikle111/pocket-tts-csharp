# C# bindings for Pocket TTS Rust lib

Bindings for [Pocket TTS (Rust/Candle)](https://github.com/babybirdprd/pocket-tts) - a native Rust port of [Kyutai's Pocket TTS](https://github.com/kyutai-labs/pocket-tts).

## Features

- **CPU-only** - Runs on CPU, no GPU required
- **Streaming** - Full-pipeline stateful streaming for zero-latency audio
- **Parallel inference** - Meet InferenceService - easy to use high-level API with multithreading support
- **Control** - Use ModelHandle API for tinkering
- **Multiplatform** - Supports Windows, Linux out of the box (MacOS also, if you build for it manually)
- **Single nuget** - Native libraries included into the nuget

## Quick Start

### Build with Docker

Simplest option if you don't have Rust toolchain installed locally

#### Linux

```bash
# Build native libraries
scripts/build-csharp-docker.sh
# Build C# wrapper
scripts/pack-nuget.sh
```

#### Windows

```bash
# Build native libraries
scripts/build-csharp-docker.ps1
# Build C# wrapper
scripts/pack-nuget.ps1
```

### Build with Rust

#### Linux

```bash
# Build native libraries
scripts/build-csharp.sh
# Build C# wrapper
scripts/pack-nuget.sh
```

#### Windows

```bash
# Build native libraries
scripts/build-csharp.ps1
# Build C# wrapper
scripts/pack-nuget.ps1
```

### Examples

```csharp
//Load model
using var model = ModelHandle.LoadFromFiles(configPath, weightsPath, tokenizerPath);
Console.WriteLine($"Loaded model. Sample reate {model.SampleRate}");

//Create PocketTtsInferenceService
var inferenceService = new PocketTtsInferenceService(model, 8);

//Add voice and assign some name do it
const string voiceName = "reference voice";
inferenceService.AddVoice(voiceName, "ref.wav");

//Run the Service
var serviceCts = new CancellationTokenSource();
var serviceRunTask = inferenceService.Run(serviceCts.Token);

var text = "Hello there, PacketTTS!";

//simple inference
var audio = await inferenceService.Generate(text, voiceName, CancellationToken.None);
Helpers.WriteWav(audio, model.SampleRate, $"out.wav");

//streaming inference
var audioChunks = new List<float>();
await foreach (var chunk in inferenceService.GenerateStream(text, voiceName, CancellationToken.None))
{
    audioChunks.AddRange(chunk);
}
Helpers.WriteWav(audioChunks, model.SampleRate, $"out_streaming.wav");

//Stop the Service
await serviceCts.CancelAsync();
await serviceRunTask;
```

Check csharp/PocketTTS.Example

## License

MIT License - see [LICENSE](../LICENSE)

## Related

- [Pocket TTS (Rust/Candle)](https://github.com/babybirdprd/pocket-tts) - Rust implementation
- [Pocket TTS (Python)](https://github.com/kyutai-labs/pocket-tts) - Original python implementation
- [Candle](https://github.com/huggingface/candle) - Rust ML framework
- [Kyutai](https://kyutai.org) - Research lab
