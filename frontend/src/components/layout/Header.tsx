'use client';

import React, { useCallback, useEffect, useRef, useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useAuth, Perfil } from '@/context/AuthContext';
import { fetchApi } from '@/lib/api';
import { formatarDataHora } from '@/lib/formatacao';
import {
  Settings,
  LogOut,
  Package,
  ClipboardCheck,
  Eye,
  BarChart3,
  History,
  TrendingUp,
  Menu,
  X,
  Boxes,
  Bell,
  ClipboardList,
} from 'lucide-react';

interface NavItem {
  href: string;
  label: string;
  icon: React.ReactNode;
}

const NAV_POR_PERFIL: Record<Perfil, NavItem[]> = {
  Supervisor: [
    { href: '/supervisor/nova-semana', label: 'Semana', icon: <Package className="w-4 h-4" /> },
    { href: '/supervisor/locais', label: 'Locais', icon: <Settings className="w-4 h-4" /> },
    { href: '/gestor/consumo', label: 'Consumo', icon: <BarChart3 className="w-4 h-4" /> },
    { href: '/historico', label: 'Histórico', icon: <History className="w-4 h-4" /> },
  ],
  Coordenador: [
    { href: '/coordenador/pedidos', label: 'Pedidos', icon: <ClipboardList className="w-4 h-4" /> },
    { href: '/historico', label: 'Histórico', icon: <History className="w-4 h-4" /> },
  ],
  Almoxarife: [
    { href: '/almoxarife/semana-atual', label: 'Separação e Retirada', icon: <ClipboardCheck className="w-4 h-4" /> },
    { href: '/almoxarife/materiais', label: 'Materiais', icon: <Boxes className="w-4 h-4" /> },
    { href: '/levantamento', label: 'Levantamento', icon: <BarChart3 className="w-4 h-4" /> },
    { href: '/historico', label: 'Histórico', icon: <History className="w-4 h-4" /> },
  ],
  Gestor: [
    { href: '/gestor/consumo', label: 'Painel de Consumo', icon: <BarChart3 className="w-4 h-4" /> },
    { href: '/gestor/semana-atual', label: 'Lista Atual', icon: <Eye className="w-4 h-4" /> },
    { href: '/levantamento', label: 'Levantamento', icon: <ClipboardCheck className="w-4 h-4" /> },
    { href: '/historico', label: 'Histórico', icon: <History className="w-4 h-4" /> },
  ],
};

// Disponível para todos os perfis
const NAV_COMUM: NavItem[] = [{ href: '/consumo-semanal', label: 'Evolução', icon: <TrendingUp className="w-4 h-4" /> }];

interface Notificacao {
  id: string;
  mensagem: string;
  dataCriacao: string;
  lida: boolean;
}

function Notificacoes() {
  const [itens, setItens] = useState<Notificacao[]>([]);
  const [aberto, setAberto] = useState(false);
  const caixaRef = useRef<HTMLDivElement>(null);

  const carregar = useCallback(async () => {
    try {
      setItens(await fetchApi<Notificacao[]>('/api/notificacoes'));
    } catch {
      /* aviso não é essencial: ignora falhas */
    }
  }, []);

  useEffect(() => {
    carregar();
    const intervalo = setInterval(carregar, 60_000);
    return () => clearInterval(intervalo);
  }, [carregar]);

  useEffect(() => {
    if (!aberto) return;
    const fechar = (e: MouseEvent) => {
      if (!caixaRef.current?.contains(e.target as Node)) setAberto(false);
    };
    document.addEventListener('mousedown', fechar);
    return () => document.removeEventListener('mousedown', fechar);
  }, [aberto]);

  const naoLidas = itens.filter((n) => !n.lida);

  const marcarLida = async (id: string) => {
    setItens((prev) => prev.map((n) => (n.id === id ? { ...n, lida: true } : n)));
    await fetchApi(`/api/notificacoes/${id}/lida`, { method: 'POST' }).catch(() => {});
  };

  return (
    <div className="relative" ref={caixaRef}>
      <button
        onClick={() => setAberto((v) => !v)}
        className="relative p-2 rounded-md text-green-100 hover:bg-[#175423] transition"
        aria-label={`Notificações${naoLidas.length ? ` (${naoLidas.length} não lidas)` : ''}`}
      >
        <Bell className="w-5 h-5" />
        {naoLidas.length > 0 && (
          <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] px-1 rounded-full bg-amber-400 text-[#412402] text-[10px] font-extrabold flex items-center justify-center">
            {naoLidas.length > 9 ? '9+' : naoLidas.length}
          </span>
        )}
      </button>
      {aberto && (
        <div className="absolute right-0 mt-2 w-80 max-w-[90vw] bg-white text-slate-800 rounded-xl shadow-2xl border border-slate-200 overflow-hidden z-50">
          <p className="px-4 py-2.5 text-xs font-bold text-slate-500 uppercase tracking-wider border-b border-slate-100">Notificações</p>
          <div className="max-h-80 overflow-y-auto divide-y divide-slate-100">
            {itens.length === 0 && <p className="px-4 py-6 text-sm text-slate-400 text-center">Nenhuma notificação.</p>}
            {itens.map((n) => (
              <button
                key={n.id}
                onClick={() => !n.lida && marcarLida(n.id)}
                className={`w-full text-left px-4 py-3 text-sm transition ${n.lida ? 'text-slate-500' : 'bg-emerald-50/60 font-semibold hover:bg-emerald-50'}`}
              >
                {n.mensagem}
                <span className="block text-[11px] text-slate-400 font-normal mt-0.5">
                  {formatarDataHora(n.dataCriacao)}
                  {!n.lida && ' · toque para marcar como lida'}
                </span>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

export default function Header() {
  const { user, logout } = useAuth();
  const pathname = usePathname();
  const [menuAberto, setMenuAberto] = useState(false);

  // Fecha o menu do celular ao trocar de página
  useEffect(() => {
    setMenuAberto(false);
  }, [pathname]);

  if (!user || pathname === '/login') return null;

  const itens = [...(NAV_POR_PERFIL[user.role] ?? []), ...NAV_COMUM];
  const isActive = (path: string) => pathname === path;

  return (
    <header className="bg-[#1F6D2E] text-white shadow-md sticky top-0 z-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between gap-3">
        <Link href="/" className="flex items-center space-x-3 flex-shrink-0">
          <div className="bg-white text-[#1F6D2E] p-2 rounded-lg font-black text-xl tracking-wider flex items-center justify-center w-10 h-10 shadow">
            N
          </div>
          <div>
            <p className="font-bold text-lg leading-tight tracking-wide">NORMATEL</p>
            <p className="text-xs text-green-200 font-medium tracking-wider">ENGENHARIA</p>
          </div>
        </Link>

        <nav className="hidden xl:flex items-center space-x-1">
          {itens.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className={`px-3 py-2 rounded-md text-sm font-medium transition flex items-center gap-2 whitespace-nowrap ${
                isActive(item.href) ? 'bg-emerald-800 text-white shadow-inner' : 'text-green-100 hover:bg-[#175423]'
              }`}
            >
              {item.icon}
              {item.label}
            </Link>
          ))}
        </nav>

        <div className="flex items-center gap-1 sm:gap-3">
          <div className="text-right hidden sm:block">
            <p className="text-sm font-semibold leading-tight max-w-[160px] truncate">{user.nome}</p>
            <span className="inline-block text-[10px] font-bold uppercase tracking-wider bg-emerald-900/60 px-2 py-0.5 rounded text-green-200">
              {user.role}
            </span>
          </div>

          <Notificacoes />

          <button
            onClick={logout}
            className="hidden xl:flex p-2 rounded-md text-green-100 hover:bg-[#175423] transition items-center gap-1 text-sm font-medium"
            title="Sair do sistema"
          >
            <LogOut className="w-4 h-4" />
            <span>Sair</span>
          </button>

          <button
            onClick={() => setMenuAberto((v) => !v)}
            className="xl:hidden p-2.5 rounded-md text-white hover:bg-[#175423] transition"
            aria-label={menuAberto ? 'Fechar menu' : 'Abrir menu'}
            aria-expanded={menuAberto}
          >
            {menuAberto ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
          </button>
        </div>
      </div>

      {menuAberto && (
        <>
          <div className="xl:hidden fixed inset-0 top-16 bg-black/30" onClick={() => setMenuAberto(false)} />
          <nav className="xl:hidden absolute left-0 right-0 top-16 bg-[#1F6D2E] border-t border-emerald-800 shadow-xl max-h-[calc(100vh-4rem)] overflow-y-auto">
            <div className="px-4 py-3 border-b border-emerald-800 sm:hidden">
              <p className="text-sm font-semibold">{user.nome}</p>
              <span className="inline-block mt-1 text-[10px] font-bold uppercase tracking-wider bg-emerald-900/60 px-2 py-0.5 rounded text-green-200">
                {user.role}
              </span>
            </div>
            <div className="px-2 py-2 space-y-1">
              {itens.map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  className={`flex items-center gap-3 px-3 py-3 rounded-lg text-base font-medium transition ${
                    isActive(item.href) ? 'bg-emerald-800 text-white' : 'text-green-50 hover:bg-[#175423]'
                  }`}
                >
                  {item.icon}
                  {item.label}
                </Link>
              ))}
              <button
                onClick={logout}
                className="w-full flex items-center gap-3 px-3 py-3 rounded-lg text-base font-medium text-green-50 hover:bg-[#175423] transition border-t border-emerald-800 mt-2 pt-4"
              >
                <LogOut className="w-4 h-4" />
                Sair
              </button>
            </div>
          </nav>
        </>
      )}
    </header>
  );
}
