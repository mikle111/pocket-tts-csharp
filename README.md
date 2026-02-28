# Pocket TTS (C#)

FFI and C# bindings for [Pocket TTS (Rust/Candle)](https://github.com/babybirdprd/pocket-tts) - a native Rust port of [Kyutai's Pocket TTS](https://github.com/kyutai-labs/pocket-tts).

## Features

- **CPU-only** - Runs on CPU, no GPU required
- **Streaming** - Full-pipeline stateful streaming for zero-latency audio
- **Parallel inference** - Meet InferenceService - easy to use high-level API with multithreading support
- **Control** - Use ModelHandle API for tinkering
- **Multiplatform** - Supports Windows and Linux out of the box (MacOS also, if you build for it manually)
- **Single nuget** - Native libraries included into the nuget

### Example

See csharp/PocketTTS.Example

```csharp
//Load model
using var model = ModelHandle.LoadFromFiles(configPath, weightsPath, tokenizerPath);
Console.WriteLine($"Loaded model. Sample reate {model.SampleRate}");

//Create PocketTtsInferenceService
const int maxParallelWorkers = 4;
var inferenceService = new PocketTtsInferenceService(model, maxParallelWorkers);

//Add voice and assign some name do it
const string voiceName = "reference voice";
inferenceService.AddVoice(voiceName, "ref.wav");

//Run the Service
var serviceCts = new CancellationTokenSource();
var serviceRunTask = inferenceService.Run(serviceCts.Token);

var text = "Hello there, PocketTTS!";

//Simple inference
var audio = await inferenceService.Generate(text, voiceName, CancellationToken.None);
Helpers.WriteWav(audio, model.SampleRate, $"out.wav");

//Streaming inference
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

## Building

The nuget is saved to nuget/

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

## License

MIT License - see [LICENSE](../LICENSE)

## Related

- [Pocket TTS (Rust/Candle)](https://github.com/babybirdprd/pocket-tts) - Rust implementation
- [Pocket TTS (Python)](https://github.com/kyutai-labs/pocket-tts) - Original python implementation
- [Candle](https://github.com/huggingface/candle) - Rust ML framework
- [Kyutai](https://kyutai.org) - Research lab
