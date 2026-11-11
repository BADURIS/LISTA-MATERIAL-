'use client';

import React, { useState } from 'react';
import { useAuth, User } from '@/context/AuthContext';
import { ShieldCheck, Package, ClipboardCheck, BarChart3, Lock, Mail, User as UserIcon } from 'lucide-react';

export default function LoginPage() {
  const { login } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [nome, setNome] = useState('');
  const [selectedRole, setSelectedRole] = useState<User['role']>('Supervisor');

  const handleFormLogin = (e: React.FormEvent) => {
    e.preventDefault();
    login(selectedRole, nome || email.split('@')[0] || `Usuário ${selectedRole}`, email, password);
  };

  const preencherDemo = (role: User['role']) => {
    const demoEmails: Record<User['role'], string> = {
      Supervisor: 'supervisor@normatel.com.br',
      Almoxarife: 'almoxarife@normatel.com.br',
      Gestor: 'gestor@normatel.com.br',
    };
    const demoNomes: Record<User['role'], string> = {
      Supervisor: 'Carlos Silva (Supervisor)',
      Almoxarife: 'João Almoxarife',
      Gestor: 'Roberto Gestor',
    };

    setEmail(demoEmails[role]);
    setPassword('123456');
    setNome(demoNomes[role]);
    setSelectedRole(role);
    login(role, demoNomes[role], demoEmails[role], '123456');
  };

  const roles: { role: User['role']; title: string; desc: string; icon: React.ReactNode; badgeColor: string }[] = [
    {
      role: 'Supervisor',
      title: 'Supervisor',
      desc: 'Cria semanas, preenche solicitações de materiais por local e envia para separação.',
      icon: <Package className="w-6 h-6 text-[#1F6D2E]" />,
      badgeColor: 'bg-emerald-100 text-[#173404] border-emerald-300',
    },
    {
      role: 'Almoxarife',
      title: 'Almoxarife',
      desc: 'Informa a quantidade separada de cada item, aponta faltas, confirma listas e registra retiradas.',
      icon: <ClipboardCheck className="w-6 h-6 text-blue-700" />,
      badgeColor: 'bg-blue-100 text-blue-900 border-blue-300',
    },
    {
      role: 'Gestor',
      title: 'Gestor',
      desc: 'Somente consulta: acompanha listas, consumo e histórico e exporta as tabelas em Excel/PDF.',
      icon: <BarChart3 className="w-6 h-6 text-purple-700" />,
      badgeColor: 'bg-purple-100 text-purple-900 border-purple-300',
    },
  ];

  return (
    <div className="min-h-[85vh] flex flex-col items-center justify-center py-8 px-4 sm:px-6 lg:px-8">
      {/* Header Branding */}
      <div className="max-w-md w-full text-center mb-6">
        <div className="mx-auto w-16 h-16 bg-[#1F6D2E] text-white rounded-2xl flex items-center justify-center text-3xl font-black shadow-lg mb-3">
          N
        </div>
        <h2 className="text-3xl font-extrabold text-slate-900 tracking-tight">NORMATEL ENGENHARIA</h2>
        <p className="mt-1 text-sm text-slate-600 font-medium">
          Gestão de Listas de Material Semanais
        </p>
      </div>

      <div className="bg-white p-8 rounded-2xl shadow-xl border border-slate-200 w-full max-w-2xl space-y-6">
        
        {/* Form de Login com Credenciais */}
        <form onSubmit={handleFormLogin} className="space-y-4 border-b border-slate-100 pb-6">
          <div className="flex items-center justify-between">
            <h3 className="text-base font-bold text-slate-900 flex items-center gap-2">
              <ShieldCheck className="w-5 h-5 text-[#1F6D2E]" />
              Login com Credenciais de Acesso
            </h3>
            <span className="text-xs font-semibold text-slate-400">Campos Obrigatórios</span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                E-mail de Acesso
              </label>
              <div className="relative">
                <Mail className="w-4 h-4 text-slate-400 absolute left-3 top-3" />
                <input
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="supervisor@normatel.com.br"
                  className="w-full pl-9 pr-4 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm font-medium outline-none focus:ring-2 focus:ring-[#1F6D2E]"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Senha
              </label>
              <div className="relative">
                <Lock className="w-4 h-4 text-slate-400 absolute left-3 top-3" />
                <input
                  type="password"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="w-full pl-9 pr-4 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm font-medium outline-none focus:ring-2 focus:ring-[#1F6D2E]"
                />
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-1">
            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Nome do Usuário (Opcional)
              </label>
              <div className="relative">
                <UserIcon className="w-4 h-4 text-slate-400 absolute left-3 top-3" />
                <input
                  type="text"
                  value={nome}
                  onChange={(e) => setNome(e.target.value)}
                  placeholder="Ex: Carlos Silva"
                  className="w-full pl-9 pr-4 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm font-medium outline-none focus:ring-2 focus:ring-[#1F6D2E]"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 mb-1">
                Papel / Perfil
              </label>
              <select
                value={selectedRole}
                onChange={(e: any) => setSelectedRole(e.target.value)}
                className="w-full px-3 py-2 bg-slate-50 border border-slate-300 rounded-lg text-sm font-semibold outline-none focus:ring-2 focus:ring-[#1F6D2E]"
              >
                <option value="Supervisor">Supervisor</option>
                <option value="Almoxarife">Almoxarife</option>
                <option value="Gestor">Gestor</option>
              </select>
            </div>
          </div>

          <button
            type="submit"
            className="w-full py-3 bg-[#1F6D2E] hover:bg-[#175423] text-white font-bold text-sm rounded-xl transition shadow-md flex items-center justify-center gap-2 mt-2"
          >
            Entrar no Sistema &rarr;
          </button>
        </form>

        {/* Seleção Rápida de Papel para Testes */}
        <div>
          <p className="text-left text-xs font-bold text-slate-400 uppercase tracking-wider mb-3">
            Ou selecione um perfil de teste pré-configurado
          </p>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            {roles.map((item) => (
              <button
                key={item.role}
                type="button"
                onClick={() => preencherDemo(item.role)}
                className="text-left p-4 rounded-xl border border-slate-200 hover:border-[#1F6D2E] hover:shadow-md transition-all duration-200 bg-white hover:bg-slate-50 flex flex-col justify-between group"
              >
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <div className="p-2 rounded-lg bg-slate-100 group-hover:bg-white transition shadow-xs">
                      {item.icon}
                    </div>
                    <span className={`text-[10px] font-bold px-2 py-0.5 rounded-full border ${item.badgeColor}`}>
                      {item.title}
                    </span>
                  </div>
                  <h3 className="font-bold text-sm text-slate-900 group-hover:text-[#1F6D2E] transition">
                    {item.title}
                  </h3>
                  <p className="text-xs text-slate-500 mt-1 leading-relaxed line-clamp-2">
                    {item.desc}
                  </p>
                </div>
                <div className="mt-3 pt-2 border-t border-slate-100 text-[11px] font-semibold text-[#1F6D2E] flex items-center justify-end gap-1">
                  Entrar como {item.title} &rarr;
                </div>
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
