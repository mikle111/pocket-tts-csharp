using System;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace PocketTTS.Inference;

internal class StreamingJob : JobBase
{
    private readonly ChannelWriter<float[]> _chunksChannel;

    public StreamingJob(string text, string voiceName, ChannelWriter<float[]> chunksChannel) : base(text, voiceName)
    {
        _chunksChannel = chunksChannel;
    }

    public override void Execute(ModelHandle model, ConcurrentDictionary<string, ModelStateHandle> voices)
    {
        using var voice = voices[VoiceName].Clone();
        model.GenerateStream(Text, voice, OnChunk, OnFinished, OnError);
        return;

        bool OnChunk(float[] chunk)
        {
            return _chunksChannel.TryWrite(chunk);
        }

        void OnFinished()
        {
            _chunksChannel.Complete();
        }

        void OnError()
        {
            _chunksChannel.Complete(new Exception("Stream error"));
        }
    }
}