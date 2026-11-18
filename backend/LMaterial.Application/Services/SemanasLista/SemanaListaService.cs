using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.SemanasLista;
using LMaterial.Domain.Entities;
using LMaterial.Domain.Enums;
using LMaterial.Domain.Exceptions;
using LMaterial.Domain.Interfaces;
using LMaterial.Domain.Repositories;

namespace LMaterial.Application.Services.SemanasLista;

public class SemanaListaService : ISemanaListaService
{
    private readonly ISemanaListaRepository _semanaListaRepository;
    private readonly IListaLocalRepository _listaLocalRepository;
    private readonly IMaterialRepository _materialRepository;
    private readonly ILocalRepository _localRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SemanaListaService(
        ISemanaListaRepository semanaListaRepository,
        IListaLocalRepository listaLocalRepository,
        IMaterialRepository materialRepository,
        ILocalRepository localRepository,
        IUsuarioRepository usuarioRepository,
        INotificacaoRepository notificacaoRepository,
        IUnitOfWork unitOfWork)
    {
        _semanaListaRepository = semanaListaRepository ?? throw new ArgumentNullException(nameof(semanaListaRepository));
        _listaLocalRepository = listaLocalRepository ?? throw new ArgumentNullException(nameof(listaLocalRepository));
        _materialRepository = materialRepository ?? throw new ArgumentNullException(nameof(materialRepository));
        _localRepository = localRepository ?? throw new ArgumentNullException(nameof(localRepository));
        _usuarioRepository = usuarioRepository ?? throw new ArgumentNullException(nameof(usuarioRepository));
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    // A semana já nasce com todos os locais ativos, prontos para receber o pedido
    public async Task<SemanaListaDto> CriarSemanaAsync(CriarSemanaRequest request, Guid supervisorId, CancellationToken ct = default)
    {
        var emAndamento = await _semanaListaRepository.GetSemanaAtualAsync(ct);
        if (emAndamento != null)
        {
            throw new DomainException(
                $"Já existe uma semana em andamento ({emAndamento.DataInicio:dd/MM/yyyy} a {emAndamento.DataFim:dd/MM/yyyy}). " +
                "Conclua as retiradas dela antes de criar uma nova.");
        }

        var semana = SemanaLista.Create(request.DataInicio, request.DataFim, supervisorId);

        var locaisAtivos = (await _localRepository.GetAtivosAsync(ct)).OrderBy(l => l.OrdemExibicao).ToList();
        if (locaisAtivos.Count == 0)
            throw new DomainException("Não há locais ativos cadastrados para montar a semana.");

        foreach (var local in locaisAtivos)
        {
            semana.AdicionarListaLocal(local.Id);
        }

        await _semanaListaRepository.AddAsync(semana, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await MapToDtoAsync(semana, ct);
    }

    public async Task<SemanaListaDto> AdicionarLocalAsync(Guid semanaId, AdicionarLocalRequest request, CancellationToken ct = default)
    {
        var semana = await ObterSemanaAsync(semanaId, ct);

        _ = await _localRepository.GetByIdAsync(request.LocalId, ct)
            ?? throw new DomainException("Local não encontrado.");

        semana.AdicionarListaLocal(request.LocalId);

        return await SalvarERetornarAsync(semana, ct);
    }

    public async Task<SemanaListaDto> AdicionarItemAsync(Guid semanaId, AdicionarItemRequest request, CancellationToken ct = default)
    {
        var semana = await ObterSemanaAsync(semanaId, ct);

        if (semana.Status != StatusSemanaLista.EmMontagem)
            throw new DomainException("A semana já foi enviada ao almoxarifado: os pedidos não podem mais ser alterados.");

        _ = await _materialRepository.GetByIdAsync(request.MaterialId, ct)
            ?? throw new DomainException("Material não encontrado.");

        ListaLocal? listaLocal = null;
        if (request.LocalId.HasValue)
        {
            listaLocal = semana.ListasLocais.FirstOrDefault(l => l.LocalId == request.LocalId.Value || l.Id == request.LocalId.Value);
        }
        else if (semana.ListasLocais.Count == 1)
        {
            listaLocal = semana.ListasLocais.First();
        }

        if (listaLocal == null)
            throw new DomainException("Lista local não encontrada para esta semana.");

        listaLocal.AdicionarItem(request.MaterialId, request.QuantidadeSolicitada);

        return await SalvarERetornarAsync(semana, ct);
    }

    public async Task<SemanaListaDto> PreencherLocalAsync(Guid semanaId, Guid localId, PreencherLocalRequest request, CancellationToken ct = default)
    {
        var semana = await ObterSemanaAsync(semanaId, ct);
        var itens = request?.Itens ?? new List<ItemPedidoRequest>();

        // Só materiais ativos do cadastro podem ser pedidos
        var materiaisAtivos = (await _materialRepository.GetAtivosAsync(ct)).Select(m => m.Id).ToHashSet();
        var desconhecido = itens.FirstOrDefault(i => i.QuantidadeSolicitada > 0 && !materiaisAtivos.Contains(i.MaterialId));
        if (desconhecido != null)
            throw new DomainException("O pedido contém um material inexistente ou inativo.");

        semana.PreencherLocal(localId, itens.Select(i => (i.MaterialId, i.QuantidadeSolicitada)));

        return await SalvarERetornarAsync(semana, ct);
    }

    public async Task<SemanaListaDto> DefinirSemPedidoAsync(Guid semanaId, Guid localId, DefinirSemPedidoRequest request, CancellationToken ct = default)
    {
        var semana = await ObterSemanaAsync(semanaId, ct);
        semana.DefinirSemPedido(localId, request.SemPedido);
        return await SalvarERetornarAsync(semana, ct);
    }

    public async Task<SemanaListaDto> EnviarParaSeparacaoAsync(Guid semanaId, CancellationToken ct = default)
    {
        var semana = await ObterSemanaAsync(semanaId, ct);

        semana.EnviarParaSeparacao();

        await NotificarPerfisAsync(
            semana,
            $"Lista da semana {semana.DataInicio:dd/MM/yyyy} a {semana.DataFim:dd/MM/yyyy} enviada para separação.",
            PerfilUsuario.Almoxarife);

        return await SalvarERetornarAsync(semana, ct);
    }

    // O almoxarife informa a quantidade separada item a item. Uma lista local fica "Separada" quando todos os seus
    // itens têm quantidade informada; a semana conclui a separação quando todas as listas com pedido estão separadas.
    public async Task<SemanaListaDto> RegistrarSeparacaoAsync(Guid semanaId, RegistrarSeparacaoRequest request, Guid almoxarifeId, CancellationToken ct = default)
    {
        var semana = await ObterSemanaAsync(semanaId, ct);

        if (semana.Status != StatusSemanaLista.EnviadaParaSeparacao)
            throw new DomainException("A separação só pode ser registrada quando a semana estiver Enviada para Separação.");

        var itens = new List<ItemSeparadoRequest>();
        if (request.Itens != null)
            itens.AddRange(request.Itens);
        if (request.ItemId.HasValue && request.QuantidadeSeparada.HasValue)
            itens.Add(new ItemSeparadoRequest(request.ItemId.Value, request.QuantidadeSeparada.Value, request.Observacao));

        if (itens.Count == 0)
            throw new DomainException("Informe ao menos um item com a quantidade separada.");

        var listasAlteradas = new HashSet<ListaLocal>();
        foreach (var itemSeparado in itens)
        {
            var lista = semana.ListasLocais.FirstOrDefault(l => l.Itens.Any(i => i.Id == itemSeparado.ItemId))
                ?? throw new DomainException("Item não encontrado nesta semana.");

            lista.RegistrarSeparacaoItem(itemSeparado.ItemId, itemSeparado.QuantidadeSeparada, itemSeparado.Observacao);
            listasAlteradas.Add(lista);
        }

        var dataSeparacao = DateTime.UtcNow;
        foreach (var lista in listasAlteradas.Where(l => l.TodosItensComQuantidadeSeparada))
        {
            lista.RegistrarSeparacao(almoxarifeId, dataSeparacao);
        }

        if (semana.ObterListasComPedido().All(l => l.Status != StatusListaLocal.Pendente))
        {
            semana.MarcarSeparacaoConcluida();
            await NotificarPerfisAsync(
                semana,
                $"Separação concluída da semana {semana.DataInicio:dd/MM/yyyy} a {semana.DataFim:dd/MM/yyyy}. Retirada liberada para os andares.",
                PerfilUsuario.Supervisor,
                PerfilUsuario.Coordenador);
        }

        return await SalvarERetornarAsync(semana, ct);
    }

    public async Task<SemanaListaDto> ConfirmarRecebimentoAsync(Guid semanaId, ConfirmarRecebimentoRequest request, CancellationToken ct = default)
    {
        var semana = await ObterSemanaAsync(semanaId, ct);

        if (request.ListaLocalId.HasValue)
        {
            var listaLocal = semana.ListasLocais.FirstOrDefault(l => l.Id == request.ListaLocalId.Value || l.LocalId == request.ListaLocalId.Value)
                ?? throw new DomainException("Lista local não encontrada.");
            listaLocal.ConfirmarRecebimento(request.PossuiPendencias);
        }
        else
        {
            foreach (var listaLocal in semana.ListasLocais.Where(l => l.Status == StatusListaLocal.Separado))
            {
                listaLocal.ConfirmarRecebimento(request.PossuiPendencias);
            }
        }

        return await SalvarERetornarAsync(semana, ct);
    }

    public async Task<SemanaListaDto> RegistrarRetiradaAsync(Guid semanaId, RegistrarRetiradaRequest request, Guid registradoPorId, CancellationToken ct = default)
    {
        var semana = await ObterSemanaAsync(semanaId, ct);

        var nome = request.NomeRetiradoPor?.Trim() ?? string.Empty;
        if (nome.Length < 3)
            throw new DomainException("Informe o nome completo da pessoa que está retirando o material.");

        var dataRetirada = DateTime.UtcNow;

        // O almoxarife entrega direto ao responsável: a lista separada é dada como recebida
        // (itens em falta viram pendência) e a retirada é registrada no mesmo passo.
        void Retirar(ListaLocal listaLocal)
        {
            if (listaLocal.Status == StatusListaLocal.Separado)
                listaLocal.ConfirmarRecebimento(possuiPendencias: false);
            listaLocal.RegistrarRetirada(nome, dataRetirada, registradoPorId);
        }

        if (request.ListaLocalId.HasValue)
        {
            var listaLocal = semana.ListasLocais.FirstOrDefault(l => l.Id == request.ListaLocalId.Value || l.LocalId == request.ListaLocalId.Value)
                ?? throw new DomainException("Lista local não encontrada.");
            if (listaLocal.Status == StatusListaLocal.Pendente)
                throw new DomainException("A separação deste local ainda não foi concluída.");
            Retirar(listaLocal);
        }
        else
        {
            foreach (var listaLocal in semana.ListasLocais.Where(l =>
                         l.Status == StatusListaLocal.Separado ||
                         l.Status == StatusListaLocal.Confirmado ||
                         l.Status == StatusListaLocal.ConfirmadoComPendencia))
            {
                Retirar(listaLocal);
            }
        }

        // A semana é finalizada quando todos os locais com pedido tiveram a retirada registrada
        if ((semana.Status == StatusSemanaLista.EnviadaParaSeparacao || semana.Status == StatusSemanaLista.SeparacaoConcluida) &&
            semana.ObterListasComPedido().All(l => l.Status == StatusListaLocal.Retirado))
        {
            semana.Finalizar();
            await NotificarPerfisAsync(
                semana,
                $"Semana {semana.DataInicio:dd/MM/yyyy} a {semana.DataFim:dd/MM/yyyy} finalizada: todas as retiradas foram registradas.",
                PerfilUsuario.Supervisor,
                PerfilUsuario.Coordenador,
                PerfilUsuario.Gestor);
        }

        return await SalvarERetornarAsync(semana, ct);
    }

    public async Task<SemanaListaDto?> GetByIdAsync(Guid semanaId, CancellationToken ct = default)
    {
        var semana = await _semanaListaRepository.GetByIdAsync(semanaId, ct);
        return semana == null ? null : await MapToDtoAsync(semana, ct);
    }

    public async Task<SemanaListaDto?> GetSemanaAtualAsync(CancellationToken ct = default)
    {
        var semana = await _semanaListaRepository.GetSemanaAtualAsync(ct);
        return semana == null ? null : await MapToDtoAsync(semana, ct);
    }

    public async Task<IReadOnlyList<SemanaResumoDto>> ListarHistoricoAsync(CancellationToken ct = default)
    {
        var semanas = await _semanaListaRepository.GetAllAsync(ct);

        return semanas
            .Select(s => new SemanaResumoDto(s.Id, s.DataInicio, s.DataFim, s.Status.ToString()))
            .ToList();
    }

    private async Task<SemanaLista> ObterSemanaAsync(Guid semanaId, CancellationToken ct)
    {
        return await _semanaListaRepository.GetByIdAsync(semanaId, ct)
            ?? throw new DomainException("Semana de lista não encontrada.");
    }

    private async Task<SemanaListaDto> SalvarERetornarAsync(SemanaLista semana, CancellationToken ct)
    {
        await _semanaListaRepository.UpdateAsync(semana, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var semanaAtualizada = await _semanaListaRepository.GetByIdAsync(semana.Id, ct) ?? semana;
        return await MapToDtoAsync(semanaAtualizada, ct);
    }

    // Cria uma notificação para cada usuário ativo dos perfis informados (aviso dentro do sistema)
    private async Task NotificarPerfisAsync(SemanaLista semana, string mensagem, params PerfilUsuario[] perfis)
    {
        var notificacoes = new List<Notificacao>();

        foreach (var perfil in perfis)
        {
            var usuarios = await _usuarioRepository.GetAtivosPorPerfilAsync(perfil);
            notificacoes.AddRange(usuarios.Select(u => Notificacao.Create(u.Id, semana.Id, mensagem)));
        }

        if (notificacoes.Count > 0)
        {
            await _notificacaoRepository.AddRangeAsync(notificacoes);
        }
    }

    private async Task<SemanaListaDto> MapToDtoAsync(SemanaLista semana, CancellationToken ct)
    {
        var locaisMap = (await _localRepository.GetAllAsync(ct)).ToDictionary(l => l.Id, l => l);
        var materiaisMap = (await _materialRepository.GetAllAsync(ct)).ToDictionary(m => m.Id, m => m);

        var listaLocalDtos = semana.ListasLocais
            .Select(l => new { Lista = l, Local = locaisMap.TryGetValue(l.LocalId, out var loc) ? loc : l.Local })
            .OrderBy(x => x.Local?.OrdemExibicao ?? int.MaxValue)
            .ThenBy(x => x.Local?.Nome)
            .Select(x => new ListaLocalDto(
                x.Lista.Id,
                x.Lista.SemanaListaId,
                x.Lista.LocalId,
                x.Local?.Nome ?? string.Empty,
                x.Lista.Status.ToString(),
                x.Lista.SeparadoPorAlmoxarifeId,
                x.Lista.DataSeparacao,
                x.Lista.NomeRetiradoPor,
                x.Lista.DataRetirada,
                x.Lista.Itens
                    .Select(i => new { Item = i, Material = materiaisMap.TryGetValue(i.MaterialId, out var mat) ? mat : i.Material })
                    .OrderBy(y => y.Material?.OrdemExibicao ?? int.MaxValue)
                    .Select(y => new ListaLocalItemDto(
                        y.Item.Id,
                        y.Item.ListaLocalId,
                        y.Item.MaterialId,
                        y.Material?.Nome ?? string.Empty,
                        y.Material?.Unidade.ToString() ?? string.Empty,
                        y.Item.QuantidadeSolicitada,
                        y.Item.QuantidadeSeparada,
                        y.Item.StatusItem.ToString(),
                        y.Item.Observacao))
                    .ToList(),
                x.Lista.AssinaturaDigital,
                x.Lista.VerificarAssinaturaDigital(),
                x.Lista.SemPedido,
                x.Local?.Tipo.ToString() ?? string.Empty,
                x.Local?.OrdemExibicao ?? 0))
            .ToList();

        return new SemanaListaDto(
            semana.Id,
            semana.DataInicio,
            semana.DataFim,
            semana.Status.ToString(),
            semana.CriadoPorSupervisorId,
            semana.DataCriacao,
            listaLocalDtos
        );
    }
}
