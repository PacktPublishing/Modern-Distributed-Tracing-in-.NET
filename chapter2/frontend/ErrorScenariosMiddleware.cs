using System.Collections.Concurrent;

namespace frontend;

public class ErrorScenariosMiddleware : IMiddleware
{
    private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.Value == "/memoryleak")
        {
            return StartMemoryLeak(cancellationTokenSource.Token);
        }
        else if (context.Request.Path.Value == "/threadpool")
        {
            return StartThreadPoolStravation(cancellationTokenSource.Token);
        }

        return next.Invoke(context);
    }

    public void Cancel()
    {
        cancellationTokenSource.Cancel();
    }

    private Task<int> StartMemoryLeak(CancellationToken cancellationToken)
    {
        var map = new ConcurrentDictionary<string, byte[]>();
        return Task.Run(async () => {
            while(!cancellationToken.IsCancellationRequested)
            {
                map.TryAdd(Guid.NewGuid().ToString("n"), new byte[1024]);
                await Task.Delay(10);
            }

            return map.Count;
        });
    }

    private Task StartThreadPoolStravation(CancellationToken cancellationToken)
    {
        var tasks = new HashSet<Task>();
        return Task.Run(async () => 
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (tasks.Count < 1000) 
                {
                    tasks.Add(Task.Run(() => Block().Result));
                }

                Task completed = await Task.WhenAny(tasks);
                tasks.Remove(completed);
                await Task.Delay(10);
            }
        });
    }

    private async Task<int> Block()
    {
        await Task.Delay(100);
        return 43;
    }
}
