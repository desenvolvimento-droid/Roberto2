using Domain.Entities.Clientes;

namespace Application.UseCases.Clientes.Get;

public static class GetClientMap
{
    public static GetClientDto ToDto(this Cliente cliente)
    {
        return new GetClientDto(cliente.Id, cliente.Nome, cliente.Status.ToString());
    }
}
