'use client';

import React, { useState, useEffect } from 'react';
import { fetchApi } from '@/lib/api';
import { FileSpreadsheet, FileText, BarChart2 } from 'lucide-react';

interface LevantamentoItem {
  materialId: string;
  nomeMaterial: string;
  unidade: string;
  quantidadeTotalSeparada: number;
}

interface LevantamentoDto {
  semanaListaId: string;
  dataInicio: string;
  dataFim: string;
  itens: LevantamentoItem[];
}

export default function AlmoxarifeLevantamentoPage() {
  const [semanaId, setSemanaId] = useState<string | null>(null);
  const [levantamento, setLevantamento] = useState<LevantamentoDto | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    carregarSemanaEData();
  }, []);

  const carregarSemanaEData = async () => {
    setLoading(true);
    try {
      const semanaAtual = await fetchApi('/api/semanas/atual');
      if (semanaAtual?.id) {
        setSemanaId(semanaAtual.id);
        const data = await fetchApi<LevantamentoDto>(`/api/semanas/${semanaAtual.id}/levantamento`);
        setLevantamento(data);
      }
    } catch (err: any) {
      setLevantamento(null);
    } finally {
      setLoading(false);
    }
  };

  const exportarLevantamento = async (formato: 'excel' | 'pdf') => {
    if (!semanaId) return;
    try {
      const blob = await fetchApi(`/api/semanas/${semanaId}/levantamento/export?formato=${formato}`);
      const url = window.URL.createObjectURL(blob as Blob);
      const a = document.createElement('a');
      a.href = url;
      const ext = formato === 'excel' ? 'xlsx' : 'pdf';
      a.download = `Levantamento-Consolidado.${ext}`;
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
      {/* Top Header */}
      <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-200 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <BarChart2 className="w-6 h-6 text-[#1F6D2E]" />
            Levantamento Consolidado da Semana
          </h2>
          {levantamento ? (
            <p className="text-sm text-slate-600 font-medium mt-1">
              Período: <span className="font-semibold text-slate-900">{levantamento.dataInicio}</span> a{' '}
              <span className="font-semibold text-slate-900">{levantamento.dataFim}</span> (Soma de todos os 23 locais da semana)
            </p>
          ) : (
            <p className="text-sm text-slate-500 mt-1">Nenhum levantamento disponível no momento.</p>
          )}
        </div>

        {levantamento && (
          <div className="flex items-center gap-2">
            <button
              onClick={() => exportarLevantamento('excel')}
              className="px-4 py-2.5 bg-emerald-700 hover:bg-emerald-800 text-white rounded-xl font-semibold text-sm transition flex items-center gap-2 shadow"
            >
              <FileSpreadsheet className="w-4 h-4" />
              Exportar Excel
            </button>
            <button
              onClick={() => exportarLevantamento('pdf')}
              className="px-4 py-2.5 bg-rose-700 hover:bg-rose-800 text-white rounded-xl font-semibold text-sm transition flex items-center gap-2 shadow"
            >
              <FileText className="w-4 h-4" />
              Exportar PDF
            </button>
          </div>
        )}
      </div>

      {/* Tabela de Itens Agregados */}
      {levantamento && (
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-[#1F6D2E] text-white text-xs font-bold uppercase tracking-wider">
                <tr>
                  <th className="px-6 py-4">#</th>
                  <th className="px-6 py-4">Material</th>
                  <th className="px-6 py-4">Unidade</th>
                  <th className="px-6 py-4 text-right">Quantidade Total Separada</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 font-medium text-slate-800">
                {levantamento.itens.map((item, idx) => (
                  <tr key={item.materialId} className="hover:bg-slate-50 transition">
                    <td className="px-6 py-3.5 text-slate-400 font-semibold">{idx + 1}</td>
                    <td className="px-6 py-3.5 font-bold text-slate-900">{item.nomeMaterial}</td>
                    <td className="px-6 py-3.5 text-slate-500 font-semibold">{item.unidade}</td>
                    <td className="px-6 py-3.5 text-right font-extrabold text-[#1F6D2E] text-base">
                      {item.quantidadeTotalSeparada}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
