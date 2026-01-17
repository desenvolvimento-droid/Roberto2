using FluentValidation;

namespace Application.UseCases.Clientes.Create;

public sealed class CreateClientValidation : AbstractValidator<CreateClientCommand>
{
    public CreateClientValidation()
    {
        RuleFor(x => x.Nome)
            .NotEmpty()
            .WithMessage("Nome é obrigatório.");
    }
}
