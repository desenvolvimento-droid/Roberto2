using Domain.BuildingBlocks.Dispacher;
using Domain.Entities.Clientes;
using Domain.Interfaces;
using FluentResults;
using MediatR;

namespace Application.UseCases.Clientes.Criar;

public sealed class CriarClienteHandler(
    IEventStoreRepository<Cliente> eventStorage,
    IEventDispatcherRepository eventDispatcher)
    : IRequestHandler<CriarClienteCommand, Result<CriarClienteResult>>
{
    public async Task<Result<CriarClienteResult>> Handle(CriarClienteCommand cmd, CancellationToken cancellationToken)
    {
        var cliente = Cliente.Create(cmd.Nome);

        await eventStorage.AppendAsync(cliente);

        var result = new CriarClienteResult(
            cliente.Id,
            cliente.CriadoEm);

        await eventDispatcher.DispatchAsync(cliente, cancellationToken);

        return result;
    }
}
