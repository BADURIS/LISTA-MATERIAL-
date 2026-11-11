namespace LMaterial.Application.DTOs.Materiais;

public record UpdateMaterialRequest(
    string Nome,
    string Unidade,
    int OrdemExibicao,
    bool Ativo = true
);
