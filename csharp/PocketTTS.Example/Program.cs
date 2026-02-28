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
                var serviceStoppingToken = new CancellationTokenSource();
                var serviceRunTask = inferenceService.Run(serviceStoppingToken.Token);
                
                //Perform inference
                async Task Simple(int taskId)
                {
                    var text = $"This is a task with id {taskId}, good job!";
                    var audio = await inferenceService.Generate(text, voiceName, CancellationToken.None);
                    Helpers.WriteWav(audio, model.SampleRate, $"task_{taskId}.wav");
                    
                    Console.WriteLine($"Completed task {taskId}");
                }

                async Task Streaming(int taskId)
                {
                    var text = $"This is a streaming task with id {taskId}, good job!";
                    var audio = new List<float>();
                    await foreach (var chunk in inferenceService.GenerateStream(text, voiceName, CancellationToken.None))
                    {
                        audio.AddRange(chunk);
                    }
                    Helpers.WriteWav(audio, model.SampleRate, $"task_{taskId}.wav");
                    
                    Console.WriteLine($"Completed task {taskId}");
                }
                
                var inferenceTasks = new List<Task>();
                for (var i = 0; i < 20; i++)
                {
                    var taskId = i;
                    inferenceTasks.Add(i % 2 == 0 
                        ? Simple(taskId) 
                        : Streaming(taskId));
                }
                await Task.WhenAll(inferenceTasks);
                
                //Stop the Service
                await serviceStoppingToken.CancelAsync();
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
