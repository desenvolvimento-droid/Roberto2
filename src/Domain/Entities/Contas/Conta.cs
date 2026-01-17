using System;
using BuildingBlocks.Core.Model;
using BuildingBlocks.Core.Event;
using Domain.Entities.Clientes;

namespace Domain.Entities.Contas;

public sealed class Conta : AggregateRoot
{
    public Guid ContaId => Id;

    public Guid ClienteId { get; private set; }

    public decimal SaldoDisponivel { get; private set; } = 0m;

    public decimal SaldoReservado { get; private set; } = 0m;

    public decimal LimiteCredito { get; private set; } = 0m;

    public ContaStatus Status { get; private set; } = ContaStatus.Inactive;

    protected Conta() { }

    // Factory
    public static Conta Create(Guid clienteId)
    {
        if (clienteId == Guid.Empty)
            throw new ArgumentException("ClienteId é obrigatório.", nameof(clienteId));

        var conta = new Conta();
        var contaId = Guid.NewGuid();

        conta.RecordEvent(new ContaCreated(contaId, clienteId));

        return conta;
    }

    public void DefineLimiteCredito(decimal limite)
    {
        if (limite < 0)
            throw new InvalidOperationException("Limite de crédito deve ser maior ou igual a zero.");

        RecordEvent(new LimiteCreditoDefinido(ContaId, limite));
    }

    public void Activate()
    {
        if (Status == ContaStatus.Active)
            throw new InvalidOperationException("Conta já está ativa.");

        if (Status == ContaStatus.Blocked)
            throw new InvalidOperationException("Conta está bloqueada.");

        RecordEvent(new ContaActivated(ContaId));
    }

    public void Reservar(decimal valor)
    {
        if (SaldoDisponivel + LimiteCredito < valor)
            throw new InvalidOperationException("Limite insuficiente.");

        RecordEvent(new ReservationCompleted(Id, valor));
    }

    public void Block()
    {
        if (Status == ContaStatus.Blocked)
            throw new InvalidOperationException("Conta já está bloqueada.");

        RecordEvent(new ContaBlocked(ContaId));
    }

    protected override void When(IDomainEvent @event)
    {
        switch (@event)
        {
            case ContaCreated e:
                Id = e.ContaId;
                ClienteId = e.ClienteId;
                SaldoDisponivel = 0m;
                SaldoReservado = 0m;
                LimiteCredito = 0m;
                Status = ContaStatus.Inactive;
                CriadoEm = e.OcorreuEm;
                break;

            case ReservationCompleted e:
                SaldoReservado += e.Valor;
                SaldoDisponivel -= e.Valor;
                break;

            case LimiteCreditoDefinido e:
                LimiteCredito = e.LimiteCredito;
                break;

            case ContaActivated:
                Status = ContaStatus.Active;
                break;

            case ContaBlocked:
                Status = ContaStatus.Blocked;
                break;

            default:
                break;
        }
    }

    protected override void ValidateInvariants()
    {
        if (Id == Guid.Empty)
            throw new InvalidOperationException("ContaId inválido.");

        if (ClienteId == Guid.Empty)
            throw new InvalidOperationException("ClienteId inválido.");

        if (SaldoDisponivel < -LimiteCredito)
            throw new InvalidOperationException("Saldo disponível excede o limite de crédito.");

        if (SaldoReservado < 0)
            throw new InvalidOperationException("Saldo reservado não pode ser negativo.");

        if (LimiteCredito < 0)
            throw new InvalidOperationException("Limite de crédito inválido.");
    }
}

public enum ContaStatus
{
    Inactive = 0,
    Active = 1,
    Blocked = 2
}
