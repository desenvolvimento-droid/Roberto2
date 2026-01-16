using Domain.Entities.Clientes;
using EventDriven.Marten.Exemple.Infra.Repository;
using Infra.Models;
using MediatR;

namespace Application.DomainEventHandlers.Cliente;

public class CriarClienteReadModelHandler(IReadModelRepository<ClienteReadModel> readModelRepository) 
    : INotificationHandler<ClienteCriadoEvent>
{
    public async Task Handle(ClienteCriadoEvent notification, CancellationToken cancellationToken)
    {
        var rm = await readModelRepository.GetByIdAsync(notification.Id, cancellationToken);
        if (rm != null)
        {
            return;
        }

        rm = new ClienteReadModel(
            notification.Id,
            notification.Nome,
            notification.OcorreuEm,
            notification.OcorreuEm);

        await readModelRepository.UpsertAsync(rm, cancellationToken);
    }
}
