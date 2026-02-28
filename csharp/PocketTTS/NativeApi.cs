using System;
using System.Runtime.InteropServices;

namespace PocketTTS;

internal static class NativeApi
{
    private const string LibraryName = "pocket_tts";
    
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern ModelHandle pocket_tts_load_from_files(
        [MarshalAs(UnmanagedType.LPStr)] string configPath,
        [MarshalAs(UnmanagedType.LPStr)] string weightsPath,
        [MarshalAs(UnmanagedType.LPStr)] string tokenizerPath);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern AudioBufferHanlde pocket_tts_generate(
        ModelHandle modelHandle,
        [MarshalAs(UnmanagedType.LPStr)] string text,
        ModelStateHandle modelStateHandle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern uint pocket_tts_sample_rate(ModelHandle modelHandle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void pocket_tts_free_audio(IntPtr bufferHandle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void pocket_tts_free(IntPtr modelHandle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern ModelStateHandle pocket_tts_get_voice_state_from_wav(
        ModelHandle modelHandle,
        [MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern ModelStateHandle pocket_tts_get_voice_state_from_safetensors(
        ModelHandle modelHandle,
        [MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern ModelStateHandle pocket_tts_copy_voice_state(ModelStateHandle modelStateHandle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void pocket_tts_free_voice_state(IntPtr modelStateHandle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void pocket_tts_create_safetensors_from_wav(ModelHandle modelHandle,
        [MarshalAs(UnmanagedType.LPStr)] string wavPath,
        [MarshalAs(UnmanagedType.LPStr)] string safetensorsPath);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern void pocket_tts_generate_stream(
        ModelHandle modelHandle,
        [MarshalAs(UnmanagedType.LPStr)] string text,
        ModelStateHandle modelStateHandle,
        StreamChunkCallback onChunk,
        StreamFinishedCallback onFinished,
        StreamErrorCallback onError,
        IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate StreamControlCode StreamChunkCallback(IntPtr bufferHandle, IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void StreamFinishedCallback(IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void StreamErrorCallback(IntPtr userData);
}