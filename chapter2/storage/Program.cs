using common;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Logging.ConfigureLogs();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
