using Daily.Carp;
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args).InjectCarp();  //×¢ÈëÅäÖÃ

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCarp().AddNormal();  

var app = builder.Build();

app.UseAuthorization();

app.UseCarp(options =>
{
    options.EnableAuthentication = true;
});

app.MapControllers();

app.Run();
