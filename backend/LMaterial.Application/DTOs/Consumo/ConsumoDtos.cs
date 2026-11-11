using System;
using System.Collections.Generic;

namespace LMaterial.Application.DTOs.Consumo;

// Consumo = quantidade separada nas listas com retirada registrada (material que efetivamente saiu do almoxarifado)

public record ConsumoMaterialDto(
    Guid MaterialId,
    string NomeMaterial,
    string Unidade,
    int QuantidadeTotal
);

public record ConsumoLocalDto(
    Guid LocalId,
    string NomeLocal,
    int QuantidadeTotal
);

public record ConsumoPeriodoDto(
    DateOnly DataInicio,
    DateOnly DataFim,
    Guid? LocalId,
    int QuantidadeSemanas,
    List<ConsumoMaterialDto> PorMaterial,
    List<ConsumoLocalDto> PorLocal
);

public record SemanaConsumoDto(
    Guid SemanaListaId,
    DateOnly DataInicio,
    DateOnly DataFim,
    string Status,
    int QuantidadeTotal
);

public record MaterialTendenciaDto(
    Guid MaterialId,
    string NomeMaterial,
    string Unidade,
    List<int> QuantidadesPorSemana,
    int SemanaAnterior,
    int UltimaSemana,
    int Variacao,
    double? VariacaoPercentual
);

public record ConsumoSemanalDto(
    List<SemanaConsumoDto> Semanas,
    List<MaterialTendenciaDto> Materiais
);
