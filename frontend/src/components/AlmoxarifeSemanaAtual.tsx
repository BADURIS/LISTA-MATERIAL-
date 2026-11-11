'use client';

import React, { useState, useEffect, useRef } from 'react';
import { fetchApi } from '@/lib/api';
import { CheckCircle2, AlertTriangle, FileSpreadsheet, FileText, UserCheck, Lock, ChevronDown, ChevronUp, Save, X } from 'lucide-react';

interface Item {
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

interface ListaLocal {
  id: string;
  semanaListaId: string;
  localId: string;
  nomeLocal: string;
  tipoLocal: string;
  status: 'Pendente' | 'Separado' | 'ConfirmadoComPendencia' | 'Confirmado' | 'Retirado';
  separadoPorAlmoxarifeId: string | null;
  dataSeparacao: string | null;
  nomeRetiradoPor: string | null;
  dataRetirada: string | null;
  semPedido: boolean;
  itens: Item[];
}

interface SemanaLista {
  id: string;
  dataInicio: string;
  dataFim: string;
  status: string;
  listasLocais: ListaLocal[];
}

// qteSeparada fica como texto para permitir o campo vazio (item ainda não conferido)
type ItemForm = { qteSeparada: string; obs: string };

const itensSolicitados = (local: ListaLocal) => local.itens.filter((i) => i.quantidadeSolicitada > 0);

const formatarData = (iso: string) => iso.split('-').reverse().join('/');

const STATUS_SEMANA: Record<string, string> = {
  EmMontagem: 'Em montagem',
  EnviadaParaSeparacao: 'Enviada para separação',
  Finalizada: 'Finalizada',
};

const calcularFalta = (solicitado: number, separado: string) => {
  if (separado === '') return null;
  return Math.max(0, solicitado - Number(separado));
};

export default function AlmoxarifeSemanaAtual({ isReadOnly = false }: { isReadOnly?: boolean }) {
  const [semana, setSemana] = useState<SemanaLista | null>(null);
  const [selectedLocalId, setSelectedLocalId] = useState<string | null>(null);
  const [itensForm, setItensForm] = useState<Record<string, ItemForm>>({});
  const [nomeRetiradoInput, setNomeRetiradoInput] = useState('');
  const [modalRetiradaAberto, setModalRetiradaAberto] = useState(false);
  const [retiradaConferida, setRetiradaConferida] = useState(false);
  const [loading, setLoading] = useState(true);
  const [salvando, setSalvando] = useState(false);
  const [mensagem, setMensagem] = useState<{ tipo: 'sucesso' | 'erro'; texto: string } | null>(null);
  const painelRef = useRef<HTMLDivElement>(null);

  // No celular o painel fica abaixo da grade de locais: rola até ele ao abrir um local
  useEffect(() => {
    if (selectedLocalId) painelRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }, [selectedLocalId]);

  useEffect(() => {
    carregarSemana();
  }, []);

  const carregarSemana = async () => {
    setLoading(true);
    try {
      const data = await fetchApi<SemanaLista>('/api/semanas/atual');
      setSemana(data);
    } catch (err: any) {
      setSemana(null);
    } finally {
      setLoading(false);
    }
  };

  const montarForm = (local: ListaLocal) => {
    const initialMap: Record<string, ItemForm> = {};
    local.itens.forEach((item) => {
      initialMap[item.id] = {
        qteSeparada: item.quantidadeSeparada === null ? '' : String(item.quantidadeSeparada),
        obs: item.observacao || '',
      };
    });
    setItensForm(initialMap);
  };

  const handleSelectLocal = (local: ListaLocal) => {
    setMensagem(null);
    if (selectedLocalId === local.localId) {
      setSelectedLocalId(null);
      return;
    }

    setSelectedLocalId(local.localId);
    setNomeRetiradoInput(local.nomeRetiradoPor || '');
    montarForm(local);
  };

  const handleQuantidadeChange = (item: Item, value: string) => {
    let novoValor = value.replace(/\D/g, '');
    if (novoValor !== '' && Number(novoValor) > item.quantidadeSolicitada) {
      novoValor = String(item.quantidadeSolicitada);
    }
    setItensForm((prev) => ({ ...prev, [item.id]: { ...prev[item.id], qteSeparada: novoValor } }));
  };

  const handleObsChange = (itemId: string, value: string) => {
    setItensForm((prev) => ({ ...prev, [itemId]: { ...prev[itemId], obs: value } }));
  };

  const separarTudo = (local: ListaLocal) => {
    setItensForm((prev) => {
      const novo = { ...prev };
      itensSolicitados(local).forEach((item) => {
        if (novo[item.id]?.qteSeparada === '') {
          novo[item.id] = { ...novo[item.id], qteSeparada: String(item.quantidadeSolicitada) };
        }
      });
      return novo;
    });
  };

  const atualizarListaNoEstado = (atualizada: ListaLocal) => {
    setSemana((prev) => {
      if (!prev) return prev;
      return { ...prev, listasLocais: prev.listasLocais.map((l) => (l.id === atualizada.id ? atualizada : l)) };
    });
  };

  // Envia ao backend todos os itens preenchidos que mudaram desde o último salvamento
  const salvarItensAlterados = async (local: ListaLocal) => {
    const alterados = itensSolicitados(local).filter((item) => {
      const form = itensForm[item.id];
      if (!form || form.qteSeparada === '') return false;
      return Number(form.qteSeparada) !== item.quantidadeSeparada || (form.obs || null) !== (item.observacao || null);
    });

    for (const item of alterados) {
      const form = itensForm[item.id];
      await fetchApi(`/api/lista-local-itens/${item.id}/separar`, {
        method: 'PUT',
        body: JSON.stringify({ quantidadeSeparada: Number(form.qteSeparada), observacao: form.obs }),
      });
    }

    const atualizada = await fetchApi<ListaLocal>(`/api/lista-locais/${local.id}`);
    atualizarListaNoEstado(atualizada);
    return { atualizada, quantidadeSalva: alterados.length };
  };

  const salvarSeparacao = async (local: ListaLocal) => {
    if (isReadOnly) return;
    setSalvando(true);
    setMensagem(null);
    try {
      const { quantidadeSalva } = await salvarItensAlterados(local);
      setMensagem({
        tipo: 'sucesso',
        texto: quantidadeSalva > 0 ? `${quantidadeSalva} item(ns) salvo(s).` : 'Nenhuma alteração para salvar.',
      });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao salvar a separação.' });
    } finally {
      setSalvando(false);
    }
  };

  const confirmarListaLocal = async (local: ListaLocal) => {
    if (isReadOnly) return;
    setMensagem(null);

    const itens = itensSolicitados(local);
    const semQuantidade = itens.filter((i) => (itensForm[i.id]?.qteSeparada ?? '') === '');
    if (semQuantidade.length > 0) {
      setMensagem({
        tipo: 'erro',
        texto: `Informe a quantidade separada de todos os itens antes de confirmar. Faltam: ${semQuantidade
          .map((i) => i.nomeMaterial)
          .join(', ')}.`,
      });
      return;
    }

    const comFalta = itens.filter((i) => (calcularFalta(i.quantidadeSolicitada, itensForm[i.id].qteSeparada) ?? 0) > 0);
    if (comFalta.length > 0) {
      const resumo = comFalta
        .map((i) => `• ${i.nomeMaterial}: faltam ${calcularFalta(i.quantidadeSolicitada, itensForm[i.id].qteSeparada)} ${i.unidade}`)
        .join('\n');
      if (!window.confirm(`Esta lista será confirmada COM PENDÊNCIA:\n\n${resumo}\n\nDeseja continuar?`)) return;
    }

    setSalvando(true);
    try {
      await salvarItensAlterados(local);
      const updated = await fetchApi<ListaLocal>(`/api/lista-locais/${local.id}/confirmar`, { method: 'POST' });
      atualizarListaNoEstado(updated);
      montarForm(updated);
      setMensagem({
        tipo: 'sucesso',
        texto:
          updated.status === 'ConfirmadoComPendencia'
            ? 'Separação confirmada com pendência. Os itens em falta ficam registrados.'
            : 'Separação confirmada. Todos os itens foram separados.',
      });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao confirmar lista.' });
    } finally {
      setSalvando(false);
    }
  };

  const abrirModalRetirada = () => {
    if (nomeRetiradoInput.trim().length < 3) {
      setMensagem({ tipo: 'erro', texto: 'Informe o nome completo da pessoa que está retirando o material.' });
      return;
    }
    setMensagem(null);
    setRetiradaConferida(false);
    setModalRetiradaAberto(true);
  };

  const registrarRetirada = async (listaLocalId: string) => {
    if (isReadOnly || !retiradaConferida) return;

    setSalvando(true);
    setMensagem(null);
    try {
      const updated = await fetchApi<ListaLocal>(`/api/lista-locais/${listaLocalId}/retirada`, {
        method: 'POST',
        body: JSON.stringify({ nomeRetiradoPor: nomeRetiradoInput.trim() }),
      });

      atualizarListaNoEstado(updated);
      setModalRetiradaAberto(false);
      setMensagem({ tipo: 'sucesso', texto: `Retirada registrada em nome de ${updated.nomeRetiradoPor}.` });
    } catch (err: any) {
      setModalRetiradaAberto(false);
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao registrar retirada.' });
    } finally {
      setSalvando(false);
    }
  };

  const exportarLocal = async (listaLocalId: string, formato: 'excel' | 'pdf') => {
    try {
      const blob = await fetchApi(`/api/lista-locais/${listaLocalId}/export?formato=${formato}`);
      const url = window.URL.createObjectURL(blob as Blob);
      const a = document.createElement('a');
      a.href = url;
      const ext = formato === 'excel' ? 'xlsx' : 'pdf';
      a.download = `Relatorio-Local.${ext}`;
      document.body.appendChild(a);
      a.click();
      a.remove();
    } catch (err: any) {
      alert('Erro ao realizar exportação.');
    }
  };

  const getBadgeStyle = (status: ListaLocal['status']) => {
    switch (status) {
      case 'Pendente':
        return 'bg-[#FAC775] text-[#412402] border-amber-300';
      case 'Separado':
        return 'bg-blue-100 text-blue-900 border-blue-300';
      case 'Confirmado':
        return 'bg-[#C0DD97] text-[#173404] border-emerald-300';
      case 'ConfirmadoComPendencia':
        return 'bg-[#FAD2D2] text-[#5C0606] border-red-300';
      case 'Retirado':
        return 'bg-slate-200 text-slate-800 border-slate-300';
      default:
        return 'bg-slate-100 text-slate-700';
    }
  };

  const getStatusLabel = (local: ListaLocal) =>
    local.semPedido ? 'Sem pedido' : local.status === 'ConfirmadoComPendencia' ? 'Com pendência' : local.status;

  // A lista só chega ao almoxarife depois que o supervisor conclui todos os pedidos e envia a semana
  const aguardandoSupervisor = semana?.status === 'EmMontagem';
  const locaisComPedido = semana ? semana.listasLocais.filter((l) => l.semPedido || itensSolicitados(l).length > 0).length : 0;

  const localSelecionado = semana?.listasLocais.find((l) => l.localId === selectedLocalId);
  const isBloqueadoParaEdicao = localSelecionado?.status === 'Retirado' || aguardandoSupervisor || !!localSelecionado?.semPedido;
  const podeEditar = !isReadOnly && !isBloqueadoParaEdicao;
  const podeRetirar =
    !isReadOnly &&
    !aguardandoSupervisor &&
    (localSelecionado?.status === 'Confirmado' || localSelecionado?.status === 'ConfirmadoComPendencia');

  const itensDoLocal = localSelecionado ? itensSolicitados(localSelecionado) : [];
  const totalConferidos = itensDoLocal.filter((i) => (itensForm[i.id]?.qteSeparada ?? '') !== '').length;
  const itensComFalta = itensDoLocal.filter(
    (i) => (calcularFalta(i.quantidadeSolicitada, itensForm[i.id]?.qteSeparada ?? '') ?? 0) > 0
  );

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-[#1F6D2E]"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Top Header */}
      <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-200 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 tracking-tight">
            {isReadOnly ? 'Consulta de Separação por Local' : 'Separação e Conferência de Materiais'}
          </h2>
          {semana ? (
            <p className="text-sm text-slate-600 font-medium mt-1">
              Semana: <span className="font-semibold text-slate-900">{formatarData(semana.dataInicio)}</span> a{' '}
              <span className="font-semibold text-slate-900">{formatarData(semana.dataFim)}</span> | Status da Semana:{' '}
              <span className="font-bold text-[#1F6D2E]">{STATUS_SEMANA[semana.status] ?? semana.status}</span>
            </p>
          ) : (
            <p className="text-sm text-slate-500 mt-1">Nenhuma semana encontrada.</p>
          )}
        </div>
      </div>

      {/* Alerta de Mensagem */}
      {mensagem && (
        <div
          className={`p-4 rounded-xl text-sm font-medium flex items-center gap-3 ${
            mensagem.tipo === 'sucesso'
              ? 'bg-emerald-50 text-emerald-800 border border-emerald-200'
              : 'bg-rose-50 text-rose-800 border border-rose-200'
          }`}
        >
          {mensagem.texto}
        </div>
      )}

      {semana && aguardandoSupervisor && (
        <div className="p-5 bg-amber-50 border border-amber-200 rounded-2xl text-amber-900 flex items-start gap-3">
          <Lock className="w-5 h-5 mt-0.5 flex-shrink-0 text-amber-700" />
          <div>
            <p className="font-bold text-sm">Lista ainda em montagem pelo supervisor</p>
            <p className="text-sm mt-1">
              A separação só é liberada depois que os pedidos de todos os locais forem realizados e a semana for
              enviada ao almoxarifado. Pedidos realizados até agora:{' '}
              <span className="font-bold">
                {locaisComPedido} de {semana.listasLocais.length} locais
              </span>
              .
            </p>
          </div>
        </div>
      )}

      {semana && !(aguardandoSupervisor && !isReadOnly) && (
        <>
          {/* Grade Responsiva de Cards (23 Locais) */}
          <div
            className="grid gap-3.5"
            style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))' }}
          >
            {semana.listasLocais.map((local) => {
              const isSelected = selectedLocalId === local.localId;

              return (
                <button
                  key={local.id}
                  onClick={() => handleSelectLocal(local)}
                  className={`p-4 rounded-xl border text-left transition-all duration-200 flex flex-col justify-between min-h-[100px] ${
                    isSelected
                      ? 'border-[#1F6D2E] ring-2 ring-[#1F6D2E]/30 bg-emerald-50/40 shadow-md'
                      : 'border-slate-200 bg-white hover:border-slate-300 hover:shadow-sm'
                  }`}
                >
                  <div>
                    <span className="font-bold text-sm text-slate-900 block truncate">
                      {local.nomeLocal}
                    </span>
                    {local.nomeRetiradoPor && (
                      <p className="text-[11px] text-slate-500 mt-1 truncate">
                        Retirado por: <span className="font-semibold text-slate-700">{local.nomeRetiradoPor}</span>
                      </p>
                    )}
                  </div>

                  <div className="flex items-center justify-between w-full mt-3">
                    <span
                      className={`text-[10px] font-extrabold px-2.5 py-0.5 rounded-full border ${(local.semPedido ? 'bg-slate-100 text-slate-600 border-slate-300' : getBadgeStyle(local.status))}`}
                    >
                      {getStatusLabel(local)}
                    </span>
                    {isSelected ? (
                      <ChevronUp className="w-4 h-4 text-[#1F6D2E]" />
                    ) : (
                      <ChevronDown className="w-4 h-4 text-slate-400" />
                    )}
                  </div>
                </button>
              );
            })}
          </div>

          {/* Painel de Detalhe do Local Selecionado */}
          {localSelecionado && (
            <div ref={painelRef} className="scroll-mt-20 bg-white rounded-2xl border-2 border-[#1F6D2E] shadow-xl p-4 sm:p-6 space-y-6 animate-fadeIn">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-200 pb-4">
                <div>
                  <div className="flex items-center gap-3">
                    <h3 className="text-xl font-bold text-slate-900">
                      Conferência: <span className="text-[#1F6D2E]">{localSelecionado.nomeLocal}</span>
                    </h3>
                    <span className={`text-xs font-bold px-3 py-1 rounded-full border ${localSelecionado.semPedido ? 'bg-slate-100 text-slate-600 border-slate-300' : getBadgeStyle(localSelecionado.status)}`}>
                      {getStatusLabel(localSelecionado)}
                    </span>
                  </div>
                  {localSelecionado.nomeRetiradoPor && (
                    <p className="text-xs text-slate-600 font-medium mt-1">
                      Retirado por <span className="font-bold text-slate-900">{localSelecionado.nomeRetiradoPor}</span> em{' '}
                      <span className="font-bold">{new Date(localSelecionado.dataRetirada!).toLocaleString('pt-BR')}</span>
                    </p>
                  )}
                  <p className="text-xs text-slate-500 font-medium mt-1">
                    {totalConferidos} de {itensDoLocal.length} itens conferidos
                    {itensComFalta.length > 0 && (
                      <span className="text-rose-700 font-bold"> · {itensComFalta.length} com falta</span>
                    )}
                  </p>
                </div>

                <div className="flex items-center gap-2">
                  <button
                    onClick={() => exportarLocal(localSelecionado.id, 'excel')}
                    className="px-3 py-2 bg-emerald-100 text-emerald-800 hover:bg-emerald-200 rounded-lg text-xs font-bold transition flex items-center gap-1.5"
                  >
                    <FileSpreadsheet className="w-4 h-4 text-emerald-700" />
                    Excel
                  </button>
                  <button
                    onClick={() => exportarLocal(localSelecionado.id, 'pdf')}
                    className="px-3 py-2 bg-rose-100 text-rose-800 hover:bg-rose-200 rounded-lg text-xs font-bold transition flex items-center gap-1.5"
                  >
                    <FileText className="w-4 h-4 text-rose-700" />
                    PDF
                  </button>
                </div>
              </div>

              {/* Ações do Almoxarife: separação */}
              {podeEditar && (
                <div className="bg-slate-50 p-4 rounded-xl border border-slate-200 flex flex-col sm:flex-row sm:items-center gap-3">
                  <button
                    onClick={() => separarTudo(localSelecionado)}
                    disabled={salvando}
                    className="px-4 py-2.5 bg-white border border-slate-300 text-slate-800 hover:bg-slate-100 rounded-xl text-sm font-semibold transition"
                    title="Preenche a quantidade solicitada nos itens ainda vazios"
                  >
                    Separar tudo o solicitado
                  </button>
                  <button
                    onClick={() => salvarSeparacao(localSelecionado)}
                    disabled={salvando}
                    className="px-4 py-2.5 bg-white border border-[#1F6D2E] text-[#1F6D2E] hover:bg-emerald-50 rounded-xl text-sm font-semibold transition flex items-center gap-2"
                  >
                    <Save className="w-4 h-4" />
                    Salvar andamento
                  </button>
                  <button
                    onClick={() => confirmarListaLocal(localSelecionado)}
                    disabled={salvando}
                    className="px-4 py-2.5 bg-blue-700 text-white hover:bg-blue-800 disabled:opacity-60 rounded-xl text-sm font-semibold transition flex items-center gap-2 shadow sm:ml-auto"
                  >
                    <CheckCircle2 className="w-4 h-4" />
                    {salvando ? 'Salvando...' : 'Confirmar Separação'}
                  </button>
                </div>
              )}

              {/* Etapa de Retirada */}
              {podeRetirar && (
                <div className="bg-emerald-50/60 p-4 rounded-xl border border-emerald-200 space-y-2">
                  <p className="text-sm font-bold text-slate-900 flex items-center gap-2">
                    <UserCheck className="w-4 h-4 text-[#1F6D2E]" />
                    Registrar retirada
                  </p>
                  <div className="flex flex-col sm:flex-row sm:items-center gap-3">
                    <input
                      type="text"
                      placeholder="Nome completo de quem está retirando *"
                      value={nomeRetiradoInput}
                      onChange={(e) => setNomeRetiradoInput(e.target.value)}
                      className="flex-1 px-3.5 py-2 bg-white border border-slate-300 rounded-lg text-sm font-semibold outline-none focus:ring-2 focus:ring-[#1F6D2E]"
                    />
                    <button
                      onClick={abrirModalRetirada}
                      disabled={salvando}
                      className="px-4 py-2 bg-[#1F6D2E] text-white hover:bg-[#175423] rounded-xl text-sm font-semibold transition flex items-center gap-2 shadow"
                    >
                      <UserCheck className="w-4 h-4" />
                      Verificar e Registrar
                    </button>
                  </div>
                </div>
              )}

              {localSelecionado.status === 'Retirado' && (
                <div className="p-3 bg-slate-100 text-slate-700 rounded-xl text-xs font-semibold flex items-center gap-2 border border-slate-200">
                  <Lock className="w-4 h-4 text-slate-500" />
                  Este local foi finalizado com a retirada registrada e não permite novas edições.
                </div>
              )}

              {/* Itens em cartões (celular) */}
              <div className="sm:hidden space-y-2">
                {itensDoLocal.length === 0 && (
                  <p className="py-6 text-center text-slate-400 text-sm">
                    {localSelecionado.semPedido ? 'Local sem pedido nesta semana: nada a separar.' : 'Nenhum material solicitado para este local.'}
                  </p>
                )}
                {itensDoLocal.map((item) => {
                  const form = itensForm[item.id] || {
                    qteSeparada: item.quantidadeSeparada === null ? '' : String(item.quantidadeSeparada),
                    obs: item.observacao || '',
                  };
                  const falta = calcularFalta(item.quantidadeSolicitada, form.qteSeparada);

                  return (
                    <div
                      key={item.id}
                      className={`p-3 rounded-xl border space-y-2 ${falta ? 'bg-rose-50/60 border-rose-200' : 'bg-white border-slate-200'}`}
                    >
                      <div className="flex items-start justify-between gap-2">
                        <p className="font-semibold text-slate-900 text-sm">{item.nomeMaterial}</p>
                        {falta === null ? (
                          <span className="text-xs text-slate-400 whitespace-nowrap">a conferir</span>
                        ) : falta > 0 ? (
                          <span className="inline-flex items-center gap-1 text-xs font-bold px-2 py-0.5 rounded bg-rose-100 text-rose-800 whitespace-nowrap">
                            <AlertTriangle className="w-3 h-3" />
                            Falta {falta} {item.unidade}
                          </span>
                        ) : (
                          <span className="text-xs font-bold px-2 py-0.5 rounded bg-emerald-100 text-emerald-800">OK</span>
                        )}
                      </div>
                      <div className="flex items-center gap-3 text-sm">
                        <span className="text-slate-500">
                          Pedido: <span className="font-bold text-slate-900">{item.quantidadeSolicitada}</span> {item.unidade}
                        </span>
                        <label className="ml-auto flex items-center gap-2 text-slate-500">
                          Separado:
                          {podeEditar ? (
                            <input
                              type="text"
                              inputMode="numeric"
                              placeholder="—"
                              value={form.qteSeparada}
                              onChange={(e) => handleQuantidadeChange(item, e.target.value)}
                              className={`w-16 h-11 px-2 bg-white border rounded-lg text-center font-bold text-base outline-none focus:ring-2 focus:ring-[#1F6D2E] ${
                                form.qteSeparada === '' ? 'border-amber-400' : 'border-slate-300'
                              }`}
                            />
                          ) : (
                            <span className="font-bold text-slate-900">{item.quantidadeSeparada ?? '—'}</span>
                          )}
                        </label>
                      </div>
                      {podeEditar ? (
                        <input
                          type="text"
                          placeholder={falta ? 'Motivo da falta (opcional)' : 'Obs (opcional)'}
                          value={form.obs}
                          onChange={(e) => handleObsChange(item.id, e.target.value)}
                          className="w-full h-10 px-3 bg-white border border-slate-200 rounded-lg text-base outline-none focus:ring-2 focus:ring-[#1F6D2E]"
                        />
                      ) : (
                        item.observacao && <p className="text-xs text-slate-500">Obs: {item.observacao}</p>
                      )}
                    </div>
                  );
                })}
              </div>

              {/* Tabela de Itens (tablet e computador) */}
              <div className="hidden sm:block overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead className="bg-slate-50 text-xs font-bold text-slate-500 uppercase border-b border-slate-200">
                    <tr>
                      <th className="px-4 py-3">Material</th>
                      <th className="px-4 py-3">Unid.</th>
                      <th className="px-4 py-3 text-center">Solicitado</th>
                      <th className="px-4 py-3 text-center">Separado</th>
                      <th className="px-4 py-3 text-center">Faltando</th>
                      <th className="px-4 py-3">Observação</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 font-medium text-slate-800">
                    {itensDoLocal.length === 0 && (
                      <tr>
                        <td colSpan={6} className="px-4 py-6 text-center text-slate-400 text-sm">
                          {localSelecionado.semPedido ? 'Local sem pedido nesta semana: nada a separar.' : 'Nenhum material solicitado para este local.'}
                        </td>
                      </tr>
                    )}
                    {itensDoLocal.map((item) => {
                      const form = itensForm[item.id] || {
                        qteSeparada: item.quantidadeSeparada === null ? '' : String(item.quantidadeSeparada),
                        obs: item.observacao || '',
                      };
                      const falta = calcularFalta(item.quantidadeSolicitada, form.qteSeparada);

                      return (
                        <tr key={item.id} className={`transition ${falta ? 'bg-rose-50/60' : 'hover:bg-slate-50/80'}`}>
                          <td className="px-4 py-3 font-semibold text-slate-900">{item.nomeMaterial}</td>
                          <td className="px-4 py-3 text-slate-500">{item.unidade}</td>
                          <td className="px-4 py-3 text-center font-bold">{item.quantidadeSolicitada}</td>
                          <td className="px-4 py-3 text-center">
                            {podeEditar ? (
                              <input
                                type="text"
                                inputMode="numeric"
                                placeholder="—"
                                value={form.qteSeparada}
                                onChange={(e) => handleQuantidadeChange(item, e.target.value)}
                                className={`w-16 px-2 py-1 bg-white border rounded text-center font-bold text-sm outline-none focus:ring-2 focus:ring-[#1F6D2E] ${
                                  form.qteSeparada === '' ? 'border-amber-400' : 'border-slate-300'
                                }`}
                              />
                            ) : (
                              <span className="font-bold">{item.quantidadeSeparada ?? '—'}</span>
                            )}
                          </td>
                          <td className="px-4 py-3 text-center">
                            {falta === null ? (
                              <span className="text-xs text-slate-400">a conferir</span>
                            ) : falta > 0 ? (
                              <span className="inline-flex items-center gap-1 text-xs font-bold px-2 py-0.5 rounded bg-rose-100 text-rose-800">
                                <AlertTriangle className="w-3 h-3" />
                                {falta} {item.unidade}
                              </span>
                            ) : (
                              <span className="text-xs font-bold px-2 py-0.5 rounded bg-emerald-100 text-emerald-800">OK</span>
                            )}
                          </td>
                          <td className="px-4 py-3">
                            {podeEditar ? (
                              <input
                                type="text"
                                placeholder={falta ? 'Motivo da falta (opcional)' : 'Obs (opcional)'}
                                value={form.obs}
                                onChange={(e) => handleObsChange(item.id, e.target.value)}
                                className="w-full px-2.5 py-1 bg-white border border-slate-200 rounded text-xs outline-none focus:ring-2 focus:ring-[#1F6D2E]"
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
            </div>
          )}
        </>
      )}

      {/* Modal de verificação da retirada */}
      {modalRetiradaAberto && localSelecionado && (
        <div className="fixed inset-0 z-[60] bg-black/40 flex items-center justify-center p-4">
          <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg p-6 space-y-4">
            <div className="flex items-start justify-between">
              <h3 className="text-lg font-bold text-slate-900">Verificar retirada</h3>
              <button onClick={() => setModalRetiradaAberto(false)} className="text-slate-400 hover:text-slate-700">
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="text-sm text-slate-700 space-y-1">
              <p>
                Local: <span className="font-bold text-slate-900">{localSelecionado.nomeLocal}</span>
              </p>
              <p>
                Quem está retirando: <span className="font-bold text-slate-900">{nomeRetiradoInput.trim()}</span>
              </p>
              <p>
                Itens entregues: <span className="font-bold text-slate-900">{itensDoLocal.length}</span>
              </p>
            </div>

            {itensDoLocal.some((i) => (i.quantidadeSeparada ?? 0) < i.quantidadeSolicitada) && (
              <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl text-xs text-rose-800 space-y-1">
                <p className="font-bold flex items-center gap-1">
                  <AlertTriangle className="w-4 h-4" /> Itens entregues com falta:
                </p>
                {itensDoLocal
                  .filter((i) => (i.quantidadeSeparada ?? 0) < i.quantidadeSolicitada)
                  .map((i) => (
                    <p key={i.id}>
                      • {i.nomeMaterial}: entregue {i.quantidadeSeparada ?? 0} de {i.quantidadeSolicitada} {i.unidade}
                    </p>
                  ))}
              </div>
            )}

            <label className="flex items-start gap-2 text-sm text-slate-800 cursor-pointer">
              <input
                type="checkbox"
                checked={retiradaConferida}
                onChange={(e) => setRetiradaConferida(e.target.checked)}
                className="mt-0.5 w-4 h-4 accent-[#1F6D2E]"
              />
              Confirmo que conferi a identidade de quem está retirando e que os materiais entregues correspondem à lista.
            </label>

            <div className="flex justify-end gap-2 pt-2">
              <button
                onClick={() => setModalRetiradaAberto(false)}
                className="px-4 py-2 rounded-xl text-sm font-semibold text-slate-700 hover:bg-slate-100"
              >
                Cancelar
              </button>
              <button
                onClick={() => registrarRetirada(localSelecionado.id)}
                disabled={!retiradaConferida || salvando}
                className="px-4 py-2 bg-[#1F6D2E] text-white hover:bg-[#175423] disabled:opacity-50 disabled:cursor-not-allowed rounded-xl text-sm font-semibold flex items-center gap-2"
              >
                <UserCheck className="w-4 h-4" />
                {salvando ? 'Registrando...' : 'Confirmar Retirada'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
