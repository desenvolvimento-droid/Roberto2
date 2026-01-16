using System.Diagnostics;
using System.Diagnostics.Metrics;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Domain.BuildingBlocks.Dispacher;
using Domain.Entities.Clientes;
using Domain.Interfaces;
using FluentResults;
using MediatR;

namespace Application.UseCases.Clientes.Criar;

public sealed class CriarClienteHandler : IRequestHandler<CriarClienteCommand, Result<CriarClienteResult>>
{
    private readonly IEventStoreRepository<Cliente> _eventStore;
    private readonly IEventDispatcherRepository _eventDispatcher;
    private readonly IValidator<CriarClienteCommand> _validator;
    private readonly ILogger<CriarClienteHandler> _logger;

 
    public CriarClienteHandler(
        IEventStoreRepository<Cliente> eventStore,
        IEventDispatcherRepository eventDispatcher,
        IValidator<CriarClienteCommand> validator,
        ILogger<CriarClienteHandler> logger)
    {
        _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<CriarClienteResult>> Handle(CriarClienteCommand request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Validação falhou: {Errors}", validation.Errors);
            return Result.Fail("Falha de validação");
        }

        var cliente = Cliente.Create(request.Nome);

        await _eventStore.AppendAsync(cliente, cancellationToken).ConfigureAwait(false);

        await _eventDispatcher.DispatchAsync(cliente, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Cliente criado: {ClienteId}", cliente.Id);

        return Result.Ok(new CriarClienteResult(cliente.Id, cliente.CriadoEm));
    }
}
