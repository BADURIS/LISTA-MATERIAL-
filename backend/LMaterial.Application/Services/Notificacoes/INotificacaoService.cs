using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.Notificacoes;

namespace LMaterial.Application.Services.Notificacoes;

public interface INotificacaoService
{
    Task<IReadOnlyList<NotificacaoDto>> ListarAsync(Guid usuarioId, bool somenteNaoLidas, CancellationToken ct = default);
    Task MarcarComoLidaAsync(Guid usuarioId, Guid notificacaoId, CancellationToken ct = default);
}
