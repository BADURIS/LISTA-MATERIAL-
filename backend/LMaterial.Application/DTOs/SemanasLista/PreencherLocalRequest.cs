using System;
using System.Collections.Generic;

namespace LMaterial.Application.DTOs.SemanasLista;

public record ItemPedidoRequest(
    Guid MaterialId,
    int QuantidadeSolicitada
);

// Pedido completo de um local: substitui o pedido anterior. Quantidade zero retira o material do pedido.
public record PreencherLocalRequest(
    List<ItemPedidoRequest> Itens
);

public record DefinirSemPedidoRequest(
    bool SemPedido
);
