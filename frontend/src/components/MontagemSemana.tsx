'use client';

import React, { useEffect, useMemo, useRef, useState } from 'react';
import { fetchApi, fetchApiOuNulo, listarTodos } from '@/lib/api';
import { formatarData, segundaFeira, somarDias, STATUS_SEMANA } from '@/lib/formatacao';
import type { ListaLocal, Material, Semana } from '@/lib/tipos';
import { Aviso, Cabecalho, Carregando, Mensagem, Vazio } from '@/components/ui';
import { Plus, Send, Save, ChevronDown, ChevronUp, Search, CalendarDays } from 'lucide-react';

// Supervisor: cria a semana, faz os pedidos e envia ao almoxarifado.
// Coordenador: só ajuda a preencher os pedidos dos locais.
export default function MontagemSemana({ podeGerenciar }: { podeGerenciar: boolean }) {
  const [semana, setSemana] = useState<Semana | null>(null);
  const [materiais, setMateriais] = useState<Material[]>([]);
  const [localSelecionadoId, setLocalSelecionadoId] = useState<string | null>(null);
  const [form, setForm] = useState<Record<string, string>>({});
  const [alterado, setAlterado] = useState(false);
  const [filtro, setFiltro] = useState('');
  const [dataInicio, setDataInicio] = useState(() => segundaFeira());
  const [dataFim, setDataFim] = useState(() => somarDias(segundaFeira(), 4));
  const [loading, setLoading] = useState(true);
  const [ocupado, setOcupado] = useState(false);
  const [mensagem, setMensagem] = useState<Mensagem>(null);
  const painelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    (async () => {
      try {
        const [s, m] = await Promise.all([
          fetchApiOuNulo<Semana>('/api/semanas-lista/atual'),
          listarTodos<Material>('/api/materiais/ativos'),
        ]);
        setSemana(s);
        setMateriais(m.sort((a, b) => a.ordemExibicao - b.ordemExibicao));
      } catch (err: any) {
        setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao carregar a semana.' });
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  // No celular o painel fica abaixo da grade de locais: rola até ele ao abrir um local
  useEffect(() => {
    if (localSelecionadoId) painelRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }, [localSelecionadoId]);

  // Avisa antes de fechar a aba com pedido não salvo
  useEffect(() => {
    if (!alterado) return;
    const aviso = (e: BeforeUnloadEvent) => e.preventDefault();
    window.addEventListener('beforeunload', aviso);
    return () => window.removeEventListener('beforeunload', aviso);
  }, [alterado]);

  const local = semana?.locais.find((l) => l.localId === localSelecionadoId) ?? null;
  const emMontagem = semana?.status === 'EmMontagem';
  const preenchido = (l: ListaLocal) => l.semPedido || l.itens.length > 0;
  const totalPreenchidos = semana?.locais.filter(preenchido).length ?? 0;
  const totalLocais = semana?.locais.length ?? 0;
  const pendentes = semana?.locais.filter((l) => !preenchido(l)) ?? [];

  const materiaisFiltrados = useMemo(() => {
    const termo = filtro.trim().toLowerCase();
    return termo ? materiais.filter((m) => m.nome.toLowerCase().includes(termo)) : materiais;
  }, [materiais, filtro]);

  const montarForm = (l: ListaLocal) => {
    const mapa: Record<string, string> = {};
    l.itens.forEach((i) => (mapa[i.materialId] = String(i.quantidadeSolicitada)));
    setForm(mapa);
    setAlterado(false);
  };

  const selecionarLocal = (l: ListaLocal) => {
    if (alterado && !window.confirm('Há quantidades não salvas neste local. Descartar as alterações?')) return;
    setMensagem(null);
    setFiltro('');
    if (localSelecionadoId === l.localId) {
      setLocalSelecionadoId(null);
      setAlterado(false);
      return;
    }
    setLocalSelecionadoId(l.localId);
    montarForm(l);
  };

  const alterarQuantidade = (materialId: string, valor: string) => {
    setForm((prev) => ({ ...prev, [materialId]: valor.replace(/\D/g, '').slice(0, 4) }));
    setAlterado(true);
  };

  const criarSemana = async () => {
    setOcupado(true);
    setMensagem(null);
    try {
      const nova = await fetchApi<Semana>('/api/semanas-lista', {
        method: 'POST',
        body: JSON.stringify({ dataInicio, dataFim }),
      });
      setSemana(nova);
      setMensagem({ tipo: 'sucesso', texto: `Semana criada com ${nova.locais.length} locais. Agora faça o pedido de cada local.` });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao criar a semana.' });
    } finally {
      setOcupado(false);
    }
  };

  const salvarLocal = async () => {
    if (!semana || !local) return;
    const itens = materiais.map((m) => ({ materialId: m.id, quantidadeSolicitada: Number(form[m.id] || 0) }));
    if (!itens.some((i) => i.quantidadeSolicitada > 0)) {
      setMensagem({ tipo: 'erro', texto: 'Informe ao menos uma quantidade, ou marque o local como "sem pedido nesta semana".' });
      return;
    }
    setOcupado(true);
    setMensagem(null);
    try {
      const atualizada = await fetchApi<Semana>(`/api/semanas-lista/${semana.id}/locais/${local.localId}/itens`, {
        method: 'PUT',
        body: JSON.stringify({ itens }),
      });
      setSemana(atualizada);
      const salvo = atualizada.locais.find((l) => l.localId === local.localId)!;
      montarForm(salvo);
      setMensagem({ tipo: 'sucesso', texto: `Pedido de ${local.nomeLocal} salvo (${salvo.itens.length} materiais).` });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao salvar o pedido.' });
    } finally {
      setOcupado(false);
    }
  };

  const definirSemPedido = async (semPedido: boolean) => {
    if (!semana || !local) return;
    if (semPedido && !window.confirm(`Marcar "${local.nomeLocal}" como sem pedido nesta semana? As quantidades deste local serão zeradas.`)) return;
    setOcupado(true);
    setMensagem(null);
    try {
      const atualizada = await fetchApi<Semana>(`/api/semanas-lista/${semana.id}/locais/${local.localId}/sem-pedido`, {
        method: 'PUT',
        body: JSON.stringify({ semPedido }),
      });
      setSemana(atualizada);
      montarForm(atualizada.locais.find((l) => l.localId === local.localId)!);
      setMensagem({
        tipo: 'sucesso',
        texto: semPedido ? `${local.nomeLocal} marcado como sem pedido nesta semana.` : `${local.nomeLocal} voltou a aguardar pedido.`,
      });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao atualizar o local.' });
    } finally {
      setOcupado(false);
    }
  };

  const enviarParaSeparacao = async () => {
    if (!semana) return;
    if (pendentes.length > 0) {
      setMensagem({ tipo: 'erro', texto: `Faça o pedido de todos os locais antes de enviar. Pendentes: ${pendentes.map((l) => l.nomeLocal).join(', ')}.` });
      return;
    }
    if (!window.confirm(`Todos os ${totalLocais} locais estão prontos.\n\nDepois do envio ao almoxarifado os pedidos não podem mais ser alterados. Enviar?`)) return;
    setOcupado(true);
    setMensagem(null);
    try {
      setSemana(await fetchApi<Semana>(`/api/semanas-lista/${semana.id}/enviar-separacao`, { method: 'POST' }));
      setLocalSelecionadoId(null);
      setMensagem({ tipo: 'sucesso', texto: 'Semana enviada ao almoxarifado. O almoxarife foi notificado.' });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao enviar para separação.' });
    } finally {
      setOcupado(false);
    }
  };

  if (loading) return <Carregando />;

  return (
    <div className="space-y-6">
      <Cabecalho
        titulo={podeGerenciar ? 'Montagem da Lista Semanal' : 'Pedidos dos Locais'}
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
      >
        {podeGerenciar && emMontagem && (
          <button
            onClick={enviarParaSeparacao}
            disabled={ocupado || pendentes.length > 0}
            className="px-4 py-2.5 rounded-xl font-semibold text-sm transition flex items-center gap-2 shadow-sm bg-emerald-600 text-white hover:bg-emerald-700 disabled:bg-slate-200 disabled:text-slate-400 disabled:cursor-not-allowed"
            title={pendentes.length > 0 ? `Faltam ${pendentes.length} locais` : 'Enviar ao almoxarifado'}
          >
            <Send className="w-4 h-4" />
            Enviar para Separação
          </button>
        )}
      </Cabecalho>

      <Aviso mensagem={mensagem} />

      {!semana && podeGerenciar && (
        <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm space-y-4 max-w-xl">
          <h3 className="font-bold text-slate-900 flex items-center gap-2">
            <CalendarDays className="w-5 h-5 text-[#1F6D2E]" /> Criar nova semana
          </h3>
          <p className="text-sm text-slate-500">A semana já é criada com todos os locais ativos, prontos para receber o pedido.</p>
          <div className="grid grid-cols-2 gap-3">
            <label className="text-xs font-semibold text-slate-700">
              Início
              <input
                type="date"
                value={dataInicio}
                onChange={(e) => {
                  setDataInicio(e.target.value);
                  if (e.target.value > dataFim) setDataFim(somarDias(e.target.value, 4));
                }}
                className="mt-1 w-full px-3 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm font-semibold outline-none focus:ring-2 focus:ring-[#1F6D2E]"
              />
            </label>
            <label className="text-xs font-semibold text-slate-700">
              Fim
              <input
                type="date"
                value={dataFim}
                min={dataInicio}
                onChange={(e) => setDataFim(e.target.value)}
                className="mt-1 w-full px-3 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm font-semibold outline-none focus:ring-2 focus:ring-[#1F6D2E]"
              />
            </label>
          </div>
          <button
            onClick={criarSemana}
            disabled={ocupado || !dataInicio || !dataFim}
            className="px-4 py-2.5 bg-[#1F6D2E] text-white rounded-xl font-semibold text-sm hover:bg-[#175423] disabled:opacity-60 transition flex items-center gap-2 shadow-sm"
          >
            <Plus className="w-4 h-4" />
            {ocupado ? 'Criando...' : 'Criar semana'}
          </button>
        </div>
      )}

      {!semana && !podeGerenciar && <Vazio>Nenhuma semana em montagem. O supervisor precisa criar a semana.</Vazio>}

      {semana && (
        <>
          <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex flex-col sm:flex-row sm:items-center justify-between gap-3">
            <p className="text-sm font-bold text-slate-700">
              Locais prontos: <span className="font-extrabold text-[#1F6D2E]">{totalPreenchidos} de {totalLocais}</span>
            </p>
            <div className="w-full sm:w-64 bg-slate-100 rounded-full h-3 overflow-hidden border border-slate-200">
              <div
                className="bg-[#1F6D2E] h-full transition-all duration-300 rounded-full"
                style={{ width: `${totalLocais > 0 ? (totalPreenchidos / totalLocais) * 100 : 0}%` }}
              />
            </div>
          </div>

          {emMontagem && pendentes.length > 0 && (
            <div className="p-4 bg-amber-50 border border-amber-200 rounded-xl text-sm text-amber-900">
              <span className="font-bold">A lista só pode ser enviada quando todos os locais tiverem pedido (ou estiverem marcados como sem pedido).</span>{' '}
              Faltam {pendentes.length}: {pendentes.map((l) => l.nomeLocal).join(', ')}.
            </div>
          )}

          {!emMontagem && (
            <div className="p-4 bg-slate-100 border border-slate-200 rounded-xl text-sm text-slate-700 font-medium">
              Esta semana já foi enviada ao almoxarifado. Os pedidos estão travados e podem apenas ser consultados.
            </div>
          )}

          <div className="grid gap-3" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))' }}>
            {semana.locais.map((l) => {
              const ok = preenchido(l);
              const selecionado = localSelecionadoId === l.localId;
              return (
                <button
                  key={l.id}
                  onClick={() => selecionarLocal(l)}
                  className={`p-3.5 rounded-xl border text-left transition-all duration-200 flex flex-col justify-between h-24 ${
                    selecionado
                      ? 'border-[#1F6D2E] ring-2 ring-[#1F6D2E]/30 bg-emerald-50/40 shadow-md'
                      : 'border-slate-200 bg-white hover:border-slate-300 hover:shadow-sm'
                  }`}
                >
                  <span className="font-bold text-sm text-slate-900 truncate">{l.nomeLocal}</span>
                  <div className="flex items-center justify-between w-full mt-2">
                    <span
                      className={`text-[11px] font-bold px-2.5 py-0.5 rounded-full ${
                        l.semPedido ? 'bg-slate-200 text-slate-700' : ok ? 'bg-[#C0DD97] text-[#173404]' : 'bg-[#FAC775] text-[#412402]'
                      }`}
                    >
                      {l.semPedido ? 'Sem pedido' : ok ? `${l.itens.length} itens` : 'Pendente'}
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
                  <h3 className="text-xl font-bold text-slate-900">
                    Pedido: <span className="text-[#1F6D2E]">{local.nomeLocal}</span>
                  </h3>
                  <p className="text-xs text-slate-500 mt-0.5">
                    {emMontagem ? 'Use TAB para passar de um campo ao outro. Deixe em branco o que não for pedido.' : 'Somente consulta.'}
                  </p>
                </div>
                {emMontagem && (
                  <div className="flex flex-wrap items-center gap-2">
                    <button
                      onClick={() => definirSemPedido(!local.semPedido)}
                      disabled={ocupado}
                      className="px-4 py-2.5 bg-white border border-slate-300 text-slate-700 rounded-xl font-semibold text-sm hover:bg-slate-100 transition"
                    >
                      {local.semPedido ? 'Desfazer "sem pedido"' : 'Sem pedido nesta semana'}
                    </button>
                    {!local.semPedido && (
                      <button
                        onClick={salvarLocal}
                        disabled={ocupado}
                        className="px-5 py-2.5 bg-[#1F6D2E] text-white rounded-xl font-semibold text-sm hover:bg-[#175423] disabled:opacity-60 transition flex items-center gap-2 shadow"
                      >
                        <Save className="w-4 h-4" />
                        {ocupado ? 'Salvando...' : alterado ? 'Salvar pedido *' : 'Salvar pedido'}
                      </button>
                    )}
                  </div>
                )}
              </div>

              {local.semPedido ? (
                <div className="p-4 bg-slate-100 border border-slate-200 rounded-xl text-sm text-slate-700 font-medium">
                  Este local está <span className="font-bold">sem pedido nesta semana</span>: conta como pronto para o envio e não entra na separação.
                </div>
              ) : (
                <>
                  <div className="relative max-w-sm">
                    <Search className="w-4 h-4 text-slate-400 absolute left-3 top-3" />
                    <input
                      type="search"
                      value={filtro}
                      onChange={(e) => setFiltro(e.target.value)}
                      placeholder="Buscar material..."
                      className="w-full pl-9 pr-3 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm outline-none focus:ring-2 focus:ring-[#1F6D2E]"
                    />
                  </div>
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                    {materiaisFiltrados.map((m) => {
                      const valor = form[m.id] ?? '';
                      const pedido = Number(valor || 0) > 0;
                      return (
                        <label
                          key={m.id}
                          className={`p-3 rounded-xl border flex items-center justify-between gap-3 transition ${
                            pedido ? 'bg-emerald-50/60 border-emerald-300' : 'bg-slate-50 border-slate-200'
                          }`}
                        >
                          <span className="min-w-0 flex-1">
                            <span className="block font-semibold text-xs text-slate-800 truncate" title={m.nome}>
                              {m.nome}
                            </span>
                            <span className="text-[10px] font-bold text-slate-400 uppercase tracking-wider">{m.unidade}</span>
                          </span>
                          <input
                            type="text"
                            inputMode="numeric"
                            placeholder="0"
                            value={valor}
                            onChange={(e) => alterarQuantidade(m.id, e.target.value)}
                            onFocus={(e) => e.target.select()}
                            disabled={!emMontagem}
                            aria-label={`Quantidade de ${m.nome}`}
                            className="w-20 h-11 sm:h-9 px-3 bg-white border border-slate-300 rounded-lg text-base sm:text-sm font-bold text-slate-900 text-center focus:ring-2 focus:ring-[#1F6D2E] outline-none disabled:bg-slate-100"
                          />
                        </label>
                      );
                    })}
                  </div>
                  {materiaisFiltrados.length === 0 && <p className="text-sm text-slate-400">Nenhum material encontrado.</p>}
                </>
              )}
            </div>
          )}
        </>
      )}
    </div>
  );
}
