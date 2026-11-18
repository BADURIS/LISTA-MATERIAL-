namespace LMaterial.Application.DTOs.Materiais;

// Excluido: o material foi apagado. Inativado: já estava em pedidos, então só saiu da lista de pedidos.
public record ExcluirMaterialResultado(
    bool Excluido,
    bool Inativado,
    MaterialDto? Material
);
