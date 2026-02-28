using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PocketTTS;

internal sealed class AudioBufferHanlde : SafeHandleZeroOrMinusOneIsInvalid
{
    internal AudioBufferHanlde(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }
    
    private AudioBufferHanlde() : base(true)
    {
    }
    
    public float[] GetAudio() {
        var fields = Marshal.PtrToStructure<AudioBufferFields>(handle);
        var length = (int)fields.Length;
        var audio = new float[length];
        Marshal.Copy(fields.Data, audio, 0, length);
        return audio;
    }

    protected override bool ReleaseHandle()
    {
        NativeApi.pocket_tts_free_audio(handle);
        return true;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct AudioBufferFields
    {
        public IntPtr Data;
        public uint Length;
    }
}