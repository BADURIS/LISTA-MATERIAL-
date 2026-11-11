using System;

namespace LMaterial.Application.DTOs.SemanasLista;

public record ListaLocalItemDto(
    Guid Id,
    Guid ListaLocalId,
    Guid MaterialId,
    string NomeMaterial,
    string Unidade,
    int QuantidadeSolicitada,
    int? QuantidadeSeparada,
    string StatusItem,
    string? Observacao
);
