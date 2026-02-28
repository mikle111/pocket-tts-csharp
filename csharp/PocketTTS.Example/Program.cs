using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PocketTTS.Inference;

namespace PocketTTS.Example
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                const string configPath = "b6369a24.yaml";
                const string weightsPath = "tts_b6369a24.safetensors";
                const string tokenizerPath = "tokenizer.model";
                
                //Load model
                using var model = ModelHandle.LoadFromFiles(configPath, weightsPath, tokenizerPath);
                Console.WriteLine($"Loaded model. Sample reate {model.SampleRate}");
                
                //Create PocketTtsInferenceService
                var inferenceService = new PocketTtsInferenceService(model, 8);
                
                //Add voice and assign some name do it
                const string voiceName = "reference voice";
                inferenceService.AddVoice(voiceName, "ref.wav");
                
                //Run the Service
                var serviceCts = new CancellationTokenSource();
                var serviceRunTask = inferenceService.Run(serviceCts.Token);
                
                var text = "Hello there, PacketTTS!";

                //simple inference
                var audio = await inferenceService.Generate(text, voiceName, CancellationToken.None);
                Helpers.WriteWav(audio, model.SampleRate, $"out.wav");

                //streaming inference
                var audioChunks = new List<float>();
                await foreach (var chunk in inferenceService.GenerateStream(text, voiceName, CancellationToken.None))
                {
                    audioChunks.AddRange(chunk);
                }
                Helpers.WriteWav(audioChunks, model.SampleRate, $"out_streaming.wav");
                
                //Stop the Service
                await serviceCts.CancelAsync();
                await serviceRunTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }
    }
}
