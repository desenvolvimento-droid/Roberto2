using CrossCutting.BuildBlocks.Repositories;
using Infra.Persistence.Mongo.Outbox;
using Infra.Services.Batch.OutboxMessage;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infra.Outbox.Mongo;

public static class Layer
{
    public static IServiceCollection AddOutBoxComMongo(this IServiceCollection services)
    {
        services.AddScoped<IBatchRepository<BatchModel>, BatchRepository>();

        return services;
    }
}
