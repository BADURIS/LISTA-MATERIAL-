'use client';

import React from 'react';
import Link from 'next/link';
import { AlertCircle, CheckCircle2, Lock } from 'lucide-react';
import { useAuth, Perfil, PAGINA_INICIAL } from '@/context/AuthContext';

export function Carregando() {
  return (
    <div className="flex items-center justify-center py-20">
      <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-[#1F6D2E]" />
    </div>
  );
}

export type Mensagem = { tipo: 'sucesso' | 'erro'; texto: string } | null;

export function Aviso({ mensagem }: { mensagem: Mensagem }) {
  if (!mensagem) return null;
  const sucesso = mensagem.tipo === 'sucesso';
  return (
    <div
      role={sucesso ? 'status' : 'alert'}
      className={`p-4 rounded-xl text-sm font-medium flex items-center gap-3 border ${
        sucesso ? 'bg-emerald-50 text-emerald-800 border-emerald-200' : 'bg-rose-50 text-rose-800 border-rose-200'
      }`}
    >
      {sucesso ? (
        <CheckCircle2 className="w-5 h-5 text-emerald-600 flex-shrink-0" />
      ) : (
        <AlertCircle className="w-5 h-5 text-rose-600 flex-shrink-0" />
      )}
      {mensagem.texto}
    </div>
  );
}

export function Cabecalho({ titulo, subtitulo, children }: { titulo: React.ReactNode; subtitulo?: React.ReactNode; children?: React.ReactNode }) {
  return (
    <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-200 flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div>
        <h2 className="text-2xl font-bold text-slate-900 tracking-tight flex items-center gap-2">{titulo}</h2>
        {subtitulo && <div className="text-sm text-slate-600 mt-1">{subtitulo}</div>}
      </div>
      {children && <div className="flex flex-wrap items-center gap-2">{children}</div>}
    </div>
  );
}

export function Vazio({ children }: { children: React.ReactNode }) {
  return (
    <div className="bg-slate-50 border-2 border-dashed border-slate-200 rounded-2xl p-12 text-center text-slate-500 font-medium text-sm">
      {children}
    </div>
  );
}

// Mostra a página só para os perfis permitidos (a API também valida cada chamada)
export function Protegido({ perfis, children }: { perfis: Perfil[]; children: React.ReactNode }) {
  const { user, loading } = useAuth();
  if (loading || !user) return <Carregando />;
  if (!perfis.includes(user.role)) {
    return (
      <div className="max-w-lg mx-auto mt-12 bg-white p-8 rounded-2xl border border-slate-200 shadow-sm text-center space-y-3">
        <Lock className="w-8 h-8 mx-auto text-slate-400" />
        <p className="font-bold text-slate-900">Esta página não está disponível para o perfil {user.role}.</p>
        <Link href={PAGINA_INICIAL[user.role]} className="inline-block text-sm font-semibold text-[#1F6D2E] hover:underline">
          Ir para a minha página inicial
        </Link>
      </div>
    );
  }
  return <>{children}</>;
}
