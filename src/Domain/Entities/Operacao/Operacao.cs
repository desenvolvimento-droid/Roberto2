using System;
using BuildingBlocks.Core.Model;
using BuildingBlocks.Core.Event;

namespace Domain.Entities.Operacao;

public sealed class Operacao : AggregateRoot
{
    public Guid OperacaoId => Id;

    public Guid ContaId { get; private set; }

    public Guid? DestinationContaId { get; private set; }

    public OperacaoTipo Tipo { get; private set; }

    public decimal Amount { get; private set; }

    public OperacaoStatus Status { get; private set; } = OperacaoStatus.Created;

    public Guid CorrelationId { get; private set; }

    protected Operacao() { }

    // Factory
    public static Operacao Create(Guid contaId, OperacaoTipo tipo, decimal amount, Guid? destinationContaId = null, Guid? correlationId = null)
    {
        if (contaId == Guid.Empty)
            throw new ArgumentException("ContaId é obrigatório.", nameof(contaId));

        if (amount <= 0)
            throw new ArgumentException("Amount deve ser maior que zero.", nameof(amount));

        if ((tipo == OperacaoTipo.Transfer || tipo == OperacaoTipo.Capture) && destinationContaId == null)
            throw new ArgumentException("DestinationContaId é obrigatório para transfer e capture.", nameof(destinationContaId));

        var operacao = new Operacao();
        var operacaoId = Guid.NewGuid();
        var corrId = correlationId ?? Guid.NewGuid();

        operacao.RecordEvent(new OperacaoCreated(operacaoId, contaId, tipo, amount, destinationContaId, corrId));

        return operacao;
    }

    public void Validate()
    {
        if (Status != OperacaoStatus.Created)
            throw new InvalidOperationException("Operacao não está no estado Created.");

        RecordEvent(new OperacaoValidated(OperacaoId));
    }

    public void Process()
    {
        if (Status != OperacaoStatus.Validated)
            throw new InvalidOperationException("Operacao precisa estar Validated para Process.");

        RecordEvent(new OperacaoProcessed(OperacaoId));
    }

    public void Confirm()
    {
        if (Status != OperacaoStatus.Processed)
            throw new InvalidOperationException("Operacao precisa estar Processed para Confirm.");

        RecordEvent(new OperacaoEvents(OperacaoId));
    }

    public void Fail(string reason)
    {
        if (Status == OperacaoStatus.Failed || Status == OperacaoStatus.Reverted)
            throw new InvalidOperationException("Operacao já está Faled ou Reverted.");

        RecordEvent(new OperacaoFailed(OperacaoId, reason));
    }

    public void Revert(string reason)
    {
        if (Status != OperacaoStatus.Processed && Status != OperacaoStatus.Confirmed)
            throw new InvalidOperationException("Operacao só pode ser revertida se Processed ou Confirmed.");

        RecordEvent(new OperacaoReverted(OperacaoId, reason));
    }

    protected override void When(IDomainEvent @event)
    {
        switch (@event)
        {
            case OperacaoCreated e:
                Id = e.OperacaoId;
                ContaId = e.ContaId;
                DestinationContaId = e.DestinationContaId;
                Tipo = e.Tipo;
                Amount = e.Amount;
                Status = OperacaoStatus.Created;
                CorrelationId = e.CorrelationId;
                CriadoEm = e.OcorreuEm;
                break;

            case OperacaoValidated:
                Status = OperacaoStatus.Validated;
                break;

            case OperacaoProcessed:
                Status = OperacaoStatus.Processed;
                break;

            case OperacaoEvents:
                Status = OperacaoStatus.Confirmed;
                break;

            case OperacaoFailed:
                Status = OperacaoStatus.Failed;
                break;

            case OperacaoReverted:
                Status = OperacaoStatus.Reverted;
                break;

            default:
                break;
        }
    }

    protected override void ValidateInvariants()
    {
        if (Id == Guid.Empty)
            throw new InvalidOperationException("OperacaoId inválido.");

        if (ContaId == Guid.Empty)
            throw new InvalidOperationException("ContaId inválido.");

        if (Amount <= 0)
            throw new InvalidOperationException("Amount deve ser maior que zero.");

        if ((Tipo == OperacaoTipo.Transfer || Tipo == OperacaoTipo.Capture) && DestinationContaId == null)
            throw new InvalidOperationException("DestinationContaId é obrigatório para transfer e capture.");
    }
}

public enum OperacaoTipo
{
    Credit = 0,
    Debit = 1,
    Reserve = 2,
    Capture = 3,
    Reversal = 4,
    Transfer = 5
}

public enum OperacaoStatus
{
    Created = 0,
    Validated = 1,
    Processed = 2,
    Confirmed = 3,
    Failed = 4,
    Reverted = 5
}
