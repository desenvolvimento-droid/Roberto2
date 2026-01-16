using FluentResults;
using MediatR;

namespace Application.UseCases.Clientes.Obter;

public record ObterClienteQuery(Guid Id) : IRequest<Result<ObterClienteResult>>;

public record ObterClienteResult(
    Guid Id,
    string Nome,
    DateTime CriadoEm,
    DateTime AtualizadoEm);