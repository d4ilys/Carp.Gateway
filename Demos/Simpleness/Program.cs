using Daily.Carp;
using Daily.Carp.Extension;

var builder = WebApplication.CreateBuilder(args).InjectCarp();  //×¢ÈëÅäÖÃ

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddCarp().AddNormal();  

var app = builder.Build();

app.UseAuthorization();

app.UseCarp();

app.MapControllers();

Task.Run(() =>
{
    while (true)
    {
        Console.ReadKey();
        var addressByServiceName = CarpApp.GetAddressByServiceName("file");
        Console.WriteLine(addressByServiceName);
    }
});

app.Run();
