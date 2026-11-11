// Formatos devolvidos pela API (LMaterial.API)

export type StatusSemana = 'EmMontagem' | 'EnviadaParaSeparacao' | 'SeparacaoConcluida' | 'Finalizada';
export type StatusLista = 'Pendente' | 'Separado' | 'ConfirmadoComPendencia' | 'Confirmado' | 'Retirado';

export interface ItemLista {
  id: string;
  listaLocalId: string;
  materialId: string;
  nomeMaterial: string;
  unidade: string;
  quantidadeSolicitada: number;
  quantidadeSeparada: number | null;
  statusItem: 'OK' | 'Faltando';
  observacao: string | null;
}

export interface ListaLocal {
  id: string;
  semanaListaId: string;
  localId: string;
  nomeLocal: string;
  status: StatusLista;
  separadoPorAlmoxarifeId: string | null;
  dataSeparacao: string | null;
  nomeRetiradoPor: string | null;
  dataRetirada: string | null;
  itens: ItemLista[];
  assinaturaDigital: string | null;
  assinaturaValida: boolean;
  semPedido: boolean;
  tipoLocal: string;
  ordemExibicao: number;
}

export interface Semana {
  id: string;
  dataInicio: string;
  dataFim: string;
  status: StatusSemana;
  criadoPorSupervisorId: string;
  dataCriacao: string;
  locais: ListaLocal[];
}

export interface SemanaResumo {
  id: string;
  dataInicio: string;
  dataFim: string;
  status: StatusSemana;
}

export interface Material {
  id: string;
  nome: string;
  unidade: string;
  ordemExibicao: number;
  ativo: boolean;
}

export interface Local {
  id: string;
  nome: string;
  ativo: boolean;
  tipo: string;
  ordemExibicao: number;
}
