using System;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace PocketTTS;

public sealed class ModelHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private ModelHandle() : base(true)
    {
    }

    public uint SampleRate => NativeApi.pocket_tts_sample_rate(this);

    public static ModelHandle LoadFromFiles(string configPath, string weightsPath, string tokenizerPath)
    {
        if (string.IsNullOrEmpty(configPath))
            throw new ArgumentNullException(nameof(configPath));
        if (string.IsNullOrEmpty(weightsPath))
            throw new ArgumentNullException(nameof(weightsPath));
        if (string.IsNullOrEmpty(tokenizerPath))
            throw new ArgumentNullException(nameof(tokenizerPath));

        var model = NativeApi.pocket_tts_load_from_files(configPath, weightsPath, tokenizerPath);
        return model;
    }
    
    public ModelStateHandle GetModelStateFromWav(string path)
    {
        if(!File.Exists(path))
        {
            throw new FileNotFoundException("Voice file not found", path);
        }
            
        var voice = NativeApi.pocket_tts_get_voice_state_from_wav(this, path);
        return voice;
    }
        
    public ModelStateHandle GetModelStateFromSafetensors(string path)
    {
        if(!File.Exists(path))
        {
            throw new FileNotFoundException("Voice file not found", path);
        }
        var voice = NativeApi.pocket_tts_get_voice_state_from_safetensors(this, path);
        return voice;
    }
        
    public float[] Generate(string text, ModelStateHandle modelState)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentNullException(nameof(text));
        ArgumentNullException.ThrowIfNull(modelState);

        using var bufferHandle = NativeApi.pocket_tts_generate(this, text, modelState);
        return bufferHandle.GetAudio();
    }

    public void GenerateStream(string text, ModelStateHandle modelState, Func<float[], bool> onChunk, Action onFinished, Action onError)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentNullException(nameof(text));
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(onChunk);
        ArgumentNullException.ThrowIfNull(onFinished);
        ArgumentNullException.ThrowIfNull(onError);

        NativeApi.pocket_tts_generate_stream(
            this,
            text,
            modelState,
            ChunkCallback,
            FinishedCallback,
            ErrorCallback,
            IntPtr.Zero);

        return;

        StreamControlCode ChunkCallback(IntPtr bufferHanlde, IntPtr userData)
        {
            try
            {
                using var bufferHandle = new AudioBufferHanlde(bufferHanlde);
                var audio = bufferHandle.GetAudio();
                return onChunk(audio)
                    ? StreamControlCode.Proceed
                    : StreamControlCode.Stop;
            }
            catch
            {
                return StreamControlCode.Stop;
            }
        }

        void FinishedCallback(IntPtr userData)
        {
            onFinished();
        }
        
        void ErrorCallback(IntPtr userData)
        {
            onError();
        }
    }

    public void CreateSafetensorsFromWav(string wavPath, string safetensorsPath)
    {
        if (!File.Exists(wavPath))
        {
            throw new FileNotFoundException("Wave file not found", wavPath);
        }
        NativeApi.pocket_tts_create_safetensors_from_wav(this, wavPath, safetensorsPath);
    }

    protected override bool ReleaseHandle()
    {
        NativeApi.pocket_tts_free(handle);
        return true;
    }
}