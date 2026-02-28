using System.Collections.Generic;
using System.IO;

namespace PocketTTS;

public static class Helpers
{
    public static void WriteWav(IReadOnlyList<float> rawAudio, uint sampleRate, string outputPath)
    {
        const int wavHeaderSize = 44;
        const int bitsPerSample = 32;
        const int bytesPerSample = 4;
        const int numChannels = 1;
            
        using var stream = new FileStream(outputPath, FileMode.Create);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8.ToArray());
        writer.Write(wavHeaderSize + rawAudio.Count * bytesPerSample - 8); //file-size (equals file-size - 8). Size of the overall file - 8 bytes
        writer.Write("WAVE"u8.ToArray()); //File Type Header
        writer.Write("fmt "u8.ToArray()); //Mark the format section. Format chunk marker. Includes trailing null.
        writer.Write(16); //Length of format data.  Always 16.
        writer.Write((short)3); //3 = float32
        writer.Write((short)numChannels); //Number of Channels
        writer.Write(sampleRate);
        writer.Write(sampleRate * numChannels * bytesPerSample); // sampleRate * channels * bytesPerSample
        writer.Write((short)(numChannels * bytesPerSample)); //channels * bytesPerSample
        writer.Write((short)bitsPerSample); //Bits per sample
        writer.Write("data"u8.ToArray()); //"data" chunk header. Marks the beginning of the data section.    
        writer.Write(rawAudio.Count * numChannels * bytesPerSample); //data length
        foreach (var sample in rawAudio)
        {
            writer.Write(sample);
        }
        writer.Flush();
        writer.Close();
    }
}