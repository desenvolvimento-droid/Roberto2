using BuildingBlocks.Core.Event;
using BuildingBlocks.Core.Model;
using MediatR;
using System.Collections.Concurrent;

namespace Domain.BuildingBlocks.Models;

public interface IProjection<TDomainEvent> 
    where TDomainEvent : IDomainEvent
{
    Task ProjectAsync(
        TDomainEvent domainEvent, 
        CancellationToken cancellationToken);
}

public abstract class Projection<TDoaminEvent> : IProjection<TDoaminEvent>
    where TDoaminEvent : IDomainEvent
{
    public abstract Task ProjectAsync(
        TDoaminEvent domainEvent,
        CancellationToken cancellationToken);
}
