using BuildingBlocks.Core.Event;
using BuildingBlocks.Core.Model;

namespace Domain.Entities.Conta;

public record Conta : AggregateRoot
{
    public Guid ClienteId { get; set; }
    public int Numero;
    protected override void When(IDomainEvent @event)
    {
        throw new NotImplementedException();
    }

    protected override void Validar()
    {
        
    }
}
