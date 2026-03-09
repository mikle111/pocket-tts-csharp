using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PocketTTS;

internal static class NativeApi
{
    private const string LibraryName = "pocket_tts";

    static NativeApi()
    {
        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), (libraryName, assembly, searchPath) =>
        {
            if (libraryName == LibraryName)
            {
                if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out var handle))
                    return handle;

                string rid = RuntimeInformation.RuntimeIdentifier;
                string ext;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    ext = ".dll";
                }else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    ext = ".so";
                }
                else
                {
                    ext = ".dyn";
                }
                string probePath = Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", libraryName + ext);

                if (NativeLibrary.TryLoad(probePath, out handle))
                    return handle;
            }
            return IntPtr.Zero;
        });
    }
    
    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern ModelHandle pocket_tts_load_from_files(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string configPath,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string weightsPath,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string tokenizerPath);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern AudioBufferHanlde pocket_tts_generate(
        ModelHandle modelHandle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string text,
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
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern ModelStateHandle pocket_tts_get_voice_state_from_safetensors(
        ModelHandle modelHandle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string path);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern ModelStateHandle pocket_tts_copy_voice_state(ModelStateHandle modelStateHandle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void pocket_tts_free_voice_state(IntPtr modelStateHandle);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void pocket_tts_create_safetensors_from_wav(ModelHandle modelHandle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string wavPath,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string safetensorsPath);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern void pocket_tts_generate_stream(
        ModelHandle modelHandle,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string text,
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