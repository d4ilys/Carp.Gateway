using Daily.Carp.Extension;

namespace WebSocket.Demo01
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddCarp();

            var app = builder.Build();

            app.UseAuthorization();

            app.UseCarp();

            app.MapControllers();

            app.Run("http://*:5031");
        }
    }
}