using FluentValidation;
using LMaterial.Application.DTOs.SemanasLista;

namespace LMaterial.Application.Validators.SemanasLista;

public class ConfirmarRecebimentoRequestValidator : AbstractValidator<ConfirmarRecebimentoRequest>
{
    public ConfirmarRecebimentoRequestValidator()
    {
        RuleFor(x => x.ListaLocalId)
            .NotEmpty().When(x => x.ListaLocalId.HasValue).WithMessage("O Id da lista local não pode ser vazio.");
    }
}
