using frontend;
using OpenTelemetry;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var storageEndpoint = builder.Configuration.GetSection("Storage").GetValue<string>("Endpoint") ?? "http://localhost:5050";
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
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();

app.Run();

static void ConfigureTelemetry(WebApplicationBuilder builder)
{
    Sdk.SetDefaultTextMapPropagator(new AWSXRayPropagator());
    
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracerProviderBuilder => 
            tracerProviderBuilder
                .AddXRayTraceId()
                .AddOtlpExporter()
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation())
        .StartWithHost();
}