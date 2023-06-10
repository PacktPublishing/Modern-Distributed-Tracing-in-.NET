using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq.Expressions;

namespace database;

public class DatabaseService
{
    private const string CreateOperation = "InsertOneAsync";
    private const string UpdateOperation = "ReplaceOneAsync";
    private const string GetOperation = "FindSingleOrDefault";
    private const string FindOperation = "Find";
    private const string BulkWriteOperation = "BulkWrite";

    private static readonly ActivitySource MongoSource = new("MongoDb");
    private static readonly Meter MongoMeter = new("MongoDb");
    private readonly Histogram<double> _operationDuration;
    private readonly IMongoCollection<Record> _records;

    private readonly string _host;
    private readonly int _port;
    private readonly string _dbName;
    private readonly string _collectionName;
    public DatabaseService(IOptions<MongoDbSettings> settings) {
        _ = settings.Value.ConnectionString ?? throw new ArgumentException("MongoDB ConnectionString cannot be null");
        _ = settings.Value.Database ?? throw new ArgumentException("MongoDB Database cannot be null");
        _ = settings.Value.Collection ?? throw new ArgumentException("MongoDB Collection cannot be null");
        var client = new MongoClient(settings.Value.ConnectionString);
        _host = client.Settings.Server.Host;
        _port = client.Settings.Server.Port;
        _dbName = settings.Value.Database;
        _collectionName = settings.Value.Collection;
        var database = client.GetDatabase(_dbName);
        _records = database.GetCollection<Record>(_collectionName);

        _operationDuration = MongoMeter.CreateHistogram<double>("db.operation.duration", "ms", "Database call duration");
    }

    private Activity? StartMongoActivity(string operation)
    {
        var act = MongoSource.StartActivity($"{operation} {_dbName}.{_collectionName}", ActivityKind.Client);
        if (act?.IsAllDataRequested != true) return act;

        return act.SetTag("db.system", "mongodb")
            .SetTag("db.name", _dbName)
            .SetTag("db.mongodb.collection", _collectionName)
            .SetTag("db.operation", operation)
            .SetTag("net.peer.name", _host)
            .SetTag("net.peer.port", _port);
    }

    public async Task Create(Record record)
    {
        var start = _operationDuration.Enabled ? Stopwatch.StartNew() : null;
        
        using var act = StartMongoActivity(CreateOperation);

        try
        {
            await _records.InsertOneAsync(record);
            TrackDuration(start, CreateOperation);
        }
        catch (Exception ex)
        {
            act?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            act?.RecordException(ex);
            TrackDuration(start, CreateOperation, ex);
            throw;
        }
    }

    public async Task<Record?> Update(string id, Record record)
    {
        var start = _operationDuration.Enabled ? Stopwatch.StartNew() : null;
        using var act = StartMongoActivity(UpdateOperation);

        try
        {
            var result = await _records.ReplaceOneAsync(x => x.Id == id, record);
            TrackDuration(start, UpdateOperation);
            return result.ModifiedCount == 1 ? record : null;
        }
        catch (Exception ex)
        {
            act?.RecordException(ex);
            act?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            TrackDuration(start, UpdateOperation, ex);
            throw;
        }
    }

    public async Task<Record?> Get(string id)
    {
        var start = _operationDuration.Enabled ? Stopwatch.StartNew() : null;
        using var act = StartMongoActivity(GetOperation);

        try
        {
            var rec = await _records.Find(r => r.Id == id).SingleOrDefaultAsync();
            TrackDuration(start, GetOperation);
            return rec;
        }
        catch (Exception ex)
        {
            act?.RecordException(ex);
            act?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            TrackDuration(start, GetOperation, ex);
            throw;
        }
    }

    public async Task<List<Record>> Get(Expression<Func<Record, bool>> filter)
    {
        var start = _operationDuration.Enabled ? Stopwatch.StartNew() : null;
        using var act = StartMongoActivity(FindOperation);

        try
        {
            var records = await _records.Find(filter).ToListAsync();
            TrackDuration(start, FindOperation);
            return records;
        }
        catch (Exception ex)
        {
            act?.RecordException(ex);
            act?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            TrackDuration(start, FindOperation, ex);

            throw;
        }
    }

    public async Task BulkCreate(List<Record> records)
    {
        var start = _operationDuration.Enabled ? Stopwatch.StartNew() : null;
        using var act = StartMongoActivity(BulkWriteOperation);

        try
        {
            var requests = records.Select(r => new InsertOneModel<Record>(r));

            AddBulkAttributes(requests, act);
            var result = await _records.BulkWriteAsync(requests);
            TrackDuration(start, BulkWriteOperation);
        }
        catch (Exception ex)
        {
            act?.RecordException(ex);
            act?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            TrackDuration(start, BulkWriteOperation, ex);

            throw;
        }
    }

    private static void AddBulkAttributes<T>(IEnumerable<WriteModel<T>> requests, Activity? act)
    {
        if (act?.IsAllDataRequested == true)
        {
            act.SetTag("db.mongodb.bulk_operations", requests.Select(r => r.ModelType).ToArray());
        }
    }

    private void TrackDuration(Stopwatch? start, string operation, Exception? ex = null)
    {
        if (start == null) return;

        string status = ex?.GetType()?.Name ?? "ok";
        _operationDuration.Record(start.ElapsedMilliseconds, new TagList()
        {
            { "db.system", "mongodb" },
            { "db.name", _dbName },
            { "db.mongodb.collection", _collectionName },
            { "db.operation", operation },
            { "db.mongodb.status", status },
            { "net.peer.name", _host },
            { "net.peer.port", _port },
        });
    }
}