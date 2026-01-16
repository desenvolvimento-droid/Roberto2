using Domain.BuildingBlocks.Models;

namespace Infra.Models;

public record ClienteReadModel(Guid clienteId, string nome, DateTime criadoEm, DateTime atualizadoEm);
