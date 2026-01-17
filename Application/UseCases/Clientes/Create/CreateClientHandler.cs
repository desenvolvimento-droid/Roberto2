using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Domain.Interfaces;
using MediatR;
using Domain.Entities.Clientes;

namespace Application.UseCases.Clientes.Create;

public sealed class CreateClientHandler : IRequestHandler<CreateClientCommand, Result<CreateClientResult>>
{
    private readonly IEventStoreRepository<Cliente> _eventStore;

    public CreateClientHandler(IEventStoreRepository<Cliente> eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<Result<CreateClientResult>> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        // validation is expected to run via pipeline; keep handler simple
        var cliente = request.ToAggregate();

        // append aggregate events
        await _eventStore.AppendAsync(cliente, cancellationToken);

        var id = cliente.Id;

        return Result.Ok(new CreateClientResult(id, cliente.Nome));
    }
}
