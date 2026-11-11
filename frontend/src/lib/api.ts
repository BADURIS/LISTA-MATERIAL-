// Vazio = mesmo endereço do site (as chamadas passam pelo proxy configurado em next.config.js)
const API_BASE = process.env.NEXT_PUBLIC_API_URL || '';

export async function fetchApi<T = any>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const token = typeof window !== 'undefined' ? localStorage.getItem('normatel_token') : null;

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    let errorData = { message: 'Erro na requisição.' };
    try {
      errorData = await response.json();
    } catch (_) {}
    throw new Error(errorData.message || `Erro ${response.status}`);
  }

  // Se o tipo do retorno for arquivo blob/arraybuffer (exportações)
  const contentType = response.headers.get('content-type');
  if (contentType && (contentType.includes('application/pdf') || contentType.includes('spreadsheetml') || contentType.includes('octet-stream'))) {
    const blob = await response.blob();
    return blob as any;
  }

  return response.json();
}
