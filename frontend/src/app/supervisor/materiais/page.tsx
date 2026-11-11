'use client';

import React, { useEffect, useMemo, useState } from 'react';
import { fetchApi, listarTodos } from '@/lib/api';
import type { Material } from '@/lib/tipos';
import { Aviso, Cabecalho, Carregando, Mensagem, Protegido } from '@/components/ui';
import { Plus, Edit2, Check, X, Power, PowerOff, Boxes, Search } from 'lucide-react';

const UNIDADES = ['UN', 'CX', 'PCT'];

function GerenciarMateriais() {
  const [materiais, setMateriais] = useState<Material[]>([]);
  const [loading, setLoading] = useState(true);
  const [filtro, setFiltro] = useState('');
  const [editandoId, setEditandoId] = useState<string | null>(null);
  const [edicao, setEdicao] = useState({ nome: '', unidade: 'UN' });
  const [modalNovo, setModalNovo] = useState(false);
  const [novo, setNovo] = useState({ nome: '', unidade: 'UN' });
  const [mensagem, setMensagem] = useState<Mensagem>(null);

  useEffect(() => {
    listarTodos<Material>('/api/materiais')
      .then((m) => setMateriais(m.sort((a, b) => a.ordemExibicao - b.ordemExibicao)))
      .catch((err) => setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao carregar materiais.' }))
      .finally(() => setLoading(false));
  }, []);

  const visiveis = useMemo(() => {
    const termo = filtro.trim().toLowerCase();
    return termo ? materiais.filter((m) => m.nome.toLowerCase().includes(termo)) : materiais;
  }, [materiais, filtro]);

  const atualizar = async (m: Material, mudancas: Partial<Material>, sucesso: string) => {
    setMensagem(null);
    try {
      const atualizado = await fetchApi<Material>(`/api/materiais/${m.id}`, {
        method: 'PUT',
        body: JSON.stringify({ nome: m.nome, unidade: m.unidade, ordemExibicao: m.ordemExibicao, ativo: m.ativo, ...mudancas }),
      });
      setMateriais((prev) => prev.map((x) => (x.id === m.id ? atualizado : x)));
      setEditandoId(null);
      setMensagem({ tipo: 'sucesso', texto: sucesso });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao atualizar o material.' });
    }
  };

  const criar = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!novo.nome.trim()) return;
    setMensagem(null);
    try {
      const ordem = Math.max(0, ...materiais.map((m) => m.ordemExibicao)) + 1;
      const criado = await fetchApi<Material>('/api/materiais', {
        method: 'POST',
        body: JSON.stringify({ nome: novo.nome.trim(), unidade: novo.unidade, ordemExibicao: ordem }),
      });
      setMateriais((prev) => [...prev, criado]);
      setNovo({ nome: '', unidade: 'UN' });
      setModalNovo(false);
      setMensagem({ tipo: 'sucesso', texto: `Material "${criado.nome}" adicionado.` });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao adicionar o material.' });
    }
  };

  if (loading) return <Carregando />;

  return (
    <div className="space-y-6">
      <Cabecalho
        titulo={
          <>
            <Boxes className="w-6 h-6 text-[#1F6D2E]" /> Materiais
          </>
        }
        subtitulo={`${materiais.filter((m) => m.ativo).length} materiais ativos aparecem na tela de pedidos. Inativar não apaga o histórico.`}
      >
        <button
          onClick={() => setModalNovo(true)}
          className="px-4 py-2.5 bg-[#1F6D2E] text-white rounded-xl font-semibold text-sm hover:bg-[#175423] transition flex items-center gap-2 shadow"
        >
          <Plus className="w-4 h-4" /> Adicionar material
        </button>
      </Cabecalho>

      <Aviso mensagem={mensagem} />

      <div className="relative max-w-sm">
        <Search className="w-4 h-4 text-slate-400 absolute left-3 top-3" />
        <input
          type="search"
          value={filtro}
          onChange={(e) => setFiltro(e.target.value)}
          placeholder="Buscar material..."
          className="w-full pl-9 pr-3 py-2 bg-white border border-slate-300 rounded-lg text-sm outline-none focus:ring-2 focus:ring-[#1F6D2E]"
        />
      </div>

      <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-slate-700">
            <thead className="bg-slate-50 text-xs font-bold text-slate-500 uppercase border-b border-slate-200">
              <tr>
                <th className="px-4 sm:px-6 py-4">Material</th>
                <th className="px-4 sm:px-6 py-4">Unidade</th>
                <th className="px-4 sm:px-6 py-4">Situação</th>
                <th className="px-4 sm:px-6 py-4 text-right">Ações</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 font-medium">
              {visiveis.map((m) => (
                <tr key={m.id} className={`hover:bg-slate-50/80 transition ${m.ativo ? '' : 'text-slate-400'}`}>
                  {editandoId === m.id ? (
                    <>
                      <td className="px-4 sm:px-6 py-3">
                        <input
                          type="text"
                          value={edicao.nome}
                          onChange={(e) => setEdicao({ ...edicao, nome: e.target.value })}
                          className="w-full px-3 py-1 bg-white border border-slate-300 rounded-lg text-sm font-bold outline-none focus:ring-2 focus:ring-[#1F6D2E]"
                          autoFocus
                        />
                      </td>
                      <td className="px-4 sm:px-6 py-3">
                        <select
                          value={edicao.unidade}
                          onChange={(e) => setEdicao({ ...edicao, unidade: e.target.value })}
                          className="px-2 py-1 bg-white border border-slate-300 rounded-lg text-sm font-semibold"
                        >
                          {UNIDADES.map((u) => (
                            <option key={u}>{u}</option>
                          ))}
                        </select>
                      </td>
                      <td />
                      <td className="px-4 sm:px-6 py-3 text-right whitespace-nowrap">
                        <button
                          onClick={() => edicao.nome.trim() && atualizar(m, { nome: edicao.nome.trim(), unidade: edicao.unidade }, 'Material atualizado.')}
                          className="p-1.5 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 mr-2"
                          aria-label="Salvar"
                        >
                          <Check className="w-4 h-4" />
                        </button>
                        <button onClick={() => setEditandoId(null)} className="p-1.5 bg-slate-200 text-slate-700 rounded-lg hover:bg-slate-300" aria-label="Cancelar">
                          <X className="w-4 h-4" />
                        </button>
                      </td>
                    </>
                  ) : (
                    <>
                      <td className="px-4 sm:px-6 py-3 font-bold text-slate-900">
                        <span className="flex items-center gap-2">
                          {m.nome}
                          <button
                            onClick={() => {
                              setEditandoId(m.id);
                              setEdicao({ nome: m.nome, unidade: m.unidade });
                            }}
                            className="text-slate-400 hover:text-[#1F6D2E] p-1 rounded"
                            aria-label={`Editar ${m.nome}`}
                          >
                            <Edit2 className="w-3.5 h-3.5" />
                          </button>
                        </span>
                      </td>
                      <td className="px-4 sm:px-6 py-3">{m.unidade}</td>
                      <td className="px-4 sm:px-6 py-3">
                        <span className={`text-xs font-bold px-2.5 py-0.5 rounded-full ${m.ativo ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-200 text-slate-600'}`}>
                          {m.ativo ? 'Ativo' : 'Inativo'}
                        </span>
                      </td>
                      <td className="px-4 sm:px-6 py-3 text-right">
                        <button
                          onClick={() => atualizar(m, { ativo: !m.ativo }, m.ativo ? `"${m.nome}" inativado.` : `"${m.nome}" reativado.`)}
                          className={`px-3 py-1.5 rounded-lg font-semibold text-xs transition inline-flex items-center gap-1 ${
                            m.ativo ? 'bg-rose-50 text-rose-700 hover:bg-rose-100' : 'bg-emerald-50 text-emerald-700 hover:bg-emerald-100'
                          }`}
                        >
                          {m.ativo ? <PowerOff className="w-3.5 h-3.5" /> : <Power className="w-3.5 h-3.5" />}
                          {m.ativo ? 'Inativar' : 'Reativar'}
                        </button>
                      </td>
                    </>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {modalNovo && (
        <div className="fixed inset-0 bg-slate-900/50 flex items-center justify-center p-4 z-50" role="dialog" aria-modal="true">
          <div className="bg-white rounded-2xl p-6 max-w-md w-full shadow-2xl border border-slate-200">
            <h3 className="text-xl font-bold text-slate-900 mb-4">Adicionar material</h3>
            <form onSubmit={criar} className="space-y-4">
              <label className="block text-xs font-bold text-slate-600 uppercase">
                Nome
                <input
                  type="text"
                  required
                  value={novo.nome}
                  onChange={(e) => setNovo({ ...novo, nome: e.target.value })}
                  className="mt-1 w-full px-4 py-2.5 rounded-lg border border-slate-300 focus:ring-2 focus:ring-[#1F6D2E] outline-none text-sm font-semibold normal-case"
                  autoFocus
                />
              </label>
              <label className="block text-xs font-bold text-slate-600 uppercase">
                Unidade
                <select
                  value={novo.unidade}
                  onChange={(e) => setNovo({ ...novo, unidade: e.target.value })}
                  className="mt-1 w-full px-4 py-2.5 rounded-lg border border-slate-300 focus:ring-2 focus:ring-[#1F6D2E] outline-none text-sm font-semibold bg-white"
                >
                  {UNIDADES.map((u) => (
                    <option key={u}>{u}</option>
                  ))}
                </select>
              </label>
              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
                <button type="button" onClick={() => setModalNovo(false)} className="px-4 py-2 text-slate-600 font-semibold text-sm hover:bg-slate-100 rounded-lg">
                  Cancelar
                </button>
                <button type="submit" className="px-5 py-2 bg-[#1F6D2E] text-white font-semibold text-sm rounded-lg hover:bg-[#175423] shadow">
                  Salvar
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

export default function SupervisorMateriaisPage() {
  return (
    <Protegido perfis={['Supervisor']}>
      <GerenciarMateriais />
    </Protegido>
  );
}
