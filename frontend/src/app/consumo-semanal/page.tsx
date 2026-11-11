'use client';

import React, { useEffect, useMemo, useState } from 'react';
import { fetchApi } from '@/lib/api';
import { TrendingUp, TrendingDown, Minus, LineChart as LineChartIcon } from 'lucide-react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';

interface SemanaConsumo {
  semanaListaId: string;
  dataInicio: string;
  dataFim: string;
  status: string;
  quantidadeTotal: number;
}

interface MaterialTendencia {
  materialId: string;
  nomeMaterial: string;
  unidade: string;
  quantidadesPorSemana: number[];
  semanaAnterior: number;
  ultimaSemana: number;
  variacao: number;
  variacaoPercentual: number | null;
}

interface ConsumoSemanal {
  semanas: SemanaConsumo[];
  materiais: MaterialTendencia[];
}

// Paleta categórica validada (ordem fixa); cada material guarda sua cor enquanto estiver selecionado
const SERIES = ['#2a78d6', '#eb6834', '#1baf7a', '#eda100', '#e87ba4'];
const MAX_SELECIONADOS = SERIES.length;
const COR_AUMENTO = '#d03b3b';
const COR_QUEDA = '#0ca30c';

const formatarData = (iso: string) => {
  const [, mes, dia] = iso.split('-');
  return `${dia}/${mes}`;
};

const formatarPercentual = (v: number | null) =>
  v === null ? 'novo' : `${v > 0 ? '+' : ''}${v.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}%`;

function Variacao({ variacao, percentual }: { variacao: number; percentual: number | null }) {
  if (variacao === 0) {
    return (
      <span className="inline-flex items-center gap-1 text-xs font-semibold text-slate-500">
        <Minus className="w-3.5 h-3.5" /> Estável
      </span>
    );
  }
  const subiu = variacao > 0;
  return (
    <span
      className="inline-flex items-center gap-1 text-xs font-bold"
      style={{ color: subiu ? COR_AUMENTO : COR_QUEDA }}
    >
      {subiu ? <TrendingUp className="w-3.5 h-3.5" /> : <TrendingDown className="w-3.5 h-3.5" />}
      <span className="text-slate-800">
        {subiu ? 'Aumentou' : 'Diminuiu'} {Math.abs(variacao)} ({formatarPercentual(percentual)})
      </span>
    </span>
  );
}

export default function ConsumoSemanalPage() {
  const [quantidadeSemanas, setQuantidadeSemanas] = useState(8);
  const [dados, setDados] = useState<ConsumoSemanal | null>(null);
  const [loading, setLoading] = useState(true);
  const [erro, setErro] = useState<string | null>(null);
  // materialId -> índice da cor
  const [selecionados, setSelecionados] = useState<Record<string, number>>({});

  useEffect(() => {
    const carregar = async () => {
      setLoading(true);
      setErro(null);
      try {
        const data = await fetchApi<ConsumoSemanal>(`/api/consumo-semanal?semanas=${quantidadeSemanas}`);
        setDados(data);
        // Começa mostrando os 3 materiais que mais aumentaram
        const iniciais: Record<string, number> = {};
        data.materiais
          .filter((m) => m.variacao > 0)
          .slice(0, 3)
          .forEach((m, idx) => (iniciais[m.materialId] = idx));
        setSelecionados(iniciais);
      } catch (err: any) {
        setErro(err.message || 'Erro ao carregar o consumo semanal.');
        setDados(null);
      } finally {
        setLoading(false);
      }
    };
    carregar();
  }, [quantidadeSemanas]);

  const toggleMaterial = (materialId: string) => {
    setSelecionados((prev) => {
      if (materialId in prev) {
        const { [materialId]: _, ...resto } = prev;
        return resto;
      }
      const usadas = new Set(Object.values(prev));
      const livre = SERIES.findIndex((_, i) => !usadas.has(i));
      if (livre === -1) return prev;
      return { ...prev, [materialId]: livre };
    });
  };

  const semanas = dados?.semanas ?? [];
  const materiais = dados?.materiais ?? [];
  const ultima = semanas[semanas.length - 1];
  const anterior = semanas[semanas.length - 2];
  const variacaoTotal = ultima && anterior ? ultima.quantidadeTotal - anterior.quantidadeTotal : 0;
  const percentualTotal =
    ultima && anterior && anterior.quantidadeTotal > 0
      ? Math.round((variacaoTotal * 1000) / anterior.quantidadeTotal) / 10
      : null;
  const maiorAumento = materiais.find((m) => m.variacao > 0);
  const maiorQueda = [...materiais].sort((a, b) => a.variacao - b.variacao).find((m) => m.variacao < 0);

  const dadosTotal = useMemo(
    () => semanas.map((s) => ({ semana: formatarData(s.dataInicio), total: s.quantidadeTotal })),
    [semanas]
  );

  const materiaisSelecionados = materiais.filter((m) => m.materialId in selecionados);
  const dadosMateriais = useMemo(
    () =>
      semanas.map((s, idx) => {
        const linha: Record<string, string | number> = { semana: formatarData(s.dataInicio) };
        materiaisSelecionados.forEach((m) => (linha[m.nomeMaterial] = m.quantidadesPorSemana[idx]));
        return linha;
      }),
    [semanas, materiaisSelecionados]
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
      {/* Cabeçalho e filtro */}
      <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-200 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <LineChartIcon className="w-6 h-6 text-[#1F6D2E]" />
            Evolução do Consumo Semanal
          </h2>
          <p className="text-sm text-slate-500 mt-1">
            Quantidade de material que saiu do almoxarifado (retiradas registradas) semana a semana.
          </p>
        </div>
        <div className="flex items-center gap-1 bg-slate-100 p-1 rounded-xl self-start">
          {[4, 8, 12].map((n) => (
            <button
              key={n}
              onClick={() => setQuantidadeSemanas(n)}
              className={`px-3 py-1.5 rounded-lg text-xs font-bold transition ${
                quantidadeSemanas === n ? 'bg-white text-[#1F6D2E] shadow' : 'text-slate-500 hover:text-slate-800'
              }`}
            >
              {n} semanas
            </button>
          ))}
        </div>
      </div>

      {erro && (
        <div className="p-4 rounded-xl text-sm font-medium bg-rose-50 text-rose-800 border border-rose-200">{erro}</div>
      )}

      {!erro && semanas.length < 2 && (
        <div className="bg-slate-50 border-2 border-dashed border-slate-200 rounded-2xl p-12 text-center text-slate-500 font-medium text-sm">
          É preciso ter pelo menos 2 semanas enviadas ao almoxarifado para comparar o consumo.
        </div>
      )}

      {semanas.length >= 2 && ultima && anterior && (
        <>
          {/* Indicadores */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm">
              <p className="text-xs font-bold text-slate-400 uppercase tracking-wider">
                Saída na semana de {formatarData(ultima.dataInicio)}
              </p>
              <p className="text-3xl font-extrabold text-slate-900 mt-2">{ultima.quantidadeTotal.toLocaleString('pt-BR')}</p>
              <div className="mt-2">
                <Variacao variacao={variacaoTotal} percentual={percentualTotal} />
                <span className="text-xs text-slate-500"> vs. semana de {formatarData(anterior.dataInicio)}</span>
              </div>
              {ultima.status !== 'Finalizada' && (
                <p className="text-[11px] text-amber-700 font-semibold mt-2">
                  Semana em andamento: ainda há retiradas a registrar.
                </p>
              )}
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm">
              <p className="text-xs font-bold text-slate-400 uppercase tracking-wider">Maior aumento</p>
              {maiorAumento ? (
                <>
                  <p className="text-lg font-extrabold text-slate-900 mt-2 truncate">{maiorAumento.nomeMaterial}</p>
                  <p className="text-xs text-slate-500">
                    {maiorAumento.semanaAnterior} → {maiorAumento.ultimaSemana} {maiorAumento.unidade}
                  </p>
                  <div className="mt-2">
                    <Variacao variacao={maiorAumento.variacao} percentual={maiorAumento.variacaoPercentual} />
                  </div>
                </>
              ) : (
                <p className="text-sm text-slate-500 mt-2">Nenhum material aumentou.</p>
              )}
            </div>

            <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm">
              <p className="text-xs font-bold text-slate-400 uppercase tracking-wider">Maior queda</p>
              {maiorQueda ? (
                <>
                  <p className="text-lg font-extrabold text-slate-900 mt-2 truncate">{maiorQueda.nomeMaterial}</p>
                  <p className="text-xs text-slate-500">
                    {maiorQueda.semanaAnterior} → {maiorQueda.ultimaSemana} {maiorQueda.unidade}
                  </p>
                  <div className="mt-2">
                    <Variacao variacao={maiorQueda.variacao} percentual={maiorQueda.variacaoPercentual} />
                  </div>
                </>
              ) : (
                <p className="text-sm text-slate-500 mt-2">Nenhum material diminuiu.</p>
              )}
            </div>
          </div>

          {/* Total por semana */}
          <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
            <h3 className="text-base font-bold text-slate-900">Total de itens que saíram por semana</h3>
            <p className="text-xs text-slate-500 mb-4">Data = início da semana</p>
            <div className="h-72">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={dadosTotal} margin={{ top: 10, right: 24, left: 0, bottom: 0 }}>
                  <CartesianGrid stroke="#e5e5e2" vertical={false} />
                  <XAxis dataKey="semana" tick={{ fontSize: 12, fill: '#52514e' }} axisLine={false} tickLine={false} />
                  <YAxis tick={{ fontSize: 12, fill: '#52514e' }} axisLine={false} tickLine={false} width={48} />
                  <Tooltip
                    formatter={(v: number) => [v.toLocaleString('pt-BR'), 'Total']}
                    labelFormatter={(l) => `Semana de ${l}`}
                    contentStyle={{ borderRadius: 12, fontSize: 12 }}
                  />
                  <Line
                    type="monotone"
                    dataKey="total"
                    stroke="#1F6D2E"
                    strokeWidth={2}
                    dot={{ r: 4, fill: '#1F6D2E', stroke: '#fff', strokeWidth: 2 }}
                    activeDot={{ r: 6 }}
                  />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </div>

          {/* Por material */}
          <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm space-y-4">
            <div>
              <h3 className="text-base font-bold text-slate-900">Consumo por material</h3>
              <p className="text-xs text-slate-500">
                Selecione até {MAX_SELECIONADOS} materiais na tabela abaixo para comparar no gráfico.
              </p>
            </div>

            {materiaisSelecionados.length > 0 ? (
              <div className="h-80">
                <ResponsiveContainer width="100%" height="100%">
                  <LineChart data={dadosMateriais} margin={{ top: 10, right: 24, left: 0, bottom: 0 }}>
                    <CartesianGrid stroke="#e5e5e2" vertical={false} />
                    <XAxis dataKey="semana" tick={{ fontSize: 12, fill: '#52514e' }} axisLine={false} tickLine={false} />
                    <YAxis tick={{ fontSize: 12, fill: '#52514e' }} axisLine={false} tickLine={false} width={48} />
                    <Tooltip labelFormatter={(l) => `Semana de ${l}`} contentStyle={{ borderRadius: 12, fontSize: 12 }} />
                    <Legend
                      wrapperStyle={{ fontSize: 12 }}
                      formatter={(valor) => <span style={{ color: '#52514e' }}>{valor}</span>}
                    />
                    {materiaisSelecionados.map((m) => (
                      <Line
                        key={m.materialId}
                        type="monotone"
                        dataKey={m.nomeMaterial}
                        stroke={SERIES[selecionados[m.materialId]]}
                        strokeWidth={2}
                        dot={{ r: 4, fill: SERIES[selecionados[m.materialId]], stroke: '#fff', strokeWidth: 2 }}
                        activeDot={{ r: 6 }}
                      />
                    ))}
                  </LineChart>
                </ResponsiveContainer>
              </div>
            ) : (
              <div className="bg-slate-50 border-2 border-dashed border-slate-200 rounded-xl p-8 text-center text-slate-400 text-sm">
                Nenhum material selecionado.
              </div>
            )}

            {/* Tabela de variação */}
            <div className="overflow-x-auto border border-slate-200 rounded-xl">
              <table className="w-full text-left text-sm">
                <thead className="bg-slate-50 text-xs font-bold text-slate-500 uppercase border-b border-slate-200">
                  <tr>
                    <th className="px-4 py-3 w-10"></th>
                    <th className="px-4 py-3">Material</th>
                    <th className="px-4 py-3 text-right">Sem. {formatarData(anterior.dataInicio)}</th>
                    <th className="px-4 py-3 text-right">Sem. {formatarData(ultima.dataInicio)}</th>
                    <th className="px-4 py-3">Variação</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 font-medium text-slate-800">
                  {materiais.map((m) => {
                    const ativo = m.materialId in selecionados;
                    const bloqueado = !ativo && Object.keys(selecionados).length >= MAX_SELECIONADOS;
                    return (
                      <tr
                        key={m.materialId}
                        onClick={() => !bloqueado && toggleMaterial(m.materialId)}
                        className={`transition ${bloqueado ? 'opacity-50' : 'cursor-pointer hover:bg-slate-50'}`}
                        title={bloqueado ? `Máximo de ${MAX_SELECIONADOS} materiais no gráfico` : 'Mostrar/ocultar no gráfico'}
                      >
                        <td className="px-4 py-2.5">
                          <span
                            className="block w-3.5 h-3.5 rounded border"
                            style={
                              ativo
                                ? { background: SERIES[selecionados[m.materialId]], borderColor: SERIES[selecionados[m.materialId]] }
                                : { borderColor: '#cbd5e1' }
                            }
                          />
                        </td>
                        <td className="px-4 py-2.5 font-semibold text-slate-900">
                          {m.nomeMaterial} <span className="text-xs text-slate-400 font-medium">({m.unidade})</span>
                        </td>
                        <td className="px-4 py-2.5 text-right">{m.semanaAnterior}</td>
                        <td className="px-4 py-2.5 text-right font-bold">{m.ultimaSemana}</td>
                        <td className="px-4 py-2.5">
                          <Variacao variacao={m.variacao} percentual={m.variacaoPercentual} />
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
