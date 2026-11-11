namespace LMaterial.Application.DTOs.Locais;

public record CreateLocalRequest(
    string Nome,
    string Tipo = "Andar",
    int OrdemExibicao = 0
);
