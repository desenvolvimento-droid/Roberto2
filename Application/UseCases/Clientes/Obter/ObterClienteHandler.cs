using EventDriven.Marten.Exemple.Infra.Repository;
using FluentResults;
using Infra.Models;
using MediatR;

namespace Application.UseCases.Clientes.Obter;

public sealed class ObterClienteHandler(IReadModelRepository<ClienteReadModel> readModelRepository) : IRequestHandler<ObterClienteQuery, Result<ObterClienteResult>>
{
    public Task<Result<ObterClienteResult>> Handle(ObterClienteQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
