using frontend;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var storageConfig = builder.Configuration.GetSection("Storage");
var storageEndpoint = storageConfig?.GetValue<string>("Endpoint") ?? "http://localhost:5050";
builder.Services.AddHttpClient("storage", httpClient =>
{
    httpClient.BaseAddress = new Uri(storageEndpoint);
});

builder.Services.AddSingleton<StorageService>();

ConfigureTelemetry(builder);

var app = builder.Build();

app.UseStatusCodePagesWithRedirects("/errors/{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

app.Run();

static void ConfigureTelemetry(WebApplicationBuilder builder)
{
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracerProviderBuilder => tracerProviderBuilder
            .AddOtlpExporter()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation())
        .WithMetrics(meterProviderBuilder => meterProviderBuilder
            .AddOtlpExporter()
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation())
        .StartWithHost();
        
    builder.Logging.AddOpenTelemetry(options =>
        options.AddOtlpExporter());
}