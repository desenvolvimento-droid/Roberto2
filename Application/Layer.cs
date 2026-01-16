using Microsoft.Extensions.DependencyInjection;

namespace Infra.ReadModel.Mongo;

public static class Layer
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(typeof(Layer).Assembly);
        });

        return services;
    }
}
