using System;
using LMaterial.Domain.Exceptions;

namespace LMaterial.Domain.Entities;

public class Notificacao
{
    public Guid Id { get; private set; }
    public Guid UsuarioDestinoId { get; private set; }
    public Guid? SemanaListaId { get; private set; }
    public string Mensagem { get; private set; } = string.Empty;
    public DateTime DataCriacao { get; private set; }
    public bool Lida { get; private set; }

    private Notificacao()
    {
    }

    private Notificacao(Guid id, Guid usuarioDestinoId, Guid? semanaListaId, string mensagem, DateTime dataCriacao)
    {
        if (usuarioDestinoId == Guid.Empty)
            throw new DomainException("O destinatário da notificação é obrigatório.");

        if (string.IsNullOrWhiteSpace(mensagem))
            throw new DomainException("A mensagem da notificação não pode ser vazia.");

        Id = id;
        UsuarioDestinoId = usuarioDestinoId;
        SemanaListaId = semanaListaId;
        Mensagem = mensagem;
        DataCriacao = dataCriacao;
        Lida = false;
    }

    public static Notificacao Create(Guid usuarioDestinoId, Guid? semanaListaId, string mensagem)
    {
        return new Notificacao(Guid.NewGuid(), usuarioDestinoId, semanaListaId, mensagem, DateTime.UtcNow);
    }

    public void MarcarComoLida() => Lida = true;
}
