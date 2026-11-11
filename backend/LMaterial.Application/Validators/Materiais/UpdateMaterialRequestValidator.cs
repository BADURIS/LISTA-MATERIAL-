using FluentValidation;
using LMaterial.Application.DTOs.Materiais;

namespace LMaterial.Application.Validators.Materiais;

public class UpdateMaterialRequestValidator : AbstractValidator<UpdateMaterialRequest>
{
    public UpdateMaterialRequestValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome do material é obrigatório.")
            .MaximumLength(150).WithMessage("O nome do material não pode ter mais de 150 caracteres.");

        RuleFor(x => x.Unidade)
            .NotEmpty().WithMessage("A unidade do material é obrigatória.")
            .MaximumLength(20).WithMessage("A unidade não pode ter mais de 20 caracteres.");

        RuleFor(x => x.OrdemExibicao)
            .GreaterThanOrEqualTo(0).WithMessage("A ordem de exibição não pode ser negativa.");
    }
}
