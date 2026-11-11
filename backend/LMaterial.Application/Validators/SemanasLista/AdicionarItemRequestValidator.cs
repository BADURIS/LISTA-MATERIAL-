using FluentValidation;
using LMaterial.Application.DTOs.SemanasLista;

namespace LMaterial.Application.Validators.SemanasLista;

public class AdicionarItemRequestValidator : AbstractValidator<AdicionarItemRequest>
{
    public AdicionarItemRequestValidator()
    {
        RuleFor(x => x.MaterialId)
            .NotEmpty().WithMessage("O Id do material é obrigatório.");

        RuleFor(x => x.QuantidadeSolicitada)
            .GreaterThan(0).WithMessage("A quantidade solicitada deve ser maior que zero.");

        RuleFor(x => x.LocalId)
            .NotEmpty().When(x => x.LocalId.HasValue).WithMessage("O Id do local não pode ser vazio.");
    }
}
