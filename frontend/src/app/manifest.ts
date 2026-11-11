import type { MetadataRoute } from 'next';

// Permite "Adicionar à tela inicial" no celular e abrir como aplicativo
export default function manifest(): MetadataRoute.Manifest {
  return {
    name: 'Normatel — Listas de Material',
    short_name: 'Normatel',
    description: 'Gestão e controle semanal de materiais por local.',
    start_url: '/',
    display: 'standalone',
    background_color: '#f8fafc',
    theme_color: '#1F6D2E',
    lang: 'pt-BR',
    icons: [{ src: '/icon.svg', sizes: 'any', type: 'image/svg+xml', purpose: 'any' }],
  };
}
