'use client';

import SemanaEmAndamento from '@/components/SemanaEmAndamento';
import { Protegido } from '@/components/ui';

export default function GestorSemanaAtualPage() {
  return (
    <Protegido perfis={['Gestor', 'Supervisor']}>
      <SemanaEmAndamento modo="consulta" />
    </Protegido>
  );
}
