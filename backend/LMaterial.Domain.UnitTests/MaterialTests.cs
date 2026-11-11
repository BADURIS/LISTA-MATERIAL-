using System;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;
using Xunit;

namespace LMaterial.Domain.UnitTests;

public class MaterialTests
{
    [Fact]
    public void Create_ComDadosValidos_DeveCriarInstancia()
    {
        // Arrange & Act
        var material = Material.Create("Cimento", Unidade.PCT, 1);

        // Assert
        Assert.NotNull(material);
        Assert.NotEqual(Guid.Empty, material.Id);
        Assert.Equal("Cimento", material.Nome);
        Assert.Equal(Unidade.PCT, material.Unidade);
        Assert.Equal(1, material.OrdemExibicao);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ComNomeInvalido_DeveLancaoDomainException(string? nomeInvalido)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            Material.Create(nomeInvalido!, Unidade.UN, 1)
        );
        Assert.Contains("nome do material não pode ser vazio", exception.Message);
    }

    [Fact]
    public void Create_ComOrdemExibicaoNegativa_DeveLancaoDomainException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            Material.Create("Areia", Unidade.CX, -1)
        );
        Assert.Contains("ordem de exibição do material não pode ser negativa", exception.Message);
    }

    [Fact]
    public void AtualizarNome_ComNomeValido_DeveAlterarNome()
    {
        // Arrange
        var material = Material.Create("Cimento", Unidade.PCT, 1);

        // Act
        material.AtualizarNome("Cimento CPII");

        // Assert
        Assert.Equal("Cimento CPII", material.Nome);
    }

    [Fact]
    public void AtualizarNome_ComNomeInvalido_DeveLancarDomainException()
    {
        // Arrange
        var material = Material.Create("Cimento", Unidade.PCT, 1);

        // Act & Assert
        Assert.Throws<DomainException>(() => material.AtualizarNome(""));
    }

    [Fact]
    public void AlterarOrdemExibicao_ComOrdemNegativa_DeveLancarDomainException()
    {
        // Arrange
        var material = Material.Create("Cimento", Unidade.PCT, 1);

        // Act & Assert
        Assert.Throws<DomainException>(() => material.AlterarOrdemExibicao(-5));
    }

    [Fact]
    public void InativarEAtivar_DeveAlterarStatusAtivo()
    {
        // Arrange
        var material = Material.Create("Argamassa", Unidade.PCT, 2);
        Assert.True(material.Ativo);

        // Act
        material.Inativar();
        Assert.False(material.Ativo);

        material.Ativar();
        Assert.True(material.Ativo);
    }
}
