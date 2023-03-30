using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args).InjectCarp();  //◊¢»Î≈‰÷√

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCarp().AddNormal();  

var app = builder.Build();

app.UseAuthorization();

app.UseCarp();

app.MapControllers();

app.Run();
