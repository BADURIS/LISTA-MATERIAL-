using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.SemanasLista;

namespace LMaterial.Application.Services.SemanasLista;

public interface ISemanaListaService
{
    Task<SemanaListaDto> CriarSemanaAsync(CriarSemanaRequest request, Guid supervisorId, CancellationToken ct = default);
    Task<SemanaListaDto> AdicionarLocalAsync(Guid semanaId, AdicionarLocalRequest request, CancellationToken ct = default);
    Task<SemanaListaDto> AdicionarItemAsync(Guid semanaId, AdicionarItemRequest request, CancellationToken ct = default);
    Task<SemanaListaDto> PreencherLocalAsync(Guid semanaId, Guid localId, PreencherLocalRequest request, CancellationToken ct = default);
    Task<SemanaListaDto> DefinirSemPedidoAsync(Guid semanaId, Guid localId, DefinirSemPedidoRequest request, CancellationToken ct = default);
    Task<SemanaListaDto> EnviarParaSeparacaoAsync(Guid semanaId, CancellationToken ct = default);
    Task<SemanaListaDto> RegistrarSeparacaoAsync(Guid semanaId, RegistrarSeparacaoRequest request, Guid almoxarifeId, CancellationToken ct = default);
    Task<SemanaListaDto> ConfirmarRecebimentoAsync(Guid semanaId, ConfirmarRecebimentoRequest request, CancellationToken ct = default);
    Task<SemanaListaDto> RegistrarRetiradaAsync(Guid semanaId, RegistrarRetiradaRequest request, Guid coordenadorId, CancellationToken ct = default);
    Task<SemanaListaDto?> GetByIdAsync(Guid semanaId, CancellationToken ct = default);
    Task<SemanaListaDto?> GetSemanaAtualAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SemanaResumoDto>> ListarHistoricoAsync(CancellationToken ct = default);
}
