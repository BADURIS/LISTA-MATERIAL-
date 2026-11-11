'use client';

import SemanaEmAndamento from '@/components/SemanaEmAndamento';
import { Protegido } from '@/components/ui';

export default function CoordenadorRetiradaPage() {
  return (
    <Protegido perfis={['Coordenador']}>
      <SemanaEmAndamento modo="coordenador" />
    </Protegido>
  );
}
