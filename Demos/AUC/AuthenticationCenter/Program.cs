
using AuthenticationCenter.DataInit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityServer()
    .AddDeveloperSigningCredential() //这仅适用于没有证书可以使用的开发场景。
    .AddInMemoryApiScopes(Config.ApiScopes)
    .AddInMemoryClients(Config.Clients)
    .AddInMemoryIdentityResources(Config.IdentityResources);

var app = builder.Build();

app.UseIdentityServer();
app.MapGet("/", () => "Hello World!");

app.Run();