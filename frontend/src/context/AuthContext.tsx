'use client';

import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import { lerSessao, login as loginApi, logout as logoutApi, Perfil, Usuario } from '@/lib/api';

export type { Perfil, Usuario };

// Página inicial de cada perfil
export const PAGINA_INICIAL: Record<Perfil, string> = {
  Supervisor: '/supervisor/nova-semana',
  Coordenador: '/coordenador/retirada',
  Almoxarife: '/almoxarife/semana-atual',
  Gestor: '/gestor/consumo',
};

interface AuthContextType {
  user: Usuario | null;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  loading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<Usuario | null>(null);
  const [loading, setLoading] = useState(true);
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    setUser(lerSessao());
    setLoading(false);
  }, []);

  // Sem sessão, qualquer página leva ao login
  useEffect(() => {
    if (!loading && !user && pathname !== '/login') {
      router.replace('/login');
    }
  }, [loading, user, pathname, router]);

  const login = useCallback(
    async (username: string, password: string) => {
      const usuario = await loginApi(username.trim(), password);
      setUser(usuario);
      router.push(PAGINA_INICIAL[usuario.role] ?? '/');
    },
    [router]
  );

  const logout = useCallback(async () => {
    await logoutApi();
    setUser(null);
    router.push('/login');
  }, [router]);

  return <AuthContext.Provider value={{ user, login, logout, loading }}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth deve ser usado dentro de um AuthProvider');
  }
  return context;
}
