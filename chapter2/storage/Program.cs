using common;
using storage;
using Microsoft.EntityFrameworkCore;


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
        options.UseInMemoryDatabase("memes");
    });

builder.Logging.ConfigureLogs();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MemeDbContext>();
    context.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();
