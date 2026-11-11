using System;

namespace LMaterial.Application.DTOs.SemanasLista;

public record ConfirmarRecebimentoRequest(
    bool PossuiPendencias = false,
    Guid? ListaLocalId = null
);
