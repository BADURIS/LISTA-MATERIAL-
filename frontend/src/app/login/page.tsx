'use client';

import React, { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth, PAGINA_INICIAL } from '@/context/AuthContext';
import { ShieldCheck, Lock, User as UserIcon, Package, ClipboardCheck, BarChart3, UserCheck } from 'lucide-react';

const PERFIS = [
  {
    username: 'supervisor',
    titulo: 'Supervisor',
    desc: 'Cria a semana, faz os pedidos por local e envia ao almoxarifado.',
    icon: <Package className="w-5 h-5 text-[#1F6D2E]" />,
  },
  {
    username: 'coordenador',
    titulo: 'Coordenador',
    desc: 'Ajuda nos pedidos, confirma o recebimento e registra a retirada.',
    icon: <UserCheck className="w-5 h-5 text-amber-700" />,
  },
  {
    username: 'almoxarife',
    titulo: 'Almoxarife',
    desc: 'Separa os materiais e informa as quantidades e faltas.',
    icon: <ClipboardCheck className="w-5 h-5 text-blue-700" />,
  },
  {
    username: 'gestor',
    titulo: 'Gestor',
    desc: 'Acompanha consumo, histórico e exporta planilhas.',
    icon: <BarChart3 className="w-5 h-5 text-purple-700" />,
  },
];

export default function LoginPage() {
  const { user, loading, login } = useAuth();
  const router = useRouter();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [erro, setErro] = useState<string | null>(null);
  const [entrando, setEntrando] = useState(false);

  // Já logado: vai direto para a página do perfil
  useEffect(() => {
    if (!loading && user) router.replace(PAGINA_INICIAL[user.role]);
  }, [loading, user, router]);

  useEffect(() => {
    if (new URLSearchParams(window.location.search).get('expirou')) {
      setErro('Sua sessão expirou. Entre novamente.');
    }
  }, []);

  const entrar = async (e: React.FormEvent) => {
    e.preventDefault();
    setErro(null);
    setEntrando(true);
    try {
      await login(username, password);
    } catch (err: any) {
      setErro(err.message || 'Não foi possível entrar. Verifique se o servidor está rodando.');
    } finally {
      setEntrando(false);
    }
  };

  return (
    <div className="min-h-[85vh] flex flex-col items-center justify-center py-8">
      <div className="max-w-md w-full text-center mb-6">
        <div className="mx-auto w-16 h-16 bg-[#1F6D2E] text-white rounded-2xl flex items-center justify-center text-3xl font-black shadow-lg mb-3">
          N
        </div>
        <h1 className="text-3xl font-extrabold text-slate-900 tracking-tight">NORMATEL ENGENHARIA</h1>
        <p className="mt-1 text-sm text-slate-600 font-medium">Gestão de Listas de Material Semanais</p>
      </div>

      <div className="bg-white p-6 sm:p-8 rounded-2xl shadow-xl border border-slate-200 w-full max-w-md space-y-6">
        <form onSubmit={entrar} className="space-y-4">
          <h2 className="text-base font-bold text-slate-900 flex items-center gap-2">
            <ShieldCheck className="w-5 h-5 text-[#1F6D2E]" />
            Entrar no sistema
          </h2>

          <div>
            <label htmlFor="usuario" className="block text-xs font-semibold text-slate-700 mb-1">
              Usuário
            </label>
            <div className="relative">
              <UserIcon className="w-4 h-4 text-slate-400 absolute left-3 top-3" />
              <input
                id="usuario"
                type="text"
                required
                autoComplete="username"
                autoCapitalize="none"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="ex.: supervisor"
                className="w-full pl-9 pr-4 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm font-medium outline-none focus:ring-2 focus:ring-[#1F6D2E]"
              />
            </div>
          </div>

          <div>
            <label htmlFor="senha" className="block text-xs font-semibold text-slate-700 mb-1">
              Senha
            </label>
            <div className="relative">
              <Lock className="w-4 h-4 text-slate-400 absolute left-3 top-3" />
              <input
                id="senha"
                type="password"
                required
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="w-full pl-9 pr-4 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm font-medium outline-none focus:ring-2 focus:ring-[#1F6D2E]"
              />
            </div>
          </div>

          {erro && (
            <p role="alert" className="text-sm font-medium text-rose-700 bg-rose-50 border border-rose-200 rounded-lg px-3 py-2">
              {erro}
            </p>
          )}

          <button
            type="submit"
            disabled={entrando}
            className="w-full py-3 bg-[#1F6D2E] hover:bg-[#175423] disabled:opacity-60 text-white font-bold text-sm rounded-xl transition shadow-md"
          >
            {entrando ? 'Entrando...' : 'Entrar'}
          </button>
        </form>

        <div>
          <p className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-2">Perfis de acesso</p>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
            {PERFIS.map((p) => (
              <button
                key={p.username}
                type="button"
                onClick={() => {
                  setUsername(p.username);
                  document.getElementById('senha')?.focus();
                }}
                className="text-left p-3 rounded-xl border border-slate-200 hover:border-[#1F6D2E] hover:bg-slate-50 transition"
                title={`Preencher o usuário "${p.username}"`}
              >
                <div className="flex items-center gap-2 font-bold text-sm text-slate-900">
                  {p.icon}
                  {p.titulo}
                </div>
                <p className="text-[11px] text-slate-500 mt-1 leading-snug">{p.desc}</p>
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
