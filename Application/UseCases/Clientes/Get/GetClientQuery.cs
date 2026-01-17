using System;
using FluentResults;
using MediatR;

namespace Application.UseCases.Clientes.Get;

public sealed record GetClientQuery(Guid ClienteId) : IRequest<Result<GetClientDto>>;
