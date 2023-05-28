using Brownfield.OpenTelemetry.Common;

var builder = WebApplication.CreateBuilder(args);

var serviceBConfig = builder.Configuration.GetSection("ServiceB");
var bEndpoint = serviceBConfig?.GetValue<string>("Endpoint") ?? "http://localhost:5050";
builder.Services.AddHttpClient("b", httpClient =>
{
    httpClient.BaseAddress = new Uri(bEndpoint);
});

builder.Services.AddControllers();

var compatibilityOptions = new CompatibilityOptions();
builder.Configuration.GetSection("compatibility").Bind(compatibilityOptions);

builder.ConfigureTelemetry(compatibilityOptions);

var app = builder.Build();
app.MapControllers();
app.Run();
