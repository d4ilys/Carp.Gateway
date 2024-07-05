using System.Text;
using Daily.Carp;
using Daily.Carp.Extension;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Primitives;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCarp();

var app = builder.Build();

app.UseAuthorization();

app.UseCarp();

app.MapControllers();

app.Run();