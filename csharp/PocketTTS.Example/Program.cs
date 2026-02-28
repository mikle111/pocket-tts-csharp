using System;
using System.Collections.Generic;
using System.IO;
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

                if (!File.Exists(configPath))
                {
                    throw new FileNotFoundException($"The file doesn't exist: {configPath}");
                }
                
                if (!File.Exists(weightsPath))
                {
                    throw new FileNotFoundException($"The file doesn't exist: {weightsPath}");
                }
                
                if (!File.Exists(tokenizerPath))
                {
                    throw new FileNotFoundException($"The file doesn't exist: {tokenizerPath}");
                }
                
                using var model = ModelHandle.LoadFromFiles(configPath, weightsPath, tokenizerPath);
                Console.WriteLine($"Model loaded! Sample rate: {model.SampleRate} Hz");
                model.CreateSafetensorsFromWav("marius.wav", "marius2.safetensors");
                
                var inferenceService = new PocketTtsInferenceService(model, 8);
                
                inferenceService.AddVoice("default_voice", "marius.wav");
                
                var generateTasks = new List<Task>();
                
                for (var i = 0; i < 20; i++)
                {
                    var taskId = i;
                    if (i % 2 == 0)
                    {
                        var text = $"This is a task with id {taskId}, good job!";
                        generateTasks.Add(Task.Run(async () =>
                        {
                            var audio = await inferenceService.Generate(text, "default_voice", CancellationToken.None);
                            Helpers.WriteWav(audio, model.SampleRate, $"task_{taskId}.wav");
                            Console.WriteLine($"Completed task {taskId}!");
                        }));
                    }
                    else
                    {
                        var text = $"This is a streaming task with id {taskId}, good job!";
                        generateTasks.Add(Task.Run(async () =>
                        {
                            var audio = new List<float>();
                            await foreach (var chunk in inferenceService.GenerateStream(text, "default_voice", CancellationToken.None))
                            {
                                audio.AddRange(chunk);
                            }
                            Helpers.WriteWav(audio.ToArray(), model.SampleRate, $"task_{taskId}.wav");
                            Console.WriteLine($"Completed task {taskId}!");
                        }));
                    }
                }
                
                var cts = new CancellationTokenSource();
                _ = inferenceService.Run(cts.Token);
                await Task.WhenAll(generateTasks);
                await cts.CancelAsync();

                // var cts = new CancellationTokenSource();
                // _ = inferenceService.Run(cts.Token);
                // for (var i = 0; i < 20; i++)
                // {
                //     var audio = await inferenceService.Generate($"This is a task with id {i}. Good job!", voice, CancellationToken.None);
                //     Helpers.WriteWav(audio, model.SampleRate, $"task_{i}.wav");
                //     Console.WriteLine($"Completed task {i}!");
                // }
                // await cts.CancelAsync();
                
                // var audio = model.Generate(text, voice);
                // Helpers.WriteWav(audio, model.SampleRate, outputPath);
                // Console.WriteLine($"Audio saved to: {outputPath}");
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
