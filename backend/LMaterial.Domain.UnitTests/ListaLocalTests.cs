using System;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;
using Xunit;

namespace LMaterial.Domain.UnitTests;

public class ListaLocalTests
{
    [Fact]
    public void Create_ComDadosValidos_DeveCriarInstanciaPendente()
    {
        // Arrange
        var semanaListaId = Guid.NewGuid();
        var localId = Guid.NewGuid();

        // Act
        var lista = ListaLocal.Create(semanaListaId, localId);

        // Assert
        Assert.NotNull(lista);
        Assert.NotEqual(Guid.Empty, lista.Id);
        Assert.Equal(semanaListaId, lista.SemanaListaId);
        Assert.Equal(localId, lista.LocalId);
        Assert.Equal(StatusListaLocal.Pendente, lista.Status);
        Assert.Null(lista.SeparadoPorAlmoxarifeId);
        Assert.Null(lista.DataSeparacao);
        Assert.Null(lista.NomeRetiradoPor);
        Assert.Null(lista.DataRetirada);
        Assert.Empty(lista.Itens);
    }

    [Fact]
    public void AdicionarItem_ComSucesso_DeveIncluirNaColecao()
    {
        // Arrange
        var lista = ListaLocal.Create(Guid.NewGuid(), Guid.NewGuid());
        var materialId = Guid.NewGuid();

        // Act
        lista.AdicionarItem(materialId, 5);

        // Assert
        Assert.Single(lista.Itens);
        var item = Assert.Single(lista.Itens);
        Assert.Equal(materialId, item.MaterialId);
        Assert.Equal(5, item.QuantidadeSolicitada);
    }

    [Fact]
    public void AdicionarItem_Duplicado_DeveLancarDomainException()
    {
        // Arrange
        var lista = ListaLocal.Create(Guid.NewGuid(), Guid.NewGuid());
        var materialId = Guid.NewGuid();
        lista.AdicionarItem(materialId, 5);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            lista.AdicionarItem(materialId, 10)
        );
        Assert.Contains("já foi adicionado", exception.Message);
    }

    [Fact]
    public void RegistrarSeparacao_ComSucesso_DeveMudarStatusParaSeparado()
    {
        // Arrange
        var lista = ListaLocal.Create(Guid.NewGuid(), Guid.NewGuid());
        lista.AdicionarItem(Guid.NewGuid(), 5);
        var almoxarifeId = Guid.NewGuid();
        var dataSeparacao = DateTime.UtcNow;

        // Act
        lista.RegistrarSeparacao(almoxarifeId, dataSeparacao);

        // Assert
        Assert.Equal(StatusListaLocal.Separado, lista.Status);
        Assert.Equal(almoxarifeId, lista.SeparadoPorAlmoxarifeId);
        Assert.Equal(dataSeparacao, lista.DataSeparacao);
    }

    [Fact]
    public void RegistrarSeparacao_SemItens_DeveLancarDomainException()
    {
        // Arrange
        var lista = ListaLocal.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            lista.RegistrarSeparacao(Guid.NewGuid(), DateTime.UtcNow)
        );
        Assert.Contains("Não é possível separar uma lista que não possui itens", exception.Message);
    }

    [Fact]
    public void ConfirmarRecebimento_SemSeparacaoPrevia_DeveLancarDomainException()
    {
        // Arrange
        var lista = ListaLocal.Create(Guid.NewGuid(), Guid.NewGuid());
        lista.AdicionarItem(Guid.NewGuid(), 5);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            lista.ConfirmarRecebimento(possuiPendencias: false)
        );
        Assert.Contains("A lista só pode ser confirmada se já tiver sido separada", exception.Message);
    }

    [Fact]
    public void ConfirmarRecebimento_ComSeparacaoPrevia_DeveMudarStatusCorretamente()
    {
        // Arrange
        var lista = ListaLocal.Create(Guid.NewGuid(), Guid.NewGuid());
        lista.AdicionarItem(Guid.NewGuid(), 5);
        lista.RegistrarSeparacao(Guid.NewGuid(), DateTime.UtcNow);

        // Act
        lista.ConfirmarRecebimento(possuiPendencias: true);

        // Assert
        Assert.Equal(StatusListaLocal.ConfirmadoComPendencia, lista.Status);

        // Act 2 (redefinindo para outro status via reconstituição para testar sem pendências)
        var listaSemPendencia = ListaLocal.CreateWithId(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            StatusListaLocal.Separado, 
            Guid.NewGuid(), 
            DateTime.UtcNow, 
            null, 
            null
        );
        listaSemPendencia.ConfirmarRecebimento(possuiPendencias: false);
        Assert.Equal(StatusListaLocal.Confirmado, listaSemPendencia.Status);
    }

    [Fact]
    public void RegistrarRetirada_SemConfirmacao_DeveLancarDomainException()
    {
        // Arrange
        var lista = ListaLocal.Create(Guid.NewGuid(), Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            lista.RegistrarRetirada("João", DateTime.UtcNow)
        );
        Assert.Contains("A lista só pode ser retirada se estiver Confirmada", exception.Message);
    }

    [Fact]
    public void RegistrarRetirada_ComConfirmacao_DeveMudarStatusParaRetirado()
    {
        // Arrange
        var lista = ListaLocal.CreateWithId(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            StatusListaLocal.Confirmado, 
            Guid.NewGuid(), 
            DateTime.UtcNow, 
            null, 
            null
        );

        var dataRetirada = DateTime.UtcNow;

        // Act
        lista.RegistrarRetirada("João", dataRetirada);

        // Assert
        Assert.Equal(StatusListaLocal.Retirado, lista.Status);
        Assert.Equal("João", lista.NomeRetiradoPor);
        // Comparado em microssegundos: a entidade grava a data com a precisão do banco
        Assert.Equal(dataRetirada.Ticks / 10, lista.DataRetirada!.Value.Ticks / 10);
    }

    [Fact]
    public void CreateWithId_DadosInvalidosDeSeparacao_DeveLancarDomainException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            ListaLocal.CreateWithId(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                StatusListaLocal.Separado, // Separado requer almoxarife e data
                separadoPorAlmoxarifeId: null, 
                dataSeparacao: null, 
                nomeRetiradoPor: null, 
                dataRetirada: null
            )
        );
        Assert.Contains("SeparadoPorAlmoxarifeId é obrigatório", exception.Message);
    }
}
