using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Domain.Interfaces;
using MediatR;
using Domain.Entities.Clientes;

namespace Application.UseCases.Clientes.Get;

public sealed class GetClientHandler : IRequestHandler<GetClientQuery, Result<GetClientDto>>
{
    private readonly IEventStoreRepository<Cliente> _eventStore;

    public GetClientHandler(IEventStoreRepository<Cliente> eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<Result<GetClientDto>> Handle(GetClientQuery request, CancellationToken cancellationToken)
    {
        var cliente = await _eventStore.LoadAsync(request.ClienteId, cancellationToken: cancellationToken);

        if (cliente is null)
            return Result.Fail<GetClientDto>("Cliente não encontrado.");

        return Result.Ok(cliente.ToDto());
    }
}
