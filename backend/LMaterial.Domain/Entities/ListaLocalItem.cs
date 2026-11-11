using System;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;

namespace LMaterial.Domain.Entities;

public class ListaLocalItem
{
    public Guid Id { get; private set; }
    public Guid ListaLocalId { get; private set; }
    public Guid MaterialId { get; private set; }
    public int QuantidadeSolicitada { get; private set; }
    public int? QuantidadeSeparada { get; private set; }
    public StatusItem StatusItem { get; private set; }
    public string? Observacao { get; private set; }

    // Relação de Navegação (opcional para o domínio puro, mas importante para mapeamentos ORM)
    public virtual Material? Material { get; private set; }

    // Construtor privado para o ORM
    private ListaLocalItem()
    {
    }

    private ListaLocalItem(Guid id, Guid listaLocalId, Guid materialId, int quantidadeSolicitada, int? quantidadeSeparada, StatusItem statusItem, string? observacao)
    {
        if (id == Guid.Empty)
            throw new DomainException("O Id do item da lista não pode ser vazio.");

        if (listaLocalId == Guid.Empty)
            throw new DomainException("O Id da lista local vinculada não pode ser vazio.");

        if (materialId == Guid.Empty)
            throw new DomainException("O Id do material não pode ser vazio.");

        if (quantidadeSolicitada <= 0)
            throw new DomainException("A quantidade solicitada deve ser maior que zero.");

        if (quantidadeSeparada.HasValue && quantidadeSeparada.Value < 0)
            throw new DomainException("A quantidade separada não pode ser negativa.");

        Id = id;
        ListaLocalId = listaLocalId;
        MaterialId = materialId;
        QuantidadeSolicitada = quantidadeSolicitada;
        QuantidadeSeparada = quantidadeSeparada;
        StatusItem = statusItem;
        Observacao = observacao;
    }

    public static ListaLocalItem Create(Guid listaLocalId, Guid materialId, int quantidadeSolicitada)
    {
        return new ListaLocalItem(
            Guid.NewGuid(), 
            listaLocalId, 
            materialId, 
            quantidadeSolicitada, 
            quantidadeSeparada: null, 
            StatusItem.OK, 
            observacao: null
        );
    }

    public static ListaLocalItem CreateWithId(Guid id, Guid listaLocalId, Guid materialId, int quantidadeSolicitada, int? quantidadeSeparada, StatusItem statusItem, string? observacao)
    {
        return new ListaLocalItem(id, listaLocalId, materialId, quantidadeSolicitada, quantidadeSeparada, statusItem, observacao);
    }

    public void RegistrarSeparacao(int quantidadeSeparada, string? observacao = null)
    {
        if (quantidadeSeparada < 0)
            throw new DomainException("A quantidade separada não pode ser negativa.");

        QuantidadeSeparada = quantidadeSeparada;
        Observacao = observacao;

        // Se a quantidade separada for menor que a solicitada, marca como Faltando, senão OK
        StatusItem = quantidadeSeparada < QuantidadeSolicitada ? StatusItem.Faltando : StatusItem.OK;
    }

    public void AtualizarQuantidadeSolicitada(int novaQuantidade)
    {
        if (novaQuantidade <= 0)
            throw new DomainException("A quantidade solicitada deve ser maior que zero.");

        QuantidadeSolicitada = novaQuantidade;
    }
}
