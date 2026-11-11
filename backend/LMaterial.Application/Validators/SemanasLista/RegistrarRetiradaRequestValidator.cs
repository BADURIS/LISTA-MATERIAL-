using FluentValidation;
using LMaterial.Application.DTOs.SemanasLista;

namespace LMaterial.Application.Validators.SemanasLista;

public class RegistrarRetiradaRequestValidator : AbstractValidator<RegistrarRetiradaRequest>
{
    public RegistrarRetiradaRequestValidator()
    {
        RuleFor(x => x.NomeRetiradoPor)
            .NotEmpty().WithMessage("O nome de quem retirou a lista é obrigatório.");

        RuleFor(x => x.ListaLocalId)
            .NotEmpty().When(x => x.ListaLocalId.HasValue).WithMessage("O Id da lista local não pode ser vazio.");
    }
}
