using System;
using System.Linq;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;
using Xunit;

namespace LMaterial.Domain.UnitTests;

// Regras trazidas do sistema LISTA MATERIAL: preenchimento por local, "sem pedido" e trava de envio
public class PreenchimentoESemPedidoTests
{
    private static readonly Guid MaterialA = Guid.NewGuid();
    private static readonly Guid MaterialB = Guid.NewGuid();
    private static readonly Guid Almoxarife = Guid.NewGuid();

    private static (SemanaLista Semana, Guid Local1, Guid Local2) SemanaComDoisLocais()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var semana = SemanaLista.Create(hoje, hoje.AddDays(6), Guid.NewGuid());
        var local1 = Guid.NewGuid();
        var local2 = Guid.NewGuid();
        semana.AdicionarListaLocal(local1);
        semana.AdicionarListaLocal(local2);
        return (semana, local1, local2);
    }

    private static ListaLocal Lista(SemanaLista semana, Guid localId) => semana.ListasLocais.Single(l => l.LocalId == localId);

    [Fact]
    public void PreencherLocal_IgnoraQuantidadeZero_EMantemIdDoItemExistente()
    {
        var (semana, local1, _) = SemanaComDoisLocais();

        semana.PreencherLocal(local1, new[] { (MaterialA, 5), (MaterialB, 0) });
        var idOriginal = Lista(semana, local1).Itens.Single().Id;

        semana.PreencherLocal(local1, new[] { (MaterialA, 8), (MaterialB, 2) });

        var lista = Lista(semana, local1);
        Assert.Equal(2, lista.Itens.Count);
        var itemA = lista.Itens.Single(i => i.MaterialId == MaterialA);
        Assert.Equal(idOriginal, itemA.Id);
        Assert.Equal(8, itemA.QuantidadeSolicitada);
    }

    [Fact]
    public void PreencherLocal_ComTudoZerado_RemoveItensEDeixaLocalPendente()
    {
        var (semana, local1, _) = SemanaComDoisLocais();
        semana.PreencherLocal(local1, new[] { (MaterialA, 5) });

        semana.PreencherLocal(local1, new[] { (MaterialA, 0) });

        Assert.Empty(Lista(semana, local1).Itens);
        Assert.False(Lista(semana, local1).EstaPreenchida);
    }

    [Fact]
    public void PreencherLocal_MaterialRepetido_DeveLancarDomainException()
    {
        var (semana, local1, _) = SemanaComDoisLocais();

        Assert.Throws<DomainException>(() => semana.PreencherLocal(local1, new[] { (MaterialA, 1), (MaterialA, 2) }));
    }

    [Fact]
    public void PreencherLocal_QuantidadeNegativa_DeveLancarDomainException()
    {
        var (semana, local1, _) = SemanaComDoisLocais();

        Assert.Throws<DomainException>(() => semana.PreencherLocal(local1, new[] { (MaterialA, -1) }));
    }

    [Fact]
    public void PreencherLocal_LocalForaDaSemana_DeveLancarDomainException()
    {
        var (semana, _, _) = SemanaComDoisLocais();

        var ex = Assert.Throws<DomainException>(() => semana.PreencherLocal(Guid.NewGuid(), new[] { (MaterialA, 1) }));
        Assert.Contains("não faz parte da semana", ex.Message);
    }

    [Fact]
    public void DefinirSemPedido_ZeraItensEContaComoPreenchido()
    {
        var (semana, local1, _) = SemanaComDoisLocais();
        semana.PreencherLocal(local1, new[] { (MaterialA, 5) });

        semana.DefinirSemPedido(local1, true);

        var lista = Lista(semana, local1);
        Assert.True(lista.SemPedido);
        Assert.Empty(lista.Itens);
        Assert.True(lista.EstaPreenchida);
    }

    [Fact]
    public void PreencherLocal_DepoisDeSemPedido_DesmarcaSemPedido()
    {
        var (semana, local1, _) = SemanaComDoisLocais();
        semana.DefinirSemPedido(local1, true);

        semana.PreencherLocal(local1, new[] { (MaterialA, 3) });

        Assert.False(Lista(semana, local1).SemPedido);
    }

    [Fact]
    public void EnviarParaSeparacao_ComLocalSemPedidoRealizado_DeveLancarDomainException()
    {
        var (semana, local1, _) = SemanaComDoisLocais();
        semana.PreencherLocal(local1, new[] { (MaterialA, 5) });

        var ex = Assert.Throws<DomainException>(() => semana.EnviarParaSeparacao());
        Assert.Contains("1 local(is) ainda sem pedido", ex.Message);
        Assert.Equal(StatusSemanaLista.EmMontagem, semana.Status);
    }

    [Fact]
    public void EnviarParaSeparacao_TodosLocaisSemPedido_DeveLancarDomainException()
    {
        var (semana, local1, local2) = SemanaComDoisLocais();
        semana.DefinirSemPedido(local1, true);
        semana.DefinirSemPedido(local2, true);

        var ex = Assert.Throws<DomainException>(() => semana.EnviarParaSeparacao());
        Assert.Contains("nenhum local tem pedido", ex.Message);
    }

    [Fact]
    public void EnviarParaSeparacao_TodosPreenchidosOuSemPedido_Envia()
    {
        var (semana, local1, local2) = SemanaComDoisLocais();
        semana.PreencherLocal(local1, new[] { (MaterialA, 5) });
        semana.DefinirSemPedido(local2, true);

        semana.EnviarParaSeparacao();

        Assert.Equal(StatusSemanaLista.EnviadaParaSeparacao, semana.Status);
    }

    [Fact]
    public void PreencherLocal_DepoisDoEnvio_DeveLancarDomainException()
    {
        var (semana, local1, local2) = SemanaComDoisLocais();
        semana.PreencherLocal(local1, new[] { (MaterialA, 5) });
        semana.DefinirSemPedido(local2, true);
        semana.EnviarParaSeparacao();

        Assert.Throws<DomainException>(() => semana.PreencherLocal(local1, new[] { (MaterialA, 9) }));
        Assert.Throws<DomainException>(() => semana.DefinirSemPedido(local2, false));
    }

    [Fact]
    public void SeparacaoConcluida_IgnoraLocaisSemPedido()
    {
        var (semana, local1, local2) = SemanaComDoisLocais();
        semana.PreencherLocal(local1, new[] { (MaterialA, 5) });
        semana.DefinirSemPedido(local2, true);
        semana.EnviarParaSeparacao();

        var lista = Lista(semana, local1);
        lista.RegistrarSeparacaoItem(lista.Itens.Single().Id, 5, null);
        lista.RegistrarSeparacao(Almoxarife, DateTime.UtcNow);

        semana.MarcarSeparacaoConcluida();

        Assert.Equal(StatusSemanaLista.SeparacaoConcluida, semana.Status);
    }

    [Fact]
    public void RegistrarSeparacaoItem_MaiorQueSolicitado_DeveLancarDomainException()
    {
        var (semana, local1, _) = SemanaComDoisLocais();
        semana.PreencherLocal(local1, new[] { (MaterialA, 5) });
        var lista = Lista(semana, local1);

        var ex = Assert.Throws<DomainException>(() => lista.RegistrarSeparacaoItem(lista.Itens.Single().Id, 6, null));
        Assert.Contains("não pode ser maior que a solicitada", ex.Message);
    }

    [Fact]
    public void RegistrarSeparacaoItem_ListaJaSeparada_DeveLancarDomainException()
    {
        var (semana, local1, _) = SemanaComDoisLocais();
        semana.PreencherLocal(local1, new[] { (MaterialA, 5) });
        var lista = Lista(semana, local1);
        var itemId = lista.Itens.Single().Id;
        lista.RegistrarSeparacaoItem(itemId, 5, null);
        lista.RegistrarSeparacao(Almoxarife, DateTime.UtcNow);

        Assert.Throws<DomainException>(() => lista.RegistrarSeparacaoItem(itemId, 3, null));
    }

    [Fact]
    public void ConfirmarRecebimento_ComItemFaltando_FicaComPendenciaMesmoSemInformar()
    {
        var (semana, local1, _) = SemanaComDoisLocais();
        semana.PreencherLocal(local1, new[] { (MaterialA, 5) });
        var lista = Lista(semana, local1);
        lista.RegistrarSeparacaoItem(lista.Itens.Single().Id, 3, "Faltou no estoque");
        lista.RegistrarSeparacao(Almoxarife, DateTime.UtcNow);

        lista.ConfirmarRecebimento(possuiPendencias: false);

        Assert.Equal(StatusListaLocal.ConfirmadoComPendencia, lista.Status);
        Assert.Equal("Faltou no estoque", lista.Itens.Single().Observacao);
    }
}
