namespace Consul.Client2.Consul
{
    public static class ConsulHelper
    {
        public static async void ConsulRegist(this IConfiguration configuration)
        {
            try
            {
                string ip = "localhost";
                int port = int.Parse(configuration["port"]);
                int weight = string.IsNullOrWhiteSpace(configuration["weight"]) ? 1 : int.Parse(configuration["weight"]);

                using (IConsulClient client = new ConsulClient(c =>
                {
                    c.Address = new Uri("http://localhost:8500/");
                    c.Datacenter = "dc1";

                }))
                {
                    await client.Agent.ServiceRegister(new AgentServiceRegistration()
                    {
                        ID = $"app-{port}",//独一无二
                        Name = "AppService",//分组
                        Address = ip,
                        Port = port,
                        Tags = new string[] { weight.ToString() },
                        Check = new AgentServiceCheck()
                        {
                            Interval = TimeSpan.FromSeconds(12),
                            HTTP = $"http://{ip}:{port}/app/Health/Index",
                            Timeout = TimeSpan.FromSeconds(5),
                            DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(20)
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Consul注册失败！{e}");
            }

        }
    }
}
