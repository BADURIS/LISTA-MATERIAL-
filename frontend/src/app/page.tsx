'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/context/AuthContext';

export default function HomePage() {
  const { user, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!loading) {
      if (!user) {
        router.push('/login');
      } else {
        switch (user.role) {
          case 'Supervisor':
            router.push('/supervisor/nova-semana');
            break;
          case 'Almoxarife':
            router.push('/almoxarife/semana-atual');
            break;
          case 'Gestor':
            router.push('/gestor/consumo');
            break;
          default:
            router.push('/login');
        }
      }
    }
  }, [user, loading, router]);

  return (
    <div className="flex items-center justify-center min-h-[60vh]">
      <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-[#1F6D2E]"></div>
    </div>
  );
}
