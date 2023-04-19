using System.Diagnostics;

namespace database;

static class Extensions
{
    private static readonly Task Completed = Task.CompletedTask;
    public static Activity SetTagIfNotNull(this Activity activity, string key, object? value)
    {
        if (value != null)
        {
            activity.SetTag(key, value);
        }

        return activity;
    }

    public static Task FireAndForget(this Task task)
    {
        if (task.Status == TaskStatus.Created)
        {
            task.Start();
        }

        return Completed;
    }

    public static Task FireAndForget(this IEnumerable<Task> task)
    {
        Task.WhenAll(task);
        return Completed;
    }
}