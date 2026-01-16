using MediatR;

namespace Application.UseCases.Clientes.Criar;

/// <summary>
/// Comando imutável contendo apenas os dados necessários para criar um cliente.
/// </summary>
public sealed record CriarClienteCommand(string Nome) : IRequest<FluentResults.Result<CriarClienteResult>>;

public sealed record CriarClienteResult(Guid Id, DateTime CriadoEm);
