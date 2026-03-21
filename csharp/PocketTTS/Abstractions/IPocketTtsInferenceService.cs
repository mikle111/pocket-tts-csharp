using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PocketTTS.Abstractions;

public interface IPocketTtsInferenceService
{
    int SampleRate { get; }
    Task Run(CancellationToken stoppingToken);
    void AddVoice(string voiceName, string voicePath);
    void AddVoice(string voiceName, ModelStateHandle voice);
    Task<float[]> Generate(string text, string voiceName, CancellationToken ct);
    IAsyncEnumerable<float[]> GenerateStream(string text, string voiceName, CancellationToken ct);
}