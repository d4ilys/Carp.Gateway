using System.Net;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using GrpcService1;

namespace GrpcClient1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string url = "https://localhost:7212"; //https
            using var channel = GrpcChannel.ForAddress(url);
            var client = new Greeter.GreeterClient(channel);
            var reply = client.SayHello(new HelloRequest()
            {
                Name = "Daily"
            });

            Console.WriteLine($"Message:{reply.Message}");
            Console.ReadKey();
        }
    }
}