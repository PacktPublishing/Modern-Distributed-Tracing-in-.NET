using common;
using storage;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;

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

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MemeDbContext>();
    if (context.Database.EnsureCreated())
    {
        context.Meme.Add(new Meme("dotnet", File.ReadAllBytes("./images/dotnet.png")));
        context.SaveChanges();
    }
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

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
                        tracerProviderBuilder
                            .ConfigureTracing(collectorEndpoint)
                            .AddEntityFrameworkCoreInstrumentation(o =>
                            {
                                o.SetDbStatementForText = true;
                                o.SetDbStatementForText = true;
                            }));

        builder.Services.AddOpenTelemetryMetrics(meterProviderBuilder =>
                        meterProviderBuilder.ConfigureMetrics(collectorEndpoint));
    } 
    else
    {
        // log correaltion is useful if you don't capture logs with OpenTelemetry
        builder.Logging.Configure(options => options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);
    }
}