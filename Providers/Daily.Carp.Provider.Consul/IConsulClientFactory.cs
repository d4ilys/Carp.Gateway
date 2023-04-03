namespace Daily.Carp.Provider.Consul
{
    using global::Consul;

    public interface IConsulClientFactory
    {
        ConsulRegistryConfiguration Config { get; set; }
        IConsulClient Get();
    }
}