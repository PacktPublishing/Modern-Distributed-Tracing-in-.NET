﻿using System.Collections.Concurrent;

namespace issues;
public class ProcessingQueue : IHostedService
{
    private readonly ConcurrentQueue<Action> _queue = new ();
    private readonly CancellationTokenSource _cts = new ();
    private readonly Task _task;

    public ProcessingQueue()
    {
        _task = new Task(() => Loop());
    }

    public void Enqueue(Action process)
    {
        _queue.Enqueue(process);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _task.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        return _task;
    }

    private Task Loop()
    {
        while(_cts.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var process))
            {
                process();
            }
        }

        return Task.CompletedTask;
    }
}