using Common.Util;
using Domain.BuildingBlocks.Models;
using Infra.Repositories.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Reflection;

namespace Infra.Repositories.Extensions;

public static class EventStoreExtensions
{
    public static async Task InitializeAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var database = scope.ServiceProvider
            .GetRequiredService<IMongoDatabase>();

        await MongoEventStoreUtil
            .EnsureAsync(database, cancellationToken);
    }

    public static IServiceCollection AddProjectionsFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        //services.Scan(scan =>
        //{
        //    scan.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
        //        .AddClasses(c => c.AssignableTo(typeof(IProjection)))
        //        .AsImplementedInterfaces()
        //        .WithScopedLifetime();
        //});

        var projectionTypes = assembly
            .GetTypes()
            .Where(t =>
                !t.IsAbstract &&
                !t.IsInterface &&
                IsProjection(t))
            .ToList();

        foreach (var implementationType in projectionTypes)
        {
            var interfaces = implementationType
                .GetInterfaces()
                .Where(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IProjection<>));

            foreach (var @interface in interfaces)
            {
                services.AddScoped(@interface, implementationType);
            }
        }

        return services;
    }

    private static bool IsProjection(Type type)
    {
        while (type != null && type != typeof(object))
        {
            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Projection<>))
            {
                return true;
            }

            type = type.BaseType!;
        }

        return false;
    }
}

