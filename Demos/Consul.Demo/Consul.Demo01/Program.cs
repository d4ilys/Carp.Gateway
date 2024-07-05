using Daily.Carp.Extension;
using Winton.Extensions.Configuration.Consul;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCarp().AddConsul();

var app = builder.Build();

app.UseAuthorization();

app.UseCarp();

app.MapControllers();

app.Run("http://*:8000");
