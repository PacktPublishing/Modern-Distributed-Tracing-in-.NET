using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using sampling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient("dummy", c => c.BaseAddress = new Uri("http://localhost:5051"));

builder.Services.AddOpenTelemetry()
    .WithTracing(tp => tp
        .SetSampler(new DebugSampler(0.1))
        .AddOtlpExporter()
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation());
    

var app = builder.Build();
app.MapControllers();
app.Run();

void ConfigureProbabilitySampler(WebApplicationBuilder builder)
{
    builder.Services.AddOpenTelemetry()
        .WithTracing(tp => tp
            .SetSampler(new TraceIdRatioBasedSampler(0.1))
            .AddOtlpExporter());
}

void ConfigureParentBasedSampler(WebApplicationBuilder builder)
{
    builder.Services.AddOpenTelemetry()
        .WithTracing(tp => tp
            .SetSampler(new ParentBasedSampler(new AlwaysOffSampler()))
            .AddOtlpExporter());
}

