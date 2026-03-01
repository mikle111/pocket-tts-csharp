using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace PocketTTS.Inference;

internal class RegularJob : JobBase
{
    private readonly TaskCompletionSource<float[]> _taskCompletionSource;


    public RegularJob(string text, string voiceName, TaskCompletionSource<float[]> taskCompletionSource) : base(
        text, voiceName)
    {
        _taskCompletionSource = taskCompletionSource;
    }

    public override void Execute(ModelHandle model, ConcurrentDictionary<string, ModelStateHandle> voices)
    {
        using var voice = voices[VoiceName].Clone();
        try
        {
            var result = model.Generate(Text, voice);
            _taskCompletionSource.SetResult(result);
        }
        catch (Exception e)
        {
            _taskCompletionSource.SetException(e);
        }
    }
}