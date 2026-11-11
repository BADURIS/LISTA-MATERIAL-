using System;
using System.Collections.Generic;
using System.Linq;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;

namespace LMaterial.Domain.Entities;

public class ListaLocal
{
    private readonly List<ListaLocalItem> _itens = new();

    public Guid Id { get; private set; }
    public Guid SemanaListaId { get; private set; }
    public Guid LocalId { get; private set; }
    public StatusListaLocal Status { get; private set; }
    public Guid? SeparadoPorAlmoxarifeId { get; private set; }
    public DateTime? DataSeparacao { get; private set; }
    public string? NomeRetiradoPor { get; private set; }
    public DateTime? DataRetirada { get; private set; }
    public Guid? RegistradoPorUsuarioId { get; private set; }
    public string? AssinaturaDigital { get; private set; }

    // Relações de Navegação
    public virtual Local? Local { get; private set; }
    public IReadOnlyCollection<ListaLocalItem> Itens => _itens.AsReadOnly();

    // Construtor privado para o ORM
    private ListaLocal()
    {
    }

    private ListaLocal(
        Guid id, 
        Guid semanaListaId, 
        Guid localId, 
        StatusListaLocal status, 
        Guid? separadoPorAlmoxarifeId, 
        DateTime? dataSeparacao, 
        string? nomeRetiradoPor, 
        DateTime? dataRetirada)
    {
        if (id == Guid.Empty)
            throw new DomainException("O Id da lista local não pode ser vazio.");

        if (semanaListaId == Guid.Empty)
            throw new DomainException("O Id da semana de lista vinculada não pode ser vazio.");

        if (localId == Guid.Empty)
            throw new DomainException("O Id do local vinculado não pode ser vazio.");

        Id = id;
        SemanaListaId = semanaListaId;
        LocalId = localId;
        Status = status;
        SeparadoPorAlmoxarifeId = separadoPorAlmoxarifeId;
        DataSeparacao = dataSeparacao;
        NomeRetiradoPor = nomeRetiradoPor;
        DataRetirada = dataRetirada;

        ValidarInvariantesDeStatus();
    }

    public static ListaLocal Create(Guid semanaListaId, Guid localId)
    {
        return new ListaLocal(
            Guid.NewGuid(), 
            semanaListaId, 
            localId, 
            StatusListaLocal.Pendente, 
            separadoPorAlmoxarifeId: null, 
            dataSeparacao: null, 
            nomeRetiradoPor: null, 
            dataRetirada: null
        );
    }

    public static ListaLocal CreateWithId(
        Guid id, 
        Guid semanaListaId, 
        Guid localId, 
        StatusListaLocal status, 
        Guid? separadoPorAlmoxarifeId, 
        DateTime? dataSeparacao, 
        string? nomeRetiradoPor, 
        DateTime? dataRetirada)
    {
        return new ListaLocal(id, semanaListaId, localId, status, separadoPorAlmoxarifeId, dataSeparacao, nomeRetiradoPor, dataRetirada);
    }

    // Métodos de Comportamento

    public void AdicionarItem(Guid materialId, int quantidadeSolicitada)
    {
        if (materialId == Guid.Empty)
            throw new DomainException("O Id do material é obrigatório.");

        if (_itens.Any(i => i.MaterialId == materialId))
            throw new DomainException("Este material já foi adicionado a esta lista local.");

        var novoItem = ListaLocalItem.Create(Id, materialId, quantidadeSolicitada);
        _itens.Add(novoItem);
    }

    public void RegistrarSeparacao(Guid almoxarifeId, DateTime dataSeparacao)
    {
        if (almoxarifeId == Guid.Empty)
            throw new DomainException("O Id do almoxarife é obrigatório para registrar a separação.");

        if (dataSeparacao == default)
            throw new DomainException("Uma data de separação válida deve ser informada.");

        if (!_itens.Any())
            throw new DomainException("Não é possível separar uma lista que não possui itens.");

        SeparadoPorAlmoxarifeId = almoxarifeId;
        DataSeparacao = dataSeparacao;
        Status = StatusListaLocal.Separado;
    }

    public void ConfirmarRecebimento(bool possuiPendencias)
    {
        if (Status != StatusListaLocal.Separado)
            throw new DomainException("A lista só pode ser confirmada se já tiver sido separada.");

        Status = possuiPendencias ? StatusListaLocal.ConfirmadoComPendencia : StatusListaLocal.Confirmado;
    }

    public void RegistrarRetirada(string nomeRetiradoPor, DateTime dataRetirada, Guid? registradoPorUsuarioId = null)
    {
        if (string.IsNullOrWhiteSpace(nomeRetiradoPor))
            throw new DomainException("O nome de quem retirou a lista é obrigatório.");

        if (dataRetirada == default)
            throw new DomainException("Uma data de retirada válida deve ser informada.");

        if (Status != StatusListaLocal.Confirmado && Status != StatusListaLocal.ConfirmadoComPendencia)
            throw new DomainException("A lista só pode ser retirada se estiver Confirmada ou Confirmada com Pendência.");

        // O PostgreSQL guarda microssegundos: a data é truncada antes de gravar para que a assinatura continue válida ao recarregar
        dataRetirada = PrecisaoDoBanco(dataRetirada);

        NomeRetiradoPor = nomeRetiradoPor.Trim();
        DataRetirada = dataRetirada;
        RegistradoPorUsuarioId = registradoPorUsuarioId;
        AssinaturaDigital = GerarAssinaturaDigital(NomeRetiradoPor, dataRetirada, registradoPorUsuarioId);
        Status = StatusListaLocal.Retirado;
    }

    // Assinatura digital: hash SHA-256 de nome, data/hora, responsável pelo registro e identificador da lista.
    // Qualquer alteração posterior nesses dados torna a assinatura inválida na verificação.
    public string GerarAssinaturaDigital(string nome, DateTime dataRetirada, Guid? registradoPorUsuarioId)
    {
        var dataNoBanco = PrecisaoDoBanco(dataRetirada);
        var conteudo = $"{Id:N}|{nome}|{dataNoBanco:yyyy-MM-ddTHH:mm:ss.ffffffZ}|{registradoPorUsuarioId:N}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(conteudo));
        return Convert.ToBase64String(hash);
    }

    public bool VerificarAssinaturaDigital()
    {
        if (string.IsNullOrEmpty(AssinaturaDigital) || string.IsNullOrEmpty(NomeRetiradoPor) || !DataRetirada.HasValue)
            return false;

        return AssinaturaDigital == GerarAssinaturaDigital(NomeRetiradoPor, DataRetirada.Value, RegistradoPorUsuarioId);
    }

    // Remove a parte abaixo de 1 microssegundo (100 ns), que o banco não preserva, e força o horário em UTC
    private static DateTime PrecisaoDoBanco(DateTime data)
    {
        var utc = data.Kind == DateTimeKind.Local ? data.ToUniversalTime() : DateTime.SpecifyKind(data, DateTimeKind.Utc);
        return new DateTime(utc.Ticks - (utc.Ticks % 10), DateTimeKind.Utc);
    }

    private void ValidarInvariantesDeStatus()
    {
        if (Status == StatusListaLocal.Separado)
        {
            if (!SeparadoPorAlmoxarifeId.HasValue || SeparadoPorAlmoxarifeId == Guid.Empty)
                throw new DomainException("SeparadoPorAlmoxarifeId é obrigatório quando o status é Separado.");

            if (!DataSeparacao.HasValue || DataSeparacao == default)
                throw new DomainException("DataSeparacao é obrigatória quando o status é Separado.");
        }

        if (Status == StatusListaLocal.Retirado)
        {
            if (string.IsNullOrWhiteSpace(NomeRetiradoPor))
                throw new DomainException("NomeRetiradoPor é obrigatório quando o status é Retirado.");

            if (!DataRetirada.HasValue || DataRetirada == default)
                throw new DomainException("DataRetirada é obrigatória quando o status é Retirado.");
        }
    }
}
