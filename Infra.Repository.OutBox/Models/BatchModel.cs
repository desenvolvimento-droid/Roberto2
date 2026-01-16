using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Infra.Services.Batch.OutboxMessage;

[BsonIgnoreExtraElements]
public sealed class BatchModel
{
    /// <summary>
    /// Identificador único do documento no MongoDB
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid BatchId { get; private set; } = Guid.NewGuid();

    [BsonRepresentation(BsonType.String)]
    public required Guid ReferenceId { get; set; } 

    /// <summary>
    /// Tipo concreto do evento (AssemblyQualifiedName)
    /// Usado para desserialização
    /// </summary>
    [BsonElement("type")]
    public string Type { get; init; } = default!;

    /// <summary>
    /// Evento serializado (JSON)
    /// </summary>
    [BsonElement("payload")]
    public string Payload { get; init; } = default!;

    /// <summary>
    /// Data/hora em que o evento ocorreu no domínio
    /// </summary>
    [BsonElement("occurredAt")]
    public DateTime OccurredAt { get; init; }

    /// <summary>
    /// Data/hora em que o processamento iniciou
    /// </summary>
    [BsonElement("processingAt")]
    public DateTime? ProcessingAt { get; set; }

    /// <summary>
    /// Data/hora em que o processamento terminou com sucesso
    /// </summary>
    [BsonElement("processedAt")]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Último erro ocorrido (se houver)
    /// </summary>
    [BsonElement("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Quantidade de tentativas de processamento
    /// </summary>
    [BsonElement("retryCount")]
    public int RetryCount { get; set; }

    /// <summary>
    /// Categoria do evento (Domain | Integration)
    /// </summary>
    [BsonElement("category")]
    public string? Category { get; init; }

    /// <summary>
    /// CorrelationId / TraceId
    /// </summary>
    [BsonElement("correlationId")]
    public string? CorrelationId { get; init; }
}
