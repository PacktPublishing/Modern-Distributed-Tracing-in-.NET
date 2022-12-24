using Azure.Monitor.OpenTelemetry.Exporter;
using frontend;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var storageEndpoint = builder.Configuration.GetSection("Storage").GetValue<string>("Endpoint");
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
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

app.Run();

static void ConfigureTelemetry(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracerProviderBuilder => tracerProviderBuilder
            .AddAzureMonitorTraceExporter(options => options.ConnectionString = connectionString)
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation())
        .WithMetrics(meterProviderBuilder => meterProviderBuilder
            .AddAzureMonitorMetricExporter(options => options.ConnectionString = connectionString)
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation())
        .StartWithHost();
        
    builder.Logging.AddOpenTelemetry(options => options
        .AddAzureMonitorLogExporter(options => options.ConnectionString = connectionString));
}