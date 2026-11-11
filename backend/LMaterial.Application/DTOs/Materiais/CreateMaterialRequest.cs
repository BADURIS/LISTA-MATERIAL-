namespace LMaterial.Application.DTOs.Materiais;

public record CreateMaterialRequest(
    string Nome,
    string Unidade,
    int OrdemExibicao
);
