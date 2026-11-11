using System;

namespace LMaterial.Application.DTOs.SemanasLista;

// O supervisor criador é obtido a partir do token autenticado, não do corpo da requisição
public record CriarSemanaRequest(
    DateOnly DataInicio,
    DateOnly DataFim
);
