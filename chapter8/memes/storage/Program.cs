using storage;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<MemeDbContext>(options => options.UseInMemoryDatabase("memes"));

var resource = ResourceBuilder.CreateDefault()
    .AddAttributes(new[] { new KeyValuePair<string, object>("service.instance.id", Environment.MachineName) });

builder.Services.AddOpenTelemetry()
    .WithTracing(b => b
        .SetResourceBuilder(resource)
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter());

builder.Logging
    .AddOpenTelemetry(b => {
        b.SetResourceBuilder(resource);
        b.ParseStateValues = true;
        b.IncludeScopes = true;
        b.AddOtlpExporter();
    });

var app = builder.Build();

app.MapControllers();

app.Use((ctx, next) => (DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 3 == 0) ? throw new Exception("bad luck") : next.Invoke());
app.Run();

