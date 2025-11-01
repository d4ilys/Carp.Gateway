using Daily.Carp;
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCarp(options =>
{
    options.CarpConfigDelegate = provider =>
    {
        var config = provider.GetRequiredService<IConfiguration>();
        var carpConfig = config.GetSection("Carps").Get<CarpConfig>();
        return carpConfig;
    };
});

var app = builder.Build();

app.UseAuthorization();

app.UseCarp();

app.MapControllers();

app.Run();