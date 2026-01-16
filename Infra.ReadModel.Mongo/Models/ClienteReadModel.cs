using Domain.BuildingBlocks.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Infra.Models;

public record ClienteReadModel : IReadModel
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; init; }

    // Campos adicionais
    public string Nome { get; init; }

    public DateTime CriadoEm { get; init; }

    public DateTime AtualizadoEm { get; init; }
}