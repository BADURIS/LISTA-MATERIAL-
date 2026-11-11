'use client';

import React, { createContext, useContext, useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';

export interface User {
  id: string;
  nome: string;
  role: 'Supervisor' | 'Almoxarife' | 'Gestor';
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  login: (role: User['role'], username?: string, email?: string, password?: string) => Promise<void>;
  logout: () => void;
  loading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const storedToken = localStorage.getItem('normatel_token');
    const storedUser = localStorage.getItem('normatel_user');

    if (storedToken && storedUser) {
      setToken(storedToken);
      setUser(JSON.parse(storedUser));
    }
    setLoading(false);
  }, []);

  const login = async (role: User['role'], username?: string, email?: string, password?: string) => {
    try {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL || ''}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ role, username: username || `Usuário ${role}`, email, password }),
      });

      const data = await res.json();
      if (res.ok && data.token) {
        localStorage.setItem('normatel_token', data.token);
        localStorage.setItem('normatel_user', JSON.stringify(data.user));
        setToken(data.token);
        setUser(data.user);

        // Redirecionamento por papel
        switch (role) {
          case 'Supervisor':
            router.push('/supervisor/nova-semana');
            break;
          case 'Almoxarife':
            router.push('/almoxarife/semana-atual');
            break;
          case 'Gestor':
            router.push('/gestor/consumo');
            break;
        }
      } else {
        alert(data.message || 'Erro ao realizar login');
      }
    } catch (err: any) {
      alert('Erro de conexão com o servidor API. Verifique se o backend está rodando.');
    }
  };

  const logout = () => {
    localStorage.removeItem('normatel_token');
    localStorage.removeItem('normatel_user');
    setToken(null);
    setUser(null);
    router.push('/login');
  };

  return (
    <AuthContext.Provider value={{ user, token, login, logout, loading }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth deve ser usado dentro de um AuthProvider');
  }
  return context;
}
