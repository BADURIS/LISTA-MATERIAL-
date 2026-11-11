'use client';

import MontagemSemana from '@/components/MontagemSemana';
import { Protegido } from '@/components/ui';

export default function SupervisorSemanaPage() {
  return (
    <Protegido perfis={['Supervisor']}>
      <MontagemSemana podeGerenciar />
    </Protegido>
  );
}
