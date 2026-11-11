using System;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;
using Xunit;

namespace LMaterial.Domain.UnitTests;

public class FluxoAssinaturaTests
{
    private static ListaLocal CriarListaConfirmada(out Guid coordenadorId)
    {
        var lista = ListaLocal.Create(Guid.NewGuid(), Guid.NewGuid());
        lista.AdicionarItem(Guid.NewGuid(), 5);
        lista.RegistrarSeparacao(Guid.NewGuid(), DateTime.UtcNow);
        lista.ConfirmarRecebimento(possuiPendencias: false);

        coordenadorId = Guid.NewGuid();
        return lista;
    }

    [Fact]
    public void RegistrarRetirada_DeveGerarAssinaturaDigitalValida()
    {
        var lista = CriarListaConfirmada(out var coordenadorId);

        lista.RegistrarRetirada("Maria Souza", DateTime.UtcNow, coordenadorId);

        Assert.Equal(StatusListaLocal.Retirado, lista.Status);
        Assert.Equal(coordenadorId, lista.RegistradoPorUsuarioId);
        Assert.False(string.IsNullOrEmpty(lista.AssinaturaDigital));
        Assert.True(lista.VerificarAssinaturaDigital());
    }

    [Fact]
    public void VerificarAssinaturaDigital_DeveFalharSeONomeForAlteradoDepoisDaAssinatura()
    {
        var lista = CriarListaConfirmada(out var coordenadorId);
        lista.RegistrarRetirada("Maria Souza", DateTime.UtcNow, coordenadorId);
        Assert.True(lista.VerificarAssinaturaDigital());

        // Simula adulteração direta do nome no banco, sem recalcular a assinatura
        typeof(ListaLocal)
            .GetProperty(nameof(ListaLocal.NomeRetiradoPor))!
            .SetValue(lista, "Joana Lima");

        Assert.False(lista.VerificarAssinaturaDigital());
    }

    [Fact]
    public void RegistrarRetirada_ComPrecisaoMaiorQueOBanco_DeveManterAssinaturaValidaAoRecarregar()
    {
        var lista = CriarListaConfirmada(out var coordenadorId);
        // Horário com 100 ns, que o PostgreSQL não preserva
        var dataComNanos = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc).AddTicks(7);

        lista.RegistrarRetirada("Maria Souza", dataComNanos, coordenadorId);

        // Simula o recarregamento do banco: o valor gravado é o que a entidade expõe após o RegistrarRetirada
        var recarregada = ListaLocal.CreateWithId(
            lista.Id, lista.SemanaListaId, lista.LocalId, lista.Status,
            lista.SeparadoPorAlmoxarifeId, lista.DataSeparacao,
            lista.NomeRetiradoPor, lista.DataRetirada);
        typeof(ListaLocal).GetProperty(nameof(ListaLocal.AssinaturaDigital))!.SetValue(recarregada, lista.AssinaturaDigital);
        typeof(ListaLocal).GetProperty(nameof(ListaLocal.RegistradoPorUsuarioId))!.SetValue(recarregada, lista.RegistradoPorUsuarioId);

        Assert.Equal(0, lista.DataRetirada!.Value.Ticks % 10);
        Assert.True(recarregada.VerificarAssinaturaDigital());
    }

    [Fact]
    public void GerarAssinaturaDigital_ComMesmosDados_DeveSerDeterministica()
    {
        var lista = CriarListaConfirmada(out var coordenadorId);
        var data = new DateTime(2026, 10, 14, 9, 30, 0, DateTimeKind.Utc);

        var primeira = lista.GerarAssinaturaDigital("Maria Souza", data, coordenadorId);
        var segunda = lista.GerarAssinaturaDigital("Maria Souza", data, coordenadorId);
        var outroNome = lista.GerarAssinaturaDigital("Joana Lima", data, coordenadorId);

        Assert.Equal(primeira, segunda);
        Assert.NotEqual(primeira, outroNome);
    }

    [Fact]
    public void MarcarSeparacaoConcluida_ComListaPendente_DeveLancarDomainException()
    {
        var semana = SemanaLista.Create(new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 16), Guid.NewGuid());
        semana.AdicionarListaLocal(Guid.NewGuid());
        semana.EnviarParaSeparacao();

        Assert.Throws<DomainException>(() => semana.MarcarSeparacaoConcluida());
    }

    [Fact]
    public void MarcarSeparacaoConcluida_QuandoTodasSeparadas_DeveMudarStatus()
    {
        var semana = SemanaLista.Create(new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 16), Guid.NewGuid());
        semana.AdicionarListaLocal(Guid.NewGuid());
        semana.EnviarParaSeparacao();

        var lista = semana.ListasLocais.Single();
        lista.AdicionarItem(Guid.NewGuid(), 2);
        lista.RegistrarSeparacao(Guid.NewGuid(), DateTime.UtcNow);

        semana.MarcarSeparacaoConcluida();

        Assert.Equal(StatusSemanaLista.SeparacaoConcluida, semana.Status);
    }

    [Fact]
    public void Finalizar_DeveAceitarSemanaComSeparacaoConcluida()
    {
        var semana = SemanaLista.Create(new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 16), Guid.NewGuid());
        semana.AdicionarListaLocal(Guid.NewGuid());
        semana.EnviarParaSeparacao();

        var lista = semana.ListasLocais.Single();
        lista.AdicionarItem(Guid.NewGuid(), 2);
        lista.RegistrarSeparacao(Guid.NewGuid(), DateTime.UtcNow);
        semana.MarcarSeparacaoConcluida();

        // Lista precisa estar em estado final para finalizar a semana
        lista.ConfirmarRecebimento(possuiPendencias: false);
        lista.RegistrarRetirada("Maria Souza", DateTime.UtcNow, Guid.NewGuid());

        semana.Finalizar();

        Assert.Equal(StatusSemanaLista.Finalizada, semana.Status);
    }
}
