using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Text.Json;

namespace database.Controllers;

[ApiController]
[Route("[controller]")]
public class RecordsController : ControllerBase
{
    private static readonly ActivitySource Source = new("Records");
    private readonly DatabaseService _database;
    private readonly CacheService _cache;

    public RecordsController(DatabaseService database, CacheService cache)
    {
        _database = database;
        _cache = cache;
    }

    [HttpGet]
    public async Task<ActionResult<List<Record>>> Get()
    {
        using var getAllActivity = Source.StartActivity("GetRecords");
        try
        {
            var records = await _database.Get(_ => true);
            if (getAllActivity?.IsAllDataRequested == true)
            {
                var ids = records.Select(static r => r.Id).ToArray();
                getAllActivity?.SetTag("app.record.count", ids.Length);
                getAllActivity?.SetTag("app.record.ids", ids);

                if (records.Count == 0)
                {
                    getAllActivity?.SetStatus(ActivityStatusCode.Error, "not found");
                    return NotFound();
                }
            }

            return records;
        }
        catch (Exception ex)
        {
            getAllActivity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            throw;
        }
    }

    [HttpGet("{id:length(24)}")]
    [Produces("application/json")]
    public async Task<ActionResult<string>> Get(string id)
    {
        // if there are other operations not related to retrieving record, '
        // GetRecord activity should not capture them
        using var act = Source.StartActivity("GetRecord");
        act?.SetTag("app.record.id", id);

        try
        {
            var recordStr = await _cache.GetRecord(id);
            if (recordStr != null)
                return recordStr;

            act?.SetTag("cache.hit", false);
            var record = await _database.Get(id);
            if (record != null) 
                return await Cache(record);
        }
        catch (Exception ex)
        {
            act?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            throw;
        }

        act?.SetStatus(ActivityStatusCode.Error, "not found");
        return NotFound();

    }

    private async Task<string> Cache(Record record)
    {
        var recordStr = JsonSerializer.Serialize(record);
        await _cache.CacheRecord(record.Id!, recordStr).FireAndForget();
        return recordStr;
    }

    [HttpPut("{id:length(24)}")]
    public async Task<IActionResult> Update(string id, Record record)
    {
        ValidateRecord(record);

        // if there are other operations not related to writing record,
        // UpdateRecord activity should not capture them
        using var act = Source.StartActivity("UpdateRecord");
        act?.SetTag("app.record.id", id);

        try
        {
            var updated = await _database.Update(id, record);
            if (updated == null)
            {
                act?.SetStatus(ActivityStatusCode.Error, "not found");
                return NotFound();
            }

            await _cache.CacheRecord(id, JsonSerializer.Serialize(updated)).FireAndForget();
            return NoContent();
        }
        catch (Exception ex)
        {
            act?.RecordException(ex);
            act?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            throw;
        }
    }

    [HttpPost]
    [Produces("application/json")]
    public async Task<ActionResult<IEnumerable<string>>> Create(List<Record> records)
    {
        foreach (var record in records)
        {
            ValidateRecord(record);
        }
        using var bulkCreateActivity = Source.StartActivity("CreateRecords");
        try
        { 
            if (bulkCreateActivity?.IsAllDataRequested == true)
            {
                var ids = records.Select(static r => r.Id).ToArray();
                bulkCreateActivity?.SetTag("app.record.count", ids.Length);
                bulkCreateActivity?.SetTag("app.record.ids", ids);
            }

            await _database.BulkCreate(records);
            
            await records.Select(r => _cache.CacheRecord(r.Id!, JsonSerializer.Serialize(r)))
                .FireAndForget();

            return Created(Request.GetDisplayUrl(), records.Select(r => r.Id!));
        }
        catch (Exception ex)
        {
            bulkCreateActivity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            throw;
        }
    }

    private static void ValidateRecord(Record record, string? id = null)
    {
        if (record.Id == null)
        {
            record.Id = id ?? Guid.NewGuid().ToString("n")[8..];
        }
        else if (record.Id != id)
        {
            throw new ArgumentException($"record.Id '{record.Id}' does not match passed id - '{id}'");
        }
    }
}
