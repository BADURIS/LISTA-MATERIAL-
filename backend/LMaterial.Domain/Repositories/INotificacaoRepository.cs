using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Domain.Entities;

namespace LMaterial.Domain.Repositories;

public interface INotificacaoRepository
{
    Task<IReadOnlyList<Notificacao>> GetByUsuarioAsync(Guid usuarioId, bool somenteNaoLidas, CancellationToken ct = default);
    Task<Notificacao?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Notificacao> notificacoes, CancellationToken ct = default);
}
