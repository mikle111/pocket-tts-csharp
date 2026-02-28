using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PocketTTS.Abstractions;

namespace PocketTTS.Inference;

public sealed class PocketTtsInferenceService : IPocketTtsInferenceService
{
    private readonly ModelHandle _model;
    private readonly int _maxParallelWorkers;
    private Channel<JobBase> _jobsChannel;
    private readonly ConcurrentDictionary<string, ModelStateHandle> _voices;
    private bool _isRunning;
    private CancellationToken _stoppingToken;


    public PocketTtsInferenceService(ModelHandle model, int maxParallelWorkers)
    {
        _model = model;
        _maxParallelWorkers = maxParallelWorkers;
        _voices = new ConcurrentDictionary<string, ModelStateHandle>();
    }

    public void AddVoice(string voiceName, string voicePath)
    {
        if (!File.Exists(voicePath))
        {
            throw new ArgumentException(null, nameof(voicePath));
        }

        ModelStateHandle voice;

        if (voicePath.EndsWith(".wav") || voicePath.EndsWith(".wave"))
        {
            voice = _model.GetModelStateFromWav(voicePath);
        }
        else if (voicePath.EndsWith(".safetensors"))
        {
            voice = _model.GetModelStateFromSafetensors(voicePath);
        }
        else
        {
            throw new ArgumentException(null, nameof(voicePath));
        }

        _voices.TryAdd(voiceName, voice);
    }

    public async Task Run(CancellationToken stoppingToken)
    {
        if (_isRunning)
        {
            throw new InvalidOperationException("Inference service is already running");
        }
        _stoppingToken = stoppingToken;
        _jobsChannel = Channel.CreateBounded<JobBase>(new BoundedChannelOptions(_maxParallelWorkers)
        {
            SingleReader = _maxParallelWorkers == 1,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
        _stoppingToken.Register(() => _jobsChannel.Writer.Complete());
        var workers = new Task[_maxParallelWorkers];
        for (var i = 0; i < _maxParallelWorkers; i++)
        {
            workers[i] = ProcessJobs(stoppingToken);
        }
        _isRunning = true;
        try
        {
            await Task.WhenAll(workers);
        }
        finally
        {
            _isRunning = false;
        }
    }

    public async Task<float[]> Generate(string text, string voiceName, CancellationToken ct)
    {
        if (!_isRunning)
        {
            throw new InvalidOperationException("Inference service is not running");
        }
        
        var jobCts = CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken, ct);
        ct = jobCts.Token;
        var taskCompletionSource = new TaskCompletionSource<float[]>();
        var job = new RegularJob(text, voiceName, taskCompletionSource);
        
        await _jobsChannel.Writer.WriteAsync(job, ct);
        return await taskCompletionSource.Task.WaitAsync(ct);
    }

    public async IAsyncEnumerable<float[]> GenerateStream(string text, string voiceName,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (!_isRunning)
        {
            throw new InvalidOperationException("Inference service is not running");
        }
        
        var jobCts = CancellationTokenSource.CreateLinkedTokenSource(_stoppingToken, ct);
        ct = jobCts.Token;
        var chunksChannel = Channel.CreateUnbounded<float[]>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        var job = new StreamingJob(text, voiceName, chunksChannel.Writer);
        
        await _jobsChannel.Writer.WriteAsync(job, ct);
        while (await chunksChannel.Reader.WaitToReadAsync(ct))
        {
            while (chunksChannel.Reader.TryRead(out var chunk))
            {
                ct.ThrowIfCancellationRequested();
                yield return chunk;
            }
        }
    }

    private async Task ProcessJobs(CancellationToken ct)
    {
        try
        {
            while (await _jobsChannel.Reader.WaitToReadAsync(ct))
            {
                while (_jobsChannel.Reader.TryRead(out var job))
                {
                    job.Execute(_model, _voices);
                }
            }
        }
        catch(OperationCanceledException)
        {
        }
    }
}