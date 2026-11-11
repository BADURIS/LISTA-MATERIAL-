'use client';

import React, { useEffect, useRef, useState } from 'react';
import { baixarArquivo, fetchApi, fetchApiOuNulo } from '@/lib/api';
import { formatarData, formatarDataHora, STATUS_LISTA, STATUS_SEMANA } from '@/lib/formatacao';
import type { ItemLista, ListaLocal, Semana, StatusLista } from '@/lib/tipos';
import { Aviso, Cabecalho, Carregando, Mensagem, Vazio } from '@/components/ui';
import {
  CheckCircle2,
  AlertTriangle,
  FileSpreadsheet,
  FileText,
  UserCheck,
  Lock,
  ChevronDown,
  ChevronUp,
  Save,
  X,
  PackageCheck,
  ShieldCheck,
  ShieldAlert,
} from 'lucide-react';

export type ModoSemana = 'almoxarife' | 'coordenador' | 'consulta';

// Quantidade separada fica como texto para permitir o campo vazio (item ainda não conferido)
type ItemForm = { qtd: string; obs: string };

const TITULOS: Record<ModoSemana, string> = {
  almoxarife: 'Separação de Materiais',
  coordenador: 'Recebimento e Retirada',
  consulta: 'Lista da Semana (consulta)',
};

const ESTILO_STATUS: Record<StatusLista, string> = {
  Pendente: 'bg-[#FAC775] text-[#412402] border-amber-300',
  Separado: 'bg-blue-100 text-blue-900 border-blue-300',
  Confirmado: 'bg-[#C0DD97] text-[#173404] border-emerald-300',
  ConfirmadoComPendencia: 'bg-[#FAD2D2] text-[#5C0606] border-red-300',
  Retirado: 'bg-slate-200 text-slate-800 border-slate-300',
};

const falta = (solicitado: number, separado: string) => (separado === '' ? null : Math.max(0, solicitado - Number(separado)));

export default function SemanaEmAndamento({ modo }: { modo: ModoSemana }) {
  const [semana, setSemana] = useState<Semana | null>(null);
  const [selecionadoId, setSelecionadoId] = useState<string | null>(null);
  const [form, setForm] = useState<Record<string, ItemForm>>({});
  const [nomeRetirada, setNomeRetirada] = useState('');
  const [modalRetirada, setModalRetirada] = useState(false);
  const [conferido, setConferido] = useState(false);
  const [loading, setLoading] = useState(true);
  const [ocupado, setOcupado] = useState(false);
  const [mensagem, setMensagem] = useState<Mensagem>(null);
  const painelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    (async () => {
      try {
        setSemana(await fetchApiOuNulo<Semana>('/api/semanas-lista/atual'));
      } catch (err: any) {
        setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao carregar a semana.' });
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  useEffect(() => {
    if (selecionadoId) painelRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }, [selecionadoId]);

  const local = semana?.locais.find((l) => l.id === selecionadoId) ?? null;
  const emMontagem = semana?.status === 'EmMontagem';
  const emSeparacao = semana?.status === 'EnviadaParaSeparacao';

  const montarForm = (l: ListaLocal) => {
    const mapa: Record<string, ItemForm> = {};
    l.itens.forEach((i) => (mapa[i.id] = { qtd: i.quantidadeSeparada === null ? '' : String(i.quantidadeSeparada), obs: i.observacao || '' }));
    setForm(mapa);
  };

  const selecionar = (l: ListaLocal) => {
    setMensagem(null);
    if (selecionadoId === l.id) {
      setSelecionadoId(null);
      return;
    }
    setSelecionadoId(l.id);
    setNomeRetirada('');
    montarForm(l);
  };

  const atualizarSemana = (nova: Semana, listaId: string) => {
    setSemana(nova);
    const lista = nova.locais.find((l) => l.id === listaId);
    if (lista) montarForm(lista);
  };

  // ----- Almoxarife -----
  const podeSeparar = modo === 'almoxarife' && emSeparacao && !!local && !local.semPedido && local.status === 'Pendente';

  const alterarQtd = (item: ItemLista, valor: string) => {
    let novo = valor.replace(/\D/g, '');
    if (novo !== '' && Number(novo) > item.quantidadeSolicitada) novo = String(item.quantidadeSolicitada);
    setForm((prev) => ({ ...prev, [item.id]: { ...prev[item.id], qtd: novo } }));
  };

  const separarTudo = () => {
    if (!local) return;
    setForm((prev) => {
      const novo = { ...prev };
      local.itens.forEach((i) => {
        if (!novo[i.id] || novo[i.id].qtd === '') novo[i.id] = { ...novo[i.id], qtd: String(i.quantidadeSolicitada) };
      });
      return novo;
    });
  };

  const salvarSeparacao = async () => {
    if (!semana || !local) return;
    const preenchidos = local.itens.filter((i) => (form[i.id]?.qtd ?? '') !== '');
    if (preenchidos.length === 0) {
      setMensagem({ tipo: 'erro', texto: 'Informe a quantidade separada de pelo menos um item.' });
      return;
    }

    // Com todos os itens preenchidos a separação do local é concluída e não pode mais ser alterada
    const conclui = preenchidos.length === local.itens.length;
    if (conclui) {
      const comFalta = local.itens.filter((i) => (falta(i.quantidadeSolicitada, form[i.id].qtd) ?? 0) > 0);
      const resumo = comFalta.length
        ? `\n\nItens com falta:\n${comFalta.map((i) => `• ${i.nomeMaterial}: faltam ${falta(i.quantidadeSolicitada, form[i.id].qtd)} ${i.unidade}`).join('\n')}`
        : '\n\nTodos os itens foram separados por completo.';
      if (!window.confirm(`Todos os itens de ${local.nomeLocal} estão conferidos. Ao salvar, a separação deste local será concluída e não poderá mais ser alterada.${resumo}\n\nConcluir?`)) return;
    }

    setOcupado(true);
    setMensagem(null);
    try {
      const nova = await fetchApi<Semana>(`/api/semanas-lista/${semana.id}/registrar-separacao`, {
        method: 'POST',
        body: JSON.stringify({
          itens: preenchidos.map((i) => ({ itemId: i.id, quantidadeSeparada: Number(form[i.id].qtd), observacao: form[i.id].obs || null })),
        }),
      });
      atualizarSemana(nova, local.id);
      setMensagem({
        tipo: 'sucesso',
        texto: conclui
          ? nova.status === 'SeparacaoConcluida'
            ? 'Separação do local concluída. Todos os locais foram separados: supervisor e coordenador foram avisados.'
            : `Separação de ${local.nomeLocal} concluída.`
          : `${preenchidos.length} de ${local.itens.length} itens salvos. Continue quando quiser.`,
      });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao salvar a separação.' });
    } finally {
      setOcupado(false);
    }
  };

  // ----- Coordenador -----
  const podeConfirmar = modo === 'coordenador' && local?.status === 'Separado';
  const podeRetirar = modo === 'coordenador' && (local?.status === 'Confirmado' || local?.status === 'ConfirmadoComPendencia');

  const confirmarRecebimento = async () => {
    if (!semana || !local) return;
    setOcupado(true);
    setMensagem(null);
    try {
      const nova = await fetchApi<Semana>(`/api/semanas-lista/${semana.id}/confirmar-recebimento`, {
        method: 'POST',
        body: JSON.stringify({ listaLocalId: local.id }),
      });
      atualizarSemana(nova, local.id);
      const status = nova.locais.find((l) => l.id === local.id)?.status;
      setMensagem({
        tipo: 'sucesso',
        texto: status === 'ConfirmadoComPendencia' ? 'Recebimento confirmado com pendência (há itens em falta).' : 'Recebimento confirmado. Agora registre a retirada.',
      });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao confirmar o recebimento.' });
    } finally {
      setOcupado(false);
    }
  };

  const abrirModalRetirada = () => {
    if (nomeRetirada.trim().length < 3) {
      setMensagem({ tipo: 'erro', texto: 'Informe o nome completo de quem está retirando o material.' });
      return;
    }
    setMensagem(null);
    setConferido(false);
    setModalRetirada(true);
  };

  const registrarRetirada = async () => {
    if (!semana || !local || !conferido) return;
    setOcupado(true);
    setMensagem(null);
    try {
      const nova = await fetchApi<Semana>(`/api/semanas-lista/${semana.id}/registrar-retirada`, {
        method: 'POST',
        body: JSON.stringify({ listaLocalId: local.id, nomeRetiradoPor: nomeRetirada.trim() }),
      });
      atualizarSemana(nova, local.id);
      setModalRetirada(false);
      setMensagem({
        tipo: 'sucesso',
        texto:
          nova.status === 'Finalizada'
            ? 'Retirada registrada. Todas as retiradas da semana foram concluídas: semana finalizada.'
            : `Retirada registrada em nome de ${nomeRetirada.trim()}, com assinatura digital.`,
      });
    } catch (err: any) {
      setModalRetirada(false);
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao registrar a retirada.' });
    } finally {
      setOcupado(false);
    }
  };

  const exportarLocal = async (formato: 'planilha' | 'pdf') => {
    if (!semana || !local) return;
    try {
      await baixarArquivo(`/api/semanas-lista/${semana.id}/listas/${local.id}/${formato}`, formato === 'pdf' ? 'lista-local.pdf' : 'lista-local.xlsx');
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao exportar.' });
    }
  };

  if (loading) return <Carregando />;

  const comPedido = semana?.locais.filter((l) => !l.semPedido) ?? [];
  const contagem = (status: StatusLista) => comPedido.filter((l) => l.status === status).length;
  const conferidos = local ? local.itens.filter((i) => (form[i.id]?.qtd ?? '') !== '').length : 0;
  const itensComFalta = local ? local.itens.filter((i) => (falta(i.quantidadeSolicitada, form[i.id]?.qtd ?? '') ?? 0) > 0) : [];
  const bloquearGrade = emMontagem && modo !== 'consulta';

  return (
    <div className="space-y-6">
      <Cabecalho
        titulo={TITULOS[modo]}
        subtitulo={
          semana ? (
            <>
              Semana <span className="font-semibold text-slate-900">{formatarData(semana.dataInicio)}</span> a{' '}
              <span className="font-semibold text-slate-900">{formatarData(semana.dataFim)}</span> · Status:{' '}
              <span className="font-bold text-[#1F6D2E]">{STATUS_SEMANA[semana.status] ?? semana.status}</span>
            </>
          ) : (
            'Nenhuma semana em andamento.'
          )
        }
      />

      <Aviso mensagem={mensagem} />

      {!semana && <Vazio>Nenhuma semana em andamento no momento.</Vazio>}

      {semana && bloquearGrade && (
        <div className="p-5 bg-amber-50 border border-amber-200 rounded-2xl text-amber-900 flex items-start gap-3">
          <Lock className="w-5 h-5 mt-0.5 flex-shrink-0 text-amber-700" />
          <div>
            <p className="font-bold text-sm">Lista ainda em montagem</p>
            <p className="text-sm mt-1">
              {modo === 'almoxarife'
                ? 'A separação é liberada quando o supervisor enviar a semana ao almoxarifado.'
                : 'O recebimento e a retirada são liberados depois da separação. Para ajudar nos pedidos, use o menu "Pedidos".'}{' '}
              Locais prontos: <span className="font-bold">{semana.locais.filter((l) => l.semPedido || l.itens.length > 0).length} de {semana.locais.length}</span>.
            </p>
          </div>
        </div>
      )}

      {semana && !bloquearGrade && (
        <>
          <div className="grid grid-cols-2 sm:grid-cols-5 gap-2 text-center">
            {(['Pendente', 'Separado', 'ConfirmadoComPendencia', 'Confirmado', 'Retirado'] as StatusLista[]).map((s) => (
              <div key={s} className={`rounded-xl border px-3 py-2 ${ESTILO_STATUS[s]}`}>
                <p className="text-xl font-extrabold">{contagem(s)}</p>
                <p className="text-[11px] font-bold uppercase tracking-wide">{STATUS_LISTA[s]}</p>
              </div>
            ))}
          </div>

          <div className="grid gap-3" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(150px, 1fr))' }}>
            {semana.locais.map((l) => {
              const selecionado = selecionadoId === l.id;
              return (
                <button
                  key={l.id}
                  onClick={() => selecionar(l)}
                  className={`p-4 rounded-xl border text-left transition-all duration-200 flex flex-col justify-between min-h-[100px] ${
                    selecionado ? 'border-[#1F6D2E] ring-2 ring-[#1F6D2E]/30 bg-emerald-50/40 shadow-md' : 'border-slate-200 bg-white hover:border-slate-300 hover:shadow-sm'
                  }`}
                >
                  <div>
                    <span className="font-bold text-sm text-slate-900 block truncate">{l.nomeLocal}</span>
                    {l.nomeRetiradoPor && <p className="text-[11px] text-slate-500 mt-1 truncate">Retirado por {l.nomeRetiradoPor}</p>}
                  </div>
                  <div className="flex items-center justify-between w-full mt-3">
                    <span
                      className={`text-[10px] font-extrabold px-2.5 py-0.5 rounded-full border ${
                        l.semPedido ? 'bg-slate-100 text-slate-600 border-slate-300' : ESTILO_STATUS[l.status]
                      }`}
                    >
                      {l.semPedido ? 'Sem pedido' : STATUS_LISTA[l.status]}
                    </span>
                    {selecionado ? <ChevronUp className="w-4 h-4 text-[#1F6D2E]" /> : <ChevronDown className="w-4 h-4 text-slate-400" />}
                  </div>
                </button>
              );
            })}
          </div>

          {local && (
            <div ref={painelRef} className="scroll-mt-20 bg-white rounded-2xl border-2 border-[#1F6D2E] shadow-xl p-4 sm:p-6 space-y-5">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-200 pb-4">
                <div>
                  <div className="flex flex-wrap items-center gap-3">
                    <h3 className="text-xl font-bold text-slate-900">
                      <span className="text-[#1F6D2E]">{local.nomeLocal}</span>
                    </h3>
                    <span
                      className={`text-xs font-bold px-3 py-1 rounded-full border ${
                        local.semPedido ? 'bg-slate-100 text-slate-600 border-slate-300' : ESTILO_STATUS[local.status]
                      }`}
                    >
                      {local.semPedido ? 'Sem pedido' : STATUS_LISTA[local.status]}
                    </span>
                  </div>
                  {local.dataRetirada && (
                    <p className="text-xs text-slate-600 font-medium mt-1 flex items-center gap-1.5 flex-wrap">
                      Retirado por <span className="font-bold text-slate-900">{local.nomeRetiradoPor}</span> em{' '}
                      <span className="font-bold">{formatarDataHora(local.dataRetirada)}</span>
                      {local.assinaturaValida ? (
                        <span className="inline-flex items-center gap-1 text-emerald-700 font-bold">
                          <ShieldCheck className="w-3.5 h-3.5" /> assinatura válida
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 text-rose-700 font-bold">
                          <ShieldAlert className="w-3.5 h-3.5" /> assinatura inválida
                        </span>
                      )}
                    </p>
                  )}
                  {!local.semPedido && (
                    <p className="text-xs text-slate-500 font-medium mt-1">
                      {conferidos} de {local.itens.length} itens com quantidade separada
                      {itensComFalta.length > 0 && <span className="text-rose-700 font-bold"> · {itensComFalta.length} com falta</span>}
                    </p>
                  )}
                </div>
                {!local.semPedido && (
                  <div className="flex items-center gap-2 self-start">
                    <button
                      onClick={() => exportarLocal('planilha')}
                      className="px-3 py-2 bg-emerald-100 text-emerald-800 hover:bg-emerald-200 rounded-lg text-xs font-bold transition flex items-center gap-1.5"
                    >
                      <FileSpreadsheet className="w-4 h-4 text-emerald-700" />
                      Excel
                    </button>
                    <button
                      onClick={() => exportarLocal('pdf')}
                      className="px-3 py-2 bg-rose-100 text-rose-800 hover:bg-rose-200 rounded-lg text-xs font-bold transition flex items-center gap-1.5"
                      title="PDF para imprimir, com espaço para assinatura"
                    >
                      <FileText className="w-4 h-4 text-rose-700" />
                      PDF
                    </button>
                  </div>
                )}
              </div>

              {podeSeparar && (
                <div className="bg-slate-50 p-4 rounded-xl border border-slate-200 flex flex-col sm:flex-row sm:items-center gap-3">
                  <button
                    onClick={separarTudo}
                    disabled={ocupado}
                    className="px-4 py-2.5 bg-white border border-slate-300 text-slate-800 hover:bg-slate-100 rounded-xl text-sm font-semibold transition"
                    title="Preenche a quantidade pedida nos itens ainda vazios"
                  >
                    Separar tudo o pedido
                  </button>
                  <p className="text-xs text-slate-500 sm:flex-1">
                    Você pode salvar aos poucos. Quando todos os itens tiverem quantidade, a separação do local é concluída.
                  </p>
                  <button
                    onClick={salvarSeparacao}
                    disabled={ocupado}
                    className="px-4 py-2.5 bg-blue-700 text-white hover:bg-blue-800 disabled:opacity-60 rounded-xl text-sm font-semibold transition flex items-center gap-2 shadow"
                  >
                    <Save className="w-4 h-4" />
                    {ocupado ? 'Salvando...' : conferidos === local.itens.length ? 'Concluir separação' : 'Salvar andamento'}
                  </button>
                </div>
              )}

              {modo === 'almoxarife' && !local.semPedido && local.status !== 'Pendente' && (
                <p className="p-3 bg-slate-100 text-slate-700 rounded-xl text-xs font-semibold flex items-center gap-2 border border-slate-200">
                  <Lock className="w-4 h-4 text-slate-500" /> Separação deste local concluída.
                </p>
              )}

              {modo === 'coordenador' && local.status === 'Pendente' && !local.semPedido && (
                <p className="p-3 bg-amber-50 text-amber-900 rounded-xl text-xs font-semibold border border-amber-200">
                  Aguardando o almoxarifado concluir a separação deste local.
                </p>
              )}

              {podeConfirmar && (
                <div className="bg-blue-50/60 p-4 rounded-xl border border-blue-200 flex flex-col sm:flex-row sm:items-center gap-3">
                  <p className="text-sm text-slate-800 sm:flex-1">
                    Confira os materiais que chegaram do almoxarifado. Itens em falta ficam registrados como pendência.
                  </p>
                  <button
                    onClick={confirmarRecebimento}
                    disabled={ocupado}
                    className="px-4 py-2.5 bg-blue-700 text-white hover:bg-blue-800 disabled:opacity-60 rounded-xl text-sm font-semibold transition flex items-center gap-2 shadow"
                  >
                    <PackageCheck className="w-4 h-4" />
                    Confirmar recebimento
                  </button>
                </div>
              )}

              {podeRetirar && (
                <div className="bg-emerald-50/60 p-4 rounded-xl border border-emerald-200 space-y-2">
                  <p className="text-sm font-bold text-slate-900 flex items-center gap-2">
                    <UserCheck className="w-4 h-4 text-[#1F6D2E]" /> Registrar retirada
                  </p>
                  <div className="flex flex-col sm:flex-row sm:items-center gap-3">
                    <input
                      type="text"
                      placeholder="Nome completo de quem está retirando *"
                      value={nomeRetirada}
                      onChange={(e) => setNomeRetirada(e.target.value)}
                      className="flex-1 px-3.5 py-2 bg-white border border-slate-300 rounded-lg text-sm font-semibold outline-none focus:ring-2 focus:ring-[#1F6D2E]"
                    />
                    <button
                      onClick={abrirModalRetirada}
                      disabled={ocupado}
                      className="px-4 py-2 bg-[#1F6D2E] text-white hover:bg-[#175423] rounded-xl text-sm font-semibold transition flex items-center gap-2 shadow"
                    >
                      <UserCheck className="w-4 h-4" /> Verificar e registrar
                    </button>
                  </div>
                </div>
              )}

              {local.semPedido ? (
                <p className="py-6 text-center text-slate-400 text-sm">Local sem pedido nesta semana: nada a separar.</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-left text-sm">
                    <thead className="bg-slate-50 text-xs font-bold text-slate-500 uppercase border-b border-slate-200">
                      <tr>
                        <th className="px-3 py-3">Material</th>
                        <th className="px-3 py-3 text-center">Pedido</th>
                        <th className="px-3 py-3 text-center">Separado</th>
                        <th className="px-3 py-3 text-center">Falta</th>
                        <th className="px-3 py-3 hidden sm:table-cell">Observação</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100 font-medium text-slate-800">
                      {local.itens.map((item) => {
                        const f = form[item.id] ?? { qtd: '', obs: '' };
                        const qtdFalta = falta(item.quantidadeSolicitada, f.qtd);
                        return (
                          <tr key={item.id} className={qtdFalta ? 'bg-rose-50/60' : ''}>
                            <td className="px-3 py-2.5">
                              <span className="font-semibold text-slate-900">{item.nomeMaterial}</span>
                              <span className="text-xs text-slate-400"> ({item.unidade})</span>
                              {podeSeparar ? (
                                <input
                                  type="text"
                                  placeholder={qtdFalta ? 'Motivo da falta (opcional)' : 'Observação (opcional)'}
                                  value={f.obs}
                                  onChange={(e) => setForm((prev) => ({ ...prev, [item.id]: { ...f, obs: e.target.value } }))}
                                  className="sm:hidden mt-1.5 w-full h-10 px-3 bg-white border border-slate-200 rounded-lg text-base outline-none focus:ring-2 focus:ring-[#1F6D2E]"
                                />
                              ) : (
                                item.observacao && <p className="sm:hidden text-xs text-slate-500">Obs: {item.observacao}</p>
                              )}
                            </td>
                            <td className="px-3 py-2.5 text-center font-bold">{item.quantidadeSolicitada}</td>
                            <td className="px-3 py-2.5 text-center">
                              {podeSeparar ? (
                                <input
                                  type="text"
                                  inputMode="numeric"
                                  placeholder="—"
                                  value={f.qtd}
                                  onChange={(e) => alterarQtd(item, e.target.value)}
                                  aria-label={`Quantidade separada de ${item.nomeMaterial}`}
                                  className={`w-16 h-11 sm:h-9 px-2 bg-white border rounded-lg text-center font-bold text-base sm:text-sm outline-none focus:ring-2 focus:ring-[#1F6D2E] ${
                                    f.qtd === '' ? 'border-amber-400' : 'border-slate-300'
                                  }`}
                                />
                              ) : (
                                <span className="font-bold">{item.quantidadeSeparada ?? '—'}</span>
                              )}
                            </td>
                            <td className="px-3 py-2.5 text-center">
                              {qtdFalta === null ? (
                                <span className="text-xs text-slate-400">a conferir</span>
                              ) : qtdFalta > 0 ? (
                                <span className="inline-flex items-center gap-1 text-xs font-bold px-2 py-0.5 rounded bg-rose-100 text-rose-800 whitespace-nowrap">
                                  <AlertTriangle className="w-3 h-3" /> {qtdFalta}
                                </span>
                              ) : (
                                <span className="text-xs font-bold px-2 py-0.5 rounded bg-emerald-100 text-emerald-800">OK</span>
                              )}
                            </td>
                            <td className="px-3 py-2.5 hidden sm:table-cell">
                              {podeSeparar ? (
                                <input
                                  type="text"
                                  placeholder={qtdFalta ? 'Motivo da falta (opcional)' : 'Opcional'}
                                  value={f.obs}
                                  onChange={(e) => setForm((prev) => ({ ...prev, [item.id]: { ...f, obs: e.target.value } }))}
                                  className="w-full px-2.5 py-1.5 bg-white border border-slate-200 rounded text-xs outline-none focus:ring-2 focus:ring-[#1F6D2E]"
                                />
                              ) : (
                                <span className="text-xs text-slate-500">{item.observacao || '-'}</span>
                              )}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}
        </>
      )}

      {modalRetirada && local && (
        <div className="fixed inset-0 z-[60] bg-black/40 flex items-center justify-center p-4" role="dialog" aria-modal="true">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg p-6 space-y-4">
            <div className="flex items-start justify-between">
              <h3 className="text-lg font-bold text-slate-900">Verificar retirada</h3>
              <button onClick={() => setModalRetirada(false)} className="text-slate-400 hover:text-slate-700" aria-label="Fechar">
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="text-sm text-slate-700 space-y-1">
              <p>
                Local: <span className="font-bold text-slate-900">{local.nomeLocal}</span>
              </p>
              <p>
                Quem está retirando: <span className="font-bold text-slate-900">{nomeRetirada.trim()}</span>
              </p>
              <p>
                Itens entregues: <span className="font-bold text-slate-900">{local.itens.length}</span>
              </p>
            </div>
            {local.itens.some((i) => (i.quantidadeSeparada ?? 0) < i.quantidadeSolicitada) && (
              <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-xs text-rose-800 space-y-1">
                <p className="font-bold flex items-center gap-1">
                  <AlertTriangle className="w-4 h-4" /> Itens entregues com falta:
                </p>
                {local.itens
                  .filter((i) => (i.quantidadeSeparada ?? 0) < i.quantidadeSolicitada)
                  .map((i) => (
                    <p key={i.id}>
                      • {i.nomeMaterial}: entregue {i.quantidadeSeparada ?? 0} de {i.quantidadeSolicitada} {i.unidade}
                    </p>
                  ))}
              </div>
            )}
            <label className="flex items-start gap-2 text-sm text-slate-800 cursor-pointer">
              <input type="checkbox" checked={conferido} onChange={(e) => setConferido(e.target.checked)} className="mt-0.5 w-4 h-4 accent-[#1F6D2E]" />
              Confirmo que conferi a identidade de quem está retirando e que os materiais entregues correspondem à lista.
            </label>
            <p className="text-xs text-slate-500">O registro gera uma assinatura digital com nome, data/hora e o seu usuário.</p>
            <div className="flex justify-end gap-2 pt-2">
              <button onClick={() => setModalRetirada(false)} className="px-4 py-2 rounded-xl text-sm font-semibold text-slate-700 hover:bg-slate-100">
                Cancelar
              </button>
              <button
                onClick={registrarRetirada}
                disabled={!conferido || ocupado}
                className="px-4 py-2 bg-[#1F6D2E] text-white hover:bg-[#175423] disabled:opacity-50 disabled:cursor-not-allowed rounded-xl text-sm font-semibold flex items-center gap-2"
              >
                <CheckCircle2 className="w-4 h-4" />
                {ocupado ? 'Registrando...' : 'Confirmar retirada'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
