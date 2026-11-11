using System;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;

namespace LMaterial.Domain.Entities;

public class Material
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public Unidade Unidade { get; private set; }
    public int OrdemExibicao { get; private set; }
    public bool Ativo { get; private set; }

    // Construtor privado para o ORM (EF Core)
    private Material()
    {
        Nome = string.Empty;
    }

    // Construtor principal privado que garante as invariantes
    private Material(Guid id, string nome, Unidade unidade, int ordemExibicao, bool ativo)
    {
        if (id == Guid.Empty)
            throw new DomainException("O Id do material não pode ser vazio.");

        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome do material não pode ser vazio ou nulo.");

        if (ordemExibicao < 0)
            throw new DomainException("A ordem de exibição do material não pode ser negativa.");

        Id = id;
        Nome = nome;
        Unidade = unidade;
        OrdemExibicao = ordemExibicao;
        Ativo = ativo;
    }

    // Método de fábrica estático
    public static Material Create(string nome, Unidade unidade, int ordemExibicao)
    {
        return new Material(Guid.NewGuid(), nome, unidade, ordemExibicao, true);
    }

    // Método de fábrica estático com ID personalizado (útil para sementes/testes/reconstituição)
    public static Material CreateWithId(Guid id, string nome, Unidade unidade, int ordemExibicao, bool ativo = true)
    {
        return new Material(id, nome, unidade, ordemExibicao, ativo);
    }

    // Métodos de comportamento para modificar propriedades mantendo invariantes
    public void AtualizarNome(string novoNome)
    {
        if (string.IsNullOrWhiteSpace(novoNome))
            throw new DomainException("O nome do material não pode ser vazio ou nulo.");

        Nome = novoNome;
    }

    public void AlterarOrdemExibicao(int novaOrdem)
    {
        if (novaOrdem < 0)
            throw new DomainException("A ordem de exibição do material não pode ser negativa.");

        OrdemExibicao = novaOrdem;
    }

    public void AlterarUnidade(Unidade novaUnidade)
    {
        Unidade = novaUnidade;
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
