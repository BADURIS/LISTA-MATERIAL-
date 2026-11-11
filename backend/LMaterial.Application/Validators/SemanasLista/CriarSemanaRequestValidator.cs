using FluentValidation;
using LMaterial.Application.DTOs.SemanasLista;

namespace LMaterial.Application.Validators.SemanasLista;

public class CriarSemanaRequestValidator : AbstractValidator<CriarSemanaRequest>
{
    public CriarSemanaRequestValidator()
    {
        RuleFor(x => x.DataInicio)
            .LessThanOrEqualTo(x => x.DataFim).WithMessage("A data de início não pode ser posterior à data de fim.");
    }
}
