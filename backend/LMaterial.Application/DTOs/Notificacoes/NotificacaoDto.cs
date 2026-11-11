using System;

namespace LMaterial.Application.DTOs.Notificacoes;

public record NotificacaoDto(
    Guid Id,
    Guid? SemanaListaId,
    string Mensagem,
    DateTime DataCriacao,
    bool Lida
);
