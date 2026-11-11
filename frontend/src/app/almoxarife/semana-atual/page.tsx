'use client';

import SemanaEmAndamento from '@/components/SemanaEmAndamento';
import { Protegido } from '@/components/ui';

export default function AlmoxarifeSemanaAtualPage() {
  return (
    <Protegido perfis={['Almoxarife']}>
      <SemanaEmAndamento modo="almoxarife" />
    </Protegido>
  );
}
