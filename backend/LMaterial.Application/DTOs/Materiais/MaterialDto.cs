using System;

namespace LMaterial.Application.DTOs.Materiais;

public record MaterialDto(
    Guid Id,
    string Nome,
    string Unidade,
    int OrdemExibicao,
    bool Ativo = true
);
