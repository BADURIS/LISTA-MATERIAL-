using System;
using System.Collections.Generic;

namespace LMaterial.Application.DTOs.SemanasLista;

public record ListaLocalDto(
    Guid Id,
    Guid SemanaListaId,
    Guid LocalId,
    string NomeLocal,
    string Status,
    Guid? SeparadoPorAlmoxarifeId,
    DateTime? DataSeparacao,
    string? NomeRetiradoPor,
    DateTime? DataRetirada,
    List<ListaLocalItemDto> Itens,
    string? AssinaturaDigital = null,
    bool AssinaturaValida = false,
    bool SemPedido = false,
    string TipoLocal = "",
    int OrdemExibicao = 0
);
