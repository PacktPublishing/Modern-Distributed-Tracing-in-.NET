using frontend;
using Memes.OpenTelemetry.Common;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var storageConfig = builder.Configuration.GetSection("Storage");
var storageEndpoint = storageConfig?.GetValue<string>("Endpoint") ?? "http://localhost:5050";
builder.Services.AddHttpClient("storage", httpClient =>
{
    httpClient.BaseAddress = new Uri(storageEndpoint);
    httpClient.Timeout = TimeSpan.FromSeconds(10);
})
.AddHttpMessageHandler<RetryHandler>();

builder.Services.AddTransient<RetryHandler>();
builder.Services.AddSingleton<EventService>();
builder.Services.AddSingleton<StorageService>();

var telemetryOptions = new MemesTelemetryConfiguration();
builder.Configuration.GetSection("Telemetry").Bind(telemetryOptions);
telemetryOptions.AspNetCoreRequestFilter = ctx => !IsStaticFile(ctx.Request.Path);
builder.ConfigureTelemetry(telemetryOptions);

var app = builder.Build();

app.UseStatusCodePagesWithRedirects("/errors/{0}");
app.UseExceptionHandler("/Error");

app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

app.Run();

static bool IsStaticFile(PathString requestPath)
{
    return requestPath.HasValue && requestPath.HasValue &&
        (requestPath.Value.EndsWith(".js") || 
         requestPath.Value.EndsWith(".css"));
}
