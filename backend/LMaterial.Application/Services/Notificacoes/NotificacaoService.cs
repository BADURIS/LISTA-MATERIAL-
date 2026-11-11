using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.Notificacoes;
using LMaterial.Domain.Exceptions;
using LMaterial.Domain.Interfaces;
using LMaterial.Domain.Repositories;

namespace LMaterial.Application.Services.Notificacoes;

public class NotificacaoService : INotificacaoService
{
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public NotificacaoService(INotificacaoRepository notificacaoRepository, IUnitOfWork unitOfWork)
    {
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<IReadOnlyList<NotificacaoDto>> ListarAsync(Guid usuarioId, bool somenteNaoLidas, CancellationToken ct = default)
    {
        var notificacoes = await _notificacaoRepository.GetByUsuarioAsync(usuarioId, somenteNaoLidas, ct);

        return notificacoes
            .Select(n => new NotificacaoDto(n.Id, n.SemanaListaId, n.Mensagem, n.DataCriacao, n.Lida))
            .ToList();
    }

    public async Task MarcarComoLidaAsync(Guid usuarioId, Guid notificacaoId, CancellationToken ct = default)
    {
        var notificacao = await _notificacaoRepository.GetByIdAsync(notificacaoId, ct);

        // Mesma mensagem para inexistente e de outro usuário, para não revelar notificações alheias
        if (notificacao == null || notificacao.UsuarioDestinoId != usuarioId)
            throw new DomainException("Notificação não encontrada.");

        notificacao.MarcarComoLida();
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
