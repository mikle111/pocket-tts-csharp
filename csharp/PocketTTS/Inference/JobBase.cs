using System.Collections.Concurrent;

namespace PocketTTS.Inference;

internal abstract class JobBase
{
    protected readonly string Text;
    protected readonly string VoiceName;

    protected JobBase(string text, string voiceName)
    {
        Text = text;
        VoiceName = voiceName;
    }

    public abstract void Execute(ModelHandle model, ConcurrentDictionary<string, ModelStateHandle> voices);
}