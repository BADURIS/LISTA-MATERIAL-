using System;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;
using Xunit;

namespace LMaterial.Domain.UnitTests;

public class ListaLocalItemTests
{
    [Fact]
    public void Create_ComDadosValidos_DeveCriarInstanciaComStatusOk()
    {
        // Arrange
        var listaLocalId = Guid.NewGuid();
        var materialId = Guid.NewGuid();

        // Act
        var item = ListaLocalItem.Create(listaLocalId, materialId, 10);

        // Assert
        Assert.NotNull(item);
        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(listaLocalId, item.ListaLocalId);
        Assert.Equal(materialId, item.MaterialId);
        Assert.Equal(10, item.QuantidadeSolicitada);
        Assert.Null(item.QuantidadeSeparada);
        Assert.Equal(StatusItem.OK, item.StatusItem);
        Assert.Null(item.Observacao);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_ComQuantidadeSolicitadaInvalida_DeveLancarDomainException(int qtdInvalida)
    {
        // Arrange
        var listaLocalId = Guid.NewGuid();
        var materialId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            ListaLocalItem.Create(listaLocalId, materialId, qtdInvalida)
        );
        Assert.Contains("quantidade solicitada deve ser maior que zero", exception.Message);
    }

    [Fact]
    public void RegistrarSeparacao_ComQuantidadeValidaIgualOuMaior_DeveAlterarStatusParaOk()
    {
        // Arrange
        var item = ListaLocalItem.Create(Guid.NewGuid(), Guid.NewGuid(), 10);

        // Act
        item.RegistrarSeparacao(10, "Separado completo");

        // Assert
        Assert.Equal(10, item.QuantidadeSeparada);
        Assert.Equal(StatusItem.OK, item.StatusItem);
        Assert.Equal("Separado completo", item.Observacao);
    }

    [Fact]
    public void RegistrarSeparacao_ComQuantidadeMenorQueSolicitada_DeveAlterarStatusParaFaltando()
    {
        // Arrange
        var item = ListaLocalItem.Create(Guid.NewGuid(), Guid.NewGuid(), 10);

        // Act
        item.RegistrarSeparacao(8, "Faltou estoque de cimento");

        // Assert
        Assert.Equal(8, item.QuantidadeSeparada);
        Assert.Equal(StatusItem.Faltando, item.StatusItem);
        Assert.Equal("Faltou estoque de cimento", item.Observacao);
    }

    [Fact]
    public void RegistrarSeparacao_ComQuantidadeNegativa_DeveLancarDomainException()
    {
        // Arrange
        var item = ListaLocalItem.Create(Guid.NewGuid(), Guid.NewGuid(), 10);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            item.RegistrarSeparacao(-1, "Tentativa errada")
        );
        Assert.Contains("quantidade separada não pode ser negativa", exception.Message);
    }
}
