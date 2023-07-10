using Daily.Carp.Extension;

namespace GrpcGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args).InjectCarp();

            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.AddCarp().AddNormal();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.UseCarp();

            app.Run("https://*:7212");
        }
    }
}