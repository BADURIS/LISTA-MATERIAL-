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

    public async Task<SemanaListaDto> CriarSemanaAsync(CriarSemanaRequest request, Guid supervisorId, CancellationToken ct = default)
    {
        var semana = SemanaLista.Create(request.DataInicio, request.DataFim, supervisorId);
        await _semanaListaRepository.AddAsync(semana, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await MapToDtoAsync(semana, ct);
    }

    public async Task<SemanaListaDto> AdicionarLocalAsync(Guid semanaId, AdicionarLocalRequest request, CancellationToken ct = default)
    {
        var semana = await _semanaListaRepository.GetByIdAsync(semanaId, ct)
            ?? throw new DomainException("Semana de lista não encontrada.");

        var local = await _localRepository.GetByIdAsync(request.LocalId, ct)
            ?? throw new DomainException("Local não encontrado.");

        semana.AdicionarListaLocal(request.LocalId);

        await _semanaListaRepository.UpdateAsync(semana, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var semanaAtualizada = await _semanaListaRepository.GetByIdAsync(semanaId, ct) ?? semana;
        return await MapToDtoAsync(semanaAtualizada, ct);
    }

    public async Task<SemanaListaDto> AdicionarItemAsync(Guid semanaId, AdicionarItemRequest request, CancellationToken ct = default)
    {
        var semana = await _semanaListaRepository.GetByIdAsync(semanaId, ct)
            ?? throw new DomainException("Semana de lista não encontrada.");

        var material = await _materialRepository.GetByIdAsync(request.MaterialId, ct)
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
        {
            throw new DomainException("Lista local não encontrada para esta semana.");
        }

        listaLocal.AdicionarItem(request.MaterialId, request.QuantidadeSolicitada);

        await _semanaListaRepository.UpdateAsync(semana, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var semanaAtualizada = await _semanaListaRepository.GetByIdAsync(semanaId, ct) ?? semana;
        return await MapToDtoAsync(semanaAtualizada, ct);
    }

    public async Task<SemanaListaDto> EnviarParaSeparacaoAsync(Guid semanaId, CancellationToken ct = default)
    {
        var semana = await _semanaListaRepository.GetByIdAsync(semanaId, ct)
            ?? throw new DomainException("Semana de lista não encontrada.");

        semana.EnviarParaSeparacao();

        await _semanaListaRepository.UpdateAsync(semana, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var semanaAtualizada = await _semanaListaRepository.GetByIdAsync(semanaId, ct) ?? semana;
        return await MapToDtoAsync(semanaAtualizada, ct);
    }

    public async Task<SemanaListaDto> RegistrarSeparacaoAsync(Guid semanaId, RegistrarSeparacaoRequest request, Guid almoxarifeId, CancellationToken ct = default)
    {
        var semana = await _semanaListaRepository.GetByIdAsync(semanaId, ct)
            ?? throw new DomainException("Semana de lista não encontrada.");

        if (semana.Status != StatusSemanaLista.EnviadaParaSeparacao)
            throw new DomainException("A separação só pode ser registrada quando a semana estiver Enviada para Separação.");

        var dataSeparacao = DateTime.UtcNow;

        if (request.Itens != null && request.Itens.Any())
        {
            foreach (var itemSeparado in request.Itens)
            {
                var targetLista = semana.ListasLocais.FirstOrDefault(l => l.Itens.Any(i => i.Id == itemSeparado.ItemId));
                if (targetLista != null)
                {
                    var item = targetLista.Itens.First(i => i.Id == itemSeparado.ItemId);
                    item.RegistrarSeparacao(itemSeparado.QuantidadeSeparada, itemSeparado.Observacao);
                }
            }
            foreach (var lista in semana.ListasLocais)
            {
                MarcarListaSeTodosItensSeparados(lista, almoxarifeId, dataSeparacao);
            }
        }
        else if (request.ItemId.HasValue && request.QuantidadeSeparada.HasValue)
        {
            var targetLista = semana.ListasLocais.FirstOrDefault(l => l.Itens.Any(i => i.Id == request.ItemId.Value));
            if (targetLista != null)
            {
                var item = targetLista.Itens.First(i => i.Id == request.ItemId.Value);
                item.RegistrarSeparacao(request.QuantidadeSeparada.Value, request.Observacao);
                MarcarListaSeTodosItensSeparados(targetLista, almoxarifeId, dataSeparacao);
            }
        }
        else
        {
            foreach (var lista in semana.ListasLocais)
            {
                if (lista.Itens.Any())
                {
                    lista.RegistrarSeparacao(almoxarifeId, dataSeparacao);
                }
            }
        }

        // Quando nenhuma lista local estiver mais pendente, a separação da semana está concluída
        if (semana.ListasLocais.All(l => l.Status != StatusListaLocal.Pendente))
        {
            semana.MarcarSeparacaoConcluida();
            await NotificarPerfisAsync(
                semana,
                $"Separação concluída da semana {semana.DataInicio:dd/MM/yyyy} a {semana.DataFim:dd/MM/yyyy}. Retirada liberada para os andares.",
                PerfilUsuario.Supervisor,
                PerfilUsuario.Coordenador);
        }

        await _semanaListaRepository.UpdateAsync(semana, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var semanaAtualizada = await _semanaListaRepository.GetByIdAsync(semanaId, ct) ?? semana;
        return await MapToDtoAsync(semanaAtualizada, ct);
    }

    public async Task<SemanaListaDto> ConfirmarRecebimentoAsync(Guid semanaId, ConfirmarRecebimentoRequest request, CancellationToken ct = default)
    {
        var semana = await _semanaListaRepository.GetByIdAsync(semanaId, ct)
            ?? throw new DomainException("Semana de lista não encontrada.");

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

        await _semanaListaRepository.UpdateAsync(semana, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var semanaAtualizada = await _semanaListaRepository.GetByIdAsync(semanaId, ct) ?? semana;
        return await MapToDtoAsync(semanaAtualizada, ct);
    }

    public async Task<SemanaListaDto> RegistrarRetiradaAsync(Guid semanaId, RegistrarRetiradaRequest request, Guid coordenadorId, CancellationToken ct = default)
    {
        var semana = await _semanaListaRepository.GetByIdAsync(semanaId, ct)
            ?? throw new DomainException("Semana de lista não encontrada.");

        var dataRetirada = DateTime.UtcNow;

        if (request.ListaLocalId.HasValue)
        {
            var listaLocal = semana.ListasLocais.FirstOrDefault(l => l.Id == request.ListaLocalId.Value || l.LocalId == request.ListaLocalId.Value)
                ?? throw new DomainException("Lista local não encontrada.");
            listaLocal.RegistrarRetirada(request.NomeRetiradoPor, dataRetirada, coordenadorId);
        }
        else
        {
            foreach (var listaLocal in semana.ListasLocais.Where(l => l.Status == StatusListaLocal.Confirmado || l.Status == StatusListaLocal.ConfirmadoComPendencia))
            {
                listaLocal.RegistrarRetirada(request.NomeRetiradoPor, dataRetirada, coordenadorId);
            }
        }

        if ((semana.Status == StatusSemanaLista.EnviadaParaSeparacao || semana.Status == StatusSemanaLista.SeparacaoConcluida) &&
            semana.ListasLocais.All(l => l.Status == StatusListaLocal.Retirado || l.Status == StatusListaLocal.Confirmado || l.Status == StatusListaLocal.ConfirmadoComPendencia))
        {
            semana.Finalizar();
        }

        await _semanaListaRepository.UpdateAsync(semana, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var semanaAtualizada = await _semanaListaRepository.GetByIdAsync(semanaId, ct) ?? semana;
        return await MapToDtoAsync(semanaAtualizada, ct);
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

    // A lista local só fica Separada quando todos os seus itens têm quantidade separada informada
    private static void MarcarListaSeTodosItensSeparados(ListaLocal lista, Guid almoxarifeId, DateTime dataSeparacao)
    {
        if (lista.Itens.Any() && lista.Itens.All(i => i.QuantidadeSeparada.HasValue))
        {
            lista.RegistrarSeparacao(almoxarifeId, dataSeparacao);
        }
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
        var locaisMap = (await _localRepository.GetAllAsync(ct)).ToDictionary(l => l.Id, l => l.Nome);
        var materiaisMap = (await _materialRepository.GetAllAsync(ct)).ToDictionary(m => m.Id, m => m);

        var listaLocalDtos = semana.ListasLocais.Select(l => new ListaLocalDto(
            l.Id,
            l.SemanaListaId,
            l.LocalId,
            locaisMap.TryGetValue(l.LocalId, out var nomeLocal) ? nomeLocal : (l.Local?.Nome ?? string.Empty),
            l.Status.ToString(),
            l.SeparadoPorAlmoxarifeId,
            l.DataSeparacao,
            l.NomeRetiradoPor,
            l.DataRetirada,
            l.Itens.Select(i => new ListaLocalItemDto(
                i.Id,
                i.ListaLocalId,
                i.MaterialId,
                materiaisMap.TryGetValue(i.MaterialId, out var mat) ? mat.Nome : (i.Material?.Nome ?? string.Empty),
                materiaisMap.TryGetValue(i.MaterialId, out mat) ? mat.Unidade.ToString() : (i.Material?.Unidade.ToString() ?? string.Empty),
                i.QuantidadeSolicitada,
                i.QuantidadeSeparada,
                i.StatusItem.ToString(),
                i.Observacao
            )).ToList(),
            l.AssinaturaDigital,
            l.VerificarAssinaturaDigital()
        )).ToList();

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
