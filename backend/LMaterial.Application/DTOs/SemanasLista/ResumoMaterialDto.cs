namespace LMaterial.Application.DTOs.SemanasLista;

// Uma linha da planilha semanal: MATERIAL | UNIDADE | SAÍDA (total separado em todos os locais)
public record ResumoMaterialDto(
    string Material,
    string Unidade,
    int Saida
);
