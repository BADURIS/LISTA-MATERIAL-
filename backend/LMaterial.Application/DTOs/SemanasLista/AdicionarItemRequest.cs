using System;

namespace LMaterial.Application.DTOs.SemanasLista;

public record AdicionarItemRequest(
    Guid MaterialId,
    int QuantidadeSolicitada,
    Guid? LocalId = null
);
