using System.Net;
using System.Net.Http;
using System.Net.Security;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using GrpcService1;
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
            ServiceProvider = Service.BuildServiceProvider();
        }


        static void Main(string[] args)
        {
            var url = "https://localhost:7212";

            while (true)
            {

                Console.ReadLine();
                using (var channel = GrpcChannel.ForAddress(url, GetGrpcChannelOptions()))
                {
                    var orderClient = new Order.OrderClient(channel);
                    var helloReply = orderClient.Pay(new OrderParam()
                    {
                        OrderId = "6666"
                    });
                    Console.WriteLine(helloReply);
                }

                using (var channel = GrpcChannel.ForAddress(url, GetGrpcChannelOptions()))
                {
                    var greeterClient = new Greeter.GreeterClient(channel);
                    var sayHello = greeterClient.SayHello(new HelloRequest()
                    {
                        Name = "Daily"
                    });
                    Console.WriteLine(sayHello);
                }

            }

            static GrpcChannelOptions GetGrpcChannelOptions()
            {
                var httpClientFactory = ServiceProvider.GetService<IHttpClientFactory>();
                var httpClient = httpClientFactory!.CreateClient("grpc");
                var grpcChannelOptions = new GrpcChannelOptions()
                {
                    HttpClient = httpClient,
                };
                return grpcChannelOptions;
            }
        }
    }


    class GrpcHandler : HttpClientHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ServerCertificateCustomValidationCallback ??= (message, cert, chain, error) => true;
            var router = request.RequestUri!.AbsolutePath;
            var url = request.RequestUri.ToString().Replace(router, "");
            //重写路由 兼容Gateway
            var newRouter = $"/basics{router}";
            request.RequestUri = new Uri(url + newRouter);
            return base.SendAsync(request, cancellationToken);
        }
    }
}