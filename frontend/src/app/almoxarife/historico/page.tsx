'use client';

import React, { useState, useEffect } from 'react';
import { fetchApi } from '@/lib/api';
import { Calendar, FileSpreadsheet, FileText, ChevronRight } from 'lucide-react';

interface ListaLocal {
  id: string;
  nomeLocal: string;
  status: string;
  nomeRetiradoPor: string | null;
  dataRetirada: string | null;
}

interface Semana {
  id: string;
  dataInicio: string;
  dataFim: string;
  status: string;
  dataCriacao: string;
  listasLocais: ListaLocal[];
}

export default function AlmoxarifeHistoricoPage() {
  const [semanas, setSemanas] = useState<Semana[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedSemana, setSelectedSemana] = useState<Semana | null>(null);

  useEffect(() => {
    carregarHistorico();
  }, []);

  const carregarHistorico = async () => {
    setLoading(true);
    try {
      const data = await fetchApi<Semana[]>('/api/semanas/historico');
      setSemanas(data);
    } catch (err: any) {
      setSemanas([]);
    } finally {
      setLoading(false);
    }
  };

  const exportarLevantamentoSemana = async (semanaId: string, formato: 'excel' | 'pdf') => {
    try {
      const blob = await fetchApi(`/api/semanas/${semanaId}/levantamento/export?formato=${formato}`);
      const url = window.URL.createObjectURL(blob as Blob);
      const a = document.createElement('a');
      a.href = url;
      const ext = formato === 'excel' ? 'xlsx' : 'pdf';
      a.download = `Levantamento-Historico.${ext}`;
      document.body.appendChild(a);
      a.click();
      a.remove();
    } catch (err: any) {
      alert('Erro ao realizar exportação.');
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-[#1F6D2E]"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-200">
        <h2 className="text-2xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
          <Calendar className="w-6 h-6 text-[#1F6D2E]" />
          Histórico Completo de Semanas
        </h2>
        <p className="text-sm text-slate-500 mt-1">
          Nenhum registro é apagado. Acesse planilhas e relatórios de qualquer semana do passado.
        </p>
      </div>

      {/* Lista de Semanas Passadas */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="space-y-3 lg:col-span-1">
          <p className="text-xs font-bold text-slate-400 uppercase tracking-wider px-1">
            Semanas Registradas
          </p>
          {semanas.map((sem) => {
            const isSelected = selectedSemana?.id === sem.id;
            return (
              <button
                key={sem.id}
                onClick={() => setSelectedSemana(sem)}
                className={`w-full p-4 rounded-xl border text-left transition flex items-center justify-between ${
                  isSelected
                    ? 'border-[#1F6D2E] bg-emerald-50/50 ring-2 ring-[#1F6D2E]/20 shadow'
                    : 'border-slate-200 bg-white hover:border-slate-300'
                }`}
              >
                <div>
                  <h3 className="font-bold text-sm text-slate-900">
                    {sem.dataInicio} a {sem.dataFim}
                  </h3>
                  <span className="inline-block mt-1 text-[10px] font-bold px-2 py-0.5 rounded bg-emerald-100 text-emerald-800">
                    {sem.status}
                  </span>
                </div>
                <ChevronRight className="w-5 h-5 text-slate-400" />
              </button>
            );
          })}
        </div>

        {/* Painel de Detalhes da Semana Selecionada */}
        <div className="lg:col-span-2">
          {selectedSemana ? (
            <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm space-y-6">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-100 pb-4">
                <div>
                  <h3 className="text-xl font-bold text-slate-900">
                    Semana: {selectedSemana.dataInicio} a {selectedSemana.dataFim}
                  </h3>
                  <p className="text-xs text-slate-500 mt-0.5">
                    Total de {selectedSemana.listasLocais.length} locais auditados nesta semana.
                  </p>
                </div>

                <div className="flex items-center gap-2">
                  <button
                    onClick={() => exportarLevantamentoSemana(selectedSemana.id, 'excel')}
                    className="px-3 py-2 bg-emerald-100 text-emerald-800 hover:bg-emerald-200 rounded-lg text-xs font-bold transition flex items-center gap-1.5"
                  >
                    <FileSpreadsheet className="w-4 h-4 text-emerald-700" />
                    Consolidado Excel
                  </button>
                  <button
                    onClick={() => exportarLevantamentoSemana(selectedSemana.id, 'pdf')}
                    className="px-3 py-2 bg-rose-100 text-rose-800 hover:bg-rose-200 rounded-lg text-xs font-bold transition flex items-center gap-1.5"
                  >
                    <FileText className="w-4 h-4 text-rose-700" />
                    Consolidado PDF
                  </button>
                </div>
              </div>

              {/* Lista de Locais da Semana Selecionada com Audit Trail de Retirada */}
              <div className="divide-y divide-slate-100">
                {selectedSemana.listasLocais.map((loc) => (
                  <div key={loc.id} className="py-3 flex items-center justify-between text-sm">
                    <div>
                      <p className="font-bold text-slate-900">{loc.nomeLocal}</p>
                      {loc.nomeRetiradoPor ? (
                        <p className="text-xs text-slate-500">
                          Retirado por: <span className="font-semibold text-slate-700">{loc.nomeRetiradoPor}</span> em{' '}
                          {new Date(loc.dataRetirada!).toLocaleString('pt-BR')}
                        </p>
                      ) : (
                        <p className="text-xs text-amber-600 font-medium">Sem registro de retirada</p>
                      )}
                    </div>

                    <span className="text-xs font-bold px-2.5 py-1 rounded bg-slate-100 text-slate-700">
                      {loc.status}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <div className="bg-slate-50 border-2 border-dashed border-slate-200 rounded-2xl p-12 text-center text-slate-400 font-medium text-sm">
              Selecione uma semana da lista para visualizar os detalhes históricos.
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
