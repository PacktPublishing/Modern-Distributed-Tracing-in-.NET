using app;
using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using testing.Controllers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<IMemoryCache, MemoryCache>();
builder.Services.AddSingleton<IStorageService, LocalStorage>();

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .ConfigureResource(resource => resource.AddService("testing-sample"))
            .AddJaegerExporter()
            .AddAspNetCoreInstrumentation()
            .AddSource(nameof(DocumentController));
    })
    .StartWithHost();


var app = builder.Build();

app.MapControllers();

app.Run();

public partial class Program {
}