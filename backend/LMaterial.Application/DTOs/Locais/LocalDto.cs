using System;

namespace LMaterial.Application.DTOs.Locais;

public record LocalDto(
    Guid Id,
    string Nome,
    bool Ativo,
    string Tipo = "",
    int OrdemExibicao = 0
);
