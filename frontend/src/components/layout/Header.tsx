'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useAuth, User } from '@/context/AuthContext';
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
} from 'lucide-react';

interface NavItem {
  href: string;
  label: string;
  icon: React.ReactNode;
}

const NAV_POR_PERFIL: Record<User['role'], NavItem[]> = {
  Supervisor: [
    { href: '/supervisor/nova-semana', label: 'Nova Semana', icon: <Package className="w-4 h-4" /> },
    { href: '/supervisor/locais', label: 'Locais', icon: <Settings className="w-4 h-4" /> },
  ],
  Almoxarife: [
    { href: '/almoxarife/semana-atual', label: 'Separação Atual', icon: <ClipboardCheck className="w-4 h-4" /> },
    { href: '/almoxarife/levantamento', label: 'Levantamento', icon: <BarChart3 className="w-4 h-4" /> },
    { href: '/almoxarife/historico', label: 'Histórico', icon: <History className="w-4 h-4" /> },
  ],
  Gestor: [
    { href: '/gestor/consumo', label: 'Painel de Consumo', icon: <BarChart3 className="w-4 h-4" /> },
    { href: '/gestor/semana-atual', label: 'Lista Atual', icon: <Eye className="w-4 h-4" /> },
    { href: '/gestor/levantamento', label: 'Levantamento', icon: <ClipboardCheck className="w-4 h-4" /> },
    { href: '/gestor/historico', label: 'Histórico', icon: <History className="w-4 h-4" /> },
  ],
};

// Disponível para todos os perfis
const NAV_COMUM: NavItem[] = [
  { href: '/consumo-semanal', label: 'Consumo Semanal', icon: <TrendingUp className="w-4 h-4" /> },
];

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
        {/* Logo + Wordmark */}
        <Link href="/" className="flex items-center space-x-3 flex-shrink-0">
          <div className="bg-white text-[#1F6D2E] p-2 rounded-lg font-black text-xl tracking-wider flex items-center justify-center w-10 h-10 shadow">
            N
          </div>
          <div>
            <h1 className="font-bold text-lg leading-tight tracking-wide">NORMATEL</h1>
            <p className="text-xs text-green-200 font-medium tracking-wider">ENGENHARIA</p>
          </div>
        </Link>

        {/* Navegação (telas grandes) */}
        <nav className="hidden lg:flex items-center space-x-1">
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

        {/* Perfil do Usuário, Logout e botão do menu */}
        <div className="flex items-center gap-1 sm:gap-3">
          <div className="text-right hidden sm:block">
            <p className="text-sm font-semibold leading-tight max-w-[160px] truncate">{user.nome}</p>
            <span className="inline-block text-[10px] font-bold uppercase tracking-wider bg-emerald-900/60 px-2 py-0.5 rounded text-green-200">
              {user.role}
            </span>
          </div>

          <button
            onClick={logout}
            className="hidden lg:flex p-2 rounded-md text-green-100 hover:bg-[#175423] transition items-center gap-1 text-sm font-medium"
            title="Sair do sistema"
          >
            <LogOut className="w-4 h-4" />
            <span>Sair</span>
          </button>

          <button
            onClick={() => setMenuAberto((v) => !v)}
            className="lg:hidden p-2.5 rounded-md text-white hover:bg-[#175423] transition"
            aria-label={menuAberto ? 'Fechar menu' : 'Abrir menu'}
            aria-expanded={menuAberto}
          >
            {menuAberto ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
          </button>
        </div>
      </div>

      {/* Menu do celular / tablet */}
      {menuAberto && (
        <>
          <div className="lg:hidden fixed inset-0 top-16 bg-black/30" onClick={() => setMenuAberto(false)} />
          <nav className="lg:hidden absolute left-0 right-0 top-16 bg-[#1F6D2E] border-t border-emerald-800 shadow-xl max-h-[calc(100vh-4rem)] overflow-y-auto">
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
