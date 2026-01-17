using System;
using FluentResults;
using MediatR;

namespace Application.UseCases.Clientes.Create;

public sealed record CreateClientCommand(
    string Nome,
    Guid? RequestId = null)
    : IRequest<Result<CreateClientResult>>;
