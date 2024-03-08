using Basics.Grpc;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;

namespace GrpcClient1
{
    internal class Program
    {
        static readonly IServiceCollection Service = new ServiceCollection();
        static readonly IServiceProvider ServiceProvider;

        static Program()
        {
            Service.AddHttpClient("grpc").ConfigurePrimaryHttpMessageHandler(() => new GrpcHandler());
            Service.AddGrpcChannel();
            ServiceProvider = Service.BuildServiceProvider();
        }


        static async Task Main(string[] args)
        {
            var channel = ServiceProvider.GetService<GrpcChannel>();
            //foreach (var item in Enumerable.Range(0, 10))
            //{
            //    var orderClient = new Order.OrderClient(channel);
            //    var helloReply = orderClient.Pay(new OrderParam()
            //    {
            //        OrderId = "6666"
            //    });
            //    Console.WriteLine(helloReply);
            //}

            while (true)
            {
                Console.ReadKey();
                var greeterClient = new Greeter.GreeterClient(channel);
                var sayHello = greeterClient.SayHello(new HelloRequest()
                {
                    Name = "Daily"
                });
                Console.WriteLine(sayHello);
                var httpClientFactory = ServiceProvider.GetService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("nossl");
                var httpResponseMessage = await httpClient.GetAsync("https://localhost:7212/basics/Home/Index");
                var readAsStringAsync = await httpResponseMessage.Content.ReadAsStringAsync();
                Console.WriteLine(readAsStringAsync);
            }
        }
    }
}


class GrpcHandler : HttpClientHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        //暂时忽略SSL
        ServerCertificateCustomValidationCallback ??= (message, cert, chain, error) => true;
        var router = request.RequestUri!.AbsolutePath;
        var url = request.RequestUri.ToString().Replace(router, "");
        var baseRouterIndex = router.IndexOf(".", StringComparison.Ordinal);
        var baseRouter = router.Substring(0, baseRouterIndex);
        //重写路由 兼容Gateway
        var newRouter = $"{baseRouter}{router}";
        request.RequestUri = new Uri(url + newRouter);
        return base.SendAsync(request, cancellationToken);
    }
}

static class Extension
{
    public static IServiceCollection AddGrpcChannel(this IServiceCollection service)
    {
        service.AddSingleton(provider =>
        {
            Console.WriteLine("初始化....");
            const string url = "https://localhost:7212";

            GrpcChannelOptions GetGrpcChannelOptions()
            {
                var httpClientFactory = provider.GetService<IHttpClientFactory>();
                var httpClient = httpClientFactory!.CreateClient("grpc");
                var grpcChannelOptions = new GrpcChannelOptions()
                {
                    HttpClient = httpClient,
                };
                return grpcChannelOptions;
            }

            return GrpcChannel.ForAddress(url, GetGrpcChannelOptions());
        });
        return service;
    }
}