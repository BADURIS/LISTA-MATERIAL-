using System;

namespace LMaterial.Application.DTOs.SemanasLista;

// Item da listagem de semanas (histórico), sem o detalhamento dos locais
public record SemanaResumoDto(
    Guid Id,
    DateOnly DataInicio,
    DateOnly DataFim,
    string Status
);
