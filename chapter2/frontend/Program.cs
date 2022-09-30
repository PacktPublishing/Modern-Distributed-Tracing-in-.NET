using frontend;
using common;

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
builder.Services.AddTransient<ErrorScenariosMiddleware>();

ConfigureTelemetry(builder);

var app = builder.Build();

app.UseStatusCodePagesWithRedirects("/errors/{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseMiddleware<ErrorScenariosMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();

static void ConfigureTelemetry(WebApplicationBuilder builder)
{
    var collectorEndpoint = builder.Configuration.GetSection("OtelCollector")?
            .GetValue<string>("Endpoint");

    // if there no collector endpoint, we won't set up OpenTelemetry, but will configure log correlation using ActivityTrackingOptions
    if (collectorEndpoint != null)
    {
        builder.Logging.AddOpenTelemetry(options => options.ConfigureLogs(collectorEndpoint));

        builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder => 
                        tracerProviderBuilder.ConfigureTracing(collectorEndpoint));

        builder.Services.AddOpenTelemetryMetrics(meterProviderBuilder => 
                        meterProviderBuilder.ConfigureMetrics(collectorEndpoint));
    }
    else
    {
        // log correaltion is useful if you don't capture logs with OpenTelemetry
        builder.Logging.Configure(options => options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);
    }
}