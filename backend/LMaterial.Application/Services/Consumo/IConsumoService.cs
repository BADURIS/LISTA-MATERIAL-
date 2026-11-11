using System;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.Consumo;

namespace LMaterial.Application.Services.Consumo;

public interface IConsumoService
{
    Task<ConsumoPeriodoDto> ObterPorPeriodoAsync(DateOnly dataInicio, DateOnly dataFim, Guid? localId, CancellationToken ct = default);
    Task<ConsumoSemanalDto> ObterEvolucaoSemanalAsync(int quantidadeSemanas, CancellationToken ct = default);
    Task<(byte[] Conteudo, string NomeArquivo)> ExportarPeriodoXlsxAsync(DateOnly dataInicio, DateOnly dataFim, Guid? localId, CancellationToken ct = default);
}
