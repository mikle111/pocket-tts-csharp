using Microsoft.Win32.SafeHandles;

namespace PocketTTS;

public sealed class ModelStateHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal ModelStateHandle() : base(true)
    {
    }

    public ModelStateHandle Clone()
    {
        return NativeApi.pocket_tts_copy_voice_state(this);
    }

    protected override bool ReleaseHandle()
    {
        NativeApi.pocket_tts_free_voice_state(handle);
        return true;
    }
}