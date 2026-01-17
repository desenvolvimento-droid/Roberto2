using System;

namespace Application.UseCases.Clientes.Get;

public sealed record GetClientDto(Guid ClienteId, string Nome, string Status);
