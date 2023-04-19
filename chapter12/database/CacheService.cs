using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;

namespace database;

public class CacheService
{
    private const string GetOperationName = "GetString";
    private const string SetOperationName = "SetString";
    private const int DefaultPort = 6379;

    private static readonly DistributedCacheEntryOptions SlidingExpiration = new () { SlidingExpiration = TimeSpan.FromSeconds(10)};
    private static readonly ActivitySource RedisSource = new("Redis");
    private static readonly Meter RedisMeter = new("Redis");
    private readonly Histogram<double> _operationDuration;

    private readonly IDistributedCache _cache;
    private readonly string? _host = null;
    private readonly int? _port = null;
    private readonly string? _address = null;
    private readonly string? _networkFamily = null;
    private readonly int? _dbIndex = null;
    public CacheService(IDistributedCache cache, IOptions<RedisCacheOptions> redisOptions)
    {
        _cache = cache;

        if (TryParseEndpoint(redisOptions.Value.ConfigurationOptions?.EndPoints, out var host, out var port, out var address, out var networkFamily))
        {
            _host = host;
            _port = port;
            _address = address;
            _networkFamily = networkFamily;
        }
        _dbIndex = redisOptions.Value.ConfigurationOptions?.DefaultDatabase;
        _operationDuration = RedisMeter.CreateHistogram<double>("db.operation.duration", "ms", "Database call duration");
    }

    public async Task<string?> GetRecord(string id)
    {
        var duration = _operationDuration.Enabled ? Stopwatch.StartNew() : null;
        using var act = StartCacheActivity(GetOperationName);

        try
        {
            var record = await _cache.GetStringAsync(id);
            act?.SetTag("cache.hit", record != null);
            TrackDuration(duration, GetOperationName, record != null);
            return record;
        }
        catch (Exception ex)
        {
            act?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            //act?.RecordException(ex); // would create enornmous telemetry volume when cache is down
            TrackDuration(duration, GetOperationName, exception: ex);
            // comment me out to fail siletly and pretend there is no record when Redis call fails.
            throw;
        }

        return null;
    }

    public async Task CacheRecord(string id, string record)
    {
        var duration = _operationDuration.Enabled ? Stopwatch.StartNew() : null;
        using var act = StartCacheActivity(SetOperationName);

        try
        {
            await _cache.SetStringAsync(id, record, SlidingExpiration);
            TrackDuration(duration, SetOperationName);
        }
        catch (Exception ex)
        {
            act?.RecordException(ex); // would create enornmous telemetry volume when cache is down
            act?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            TrackDuration(duration, SetOperationName, exception: ex);
        }
    }

    private Activity? StartCacheActivity(string operation)
    {
        var act = RedisSource.StartActivity(operation, ActivityKind.Client);
        if (act?.IsAllDataRequested != true) return act;
        return act.SetTag("db.operation", operation)
            .SetTag("db.system", "redis")
            .SetTagIfNotNull("db.redis.database_index", _dbIndex)
            .SetTagIfNotNull("net.peer.name", _host)
            .SetTagIfNotNull("net.peer.port", _port)
            .SetTagIfNotNull("net.sock.peer.addr", _address)
            .SetTagIfNotNull("net.sock.family", _networkFamily);
    }


    private void TrackDuration(Stopwatch? duration, string operation, bool? hit = null, Exception? exception = null)
    {
        if (duration == null) return;

        TagList tags = new()
        {
            { "db.operation", operation },
            { "db.system", "redis" },
            { "db.redis.status", exception?.GetType()?.Name ?? "ok" },
        };

        AddTagIfNotNull(ref tags, "cache.hit", hit);
        AddTagIfNotNull(ref tags, "net.peer.name", _host);
        AddTagIfNotNull(ref tags, "net.sock.peer.addr", _address);
        AddTagIfNotNull(ref tags, "net.peer.port", _port);

        _operationDuration.Record(duration.ElapsedMilliseconds, tags);
    }

    private static void AddTagIfNotNull(ref TagList tags, string key, object? value)
    {
        if (value != null)
        {
            tags.Add(key, value);
        }
    }

    private static bool TryParseEndpoint(EndPointCollection? endpoints, out string? host, out int? port, out string? address, out string? networkFamily)
    {
        host = null;
        port = null;
        address = null;
        networkFamily = null;

        var first = endpoints?.FirstOrDefault();

        if (first is DnsEndPoint dnsEndpoint)
        {
            host = dnsEndpoint.Host;
            port = dnsEndpoint.Port == DefaultPort ? null : dnsEndpoint.Port;
            return true;
        }
        else if (first is UnixDomainSocketEndPoint)
        {
            address = first.ToString();
            networkFamily = "unix";
            return true;
        }
        else if (first is IPEndPoint ipEndpoint)
        {
            address = ipEndpoint.Address.ToString();
            port = ipEndpoint.Port == DefaultPort ? null : ipEndpoint.Port;
            // "inet" is default family, no need to set it
            networkFamily = ipEndpoint.AddressFamily == AddressFamily.InterNetworkV6 ? "inet6" : null;
            return true;
        }

        return false;
    }
}
