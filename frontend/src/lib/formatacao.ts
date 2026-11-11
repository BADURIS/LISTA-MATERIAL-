// Datas da API chegam como "AAAA-MM-DD" (DateOnly) ou ISO completo (data/hora em UTC)

export const formatarData = (iso: string) => iso.slice(0, 10).split('-').reverse().join('/');

export const formatarDataCurta = (iso: string) => {
  const [, mes, dia] = iso.slice(0, 10).split('-');
  return `${dia}/${mes}`;
};

export const formatarDataHora = (iso: string) => new Date(iso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });

export const STATUS_SEMANA: Record<string, string> = {
  EmMontagem: 'Em montagem',
  EnviadaParaSeparacao: 'Em separação',
  SeparacaoConcluida: 'Separação concluída',
  Finalizada: 'Finalizada',
};

export const STATUS_LISTA: Record<string, string> = {
  Pendente: 'Pendente',
  Separado: 'Separado',
  ConfirmadoComPendencia: 'Com pendência',
  Confirmado: 'Recebido',
  Retirado: 'Retirado',
};

// Segunda-feira da semana de uma data, em "AAAA-MM-DD" (fuso local)
export function segundaFeira(base = new Date()): string {
  const d = new Date(base.getFullYear(), base.getMonth(), base.getDate());
  d.setDate(d.getDate() - ((d.getDay() + 6) % 7));
  return paraIsoLocal(d);
}

export function somarDias(iso: string, dias: number): string {
  const [a, m, d] = iso.split('-').map(Number);
  return paraIsoLocal(new Date(a, m - 1, d + dias));
}

export function paraIsoLocal(d: Date): string {
  const mes = String(d.getMonth() + 1).padStart(2, '0');
  const dia = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${mes}-${dia}`;
}
