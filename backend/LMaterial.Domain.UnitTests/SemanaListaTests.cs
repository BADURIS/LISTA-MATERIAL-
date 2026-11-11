using System;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;
using Xunit;

namespace LMaterial.Domain.UnitTests;

public class SemanaListaTests
{
    [Fact]
    public void Create_ComDadosValidos_DeveCriarInstanciaEmMontagem()
    {
        // Arrange
        var dataInicio = DateOnly.FromDateTime(DateTime.UtcNow);
        var dataFim = dataInicio.AddDays(7);
        var supervisorId = Guid.NewGuid();

        // Act
        var semana = SemanaLista.Create(dataInicio, dataFim, supervisorId);

        // Assert
        Assert.NotNull(semana);
        Assert.NotEqual(Guid.Empty, semana.Id);
        Assert.Equal(dataInicio, semana.DataInicio);
        Assert.Equal(dataFim, semana.DataFim);
        Assert.Equal(StatusSemanaLista.EmMontagem, semana.Status);
        Assert.Equal(supervisorId, semana.CriadoPorSupervisorId);
        Assert.True((DateTime.UtcNow - semana.DataCriacao).TotalSeconds < 5);
        Assert.Empty(semana.ListasLocais);
    }

    [Fact]
    public void Create_DataInicioPosteriorADataFim_DeveLancarDomainException()
    {
        // Arrange
        var dataInicio = DateOnly.FromDateTime(DateTime.UtcNow);
        var dataFim = dataInicio.AddDays(-1);
        var supervisorId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            SemanaLista.Create(dataInicio, dataFim, supervisorId)
        );
        Assert.Contains("data de início não pode ser posterior à data de fim", exception.Message);
    }

    [Fact]
    public void AdicionarListaLocal_ComSucesso_DeveIncluirNaColecao()
    {
        // Arrange
        var semana = SemanaLista.Create(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7), Guid.NewGuid());
        var localId = Guid.NewGuid();

        // Act
        semana.AdicionarListaLocal(localId);

        // Assert
        Assert.Single(semana.ListasLocais);
        var lista = Assert.Single(semana.ListasLocais);
        Assert.Equal(localId, lista.LocalId);
        Assert.Equal(semana.Id, lista.SemanaListaId);
    }

    [Fact]
    public void AdicionarListaLocal_Duplicada_DeveLancarDomainException()
    {
        // Arrange
        var semana = SemanaLista.Create(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7), Guid.NewGuid());
        var localId = Guid.NewGuid();
        semana.AdicionarListaLocal(localId);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            semana.AdicionarListaLocal(localId)
        );
        Assert.Contains("Já existe uma lista de materiais para este local nesta semana", exception.Message);
    }

    [Fact]
    public void AdicionarListaLocal_ForaDeMontagem_DeveLancarDomainException()
    {
        // Arrange
        var semana = SemanaLista.Create(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7), Guid.NewGuid());
        semana.AdicionarListaLocal(Guid.NewGuid());
        semana.EnviarParaSeparacao();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            semana.AdicionarListaLocal(Guid.NewGuid())
        );
        Assert.Contains("Novas listas de locais só podem ser adicionadas enquanto a semana estiver Em Montagem", exception.Message);
    }

    [Fact]
    public void EnviarParaSeparacao_SemListas_DeveLancarDomainException()
    {
        // Arrange
        var semana = SemanaLista.Create(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7), Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            semana.EnviarParaSeparacao()
        );
        Assert.Contains("Não é possível enviar para separação uma semana de lista sem nenhum local associado", exception.Message);
    }

    [Fact]
    public void EnviarParaSeparacao_ComListas_DeveMudarStatus()
    {
        // Arrange
        var semana = SemanaLista.Create(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7), Guid.NewGuid());
        semana.AdicionarListaLocal(Guid.NewGuid());

        // Act
        semana.EnviarParaSeparacao();

        // Assert
        Assert.Equal(StatusSemanaLista.EnviadaParaSeparacao, semana.Status);
    }

    [Fact]
    public void Finalizar_ComListasPendentes_DeveLancarDomainException()
    {
        // Arrange
        var semana = SemanaLista.Create(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7), Guid.NewGuid());
        semana.AdicionarListaLocal(Guid.NewGuid());
        semana.EnviarParaSeparacao();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            semana.Finalizar()
        );
        Assert.Contains("existem listas locais pendentes", exception.Message);
    }

    [Fact]
    public void Finalizar_ComTodasAsListasFinalizadas_DeveMudarStatusParaFinalizada()
    {
        // Arrange
        var semanaId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();
        var semana = SemanaLista.CreateWithId(
            semanaId, 
            DateOnly.FromDateTime(DateTime.UtcNow), 
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7), 
            StatusSemanaLista.EnviadaParaSeparacao, 
            supervisorId, 
            DateTime.UtcNow
        );

        // Simulando listas em status finalizado
        var lista1 = ListaLocal.CreateWithId(Guid.NewGuid(), semanaId, Guid.NewGuid(), StatusListaLocal.Confirmado, Guid.NewGuid(), DateTime.UtcNow, null, null);
        var lista2 = ListaLocal.CreateWithId(Guid.NewGuid(), semanaId, Guid.NewGuid(), StatusListaLocal.Retirado, Guid.NewGuid(), DateTime.UtcNow, "João", DateTime.UtcNow);

        // Acessando backing field via Reflection ou mockando
        // Como no nosso modelo _listasLocais é uma lista interna privada, podemos usar um método de teste ou Reflection.
        // Vamos usar Reflection para preencher o backing field privado _listasLocais para manter o encapsulamento puro.
        var type = typeof(SemanaLista);
        var field = type.GetField("_listasLocais", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = (List<ListaLocal>?)field?.GetValue(semana);
        list?.Add(lista1);
        list?.Add(lista2);

        // Act
        semana.Finalizar();

        // Assert
        Assert.Equal(StatusSemanaLista.Finalizada, semana.Status);
    }
}
