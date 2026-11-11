'use client';

import MontagemSemana from '@/components/MontagemSemana';
import { Protegido } from '@/components/ui';

export default function CoordenadorPedidosPage() {
  return (
    <Protegido perfis={['Coordenador']}>
      <MontagemSemana podeGerenciar={false} />
    </Protegido>
  );
}
