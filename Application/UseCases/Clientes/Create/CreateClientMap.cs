using Domain.Entities.Clientes;

namespace Application.UseCases.Clientes.Create;

public static class CreateClientMap
{
    public static Cliente ToAggregate(this CreateClientCommand command)
    {
        return Cliente.Create(command.Nome);
    }
}
