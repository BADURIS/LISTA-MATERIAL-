using FluentValidation;
using LMaterial.Application.DTOs.Locais;

namespace LMaterial.Application.Validators.Locais;

public class CreateLocalRequestValidator : AbstractValidator<CreateLocalRequest>
{
    public CreateLocalRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome do local é obrigatório.")
            .MaximumLength(150).WithMessage("O nome do local não pode ter mais de 150 caracteres.");
    }
}
