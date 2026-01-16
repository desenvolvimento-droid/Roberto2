using BuildingBlocks.Core.Event;
using Domain.BuildingBlocks.Models;
using MediatR;

namespace CrossCutting.BuildBlocks.Behaviors;

public class DomainEventProjectionBehavior<TDomainEvent>(
        Projection<TDomainEvent> projection) 
    : INotificationHandler<TDomainEvent>
    where TDomainEvent : class, IDomainEvent, INotification
{
    public async Task Handle(TDomainEvent notification, CancellationToken cancellationToken)
        => await projection.ProjectAsync(notification, cancellationToken);
    
}