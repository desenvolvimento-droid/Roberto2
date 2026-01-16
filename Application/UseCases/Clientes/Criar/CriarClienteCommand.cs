using FluentResults;
using MediatR;

namespace Application.UseCases.Clientes.Criar;

public record CriarClienteCommand(string Nome) : IRequest<Result<CriarClienteResult>>;

public record CriarClienteResult(Guid Id, DateTime CriadoEm);