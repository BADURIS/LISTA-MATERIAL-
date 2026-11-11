namespace LMaterial.Application.DTOs.Locais;

public record UpdateLocalRequest(
    string Nome,
    bool Ativo,
    string Tipo = "Andar",
    int OrdemExibicao = 0
);
