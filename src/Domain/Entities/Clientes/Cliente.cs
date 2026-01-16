using System;
using BuildingBlocks.Core.Model;
using BuildingBlocks.Core.Event;

namespace Domain.Entities.Clientes;

public sealed class Cliente : AggregateRoot
{
    public string Nome { get; private set; } = string.Empty;

    public ClienteStatus Status { get; private set; } = ClienteStatus.Inactive;

    protected Cliente() { }

    // Factory
    public static Cliente Create(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        var cliente = new Cliente();
        var clienteId = Guid.NewGuid();

        cliente.RecordEvent(new ClienteCreated(clienteId, nome));

        return cliente;
    }

    public void Activate()
    {
        if (Status == ClienteStatus.Active)
            throw new InvalidOperationException("Cliente já está ativo.");

        RecordEvent(new ClienteActivated(Id));
    }

    public void Deactivate()
    {
        if (Status == ClienteStatus.Inactive)
            throw new InvalidOperationException("Cliente já está inativo.");

        RecordEvent(new ClienteDeactivated(Id));
    }

    protected override void When(IDomainEvent @event)
    {
        switch (@event)
        {
            case ClienteCreated e:
                Id = e.ClienteId;
                Nome = e.Nome;
                Status = ClienteStatus.Inactive;
                CriadoEm = e.OcorreuEm;
                break;

            case ClienteActivated:
                Status = ClienteStatus.Active;
                break;

            case ClienteDeactivated:
                Status = ClienteStatus.Inactive;
                break;
        }
    }

    protected override void ValidateInvariants()
    {
        if (Id == Guid.Empty)
            throw new InvalidOperationException("ClienteId inválido.");

        if (string.IsNullOrWhiteSpace(Nome))
            throw new InvalidOperationException("Nome do cliente é obrigatório.");
    }
}

public enum ClienteStatus
{
    Inactive = 0,
    Active = 1
}
