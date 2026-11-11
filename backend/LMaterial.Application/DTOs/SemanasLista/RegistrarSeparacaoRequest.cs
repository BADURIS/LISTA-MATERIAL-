using System;
using System.Collections.Generic;

namespace LMaterial.Application.DTOs.SemanasLista;

public record ItemSeparadoRequest(
    Guid ItemId,
    int QuantidadeSeparada,
    string? Observacao = null
);

// O almoxarife responsável é obtido a partir do token autenticado
public record RegistrarSeparacaoRequest(
    List<ItemSeparadoRequest>? Itens = null,
    Guid? ItemId = null,
    int? QuantidadeSeparada = null,
    string? Observacao = null
);
