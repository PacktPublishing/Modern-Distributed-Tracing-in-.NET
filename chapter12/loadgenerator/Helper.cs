using System.Net.Http.Json;
using System.Text;

namespace loadgenerator;

internal class Helper
{
    private record struct NextAccess(int Delta, DateTime Time);

    private readonly object lck = new ();
    private static readonly ThreadLocal<Random> Random = new(() => new Random());
    private readonly HttpClient _service;
    private readonly PriorityQueue<string, NextAccess> _records;
    private readonly TimeSpan _maxFirstInterval;

    public Helper(string endpoint, TimeSpan firstIntervalBetweenReads)
    {
        _records = new(Comparer<NextAccess>.Create((a, b) => DateTime.Compare(a.Time, b.Time)));
        _service = new ()
        {
            BaseAddress = new Uri(endpoint),
            Timeout = TimeSpan.FromSeconds(5)
        };
        _maxFirstInterval = firstIntervalBetweenReads;
    }

    public async Task GetOrCreate()
    {
        bool hasItem = false;
        string? record = null;
        NextAccess accessTime = default;
        lock (lck)
        {
            hasItem = _records.TryDequeue(out record, out accessTime);
        }

        if (!hasItem || accessTime.Delta > 30000)
        {
            await CreateItem();
            return;
        }

        if (accessTime.Time > DateTime.UtcNow)
        {
            Enqueue(record!, accessTime.Delta, accessTime.Time);
            await CreateItem();
        }
        else
        {
            await GetItem(record!);

            var newDelta = accessTime.Delta * (Random.Value!.NextDouble() + 1);
            Enqueue(record!, newDelta);
        }
    }

    private void Enqueue(string record, double delta, DateTime accessTime = default)
    {
        lock (lck)
        {
            _records.Enqueue(record, new NextAccess((int)delta, accessTime == default ? DateTime.UtcNow.AddMilliseconds(delta) : accessTime));
        }
    }

    private async Task GetItem(string record)
    {
        try
        {
            var response = await _service.GetAsync("/records/" + record);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

    }

    public async Task CreateItem()
    {
        try
        {
            var response = await _service.PostAsJsonAsync("/records", new[] {new { name = GenerateRandomString(32) }});
            response.EnsureSuccessStatusCode();
            var records = await response.Content.ReadFromJsonAsync<string[]>();
            var delta = Random.Value!.Next(0, (int)_maxFirstInterval.TotalMilliseconds);
            lock (lck)
            {
                _records.Enqueue(records!.Single(), new NextAccess(delta, DateTime.UtcNow.AddMilliseconds(delta)));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private static string GenerateRandomString(int length)
    {
        var builder = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            builder.Append(Random.Value!.Next(32, 127));
        }

        return builder.ToString();
    }
}
