using BuildingBlocks.Core.Event;
using BuildingBlocks.Core.Model;
using Domain.Exceptions;

namespace Domain.Entities.Clientes;

public record Cliente : AggregateRoot
{
    public string Nome { get; private set; }

    public static Cliente Create(string nome)
    {
        var cliente = new Cliente();

        var clienteCriadoEvent = new ClienteCriadoEvent
        (
            Guid.NewGuid(),
            nome
        );

        cliente.RecordEvent(clienteCriadoEvent);

        return cliente;
    }

    public void ComNome(string nome)
    {
        if (Nome == nome)
            return;

        var nomeAtualizadoEvent = new NomeClienteAtualizadoEvent
        (
            Id == Guid.Empty ? Guid.NewGuid() : Id,
            nome
        );

        // Aplica e registra o evento usando a API do AggregateRoot
        RecordEvent(nomeAtualizadoEvent);
    }

    protected override void ValidateInvariants()
    {
        if (string.IsNullOrEmpty(Nome))
            throw new DomainException("Nome não pode ser nulo");
    }

    protected override void When(IDomainEvent @event)
    {
        switch (@event)
        {
            case ClienteCriadoEvent e:
                Id = e.Id;
                Nome = e.Nome;
                break;
            case NomeClienteAtualizadoEvent e:
                Id = e.Id;
                Nome = e.Nome;
                break;
            default:
                throw new DomainException("Evento não criado");
        }
    }
}