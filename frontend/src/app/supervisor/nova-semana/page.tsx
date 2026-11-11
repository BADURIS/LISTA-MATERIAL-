'use client';

import React, { useState, useEffect, useRef } from 'react';
import { fetchApi } from '@/lib/api';
import { Plus, Send, CheckCircle2, AlertCircle, Save, ChevronDown, ChevronUp } from 'lucide-react';

interface MaterialItem {
  materialId: string;
  nomeMaterial: string;
  unidade: string;
  quantidadeSolicitada: number;
}

interface ListaLocal {
  id: string;
  semanaListaId: string;
  localId: string;
  nomeLocal: string;
  tipoLocal: string;
  status: string;
  semPedido: boolean;
  itens: {
    id: string;
    materialId: string;
    nomeMaterial: string;
    unidade: string;
    quantidadeSolicitada: number;
  }[];
}

interface SemanaLista {
  id: string;
  dataInicio: string;
  dataFim: string;
  status: string;
  listasLocais: ListaLocal[];
}

export default function SupervisorNovaSemanaPage() {
  const [semana, setSemana] = useState<SemanaLista | null>(null);
  const [materiaisSeeded, setMateriaisSeeded] = useState<any[]>([]);
  const [selectedLocalId, setSelectedLocalId] = useState<string | null>(null);
  const [quantidadesForm, setQuantidadesForm] = useState<Record<string, number>>({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [mensagemStatus, setMensagemStatus] = useState<{ tipo: 'sucesso' | 'erro'; texto: string } | null>(null);
  const painelRef = useRef<HTMLDivElement>(null);

  // No celular o painel fica abaixo da grade de locais: rola até ele ao abrir um local
  useEffect(() => {
    if (selectedLocalId) painelRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }, [selectedLocalId]);

  useEffect(() => {
    carregarDados();
  }, []);

  const carregarDados = async () => {
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

  const criarNovaSemana = async () => {
    setLoading(true);
    setMensagemStatus(null);
    try {
      const novaSemana = await fetchApi<SemanaLista>('/api/semanas', { method: 'POST' });
      setSemana(novaSemana);
      setSelectedLocalId(null);
      setMensagemStatus({ tipo: 'sucesso', texto: 'Nova semana criada com sucesso!' });
    } catch (err: any) {
      setMensagemStatus({ tipo: 'erro', texto: err.message || 'Erro ao criar nova semana.' });
    } finally {
      setLoading(false);
    }
  };

  const handleSelectLocal = (local: ListaLocal) => {
    if (selectedLocalId === local.localId) {
      setSelectedLocalId(null);
      return;
    }

    setSelectedLocalId(local.localId);
    
    // Inicializar o formulário com os valores existentes
    const qteMap: Record<string, number> = {};
    local.itens.forEach((item) => {
      qteMap[item.materialId] = item.quantidadeSolicitada;
    });
    setQuantidadesForm(qteMap);
  };

  const handleQteChange = (materialId: string, value: string) => {
    const num = Math.max(0, parseInt(value) || 0);
    setQuantidadesForm((prev) => ({ ...prev, [materialId]: num }));
  };

  const salvarPreenchimentoLocal = async (localId: string) => {
    if (!semana) return;
    setSaving(true);
    setMensagemStatus(null);

    const payload = Object.entries(quantidadesForm).map(([materialId, quantidadeSolicitada]) => ({
      materialId,
      quantidadeSolicitada,
    }));

    try {
      const updatedListaLocal = await fetchApi(`/api/semanas/${semana.id}/locais/${localId}`, {
        method: 'PUT',
        body: JSON.stringify({ itens: payload }),
      });

      // Atualiza o estado da semana localmente
      setSemana((prev) => {
        if (!prev) return prev;
        const novasListas = prev.listasLocais.map((l) =>
          l.localId === localId ? updatedListaLocal : l
        );
        return { ...prev, listasLocais: novasListas };
      });

      setMensagemStatus({ tipo: 'sucesso', texto: 'Quantidades salvas para o local com sucesso!' });
    } catch (err: any) {
      setMensagemStatus({ tipo: 'erro', texto: err.message || 'Erro ao salvar quantidades.' });
    } finally {
      setSaving(false);
    }
  };

  const definirSemPedido = async (local: ListaLocal, semPedido: boolean) => {
    if (!semana) return;
    if (
      semPedido &&
      !window.confirm(`Marcar "${local.nomeLocal}" como sem pedido nesta semana? As quantidades lançadas neste local serão zeradas.`)
    ) {
      return;
    }

    setSaving(true);
    setMensagemStatus(null);
    try {
      const atualizado = await fetchApi<ListaLocal>(`/api/semanas/${semana.id}/locais/${local.localId}/sem-pedido`, {
        method: 'PUT',
        body: JSON.stringify({ semPedido }),
      });
      setSemana((prev) =>
        prev ? { ...prev, listasLocais: prev.listasLocais.map((l) => (l.localId === local.localId ? atualizado : l)) } : prev
      );
      const qteMap: Record<string, number> = {};
      atualizado.itens.forEach((item) => (qteMap[item.materialId] = item.quantidadeSolicitada));
      setQuantidadesForm(qteMap);
      setMensagemStatus({
        tipo: 'sucesso',
        texto: semPedido ? `${local.nomeLocal} marcado como sem pedido nesta semana.` : `${local.nomeLocal} voltou a aguardar pedido.`,
      });
    } catch (err: any) {
      setMensagemStatus({ tipo: 'erro', texto: err.message || 'Erro ao atualizar o local.' });
    } finally {
      setSaving(false);
    }
  };

  const enviarParaSeparacao = async () => {
    if (!semana) return;

    const pendentes = semana.listasLocais.filter((l) => !isLocalPreenchido(l));
    if (pendentes.length > 0) {
      setMensagemStatus({
        tipo: 'erro',
        texto: `Realize os pedidos de todos os locais antes de enviar ao almoxarifado. Pendentes: ${pendentes
          .map((l) => l.nomeLocal)
          .join(', ')}.`,
      });
      return;
    }

    if (
      !window.confirm(
        `Todos os ${semana.listasLocais.length} locais estão com pedido realizado.\n\nApós o envio ao almoxarifado os pedidos não poderão mais ser alterados. Deseja enviar?`
      )
    ) {
      return;
    }

    setEnviando(true);
    setMensagemStatus(null);

    try {
      await fetchApi(`/api/semanas/${semana.id}/enviar`, { method: 'POST' });
      await carregarDados();
      setMensagemStatus({ tipo: 'sucesso', texto: 'Semana enviada para separação com sucesso!' });
    } catch (err: any) {
      setMensagemStatus({ tipo: 'erro', texto: err.message || 'Erro ao enviar para separação.' });
    } finally {
      setEnviando(false);
    }
  };

  const localSelecionado = semana?.listasLocais.find((l) => l.localId === selectedLocalId);

  // Um local é considerado "Preenchido" se possui itens salvos com quantidade > 0
  const isLocalPreenchido = (l: ListaLocal) => {
    return l.semPedido || (l.itens.length > 0 && l.itens.some((i) => i.quantidadeSolicitada > 0));
  };

  const todosPreenchidos = semana
    ? semana.listasLocais.length > 0 && semana.listasLocais.every(isLocalPreenchido)
    : false;

  const totalPreenchidos = semana ? semana.listasLocais.filter(isLocalPreenchido).length : 0;
  const totalLocais = semana ? semana.listasLocais.length : 0;
  const locaisPendentes = semana ? semana.listasLocais.filter((l) => !isLocalPreenchido(l)) : [];
  const emMontagem = semana?.status === 'EmMontagem';

  if (loading && !semana) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-[#1F6D2E]"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Top Header & Action */}
      <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-200 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 tracking-tight">
            Montagem de Lista Semanal
          </h2>
          {semana ? (
            <p className="text-sm text-slate-600 font-medium mt-1">
              Período: <span className="font-semibold text-slate-900">{semana.dataInicio}</span> a{' '}
              <span className="font-semibold text-slate-900">{semana.dataFim}</span> | Status:{' '}
              <span className="font-bold text-[#1F6D2E] uppercase">{semana.status}</span>
            </p>
          ) : (
            <p className="text-sm text-slate-500 mt-1">
              Nenhuma semana em montagem ativa no momento.
            </p>
          )}
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <button
            onClick={criarNovaSemana}
            className="px-4 py-2.5 bg-[#1F6D2E] text-white rounded-xl font-semibold text-sm hover:bg-[#175423] transition flex items-center gap-2 shadow-sm"
          >
            <Plus className="w-4 h-4" />
            Criar Nova Semana
          </button>

          {semana && semana.status === 'EmMontagem' && (
            <button
              onClick={enviarParaSeparacao}
              disabled={!todosPreenchidos || enviando}
              className={`px-4 py-2.5 rounded-xl font-semibold text-sm transition flex items-center gap-2 shadow-sm ${
                todosPreenchidos
                  ? 'bg-emerald-600 text-white hover:bg-emerald-700 cursor-pointer'
                  : 'bg-slate-200 text-slate-400 cursor-not-allowed'
              }`}
              title={
                !todosPreenchidos
                  ? `Preencha todos os ${totalLocais} locais para liberar o envio (${totalPreenchidos}/${totalLocais} preenchidos)`
                  : 'Enviar para separação no Almoxarifado'
              }
            >
              <Send className="w-4 h-4" />
              {enviando ? 'Enviando...' : 'Enviar para Separação'}
            </button>
          )}
        </div>
      </div>

      {/* Alerta de Mensagem */}
      {mensagemStatus && (
        <div
          className={`p-4 rounded-xl text-sm font-medium flex items-center gap-3 ${
            mensagemStatus.tipo === 'sucesso'
              ? 'bg-emerald-50 text-emerald-800 border border-emerald-200'
              : 'bg-rose-50 text-rose-800 border border-rose-200'
          }`}
        >
          {mensagemStatus.tipo === 'sucesso' ? (
            <CheckCircle2 className="w-5 h-5 text-emerald-600 flex-shrink-0" />
          ) : (
            <AlertCircle className="w-5 h-5 text-rose-600 flex-shrink-0" />
          )}
          {mensagemStatus.texto}
        </div>
      )}

      {semana && (
        <>
          {/* Barra de Progresso */}
          <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <div className="flex items-center gap-3">
              <span className="text-sm font-bold text-slate-700">Progresso dos Locais:</span>
              <span className="text-sm font-extrabold text-[#1F6D2E]">
                {totalPreenchidos} de {totalLocais} preenchidos
              </span>
            </div>
            <div className="w-full sm:w-64 bg-slate-100 rounded-full h-3 overflow-hidden border border-slate-200">
              <div
                className="bg-[#1F6D2E] h-full transition-all duration-300 rounded-full"
                style={{ width: `${totalLocais > 0 ? (totalPreenchidos / totalLocais) * 100 : 0}%` }}
              ></div>
            </div>
          </div>

          {emMontagem && locaisPendentes.length > 0 && (
            <div className="p-4 bg-amber-50 border border-amber-200 rounded-xl text-sm text-amber-900">
              <span className="font-bold">A lista só pode ser enviada ao almoxarifado depois que todos os pedidos forem realizados.</span>{' '}
              Faltam {locaisPendentes.length} local(is): {locaisPendentes.map((l) => l.nomeLocal).join(', ')}.
            </div>
          )}

          {!emMontagem && (
            <div className="p-4 bg-slate-100 border border-slate-200 rounded-xl text-sm text-slate-700 font-medium">
              Esta semana já foi enviada ao almoxarifado. Os pedidos estão travados e não podem mais ser alterados.
            </div>
          )}

          {/* Grade Responsiva de Cards (23 Locais) */}
          <div
            className="grid gap-3"
            style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))' }}
          >
            {semana.listasLocais.map((local) => {
              const preenchido = isLocalPreenchido(local);
              const isSelected = selectedLocalId === local.localId;

              return (
                <button
                  key={local.id}
                  onClick={() => handleSelectLocal(local)}
                  className={`p-3.5 rounded-xl border text-left transition-all duration-200 flex flex-col justify-between h-24 ${
                    isSelected
                      ? 'border-[#1F6D2E] ring-2 ring-[#1F6D2E]/30 bg-emerald-50/40 shadow-md'
                      : 'border-slate-200 bg-white hover:border-slate-300 hover:shadow-sm'
                  }`}
                >
                  <span className="font-bold text-sm text-slate-900 truncate">
                    {local.nomeLocal}
                  </span>

                  <div className="flex items-center justify-between w-full mt-2">
                    <span
                      className={`text-[11px] font-bold px-2.5 py-0.5 rounded-full ${
                        local.semPedido
                          ? 'bg-slate-200 text-slate-700'
                          : preenchido
                          ? 'bg-[#C0DD97] text-[#173404]'
                          : 'bg-[#FAC775] text-[#412402]'
                      }`}
                    >
                      {local.semPedido ? 'Sem pedido' : preenchido ? 'Preenchido' : 'Pendente'}
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

          {/* Painel de Detalhe de Preenchimento Rápido (Expansível abaixo da grade) */}
          {localSelecionado && (
            <div ref={painelRef} className="scroll-mt-20 bg-white rounded-2xl border-2 border-[#1F6D2E] shadow-xl p-4 sm:p-6 space-y-6 animate-fadeIn">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-200 pb-4">
                <div>
                  <h3 className="text-xl font-bold text-slate-900 flex items-center gap-2">
                    Preencher Material: <span className="text-[#1F6D2E]">{localSelecionado.nomeLocal}</span>
                  </h3>
                  <p className="text-xs text-slate-500 mt-0.5">
                    Navegue entre os campos de quantidade usando a tecla TAB para preenchimento ágil.
                  </p>
                </div>

                {emMontagem && (
                <div className="flex flex-wrap items-center gap-2 self-start sm:self-auto">
                  <button
                    onClick={() => definirSemPedido(localSelecionado, !localSelecionado.semPedido)}
                    disabled={saving}
                    className="px-4 py-2.5 bg-white border border-slate-300 text-slate-700 rounded-xl font-semibold text-sm hover:bg-slate-100 transition"
                  >
                    {localSelecionado.semPedido ? 'Desfazer "sem pedido"' : 'Sem pedido nesta semana'}
                  </button>
                  {!localSelecionado.semPedido && (
                    <button
                      onClick={() => salvarPreenchimentoLocal(localSelecionado.localId)}
                      disabled={saving}
                      className="px-5 py-2.5 bg-[#1F6D2E] text-white rounded-xl font-semibold text-sm hover:bg-[#175423] transition flex items-center gap-2 shadow"
                    >
                      <Save className="w-4 h-4" />
                      {saving ? 'Salvando...' : 'Salvar Local'}
                    </button>
                  )}
                </div>
                )}
              </div>

              {localSelecionado.semPedido && (
                <div className="p-4 bg-slate-100 border border-slate-200 rounded-xl text-sm text-slate-700 font-medium">
                  Este local foi marcado como <span className="font-bold">sem pedido nesta semana</span>. Ele conta como
                  concluído para o envio e não aparece para separação no almoxarifado.
                </div>
              )}

              {/* Lista de Materiais em Grade / Tabela Otimizada */}
              {!localSelecionado.semPedido && (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {localSelecionado.itens.map((item) => (
                  <div
                    key={item.materialId}
                    className="p-3.5 rounded-xl border border-slate-200 bg-slate-50 hover:bg-white hover:border-slate-300 transition flex items-center justify-between gap-3 shadow-xs"
                  >
                    <div className="min-w-0 flex-1">
                      <p className="font-semibold text-xs text-slate-800 truncate">
                        {item.nomeMaterial}
                      </p>
                      <span className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">
                        Unidade: {item.unidade}
                      </span>
                    </div>

                    <input
                      type="number"
                      min="0"
                      value={quantidadesForm[item.materialId] ?? 0}
                      onChange={(e) => handleQteChange(item.materialId, e.target.value)}
                      onFocus={(e) => e.target.select()}
                      disabled={!emMontagem}
                      className="w-20 h-11 sm:h-auto px-3 py-1.5 bg-white border border-slate-300 rounded-lg text-base sm:text-sm font-bold text-slate-900 text-center focus:ring-2 focus:ring-[#1F6D2E] focus:border-transparent outline-none shadow-inner"
                    />
                  </div>
                ))}
              </div>
              )}
            </div>
          )}
        </>
      )}
    </div>
  );
}
