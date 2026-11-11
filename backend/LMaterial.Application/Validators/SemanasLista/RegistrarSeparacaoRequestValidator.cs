using FluentValidation;
using LMaterial.Application.DTOs.SemanasLista;

namespace LMaterial.Application.Validators.SemanasLista;

public class ItemSeparadoRequestValidator : AbstractValidator<ItemSeparadoRequest>
{
    public ItemSeparadoRequestValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("O Id do item é obrigatório.");

        RuleFor(x => x.QuantidadeSeparada)
            .GreaterThanOrEqualTo(0).WithMessage("A quantidade separada não pode ser negativa.");
    }
}

public class RegistrarSeparacaoRequestValidator : AbstractValidator<RegistrarSeparacaoRequest>
{
    public RegistrarSeparacaoRequestValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty().When(x => x.ItemId.HasValue).WithMessage("O Id do item não pode ser vazio.");

        RuleFor(x => x.QuantidadeSeparada)
            .GreaterThanOrEqualTo(0).When(x => x.QuantidadeSeparada.HasValue).WithMessage("A quantidade separada não pode ser negativa.");

        RuleForEach(x => x.Itens)
            .SetValidator(new ItemSeparadoRequestValidator())
            .When(x => x.Itens != null);
    }
}
