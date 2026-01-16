using FluentValidation;

namespace Application.UseCases.Clientes.Criar;

public sealed class CriarClienteValidator : AbstractValidator<CriarClienteCommand>
{
    public CriarClienteValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter ao menos 2 caracteres.")
            .MaximumLength(200).WithMessage("Nome deve ter no máximo 200 caracteres.");
    }
}

