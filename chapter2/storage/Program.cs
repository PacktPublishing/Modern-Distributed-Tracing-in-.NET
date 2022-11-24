using storage;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddControllers();

var mySqlConnectionString = builder.Configuration.GetConnectionString("MySql");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 30));

builder.Services.AddDbContext<MemeDbContext>(options =>
{
    if (mySqlConnectionString != null)
    {
        options.UseMySql(mySqlConnectionString, serverVersion, options => options.EnableRetryOnFailure());
    }
    else
    {
        options.UseInMemoryDatabase("memes");
    }
});

ConfigureTelemetry(builder);

var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MemeDbContext>();
    if (context.Database.EnsureCreated())
    {
        context.Meme.Add(new Meme("dotnet", File.ReadAllBytes("./images/dotnet.png")));
        context.SaveChanges();
    }
}

app.MapControllers();

app.Run();

static void ConfigureTelemetry(WebApplicationBuilder builder)
{
    builder.Services.AddOpenTelemetryTracing(tracerProviderBuilder => tracerProviderBuilder
        .AddJaegerExporter()
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation());

    builder.Services.AddOpenTelemetryMetrics(meterProviderBuilder => meterProviderBuilder
        .AddPrometheusExporter()
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddProcessInstrumentation()
        .AddRuntimeInstrumentation());
}