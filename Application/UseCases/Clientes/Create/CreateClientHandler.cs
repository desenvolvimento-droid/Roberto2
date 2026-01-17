using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Domain.Interfaces;
using MediatR;
using Domain.Entities.Clientes;

namespace Application.UseCases.Clientes.Create;

public sealed class CreateClientHandler : IRequestHandler<CreateClientCommand, Result<CreateClientResult>>
{
    private readonly IEventStoreRepository<Cliente> _eventStore;

    public CreateClientHandler(IEventStoreRepository<Cliente> eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task<Result<CreateClientResult>> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        // validation is expected to run via pipeline; keep handler simple
        var cliente = request.ToAggregate();

        // Enrich uncommitted events with request metadata for auditing/idempotency
        var requestId = request.RequestId?.ToString() ?? Guid.NewGuid().ToString();
        foreach (var evt in cliente.GetUncommittedEvents())
        {
            try
            {
                if (evt.Metadata == null)
                    evt.Metadata = new Dictionary<string, string?>();

                // These values are best-effort; caller or middleware may replace them.
                evt.Metadata["origin"] = "api";
                evt.Metadata["requestId"] = requestId;
                // userId may be set by pipeline/auth middleware; leave null otherwise
                if (!evt.Metadata.ContainsKey("userId")) evt.Metadata["userId"] = null;
                evt.Metadata["timestamp"] = evt.OcorreuEm.ToString("o");
            }
            catch
            {
                // best-effort metadata enrichment
            }
        }

        // append aggregate events
        await _eventStore.AppendAsync(cliente, cancellationToken);

        var id = cliente.Id;

        return Result.Ok(new CreateClientResult(id, cliente.Nome));
    }
}
