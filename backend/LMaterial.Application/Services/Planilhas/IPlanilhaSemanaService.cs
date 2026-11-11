using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LMaterial.Application.DTOs.SemanasLista;

namespace LMaterial.Application.Services.Planilhas;

public interface IPlanilhaSemanaService
{
    Task<IReadOnlyList<ResumoMaterialDto>> GerarResumoAsync(Guid semanaId, CancellationToken ct = default);
    Task<(byte[] Conteudo, string NomeArquivo)> GerarXlsxAsync(Guid semanaId, CancellationToken ct = default);
}
