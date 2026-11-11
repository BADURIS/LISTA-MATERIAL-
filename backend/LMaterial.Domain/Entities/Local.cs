using System;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;

namespace LMaterial.Domain.Entities;

public class Local
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public TipoLocal Tipo { get; private set; }
    public int OrdemExibicao { get; private set; }
    public bool Ativo { get; private set; }

    // Construtor privado para o ORM
    private Local()
    {
        Nome = string.Empty;
    }

    private Local(Guid id, string nome, TipoLocal tipo, int ordemExibicao, bool ativo)
    {
        if (id == Guid.Empty)
            throw new DomainException("O Id do local não pode ser vazio.");

        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do local não pode ser vazio ou nulo.");

        if (ordemExibicao < 0)
            throw new DomainException("A ordem de exibição do local não pode ser negativa.");

        Id = id;
        Nome = nome;
        Tipo = tipo;
        OrdemExibicao = ordemExibicao;
        Ativo = ativo;
    }

    public static Local Create(string nome, TipoLocal tipo, int ordemExibicao)
    {
        return new Local(Guid.NewGuid(), nome, tipo, ordemExibicao, true);
    }

    public static Local CreateWithId(Guid id, string nome, TipoLocal tipo, int ordemExibicao, bool ativo = true)
    {
        return new Local(id, nome, tipo, ordemExibicao, ativo);
    }

    public void AtualizarNome(string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
            throw new DomainException("O nome do local não pode ser vazio ou nulo.");

        Nome = novoNome;
    }

    public void AlterarOrdemExibicao(int novaOrdem)
    {
        if (novaOrdem < 0)
            throw new DomainException("A ordem de exibição do local não pode ser negativa.");

        OrdemExibicao = novaOrdem;
    }

    public void AlterarTipo(TipoLocal novoTipo)
    {
        Tipo = novoTipo;
    }

    public void Ativar()
    {
        Ativo = true;
    }

    public void Inativar()
    {
        Ativo = false;
    }
}
