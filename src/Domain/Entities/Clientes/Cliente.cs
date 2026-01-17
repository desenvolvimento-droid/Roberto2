using BuildingBlocks.Core.Event;
using BuildingBlocks.Core.Model;

namespace Domain.Entities.Clientes;

public sealed class Cliente : AggregateRoot
{
    public string Nome { get; private set; } = string.Empty;

    public ClienteStatus Status { get; private set; } = ClienteStatus.Inactive;

    public Cliente() { }

    // Factory
    public static Cliente Create(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        var cliente = new Cliente();
        var clienteId = Guid.NewGuid();

        var ev = new ClienteCreated(clienteId, nome);
        // Basic metadata for events created inside domain (no request context available here)
        ev.Metadata["origin"] = "domain";
        ev.Metadata["timestamp"] = ev.OcorreuEm.ToString("o");

        cliente.RecordEvent(ev);

        return cliente;
    }

    public void Activate()
    {
        if (Status == ClienteStatus.Active)
            throw new InvalidOperationException("Cliente já está ativo.");

        var act = new ClienteActivated(Id);
        act.Metadata["origin"] = "domain";
        act.Metadata["timestamp"] = act.OcorreuEm.ToString("o");
        RecordEvent(act);
    }

    public void Deactivate()
    {
        if (Status == ClienteStatus.Inactive)
            throw new InvalidOperationException("Cliente já está inativo.");

        var deact = new ClienteDeactivated(Id);
        deact.Metadata["origin"] = "domain";
        deact.Metadata["timestamp"] = deact.OcorreuEm.ToString("o");
        RecordEvent(deact);
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
