using Brownfield.OpenTelemetry.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var compatibilityOptions = new CompatibilityOptions();
builder.Configuration.GetSection("Compatibility").Bind(compatibilityOptions);

builder.ConfigureTelemetry(compatibilityOptions);

var app = builder.Build();

app.MapControllers();

app.Run();
