'use client';

import React, { useEffect, useState } from 'react';
import { baixarArquivo, fetchApi } from '@/lib/api';
import { formatarData, STATUS_SEMANA } from '@/lib/formatacao';
import type { SemanaResumo } from '@/lib/tipos';
import { Aviso, Cabecalho, Carregando, Mensagem, Protegido, Vazio } from '@/components/ui';
import { FileSpreadsheet, FileText, BarChart2 } from 'lucide-react';

interface LinhaResumo {
  material: string;
  unidade: string;
  saida: number;
}

// Total separado por material na semana, somando todos os locais (planilha MATERIAL | UNIDADE | SAÍDA)
function Levantamento() {
  const [semanas, setSemanas] = useState<SemanaResumo[]>([]);
  const [semanaId, setSemanaId] = useState('');
  const [linhas, setLinhas] = useState<LinhaResumo[]>([]);
  const [mostrarZerados, setMostrarZerados] = useState(false);
  const [loading, setLoading] = useState(true);
  const [mensagem, setMensagem] = useState<Mensagem>(null);

  useEffect(() => {
    (async () => {
      try {
        const lista = (await fetchApi<SemanaResumo[]>('/api/semanas-lista/historico')).filter((s) => s.status !== 'EmMontagem');
        setSemanas(lista);
        if (lista.length > 0) setSemanaId(lista[0].id);
        else setLoading(false);
      } catch (err: any) {
        setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao carregar as semanas.' });
        setLoading(false);
      }
    })();
  }, []);

  useEffect(() => {
    if (!semanaId) return;
    setLoading(true);
    fetchApi<LinhaResumo[]>(`/api/semanas-lista/${semanaId}/resumo`)
      .then(setLinhas)
      .catch((err) => setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao carregar o levantamento.' }))
      .finally(() => setLoading(false));
  }, [semanaId]);

  const exportar = async (formato: 'planilha' | 'pdf') => {
    try {
      await baixarArquivo(`/api/semanas-lista/${semanaId}/${formato}`, formato === 'pdf' ? 'levantamento.pdf' : 'levantamento.xlsx');
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao exportar.' });
    }
  };

  const semana = semanas.find((s) => s.id === semanaId);
  const visiveis = mostrarZerados ? linhas : linhas.filter((l) => l.saida > 0);
  const total = linhas.reduce((soma, l) => soma + l.saida, 0);

  return (
    <div className="space-y-6">
      <Cabecalho
        titulo={
          <>
            <BarChart2 className="w-6 h-6 text-[#1F6D2E]" /> Levantamento da Semana
          </>
        }
        subtitulo="Total separado por material, somando todos os locais da semana."
      >
        {semanaId && (
          <>
            <button
              onClick={() => exportar('planilha')}
              className="px-4 py-2.5 bg-emerald-700 hover:bg-emerald-800 text-white rounded-xl font-semibold text-sm transition flex items-center gap-2 shadow"
            >
              <FileSpreadsheet className="w-4 h-4" /> Exportar Excel
            </button>
            <button
              onClick={() => exportar('pdf')}
              className="px-4 py-2.5 bg-rose-700 hover:bg-rose-800 text-white rounded-xl font-semibold text-sm transition flex items-center gap-2 shadow"
            >
              <FileText className="w-4 h-4" /> Exportar PDF
            </button>
          </>
        )}
      </Cabecalho>

      <Aviso mensagem={mensagem} />

      {semanas.length === 0 && !loading && <Vazio>Nenhuma semana enviada ao almoxarifado ainda.</Vazio>}

      {semanas.length > 0 && (
        <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-sm flex flex-col sm:flex-row sm:items-center gap-3 justify-between">
          <label className="text-sm font-semibold text-slate-700 flex items-center gap-2">
            Semana
            <select
              value={semanaId}
              onChange={(e) => setSemanaId(e.target.value)}
              className="px-3 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm font-semibold outline-none focus:ring-2 focus:ring-[#1F6D2E]"
            >
              {semanas.map((s) => (
                <option key={s.id} value={s.id}>
                  {formatarData(s.dataInicio)} a {formatarData(s.dataFim)} ({STATUS_SEMANA[s.status]})
                </option>
              ))}
            </select>
          </label>
          <label className="text-sm text-slate-600 flex items-center gap-2 cursor-pointer">
            <input type="checkbox" checked={mostrarZerados} onChange={(e) => setMostrarZerados(e.target.checked)} className="w-4 h-4 accent-[#1F6D2E]" />
            Mostrar materiais sem saída
          </label>
        </div>
      )}

      {loading && <Carregando />}

      {!loading && semana && (
        <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-[#1F6D2E] text-white text-xs font-bold uppercase tracking-wider">
                <tr>
                  <th className="px-6 py-4">Material</th>
                  <th className="px-6 py-4">Unidade</th>
                  <th className="px-6 py-4 text-right">Saída</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 font-medium text-slate-800">
                {visiveis.map((l) => (
                  <tr key={l.material} className={`hover:bg-slate-50 transition ${l.saida === 0 ? 'text-slate-400' : ''}`}>
                    <td className="px-6 py-3 font-bold">{l.material}</td>
                    <td className="px-6 py-3">{l.unidade}</td>
                    <td className="px-6 py-3 text-right font-extrabold text-[#1F6D2E]">{l.saida}</td>
                  </tr>
                ))}
                {visiveis.length === 0 && (
                  <tr>
                    <td colSpan={3} className="px-6 py-8 text-center text-slate-400">
                      Nenhuma quantidade separada nesta semana ainda.
                    </td>
                  </tr>
                )}
              </tbody>
              <tfoot className="bg-slate-50 font-bold text-slate-900">
                <tr>
                  <td className="px-6 py-3" colSpan={2}>
                    Total
                  </td>
                  <td className="px-6 py-3 text-right">{total}</td>
                </tr>
              </tfoot>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}

export default function LevantamentoPage() {
  return (
    <Protegido perfis={['Almoxarife', 'Gestor', 'Supervisor']}>
      <Levantamento />
    </Protegido>
  );
}
