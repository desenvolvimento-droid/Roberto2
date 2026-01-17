using System;

namespace Application.UseCases.Clientes.Create;

public sealed record CreateClientResult(Guid ClienteId, string Nome);
