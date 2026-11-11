'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth, PAGINA_INICIAL } from '@/context/AuthContext';
import { Carregando } from '@/components/ui';

export default function HomePage() {
  const { user, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!loading) router.replace(user ? PAGINA_INICIAL[user.role] : '/login');
  }, [user, loading, router]);

  return <Carregando />;
}
