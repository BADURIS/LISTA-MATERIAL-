'use client';

import React, { useEffect, useState } from 'react';
import { fetchApi, listarTodos } from '@/lib/api';
import type { Local } from '@/lib/tipos';
import { Aviso, Cabecalho, Carregando, Mensagem, Protegido } from '@/components/ui';
import { Plus, Edit2, Check, X, Power, PowerOff, Building } from 'lucide-react';

const TIPOS: Record<string, string> = { Andar: 'Andar', AreaComum: 'Área comum' };

function GerenciarLocais() {
  const [locais, setLocais] = useState<Local[]>([]);
  const [loading, setLoading] = useState(true);
  const [editandoId, setEditandoId] = useState<string | null>(null);
  const [nomeEdicao, setNomeEdicao] = useState('');
  const [modalNovo, setModalNovo] = useState(false);
  const [novoNome, setNovoNome] = useState('');
  const [novoTipo, setNovoTipo] = useState<'Andar' | 'AreaComum'>('Andar');
  const [mensagem, setMensagem] = useState<Mensagem>(null);

  useEffect(() => {
    listarTodos<Local>('/api/locais')
      .then((l) => setLocais(l.sort((a, b) => a.ordemExibicao - b.ordemExibicao)))
      .catch((err) => setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao carregar locais.' }))
      .finally(() => setLoading(false));
  }, []);

  const atualizar = async (local: Local, mudancas: Partial<Local>, sucesso: string) => {
    setMensagem(null);
    try {
      const atualizado = await fetchApi<Local>(`/api/locais/${local.id}`, {
        method: 'PUT',
        body: JSON.stringify({ nome: local.nome, ativo: local.ativo, tipo: local.tipo, ordemExibicao: local.ordemExibicao, ...mudancas }),
      });
      setLocais((prev) => prev.map((l) => (l.id === local.id ? atualizado : l)));
      setEditandoId(null);
      setMensagem({ tipo: 'sucesso', texto: sucesso });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao atualizar o local.' });
    }
  };

  const criar = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!novoNome.trim()) return;
    setMensagem(null);
    try {
      const ordem = Math.max(0, ...locais.map((l) => l.ordemExibicao)) + 1;
      const novo = await fetchApi<Local>('/api/locais', {
        method: 'POST',
        body: JSON.stringify({ nome: novoNome.trim(), tipo: novoTipo, ordemExibicao: ordem }),
      });
      setLocais((prev) => [...prev, novo]);
      setNovoNome('');
      setModalNovo(false);
      setMensagem({ tipo: 'sucesso', texto: `Local "${novo.nome}" adicionado. Ele entra a partir da próxima semana criada.` });
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao adicionar o local.' });
    }
  };

  if (loading) return <Carregando />;

  return (
    <div className="space-y-6">
      <Cabecalho
        titulo={
          <>
            <Building className="w-6 h-6 text-[#1F6D2E]" /> Locais
          </>
        }
        subtitulo="Locais ativos entram automaticamente em cada nova semana. Inativar não apaga o histórico."
      >
        <button
          onClick={() => setModalNovo(true)}
          className="px-4 py-2.5 bg-[#1F6D2E] text-white rounded-xl font-semibold text-sm hover:bg-[#175423] transition flex items-center gap-2 shadow"
        >
          <Plus className="w-4 h-4" /> Adicionar local
        </button>
      </Cabecalho>

      <Aviso mensagem={mensagem} />

      <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-slate-700">
            <thead className="bg-slate-50 text-xs font-bold text-slate-500 uppercase border-b border-slate-200">
              <tr>
                <th className="px-4 sm:px-6 py-4">Ordem</th>
                <th className="px-4 sm:px-6 py-4">Local</th>
                <th className="px-4 sm:px-6 py-4">Tipo</th>
                <th className="px-4 sm:px-6 py-4">Situação</th>
                <th className="px-4 sm:px-6 py-4 text-right">Ações</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 font-medium">
              {locais.map((local) => (
                <tr key={local.id} className={`hover:bg-slate-50/80 transition ${local.ativo ? '' : 'text-slate-400'}`}>
                  <td className="px-4 sm:px-6 py-3 text-slate-400 font-semibold">{local.ordemExibicao}</td>
                  <td className="px-4 sm:px-6 py-3 font-bold text-slate-900">
                    {editandoId === local.id ? (
                      <div className="flex items-center gap-2">
                        <input
                          type="text"
                          value={nomeEdicao}
                          onChange={(e) => setNomeEdicao(e.target.value)}
                          className="px-3 py-1 bg-white border border-slate-300 rounded-lg text-sm font-bold outline-none focus:ring-2 focus:ring-[#1F6D2E]"
                          autoFocus
                        />
                        <button
                          onClick={() => nomeEdicao.trim() && atualizar(local, { nome: nomeEdicao.trim() }, 'Nome do local atualizado.')}
                          className="p-1.5 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700"
                          aria-label="Salvar nome"
                        >
                          <Check className="w-4 h-4" />
                        </button>
                        <button onClick={() => setEditandoId(null)} className="p-1.5 bg-slate-200 text-slate-700 rounded-lg hover:bg-slate-300" aria-label="Cancelar">
                          <X className="w-4 h-4" />
                        </button>
                      </div>
                    ) : (
                      <span className="flex items-center gap-2">
                        {local.nome}
                        <button
                          onClick={() => {
                            setEditandoId(local.id);
                            setNomeEdicao(local.nome);
                          }}
                          className="text-slate-400 hover:text-[#1F6D2E] p-1 rounded"
                          aria-label={`Renomear ${local.nome}`}
                        >
                          <Edit2 className="w-3.5 h-3.5" />
                        </button>
                      </span>
                    )}
                  </td>
                  <td className="px-4 sm:px-6 py-3">
                    <span className="bg-slate-100 text-slate-700 text-xs px-2.5 py-1 rounded-md font-semibold">{TIPOS[local.tipo] ?? local.tipo}</span>
                  </td>
                  <td className="px-4 sm:px-6 py-3">
                    <span className={`text-xs font-bold px-2.5 py-0.5 rounded-full ${local.ativo ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-200 text-slate-600'}`}>
                      {local.ativo ? 'Ativo' : 'Inativo'}
                    </span>
                  </td>
                  <td className="px-4 sm:px-6 py-3 text-right">
                    <button
                      onClick={() => atualizar(local, { ativo: !local.ativo }, local.ativo ? `"${local.nome}" inativado.` : `"${local.nome}" reativado.`)}
                      className={`px-3 py-1.5 rounded-lg font-semibold text-xs transition inline-flex items-center gap-1 ${
                        local.ativo ? 'bg-rose-50 text-rose-700 hover:bg-rose-100' : 'bg-emerald-50 text-emerald-700 hover:bg-emerald-100'
                      }`}
                    >
                      {local.ativo ? <PowerOff className="w-3.5 h-3.5" /> : <Power className="w-3.5 h-3.5" />}
                      {local.ativo ? 'Inativar' : 'Reativar'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {modalNovo && (
        <div className="fixed inset-0 bg-slate-900/50 flex items-center justify-center p-4 z-50" role="dialog" aria-modal="true">
          <div className="bg-white rounded-2xl p-6 max-w-md w-full shadow-2xl border border-slate-200">
            <h3 className="text-xl font-bold text-slate-900 mb-4">Adicionar local</h3>
            <form onSubmit={criar} className="space-y-4">
              <label className="block text-xs font-bold text-slate-600 uppercase">
                Nome
                <input
                  type="text"
                  required
                  placeholder="Ex.: Andar 17"
                  value={novoNome}
                  onChange={(e) => setNovoNome(e.target.value)}
                  className="mt-1 w-full px-4 py-2.5 rounded-lg border border-slate-300 focus:ring-2 focus:ring-[#1F6D2E] outline-none text-sm font-semibold normal-case"
                  autoFocus
                />
              </label>
              <label className="block text-xs font-bold text-slate-600 uppercase">
                Tipo
                <select
                  value={novoTipo}
                  onChange={(e) => setNovoTipo(e.target.value as 'Andar' | 'AreaComum')}
                  className="mt-1 w-full px-4 py-2.5 rounded-lg border border-slate-300 focus:ring-2 focus:ring-[#1F6D2E] outline-none text-sm font-semibold bg-white normal-case"
                >
                  <option value="Andar">Andar</option>
                  <option value="AreaComum">Área comum</option>
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

export default function SupervisorLocaisPage() {
  return (
    <Protegido perfis={['Supervisor']}>
      <GerenciarLocais />
    </Protegido>
  );
}
