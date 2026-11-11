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

    // Marcado quando o local não precisa de material na semana: conta como preenchido e fica fora da separação
    public bool SemPedido { get; private set; }

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
        SemPedido = false;
    }

    // Um local está pronto para envio quando tem ao menos um item solicitado ou foi marcado como sem pedido
    public bool EstaPreenchida => SemPedido || _itens.Count > 0;

    // Substitui o pedido do local de uma só vez (tela de preenchimento). Quantidade zero remove o material do pedido.
    // Os itens que continuam no pedido mantêm o mesmo Id.
    public void DefinirItens(IEnumerable<(Guid MaterialId, int Quantidade)> itens)
    {
        if (itens == null)
            throw new DomainException("Os itens do pedido são obrigatórios.");

        if (Status != StatusListaLocal.Pendente)
            throw new DomainException("O pedido deste local não pode mais ser alterado.");

        var pedido = itens.ToList();

        if (pedido.Any(i => i.MaterialId == Guid.Empty))
            throw new DomainException("O Id do material é obrigatório.");

        if (pedido.Any(i => i.Quantidade < 0))
            throw new DomainException("A quantidade solicitada não pode ser negativa.");

        if (pedido.GroupBy(i => i.MaterialId).Any(g => g.Count() > 1))
            throw new DomainException("O mesmo material foi informado mais de uma vez no pedido.");

        var desejado = pedido.Where(i => i.Quantidade > 0).ToDictionary(i => i.MaterialId, i => i.Quantidade);

        _itens.RemoveAll(i => !desejado.ContainsKey(i.MaterialId));

        foreach (var (materialId, quantidade) in desejado)
        {
            var existente = _itens.FirstOrDefault(i => i.MaterialId == materialId);
            if (existente != null)
                existente.AtualizarQuantidadeSolicitada(quantidade);
            else
                _itens.Add(ListaLocalItem.Create(Id, materialId, quantidade));
        }

        if (_itens.Count > 0)
            SemPedido = false;
    }

    // Marcar como sem pedido zera o pedido do local
    public void DefinirSemPedido(bool semPedido)
    {
        if (Status != StatusListaLocal.Pendente)
            throw new DomainException("O pedido deste local não pode mais ser alterado.");

        if (semPedido)
            _itens.Clear();

        SemPedido = semPedido;
    }

    // Registro da quantidade separada de um item pelo almoxarife, enquanto a lista ainda não foi fechada
    public void RegistrarSeparacaoItem(Guid itemId, int quantidadeSeparada, string? observacao)
    {
        if (SemPedido)
            throw new DomainException("Este local está sem pedido nesta semana e não tem nada a separar.");

        if (Status != StatusListaLocal.Pendente)
            throw new DomainException("A separação deste local já foi concluída e não pode mais ser alterada.");

        var item = _itens.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException("Item não encontrado nesta lista.");

        if (quantidadeSeparada > item.QuantidadeSolicitada)
            throw new DomainException($"A quantidade separada ({quantidadeSeparada}) não pode ser maior que a solicitada ({item.QuantidadeSolicitada}).");

        item.RegistrarSeparacao(quantidadeSeparada, string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim());
    }

    public bool TodosItensComQuantidadeSeparada => _itens.Count > 0 && _itens.All(i => i.QuantidadeSeparada.HasValue);

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

        // Item separado em quantidade menor que a solicitada é pendência mesmo que não seja informada
        var temFalta = _itens.Any(i => i.StatusItem == StatusItem.Faltando);
        Status = possuiPendencias || temFalta ? StatusListaLocal.ConfirmadoComPendencia : StatusListaLocal.Confirmado;
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
