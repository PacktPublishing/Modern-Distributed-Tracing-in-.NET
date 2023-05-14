using storage;
using Microsoft.EntityFrameworkCore;
using Memes.OpenTelemetry.Common;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<EventService>();

var mySqlConnectionString = builder.Configuration.GetConnectionString("MySql");
var serverVersion = new MySqlServerVersion(new Version(8, 0, 30));

builder.Services.AddDbContext<MemeDbContext>(options => options
    .UseMySql(mySqlConnectionString, serverVersion, options => options.EnableRetryOnFailure()));

var config = new MemesTelemetryConfiguration();
builder.Configuration.GetSection("Telemetry").Bind(config);
config.ConfigureTracing = o => o.AddEntityFrameworkCoreInstrumentation();

builder.ConfigureTelemetry(config);

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