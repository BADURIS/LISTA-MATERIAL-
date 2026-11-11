using System;
using System.Collections.Generic;

namespace LMaterial.Application.DTOs.SemanasLista;

public record SemanaListaDto(
    Guid Id,
    DateOnly DataInicio,
    DateOnly DataFim,
    string Status,
    Guid CriadoPorSupervisorId,
    DateTime DataCriacao,
    List<ListaLocalDto> Locais
);
