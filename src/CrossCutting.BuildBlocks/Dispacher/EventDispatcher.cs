using BuildingBlocks.Core.Event;
using BuildingBlocks.Core.Model;
using Domain.BuildingBlocks.Models;
using MassTransit.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.BuildingBlocks.Dispacher;

public interface IEventDispatcherRepository
{
    Task DispatchAsync(
        AggregateRoot aggregate,
        CancellationToken cancellationToken = default);
}


