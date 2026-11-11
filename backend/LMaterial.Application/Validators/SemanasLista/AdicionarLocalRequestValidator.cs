using FluentValidation;
using LMaterial.Application.DTOs.SemanasLista;

namespace LMaterial.Application.Validators.SemanasLista;

public class AdicionarLocalRequestValidator : AbstractValidator<AdicionarLocalRequest>
{
    public AdicionarLocalRequestValidator()
    {
        RuleFor(x => x.LocalId)
            .NotEmpty().WithMessage("O Id do local é obrigatório.");
    }
}
