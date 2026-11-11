'use client';

import React, { useState, useEffect } from 'react';
import { fetchApi } from '@/lib/api';
import { Plus, Edit2, Check, X, Power, PowerOff, Building } from 'lucide-react';

interface Local {
  id: string;
  nome: string;
  tipo: string;
  ordemExibicao: number;
  ativo: boolean;
}

export default function SupervisorLocaisPage() {
  const [locais, setLocais] = useState<Local[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [novoNomeInput, setNovoNomeInput] = useState('');
  const [mostrarModalAdd, setMostrarModalAdd] = useState(false);
  const [addNome, setAddNome] = useState('');
  const [addTipo, setAddTipo] = useState<'Andar' | 'AreaComum'>('Andar');
  const [mensagem, setMensagem] = useState<{ tipo: 'sucesso' | 'erro'; texto: string } | null>(null);

  useEffect(() => {
    carregarLocais();
  }, []);

  const carregarLocais = async () => {
    setLoading(true);
    try {
      const data = await fetchApi<Local[]>('/api/locais');
      setLocais(data);
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: 'Erro ao carregar locais.' });
    } finally {
      setLoading(false);
    }
  };

  const salvarEdicaoNome = async (localId: string) => {
    if (!novoNomeInput.trim()) return;
    setMensagem(null);
    try {
      const updated = await fetchApi<Local>(`/api/locais/${localId}`, {
        method: 'PUT',
        body: JSON.stringify({ novoNome: novoNomeInput }),
      });
      setLocais((prev) => prev.map((l) => (l.id === localId ? updated : l)));
      setEditingId(null);
      setMensagem({ tipo: 'sucesso', texto: 'Nome do local atualizado com sucesso!' });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao atualizar nome.' });
    }
  };

  const inativarLocal = async (localId: string) => {
    setMensagem(null);
    try {
      await fetchApi(`/api/locais/${localId}/inativar`, { method: 'POST' });
      setLocais((prev) => prev.map((l) => (l.id === localId ? { ...l, ativo: false } : l)));
      setMensagem({ tipo: 'sucesso', texto: 'Local inativado com sucesso!' });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao inativar local.' });
    }
  };

  const reativarLocal = async (localId: string) => {
    setMensagem(null);
    try {
      await fetchApi(`/api/locais/${localId}/reativar`, { method: 'POST' });
      setLocais((prev) => prev.map((l) => (l.id === localId ? { ...l, ativo: true } : l)));
      setMensagem({ tipo: 'sucesso', texto: 'Local reativado com sucesso!' });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao reativar local.' });
    }
  };

  const criarLocal = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!addNome.trim()) return;
    setMensagem(null);

    const proximaOrdem = locais.length + 1;
    try {
      const novoLocal = await fetchApi<Local>('/api/locais', {
        method: 'POST',
        body: JSON.stringify({
          nome: addNome,
          tipo: addTipo,
          ordemExibicao: proximaOrdem,
        }),
      });
      setLocais((prev) => [...prev, novoLocal]);
      setAddNome('');
      setMostrarModalAdd(false);
      setMensagem({ tipo: 'sucesso', texto: 'Novo local adicionado com sucesso!' });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao adicionar local.' });
    }
  };

  return (
    <div className="space-y-6">
      {/* Top Header */}
      <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-200 flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <Building className="w-6 h-6 text-[#1F6D2E]" />
            Gerenciamento de Locais
          </h2>
          <p className="text-sm text-slate-500 mt-1">
            Cadastre novos locais, renomeie ou inative/reative locais para as próximas semanas.
          </p>
        </div>

        <button
          onClick={() => setMostrarModalAdd(true)}
          className="px-4 py-2.5 bg-[#1F6D2E] text-white rounded-xl font-semibold text-sm hover:bg-[#175423] transition flex items-center gap-2 shadow"
        >
          <Plus className="w-4 h-4" />
          Adicionar Local
        </button>
      </div>

      {/* Mensagem de Feedback */}
      {mensagem && (
        <div
          className={`p-4 rounded-xl text-sm font-medium ${
            mensagem.tipo === 'sucesso'
              ? 'bg-emerald-50 text-emerald-800 border border-emerald-200'
              : 'bg-rose-50 text-rose-800 border border-rose-200'
          }`}
        >
          {mensagem.texto}
        </div>
      )}

      {/* Tabela / Lista de Locais */}
      <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-slate-700">
            <thead className="bg-slate-50 text-xs font-bold text-slate-500 uppercase border-b border-slate-200">
              <tr>
                <th className="px-6 py-4">Ordem</th>
                <th className="px-6 py-4">Nome do Local</th>
                <th className="px-6 py-4">Tipo</th>
                <th className="px-6 py-4">Status</th>
                <th className="px-6 py-4 text-right">Ações</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 font-medium">
              {locais.map((local) => (
                <tr key={local.id} className="hover:bg-slate-50/80 transition">
                  <td className="px-6 py-4 text-slate-400 font-semibold">{local.ordemExibicao}</td>
                  <td className="px-6 py-4 font-bold text-slate-900">
                    {editingId === local.id ? (
                      <div className="flex items-center gap-2">
                        <input
                          type="text"
                          value={novoNomeInput}
                          onChange={(e) => setNovoNomeInput(e.target.value)}
                          className="px-3 py-1 bg-white border border-slate-300 rounded-lg text-sm font-bold text-slate-900 outline-none focus:ring-2 focus:ring-[#1F6D2E]"
                        />
                        <button
                          onClick={() => salvarEdicaoNome(local.id)}
                          className="p-1.5 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700"
                          title="Salvar"
                        >
                          <Check className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => setEditingId(null)}
                          className="p-1.5 bg-slate-200 text-slate-700 rounded-lg hover:bg-slate-300"
                          title="Cancelar"
                        >
                          <X className="w-4 h-4" />
                        </button>
                      </div>
                    ) : (
                      <span className="flex items-center gap-2">
                        {local.nome}
                        <button
                          onClick={() => {
                            setEditingId(local.id);
                            setNovoNomeInput(local.nome);
                          }}
                          className="text-slate-400 hover:text-[#1F6D2E] p-1 rounded"
                          title="Renomear inline"
                        >
                          <Edit2 className="w-3.5 h-3.5" />
                        </button>
                      </span>
                    )}
                  </td>
                  <td className="px-6 py-4">
                    <span className="bg-slate-100 text-slate-700 text-xs px-2.5 py-1 rounded-md font-semibold">
                      {local.tipo}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <span
                      className={`text-xs font-bold px-2.5 py-0.5 rounded-full ${
                        local.ativo
                          ? 'bg-emerald-100 text-emerald-800'
                          : 'bg-slate-200 text-slate-600'
                      }`}
                    >
                      {local.ativo ? 'Ativo' : 'Inativo'}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-right">
                    {local.ativo ? (
                      <button
                        onClick={() => inativarLocal(local.id)}
                        className="px-3 py-1.5 bg-rose-50 text-rose-700 hover:bg-rose-100 rounded-lg font-semibold text-xs transition flex items-center gap-1 ml-auto"
                      >
                        <PowerOff className="w-3.5 h-3.5" />
                        Inativar
                      </button>
                    ) : (
                      <button
                        onClick={() => reativarLocal(local.id)}
                        className="px-3 py-1.5 bg-emerald-50 text-emerald-700 hover:bg-emerald-100 rounded-lg font-semibold text-xs transition flex items-center gap-1 ml-auto"
                      >
                        <Power className="w-3.5 h-3.5" />
                        Reativar
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal Adicionar Local */}
      {mostrarModalAdd && (
        <div className="fixed inset-0 bg-slate-900/50 backdrop-blur-xs flex items-center justify-center p-4 z-50 animate-fadeIn">
          <div className="bg-white rounded-2xl p-6 max-w-md w-full shadow-2xl border border-slate-200">
            <h3 className="text-xl font-bold text-slate-900 mb-4">Adicionar Novo Local</h3>

            <form onSubmit={criarLocal} className="space-y-4">
              <div>
                <label className="block text-xs font-bold text-slate-600 uppercase mb-1">
                  Nome do Local
                </label>
                <input
                  type="text"
                  required
                  placeholder="Ex: Andar 17 ou COI3"
                  value={addNome}
                  onChange={(e) => setAddNome(e.target.value)}
                  className="w-full px-4 py-2.5 rounded-lg border border-slate-300 focus:ring-2 focus:ring-[#1F6D2E] outline-none text-sm font-semibold"
                />
              </div>

              <div>
                <label className="block text-xs font-bold text-slate-600 uppercase mb-1">
                  Tipo do Local
                </label>
                <select
                  value={addTipo}
                  onChange={(e: any) => setAddTipo(e.target.value)}
                  className="w-full px-4 py-2.5 rounded-lg border border-slate-300 focus:ring-2 focus:ring-[#1F6D2E] outline-none text-sm font-semibold bg-white"
                >
                  <option value="Andar">Andar</option>
                  <option value="AreaComum">Área Comum</option>
                </select>
              </div>

              <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setMostrarModalAdd(false)}
                  className="px-4 py-2 text-slate-600 font-semibold text-sm hover:bg-slate-100 rounded-lg transition"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  className="px-5 py-2 bg-[#1F6D2E] text-white font-semibold text-sm rounded-lg hover:bg-[#175423] transition shadow"
                >
                  Salvar Local
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
