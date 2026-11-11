using System;
using System.Collections.Generic;
using System.Linq;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;

namespace LMaterial.Domain.Entities;

public class SemanaLista
{
    private readonly List<ListaLocal> _listasLocais = new();

    public Guid Id { get; private set; }
    public DateOnly DataInicio { get; private set; }
    public DateOnly DataFim { get; private set; }
    public StatusSemanaLista Status { get; private set; }
    public Guid CriadoPorSupervisorId { get; private set; }
    public DateTime DataCriacao { get; private set; }

    // Relação de Navegação
    public IReadOnlyCollection<ListaLocal> ListasLocais => _listasLocais.AsReadOnly();

    // Construtor privado para o ORM
    private SemanaLista()
    {
    }

    private SemanaLista(
        Guid id, 
        DateOnly dataInicio, 
        DateOnly dataFim, 
        StatusSemanaLista status, 
        Guid criadoPorSupervisorId, 
        DateTime dataCriacao)
    {
        if (id == Guid.Empty)
            throw new DomainException("O Id da semana de lista não pode ser vazio.");

        if (criadoPorSupervisorId == Guid.Empty)
            throw new DomainException("O Id do supervisor criador não pode ser vazio.");

        if (dataInicio > dataFim)
            throw new DomainException("A data de início não pode ser posterior à data de fim.");

        if (dataCriacao == default)
            throw new DomainException("Uma data de criação válida deve ser fornecida.");

        Id = id;
        DataInicio = dataInicio;
        DataFim = dataFim;
        Status = status;
        CriadoPorSupervisorId = criadoPorSupervisorId;
        DataCriacao = dataCriacao;
    }

    public static SemanaLista Create(DateOnly dataInicio, DateOnly dataFim, Guid supervisorId)
    {
        return new SemanaLista(
            Guid.NewGuid(), 
            dataInicio, 
            dataFim, 
            StatusSemanaLista.EmMontagem, 
            supervisorId, 
            DateTime.UtcNow
        );
    }

    public static SemanaLista CreateWithId(
        Guid id, 
        DateOnly dataInicio, 
        DateOnly dataFim, 
        StatusSemanaLista status, 
        Guid supervisorId, 
        DateTime dataCriacao)
    {
        return new SemanaLista(id, dataInicio, dataFim, status, supervisorId, dataCriacao);
    }

    // Métodos de Comportamento

    public void AdicionarListaLocal(Guid localId)
    {
        if (localId == Guid.Empty)
            throw new DomainException("O Id do local é obrigatório.");

        if (Status != StatusSemanaLista.EmMontagem)
            throw new DomainException("Novas listas de locais só podem ser adicionadas enquanto a semana estiver Em Montagem.");

        if (_listasLocais.Any(l => l.LocalId == localId))
            throw new DomainException("Já existe uma lista de materiais para este local nesta semana.");

        var novaListaLocal = ListaLocal.Create(Id, localId);
        _listasLocais.Add(novaListaLocal);
    }

    public void EnviarParaSeparacao()
    {
        if (Status != StatusSemanaLista.EmMontagem)
            throw new DomainException("Apenas semanas de lista Em Montagem podem ser enviadas para separação.");

        if (!_listasLocais.Any())
            throw new DomainException("Não é possível enviar para separação uma semana de lista sem nenhum local associado.");

        Status = StatusSemanaLista.EnviadaParaSeparacao;
    }

    public void MarcarSeparacaoConcluida()
    {
        if (Status != StatusSemanaLista.EnviadaParaSeparacao)
            throw new DomainException("Apenas semanas de lista Enviadas Para Separação podem ter a separação concluída.");

        if (_listasLocais.Any(l => l.Status == StatusListaLocal.Pendente))
            throw new DomainException("Não é possível concluir a separação enquanto houver listas locais pendentes.");

        Status = StatusSemanaLista.SeparacaoConcluida;
    }

    public void Finalizar()
    {
        if (Status != StatusSemanaLista.EnviadaParaSeparacao && Status != StatusSemanaLista.SeparacaoConcluida)
            throw new DomainException("Apenas semanas de lista que foram Enviadas Para Separação podem ser finalizadas.");

        // Regra de negócio: todas as listas de locais associadas devem estar em um estado final (ex: Retirado, Confirmado ou ConfirmadoComPendencia)
        if (_listasLocais.Any(l => l.Status != StatusListaLocal.Retirado && 
                                   l.Status != StatusListaLocal.Confirmado && 
                                   l.Status != StatusListaLocal.ConfirmadoComPendencia))
        {
            throw new DomainException("Não é possível finalizar a semana pois existem listas locais pendentes de separação ou retirada.");
        }

        Status = StatusSemanaLista.Finalizada;
    }
}
