'use client';

import React, { useState, useEffect } from 'react';
import { fetchApi } from '@/lib/api';
import {
  PieChart as PieChartIcon,
  BarChart3,
  Calendar,
  Filter,
  FileSpreadsheet,
  FileText,
  Building,
} from 'lucide-react';
import {
  PieChart,
  Pie,
  Cell,
  Tooltip,
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Legend,
} from 'recharts';

interface LocalDto {
  id: string;
  nome: string;
}

interface ConsumoMaterial {
  materialId: string;
  nomeMaterial: string;
  unidade: string;
  quantidadeTotal: number;
}

interface ConsumoLocal {
  localId: string;
  nomeLocal: string;
  quantidadeTotal: number;
}

interface ConsumoGeralDto {
  dataInicio: string;
  dataFim: string;
  porMaterial: ConsumoMaterial[];
  porLocal: ConsumoLocal[];
}

const COLORS = [
  '#1F6D2E', '#2563EB', '#D97706', '#7C3AED', '#DC2626',
  '#059669', '#0284C7', '#EA580C', '#9333EA', '#E11D48',
  '#16A34A', '#0891B2', '#CA8A04', '#4F46E5', '#BE123C'
];

export default function GestorConsumoPage() {
  const [dataInicio, setDataInicio] = useState<string>(() => {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return d.toISOString().split('T')[0];
  });
  const [dataFim, setDataFim] = useState<string>(() => new Date().toISOString().split('T')[0]);
  const [selectedLocalId, setSelectedLocalId] = useState<string>('');
  const [locais, setLocais] = useState<LocalDto[]>([]);
  const [consumo, setConsumo] = useState<ConsumoGeralDto | null>(null);
  const [modoVisualizacao, setModoVisualizacao] = useState<'material' | 'local'>('material');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    carregarLocais();
  }, []);

  useEffect(() => {
    carregarDadosConsumo();
  }, [dataInicio, dataFim, selectedLocalId]);

  const carregarLocais = async () => {
    try {
      const data = await fetchApi<LocalDto[]>('/api/locais');
      setLocais(data);
    } catch (_) {}
  };

  const carregarDadosConsumo = async () => {
    setLoading(true);
    try {
      if (selectedLocalId) {
        const matData = await fetchApi<ConsumoMaterial[]>(
          `/api/consumo/local/${selectedLocalId}?dataInicio=${dataInicio}&dataFim=${dataFim}`
        );
        setConsumo({
          dataInicio,
          dataFim,
          porMaterial: matData,
          porLocal: [],
        });
      } else {
        const geralData = await fetchApi<ConsumoGeralDto>(
          `/api/consumo/geral?dataInicio=${dataInicio}&dataFim=${dataFim}`
        );
        setConsumo(geralData);
      }
    } catch (_) {
      setConsumo(null);
    } finally {
      setLoading(false);
    }
  };

  const aplicarAtalhoData = (tipo: '4semanas' | 'mes' | 'ano') => {
    const hoje = new Date();
    let inicio = new Date();

    if (tipo === '4semanas') {
      inicio.setDate(hoje.getDate() - 28);
    } else if (tipo === 'mes') {
      inicio = new Date(hoje.getFullYear(), hoje.getMonth(), 1);
    } else if (tipo === 'ano') {
      inicio = new Date(hoje.getFullYear(), 0, 1);
    }

    setDataInicio(inicio.toISOString().split('T')[0]);
    setDataFim(hoje.toISOString().split('T')[0]);
  };

  const exportarConsumo = async (formato: 'excel' | 'pdf') => {
    try {
      const endpoint = selectedLocalId
        ? `/api/consumo/local/${selectedLocalId}/export?dataInicio=${dataInicio}&dataFim=${dataFim}&formato=${formato}`
        : `/api/consumo/geral/export?dataInicio=${dataInicio}&dataFim=${dataFim}&formato=${formato}`;

      const blob = await fetchApi(endpoint);
      const url = window.URL.createObjectURL(blob as Blob);
      const a = document.createElement('a');
      a.href = url;
      const ext = formato === 'excel' ? 'xlsx' : 'pdf';
      a.download = `Relatorio-Consumo.${ext}`;
      document.body.appendChild(a);
      a.click();
      a.remove();
    } catch (err: any) {
      alert('Erro ao realizar exportação de consumo.');
    }
  };

  return (
    <div className="space-y-6">
      {/* Top Header */}
      <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-200 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <BarChart3 className="w-6 h-6 text-[#1F6D2E]" />
            Painel Analítico de Consumo
          </h2>
          <p className="text-sm text-slate-500 mt-1">
            Visão gerencial consolidada por período e localização com gráficos dinâmicos.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => exportarConsumo('excel')}
            className="px-4 py-2.5 bg-emerald-700 hover:bg-emerald-800 text-white rounded-xl font-semibold text-sm transition flex items-center gap-2 shadow"
          >
            <FileSpreadsheet className="w-4 h-4" />
            Excel
          </button>
          <button
            onClick={() => exportarConsumo('pdf')}
            className="px-4 py-2.5 bg-rose-700 hover:bg-rose-800 text-white rounded-xl font-semibold text-sm transition flex items-center gap-2 shadow"
          >
            <FileText className="w-4 h-4" />
            PDF
          </button>
        </div>
      </div>

      {/* Painel de Filtros */}
      <div className="bg-white p-5 rounded-2xl border border-slate-200 shadow-sm space-y-4">
        <div className="flex items-center gap-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
          <Filter className="w-4 h-4 text-[#1F6D2E]" />
          Filtros de Pesquisa
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-4 gap-4 items-end">
          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Data Início</label>
            <input
              type="date"
              value={dataInicio}
              onChange={(e) => setDataInicio(e.target.value)}
              className="w-full px-3 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm font-semibold outline-none focus:ring-2 focus:ring-[#1F6D2E]"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Data Fim</label>
            <input
              type="date"
              value={dataFim}
              onChange={(e) => setDataFim(e.target.value)}
              className="w-full px-3 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm font-semibold outline-none focus:ring-2 focus:ring-[#1F6D2E]"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 mb-1">Filtrar por Local</label>
            <select
              value={selectedLocalId}
              onChange={(e) => setSelectedLocalId(e.target.value)}
              className="w-full px-3 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm font-semibold outline-none focus:ring-2 focus:ring-[#1F6D2E]"
            >
              <option value="">Todos os Locais (Geral)</option>
              {locais.map((loc) => (
                <option key={loc.id} value={loc.id}>
                  {loc.nome}
                </option>
              ))}
            </select>
          </div>

          {/* Atalhos Rápidos */}
          <div className="flex items-center gap-1.5 pb-0.5">
            <button
              onClick={() => aplicarAtalhoData('4semanas')}
              className="px-2.5 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-semibold rounded-lg transition"
            >
              4 Semanas
            </button>
            <button
              onClick={() => aplicarAtalhoData('mes')}
              className="px-2.5 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-semibold rounded-lg transition"
            >
              Este Mês
            </button>
            <button
              onClick={() => aplicarAtalhoData('ano')}
              className="px-2.5 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-semibold rounded-lg transition"
            >
              Este Ano
            </button>
          </div>
        </div>
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-[#1F6D2E]"></div>
        </div>
      ) : (
        <>
          {/* Seletor de Visão Gráfica */}
          <div className="flex items-center justify-between bg-white p-4 rounded-xl border border-slate-200 shadow-sm">
            <div className="flex items-center gap-2">
              <button
                onClick={() => setModoVisualizacao('material')}
                className={`px-4 py-2 rounded-lg text-xs font-bold transition flex items-center gap-2 ${
                  modoVisualizacao === 'material'
                    ? 'bg-[#1F6D2E] text-white shadow'
                    : 'bg-slate-100 text-slate-700 hover:bg-slate-200'
                }`}
              >
                <PieChartIcon className="w-4 h-4" />
                Por Material (Pizza)
              </button>

              {!selectedLocalId && (
                <button
                  onClick={() => setModoVisualizacao('local')}
                  className={`px-4 py-2 rounded-lg text-xs font-bold transition flex items-center gap-2 ${
                    modoVisualizacao === 'local'
                      ? 'bg-[#1F6D2E] text-white shadow'
                      : 'bg-slate-100 text-slate-700 hover:bg-slate-200'
                  }`}
                >
                  <BarChart3 className="w-4 h-4" />
                  Por Local (Barras)
                </button>
              )}
            </div>

            <span className="text-xs font-medium text-slate-500 hidden sm:inline">
              Período: <strong className="text-slate-800">{dataInicio}</strong> a <strong className="text-slate-800">{dataFim}</strong>
            </span>
          </div>

          {/* Gráfico Dinâmico */}
          <div className="bg-white p-6 rounded-2xl border border-slate-200 shadow-sm min-h-[380px] flex items-center justify-center">
            {modoVisualizacao === 'material' && consumo?.porMaterial && consumo.porMaterial.length > 0 ? (
              <div className="w-full h-80">
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie
                      data={consumo.porMaterial.slice(0, 10)}
                      dataKey="quantidadeTotal"
                      nameKey="nomeMaterial"
                      cx="50%"
                      cy="50%"
                      outerRadius={100}
                      label={({ name, percent }) => `${name} (${(percent * 100).toFixed(0)}%)`}
                    >
                      {consumo.porMaterial.slice(0, 10).map((_, index) => (
                        <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip formatter={(value: any) => [`${value} unidades`, 'Quantidade Total']} />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            ) : modoVisualizacao === 'local' && consumo?.porLocal && consumo.porLocal.length > 0 ? (
              <div className="w-full h-80">
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={consumo.porLocal}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="nomeLocal" tick={{ fontSize: 11 }} />
                    <YAxis />
                    <Tooltip formatter={(value: any) => [`${value} itens`, 'Consumo Total']} />
                    <Legend />
                    <Bar dataKey="quantidadeTotal" name="Quantidade Total Consumida" fill="#1F6D2E" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            ) : (
              <p className="text-slate-400 font-medium text-sm">
                Nenhum dado de consumo encontrado para os filtros selecionados.
              </p>
            )}
          </div>

          {/* Tabela Detalhada de Valores Exatos */}
          <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
            <div className="px-6 py-4 border-b border-slate-100 font-bold text-slate-900 text-sm">
              Valores Exatos do Consumo
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="bg-slate-50 text-xs font-bold text-slate-500 uppercase">
                  <tr>
                    <th className="px-6 py-3">Material</th>
                    <th className="px-6 py-3">Unidade</th>
                    <th className="px-6 py-3 text-right">Quantidade Total Consumida</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 font-medium text-slate-800">
                  {consumo?.porMaterial.map((item) => (
                    <tr key={item.materialId} className="hover:bg-slate-50 transition">
                      <td className="px-6 py-3 font-bold text-slate-900">{item.nomeMaterial}</td>
                      <td className="px-6 py-3 text-slate-500">{item.unidade}</td>
                      <td className="px-6 py-3 text-right font-extrabold text-[#1F6D2E]">
                        {item.quantidadeTotal}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
