using System;

namespace LMaterial.Application.DTOs.SemanasLista;

public record RegistrarRetiradaRequest(
    string NomeRetiradoPor,
    Guid? ListaLocalId = null
);
