using System;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;
using Xunit;

namespace LMaterial.Domain.UnitTests;

public class LocalTests
{
    [Fact]
    public void Create_ComDadosValidos_DeveCriarInstanciaAtiva()
    {
        // Arrange & Act
        var local = Local.Create("1º Andar", TipoLocal.Andar, 2);

        // Assert
        Assert.NotNull(local);
        Assert.NotEqual(Guid.Empty, local.Id);
        Assert.Equal("1º Andar", local.Nome);
        Assert.Equal(TipoLocal.Andar, local.Tipo);
        Assert.Equal(2, local.OrdemExibicao);
        Assert.True(local.Ativo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ComNomeInvalido_DeveLancarDomainException(string? nomeInvalido)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            Local.Create(nomeInvalido!, TipoLocal.AreaComum, 1)
        );
        Assert.Contains("nome do local não pode ser vazio", exception.Message);
    }

    [Fact]
    public void Create_ComOrdemExibicaoNegativa_DeveLancarDomainException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            Local.Create("Refeitório", TipoLocal.AreaComum, -2)
        );
        Assert.Contains("ordem de exibição do local não pode ser negativa", exception.Message);
    }

    [Fact]
    public void Inativar_DeveAlterarAtivoParaFalse()
    {
        // Arrange
        var local = Local.Create("1º Andar", TipoLocal.Andar, 2);

        // Act
        local.Inativar();

        // Assert
        Assert.False(local.Ativo);
    }

    [Fact]
    public void Ativar_DeveAlterarAtivoParaTrue()
    {
        // Arrange
        var local = Local.Create("1º Andar", TipoLocal.Andar, 2);
        local.Inativar();

        // Act
        local.Ativar();

        // Assert
        Assert.True(local.Ativo);
    }
}
