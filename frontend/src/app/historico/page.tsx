'use client';

import React, { useEffect, useState } from 'react';
import { baixarArquivo, fetchApi } from '@/lib/api';
import { formatarData, formatarDataHora, STATUS_LISTA, STATUS_SEMANA } from '@/lib/formatacao';
import type { Semana, SemanaResumo } from '@/lib/tipos';
import { useAuth } from '@/context/AuthContext';
import { Aviso, Cabecalho, Carregando, Mensagem, Protegido, Vazio } from '@/components/ui';
import { Calendar, FileSpreadsheet, FileText, ChevronRight, ShieldCheck, ShieldAlert } from 'lucide-react';

function Historico() {
  const { user } = useAuth();
  const [semanas, setSemanas] = useState<SemanaResumo[]>([]);
  const [detalhe, setDetalhe] = useState<Semana | null>(null);
  const [selecionadaId, setSelecionadaId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [carregandoDetalhe, setCarregandoDetalhe] = useState(false);
  const [mensagem, setMensagem] = useState<Mensagem>(null);

  // A planilha consolidada da semana não é liberada para o Coordenador
  const podeExportarSemana = user?.role !== 'Coordenador';

  useEffect(() => {
    fetchApi<SemanaResumo[]>('/api/semanas-lista/historico')
      .then(setSemanas)
      .catch((err) => setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao carregar o histórico.' }))
      .finally(() => setLoading(false));
  }, []);

  const abrir = async (id: string) => {
    setSelecionadaId(id);
    setCarregandoDetalhe(true);
    try {
      setDetalhe(await fetchApi<Semana>(`/api/semanas-lista/${id}`));
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao abrir a semana.' });
    } finally {
      setCarregandoDetalhe(false);
    }
  };

  const exportar = async (endpoint: string) => {
    try {
      await baixarArquivo(endpoint, 'planilha.xlsx');
    } catch (err: any) {
      setMensagem({ tipo: 'erro', texto: err.message || 'Erro ao exportar.' });
    }
  };

  if (loading) return <Carregando />;

  return (
    <div className="space-y-6">
      <Cabecalho
        titulo={
          <>
            <Calendar className="w-6 h-6 text-[#1F6D2E]" /> Histórico de Semanas
          </>
        }
        subtitulo="Nenhum registro é apagado. Consulte e exporte qualquer semana anterior."
      />

      <Aviso mensagem={mensagem} />

      {semanas.length === 0 ? (
        <Vazio>Nenhuma semana registrada ainda.</Vazio>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="space-y-2 lg:col-span-1">
            {semanas.map((s) => (
              <button
                key={s.id}
                onClick={() => abrir(s.id)}
                className={`w-full p-4 rounded-xl border text-left transition flex items-center justify-between ${
                  selecionadaId === s.id ? 'border-[#1F6D2E] bg-emerald-50/50 ring-2 ring-[#1F6D2E]/20 shadow' : 'border-slate-200 bg-white hover:border-slate-300'
                }`}
              >
                <div>
                  <p className="font-bold text-sm text-slate-900">
                    {formatarData(s.dataInicio)} a {formatarData(s.dataFim)}
                  </p>
                  <span
                    className={`inline-block mt-1 text-[10px] font-bold px-2 py-0.5 rounded ${
                      s.status === 'Finalizada' ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800'
                    }`}
                  >
                    {STATUS_SEMANA[s.status]}
                  </span>
                </div>
                <ChevronRight className="w-5 h-5 text-slate-400" />
              </button>
            ))}
          </div>

          <div className="lg:col-span-2">
            {carregandoDetalhe ? (
              <Carregando />
            ) : detalhe ? (
              <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm space-y-4">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-100 pb-4">
                  <div>
                    <h3 className="text-xl font-bold text-slate-900">
                      {formatarData(detalhe.dataInicio)} a {formatarData(detalhe.dataFim)}
                    </h3>
                    <p className="text-xs text-slate-500 mt-0.5">
                      {detalhe.locais.filter((l) => !l.semPedido).length} locais com pedido · {detalhe.locais.filter((l) => l.semPedido).length} sem pedido
                    </p>
                  </div>
                  {podeExportarSemana && detalhe.status !== 'EmMontagem' && (
                    <div className="flex items-center gap-2 self-start">
                      <button
                        onClick={() => exportar(`/api/semanas-lista/${detalhe.id}/planilha`)}
                        className="px-3 py-2 bg-emerald-100 text-emerald-800 hover:bg-emerald-200 rounded-lg text-xs font-bold transition flex items-center gap-1.5"
                      >
                        <FileSpreadsheet className="w-4 h-4 text-emerald-700" /> Consolidado Excel
                      </button>
                      <button
                        onClick={() => exportar(`/api/semanas-lista/${detalhe.id}/pdf`)}
                        className="px-3 py-2 bg-rose-100 text-rose-800 hover:bg-rose-200 rounded-lg text-xs font-bold transition flex items-center gap-1.5"
                      >
                        <FileText className="w-4 h-4 text-rose-700" /> Consolidado PDF
                      </button>
                    </div>
                  )}
                </div>

                <div className="divide-y divide-slate-100">
                  {detalhe.locais.map((l) => (
                    <div key={l.id} className="py-3 flex items-center justify-between gap-3 text-sm">
                      <div className="min-w-0">
                        <p className="font-bold text-slate-900">{l.nomeLocal}</p>
                        {l.semPedido ? (
                          <p className="text-xs text-slate-400">Sem pedido nesta semana</p>
                        ) : l.dataRetirada ? (
                          <p className="text-xs text-slate-500 flex items-center gap-1 flex-wrap">
                            Retirado por <span className="font-semibold text-slate-700">{l.nomeRetiradoPor}</span> em {formatarDataHora(l.dataRetirada)}
                            {l.assinaturaValida ? (
                              <ShieldCheck className="w-3.5 h-3.5 text-emerald-600" aria-label="Assinatura válida" />
                            ) : (
                              <ShieldAlert className="w-3.5 h-3.5 text-rose-600" aria-label="Assinatura inválida" />
                            )}
                          </p>
                        ) : (
                          <p className="text-xs text-amber-600 font-medium">Sem registro de retirada</p>
                        )}
                      </div>
                      <div className="flex items-center gap-2 flex-shrink-0">
                        <span className="text-xs font-bold px-2.5 py-1 rounded bg-slate-100 text-slate-700">
                          {l.semPedido ? 'Sem pedido' : STATUS_LISTA[l.status]}
                        </span>
                        {!l.semPedido && (
                          <button
                            onClick={() => exportar(`/api/semanas-lista/${detalhe.id}/listas/${l.id}/planilha`)}
                            className="p-1.5 rounded-lg text-emerald-700 hover:bg-emerald-50"
                            title={`Planilha de ${l.nomeLocal}`}
                            aria-label={`Exportar planilha de ${l.nomeLocal}`}
                          >
                            <FileSpreadsheet className="w-4 h-4" />
                          </button>
                        )}
                        {!l.semPedido && (
                          <button
                            onClick={() => exportar(`/api/semanas-lista/${detalhe.id}/listas/${l.id}/pdf`)}
                            className="p-1.5 rounded-lg text-rose-700 hover:bg-rose-50"
                            title={`PDF de ${l.nomeLocal}`}
                            aria-label={`Exportar PDF de ${l.nomeLocal}`}
                          >
                            <FileText className="w-4 h-4" />
                          </button>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            ) : (
              <Vazio>Selecione uma semana para ver os detalhes.</Vazio>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

export default function HistoricoPage() {
  return (
    <Protegido perfis={['Supervisor', 'Coordenador', 'Almoxarife', 'Gestor']}>
      <Historico />
    </Protegido>
  );
}
