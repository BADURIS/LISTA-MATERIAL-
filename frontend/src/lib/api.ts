// Vazio = mesmo endereço do site (as chamadas passam pelo proxy configurado em next.config.js)
const API_BASE = process.env.NEXT_PUBLIC_API_URL || '';

const CHAVE_ACCESS = 'lm_access_token';
const CHAVE_REFRESH = 'lm_refresh_token';
const CHAVE_USUARIO = 'lm_usuario';

export type Perfil = 'Supervisor' | 'Coordenador' | 'Almoxarife' | 'Gestor';

export interface Usuario {
  username: string;
  nome: string;
  role: Perfil;
}

interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresInSeconds: number;
  username: string;
  nomeCompleto: string;
  role: Perfil;
}

export class ApiError extends Error {
  constructor(message: string, public status: number) {
    super(message);
  }
}

// ---------- Sessão ----------

export function lerSessao(): Usuario | null {
  if (typeof window === 'undefined') return null;
  const bruto = localStorage.getItem(CHAVE_USUARIO);
  if (!bruto || !localStorage.getItem(CHAVE_ACCESS)) return null;
  try {
    return JSON.parse(bruto) as Usuario;
  } catch {
    return null;
  }
}

function salvarSessao(r: LoginResponse): Usuario {
  const usuario: Usuario = { username: r.username, nome: r.nomeCompleto, role: r.role };
  localStorage.setItem(CHAVE_ACCESS, r.accessToken);
  localStorage.setItem(CHAVE_REFRESH, r.refreshToken);
  localStorage.setItem(CHAVE_USUARIO, JSON.stringify(usuario));
  return usuario;
}

export function limparSessao() {
  localStorage.removeItem(CHAVE_ACCESS);
  localStorage.removeItem(CHAVE_REFRESH);
  localStorage.removeItem(CHAVE_USUARIO);
}

async function lerMensagemErro(response: Response): Promise<string> {
  try {
    const dados = await response.json();
    const detalhe = Array.isArray(dados?.errors) && dados.errors[0]?.message ? ` ${dados.errors[0].message}` : '';
    return (dados?.message || `Erro ${response.status}`) + detalhe;
  } catch {
    return response.status === 403 ? 'Seu perfil não tem permissão para esta ação.' : `Erro ${response.status}`;
  }
}

export async function login(username: string, password: string): Promise<Usuario> {
  const response = await fetch(`${API_BASE}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
  });
  if (!response.ok) {
    if (response.status === 429) throw new ApiError('Muitas tentativas de login. Aguarde um minuto e tente novamente.', 429);
    throw new ApiError(await lerMensagemErro(response), response.status);
  }
  return salvarSessao(await response.json());
}

export async function logout() {
  const token = localStorage.getItem(CHAVE_ACCESS);
  if (token) {
    // Revoga o refresh token no servidor; se falhar, a sessão local é encerrada mesmo assim
    await fetch(`${API_BASE}/api/auth/revoke`, { method: 'POST', headers: { Authorization: `Bearer ${token}` } }).catch(() => {});
  }
  limparSessao();
}

// O access token dura poucos minutos: ao receber 401 tenta renovar uma única vez (compartilhado entre chamadas simultâneas)
let renovacaoEmAndamento: Promise<boolean> | null = null;

function renovarSessao(): Promise<boolean> {
  if (!renovacaoEmAndamento) {
    renovacaoEmAndamento = (async () => {
      const accessToken = localStorage.getItem(CHAVE_ACCESS);
      const refreshToken = localStorage.getItem(CHAVE_REFRESH);
      if (!accessToken || !refreshToken) return false;
      try {
        const response = await fetch(`${API_BASE}/api/auth/refresh-token`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ accessToken, refreshToken }),
        });
        if (!response.ok) return false;
        salvarSessao(await response.json());
        return true;
      } catch {
        return false;
      }
    })().finally(() => {
      renovacaoEmAndamento = null;
    });
  }
  return renovacaoEmAndamento;
}

function sessaoExpirada(): never {
  limparSessao();
  if (typeof window !== 'undefined' && window.location.pathname !== '/login') {
    window.location.href = '/login?expirou=1';
  }
  throw new ApiError('Sua sessão expirou. Entre novamente.', 401);
}

async function requisicao(endpoint: string, options: RequestInit, tentarRenovar = true): Promise<Response> {
  const token = localStorage.getItem(CHAVE_ACCESS);
  const headers: Record<string, string> = {
    ...(options.body ? { 'Content-Type': 'application/json' } : {}),
    ...(options.headers as Record<string, string>),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };

  const response = await fetch(`${API_BASE}${endpoint}`, { ...options, headers });

  if (response.status === 401) {
    if (tentarRenovar && (await renovarSessao())) return requisicao(endpoint, options, false);
    sessaoExpirada();
  }

  if (!response.ok) throw new ApiError(await lerMensagemErro(response), response.status);
  return response;
}

export async function fetchApi<T = unknown>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const response = await requisicao(endpoint, options);
  if (response.status === 204) return null as T;
  return (await response.json()) as T;
}

// Igual a fetchApi, mas devolve null quando o recurso não existe (ex.: nenhuma semana em andamento)
export async function fetchApiOuNulo<T = unknown>(endpoint: string): Promise<T | null> {
  try {
    return await fetchApi<T>(endpoint);
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) return null;
    throw err;
  }
}

// Baixa um arquivo (planilhas) usando o nome enviado pelo servidor
export async function baixarArquivo(endpoint: string, nomePadrao: string) {
  const response = await requisicao(endpoint, {});
  const disposicao = response.headers.get('content-disposition') || '';
  const nome =
    /filename\*=UTF-8''([^;]+)/i.exec(disposicao)?.[1] ?? /filename="?([^";]+)"?/i.exec(disposicao)?.[1] ?? nomePadrao;

  const url = URL.createObjectURL(await response.blob());
  const link = document.createElement('a');
  link.href = url;
  link.download = decodeURIComponent(nome);
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

// Lista paginada da API (materiais e locais): busca a primeira página com o tamanho máximo
export async function listarTodos<T>(endpoint: string): Promise<T[]> {
  const separador = endpoint.includes('?') ? '&' : '?';
  const pagina = await fetchApi<{ items: T[] }>(`${endpoint}${separador}pageSize=100`);
  return pagina.items;
}
