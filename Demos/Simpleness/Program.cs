using System.Text;
using Daily.Carp;
using Daily.Carp.Extension;
using Microsoft.AspNetCore.Http.Extensions;
using Simpleness;

var builder = WebApplication.CreateBuilder(args).InjectCarp(); //◊¢»Î≈‰÷√

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCarp().AddNormal();

var app = builder.Build();

app.UseFailover();

app.UseAuthorization();

app.UseCarp();

app.MapControllers();

app.Run();