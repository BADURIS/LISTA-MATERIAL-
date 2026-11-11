using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Repositories;
using LMaterial.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace LMaterial.Infrastructure.Repositories;

public class NotificacaoRepository : INotificacaoRepository
{
    private readonly AppDbContext _context;

    public NotificacaoRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<Notificacao>> GetByUsuarioAsync(Guid usuarioId, bool somenteNaoLidas, CancellationToken ct = default)
    {
        var query = _context.Notificacoes.Where(n => n.UsuarioDestinoId == usuarioId);

        if (somenteNaoLidas)
            query = query.Where(n => !n.Lida);

        return await query.OrderByDescending(n => n.DataCriacao).Take(100).ToListAsync(ct);
    }

    public Task<Notificacao?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.Notificacoes.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task AddRangeAsync(IEnumerable<Notificacao> notificacoes, CancellationToken ct = default)
        => await _context.Notificacoes.AddRangeAsync(notificacoes, ct);
}
