using FluentValidation;

namespace Application.UseCases.Clientes.Obter;

public class ObterClienteValidator : AbstractValidator<ObterClienteQuery>
{
    public ObterClienteValidator()
    {
        //validar ID
    }
}
