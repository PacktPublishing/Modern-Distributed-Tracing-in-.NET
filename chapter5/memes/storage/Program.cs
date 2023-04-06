using storage;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var mySqlConnectionString = builder.Configuration.GetConnectionString("MySql");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 30));

builder.Services.AddDbContext<MemeDbContext>(options => options
    .UseMySql(mySqlConnectionString, serverVersion, options => options.EnableRetryOnFailure()));

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

app.MapControllers();

app.Run();

static void ConfigureTelemetry(WebApplicationBuilder builder)
{
    var resourceBuilder = ResourceBuilder.CreateDefault()
        .AddService("storage", "memes", "1.0.0");
    builder.Services.AddOpenTelemetry()
       .WithTracing(buidler => buidler
            .SetResourceBuilder(resourceBuilder)
            .AddProcessor<MemeNameEnrichingProcessor>()
            .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(1)))
            .AddAspNetCoreInstrumentation(o =>
            {
                o.EnrichWithHttpRequest = (activity, request) =>
                    activity.SetTag("http.request_content_length", request.ContentLength);

                o.EnrichWithHttpResponse = (activity, response) =>
                    activity.SetTag("http.response_content_length", response.ContentLength);

                o.RecordException = true;
            })
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter())
        .WithMetrics(buidler => buidler
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter());
}