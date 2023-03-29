using Daily.Carp.Extension;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args).InjectCarp();

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCarp().AddKubernetes();

var app = builder.Build();

app.UseAuthorization();

app.UseCarp();

app.MapControllers();

app.Run("http://*:6005");
