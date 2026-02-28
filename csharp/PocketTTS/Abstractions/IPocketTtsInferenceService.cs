using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PocketTTS.Abstractions;

public interface IPocketTtsInferenceService
{
    Task Run(CancellationToken stoppingToken);
    void AddVoice(string voiceName, string voicePath);
    Task<float[]> Generate(string text, string voiceName, CancellationToken ct);
    IAsyncEnumerable<float[]> GenerateStream(string text, string voiceName, CancellationToken ct);
}