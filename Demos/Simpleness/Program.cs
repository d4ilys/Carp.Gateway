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
    options.CustomAuthenticationAsync.Add("BearerToken", async () =>
    {
        Console.WriteLine("111");
        return await Task.FromResult(false);
    });
    options.CustomAuthenticationAsync.Add("VisaVerification", async () =>
    {
        Console.WriteLine("222");
        return await Task.FromResult(true);
    });
});

app.MapControllers();

app.Run();
