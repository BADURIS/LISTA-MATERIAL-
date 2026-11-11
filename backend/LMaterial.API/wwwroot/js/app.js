/**
 * LMaterial — Single Page Application (SPA) Client
 *
 * Perfis: Supervisor (monta e envia a semana), Coordenador (preenche itens, confirma recebimento e registra retirada),
 * Almoxarife (registra a separação), Gestor (consulta totais e exporta a planilha).
 */

const API_BASE = '/api';
const SESSION_KEY = 'lmaterial.sessao';
const INTERVALO_NOTIFICACOES_MS = 60000;

// Cache em Memória
let state = {
  sessao: null,
  semanaAtual: null,
  materiais: [],
  locais: [],
  notificacoes: []
};

let notificacoesTimer = null;
let renovacaoEmAndamento = null;

// Inicialização da Aplicação
document.addEventListener('DOMContentLoaded', () => {
  initLogin();
  initTabs();
  initFormDates();
  initEventListeners();

  state.sessao = lerSessao();
  if (state.sessao) {
    iniciarAplicacao();
  } else {
    mostrarLogin();
  }
});

// --------------------------------------------------------------------------
// Sessão e Autenticação
// --------------------------------------------------------------------------
function lerSessao() {
  try {
    return JSON.parse(sessionStorage.getItem(SESSION_KEY));
  } catch (_) {
    return null;
  }
}

function salvarSessao(sessao) {
  try {
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(sessao));
  } catch (_) {
    // Sem armazenamento disponível: a sessão fica apenas em memória
  }
}

function limparSessao() {
  try {
    sessionStorage.removeItem(SESSION_KEY);
  } catch (_) {
    // Nada a limpar
  }
}

function montarSessao(dados) {
  return {
    accessToken: dados.accessToken,
    refreshToken: dados.refreshToken,
    username: dados.username,
    nomeCompleto: dados.nomeCompleto,
    role: dados.role
  };
}

function cabecalhoAutorizacao() {
  return state.sessao ? { Authorization: `Bearer ${state.sessao.accessToken}` } : {};
}

function initLogin() {
  document.getElementById('formLogin')?.addEventListener('submit', handleLogin);
}

async function handleLogin(e) {
  e.preventDefault();

  const username = document.getElementById('loginUsuario').value.trim();
  const password = document.getElementById('loginSenha').value;
  const erro = document.getElementById('loginErro');
  erro.textContent = '';

  try {
    const response = await fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });

    if (!response.ok) {
      erro.textContent = 'Usuário ou senha inválidos.';
      return;
    }

    state.sessao = montarSessao(await response.json());
    salvarSessao(state.sessao);
    document.getElementById('formLogin').reset();
    iniciarAplicacao();
  } catch (_) {
    erro.textContent = 'Não foi possível conectar à API.';
  }
}

async function handleSair() {
  if (state.sessao) {
    try {
      await fetch(`${API_BASE}/auth/revoke`, {
        method: 'POST',
        headers: cabecalhoAutorizacao()
      });
    } catch (_) {
      // Mesmo sem resposta da API, a sessão local é encerrada
    }
  }

  encerrarSessao();
}

function encerrarSessao() {
  limparSessao();
  state.sessao = null;
  state.notificacoes = [];
  clearInterval(notificacoesTimer);
  notificacoesTimer = null;
  mostrarLogin();
}

function mostrarLogin() {
  document.getElementById('loginScreen')?.classList.remove('hidden');
  document.getElementById('loginUsuario')?.focus();
}

// Renova o access token com o refresh token. Chamadas simultâneas compartilham a mesma renovação,
// pois cada refresh token é rotacionado e só pode ser usado uma vez.
function renovarToken() {
  if (!renovacaoEmAndamento) {
    renovacaoEmAndamento = executarRenovacao().finally(() => {
      renovacaoEmAndamento = null;
    });
  }
  return renovacaoEmAndamento;
}

async function executarRenovacao() {
  if (!state.sessao?.refreshToken) return false;

  try {
    const response = await fetch(`${API_BASE}/auth/refresh-token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        accessToken: state.sessao.accessToken,
        refreshToken: state.sessao.refreshToken
      })
    });

    if (!response.ok) return false;

    state.sessao = montarSessao(await response.json());
    salvarSessao(state.sessao);
    return true;
  } catch (_) {
    return false;
  }
}

function iniciarAplicacao() {
  document.getElementById('loginScreen')?.classList.add('hidden');

  const { nomeCompleto, role } = state.sessao;
  document.getElementById('usuarioInfo').innerHTML = `<strong>${escapeHtml(nomeCompleto)}</strong> · ${escapeHtml(role)}`;

  aplicarPermissoes(role);
  loadAllData();

  if (role === 'Gestor') {
    loadHistoricoGestor();
  }

  carregarNotificacoes();
  clearInterval(notificacoesTimer);
  notificacoesTimer = setInterval(carregarNotificacoes, INTERVALO_NOTIFICACOES_MS);
}

// Mostra apenas as abas, seções e botões liberados para o perfil logado
function aplicarPermissoes(role) {
  document.querySelectorAll('[data-perfis]').forEach(el => {
    const perfis = el.dataset.perfis.split(',').map(p => p.trim());
    el.classList.toggle('perfil-oculto', !perfis.includes(role));
  });

  const abaAtiva = document.querySelector('.nav-tab.active');
  if (!abaAtiva || abaAtiva.classList.contains('perfil-oculto')) {
    const primeiraVisivel = document.querySelector('.nav-tab:not(.perfil-oculto)');
    primeiraVisivel?.click();
  }
}

// --------------------------------------------------------------------------
// Navegação por Abas
// --------------------------------------------------------------------------
function initTabs() {
  const tabs = document.querySelectorAll('.nav-tab');
  tabs.forEach(tab => {
    tab.addEventListener('click', () => {
      tabs.forEach(t => t.classList.remove('active'));
      document.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));

      tab.classList.add('active');
      const targetPanel = document.getElementById(tab.dataset.tab);
      if (targetPanel) targetPanel.classList.add('active');
    });
  });
}

function initFormDates() {
  const today = new Date().toISOString().split('T')[0];
  const nextWeek = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];

  const dataInicioInput = document.getElementById('inputDataInicio');
  const dataFimInput = document.getElementById('inputDataFim');

  if (dataInicioInput) dataInicioInput.value = today;
  if (dataFimInput) dataFimInput.value = nextWeek;
}

// --------------------------------------------------------------------------
// Event Listeners
// --------------------------------------------------------------------------
function initEventListeners() {
  document.getElementById('btnSair')?.addEventListener('click', handleSair);
  document.getElementById('btnRefreshDashboard')?.addEventListener('click', loadAllData);

  document.getElementById('btnNotificacoes')?.addEventListener('click', () => {
    document.getElementById('painelNotificacoes').classList.toggle('hidden');
  });

  // Forms
  document.getElementById('formCriarSemana')?.addEventListener('submit', handleCriarSemana);
  document.getElementById('formAdicionarLocal')?.addEventListener('submit', handleAdicionarLocal);
  document.getElementById('formAdicionarItem')?.addEventListener('submit', handleAdicionarItem);
  document.getElementById('btnEnviarSeparacao')?.addEventListener('click', handleEnviarSeparacao);

  // Gestor
  document.getElementById('btnCarregarResumo')?.addEventListener('click', handleCarregarResumo);
  document.getElementById('btnExportarPlanilha')?.addEventListener('click', handleExportarPlanilha);

  // Forms Catálogo
  document.getElementById('btnToggleNovoMaterial')?.addEventListener('click', () => {
    document.getElementById('formNovoMaterial').classList.toggle('hidden');
  });
  document.getElementById('btnToggleNovoLocal')?.addEventListener('click', () => {
    document.getElementById('formNovoLocal').classList.toggle('hidden');
  });

  document.getElementById('formNovoMaterial')?.addEventListener('submit', handleNovoMaterial);
  document.getElementById('formNovoLocal')?.addEventListener('submit', handleNovoLocal);

  // Dynamic Event for Local change in Item Form
  document.getElementById('selectSemanaParaItem')?.addEventListener('change', updateSelectLocaisDoItem);
}

// --------------------------------------------------------------------------
// Comunicação com a API REST
// --------------------------------------------------------------------------
async function fetchAPI(endpoint, options = {}, opcoes = {}) {
  const executar = () => fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
      ...cabecalhoAutorizacao()
    }
  });

  try {
    let response = await executar();

    // Access token expirado: renova uma vez e repete a requisição
    if (response.status === 401 && await renovarToken()) {
      response = await executar();
    }

    if (response.status === 401) {
      encerrarSessao();
      showToast('Sua sessão expirou. Entre novamente.', 'error');
      throw new Error('Sessão expirada.');
    }

    if (response.status === 403) {
      const msg = 'Seu perfil não tem permissão para esta ação.';
      if (!opcoes.silencioso) showToast(msg, 'error');
      throw new Error(msg);
    }

    if (response.status === 404) {
      return null;
    }

    if (response.status === 204) {
      return null;
    }

    const data = await response.json();

    if (!response.ok) {
      const errorMsg = data.message || data.title || 'Ocorreu um erro na requisição.';
      if (!opcoes.silencioso) showToast(errorMsg, 'error');
      throw new Error(errorMsg);
    }

    return data;
  } catch (err) {
    console.error(`Erro na API [${endpoint}]:`, err);
    throw err;
  }
}

// --------------------------------------------------------------------------
// Carga de Dados Inicial
// --------------------------------------------------------------------------
async function loadAllData() {
  if (!state.sessao) return;

  try {
    await Promise.all([
      loadMateriais(),
      loadLocais(),
      loadSemanaAtual()
    ]);
  } catch (err) {
    console.error('Erro ao carregar dados:', err);
  }
}

async function loadMateriais() {
  // A API pagina a resposta; pageSize=100 cobre todo o cadastro
  const pagina = await fetchAPI('/materiais?pageSize=100');
  const materiais = pagina?.items ?? [];
  if (materiais) {
    state.materiais = materiais;
    renderTableMateriais(materiais);
    populateSelectMateriais(materiais);
  }
}

async function loadLocais() {
  const pagina = await fetchAPI('/locais?pageSize=100');
  const locais = pagina?.items ?? [];
  if (locais) {
    state.locais = locais;
    renderTableLocais(locais);
    populateSelectLocais(locais);
  }
}

async function loadSemanaAtual() {
  const semana = await fetchAPI('/semanas-lista/atual');
  state.semanaAtual = semana;
  renderDashboard(semana);
  renderSeparacao(semana);
  renderRetirada(semana);
  updateSelectSemanas(semana);
}

async function loadHistoricoGestor() {
  const semanas = await fetchAPI('/semanas-lista/historico');
  const select = document.getElementById('selectSemanaGestor');
  if (!select) return;

  if (!semanas || semanas.length === 0) {
    select.innerHTML = '<option value="">Nenhuma semana registrada</option>';
    return;
  }

  select.innerHTML = '<option value="">Selecione a semana...</option>' +
    semanas.map(s => `<option value="${s.id}">${formatarPeriodo(s.dataInicio, s.dataFim)} [${s.status}]</option>`).join('');
}

// --------------------------------------------------------------------------
// Notificações (dentro do sistema)
// --------------------------------------------------------------------------
async function carregarNotificacoes() {
  if (!state.sessao) return;

  try {
    const notificacoes = await fetchAPI('/notificacoes', {}, { silencioso: true });
    state.notificacoes = notificacoes || [];
    renderNotificacoes();
  } catch (_) {
    // Falha silenciosa: a próxima atualização tenta novamente
  }
}

function renderNotificacoes() {
  const naoLidas = state.notificacoes.filter(n => !n.lida).length;

  const badge = document.getElementById('badgeNotificacoes');
  badge.textContent = naoLidas;
  badge.classList.toggle('hidden', naoLidas === 0);

  const lista = document.getElementById('listaNotificacoes');
  document.getElementById('notificacoesVazio').classList.toggle('hidden', state.notificacoes.length > 0);

  lista.innerHTML = state.notificacoes.map(n => `
    <button class="notificacao-item ${n.lida ? '' : 'nao-lida'}" onclick="handleMarcarNotificacaoLida('${n.id}')">
      ${escapeHtml(n.mensagem)}
      <small>${new Date(n.dataCriacao).toLocaleString('pt-BR')}</small>
    </button>
  `).join('');
}

async function handleMarcarNotificacaoLida(notificacaoId) {
  try {
    await fetchAPI(`/notificacoes/${notificacaoId}/lida`, { method: 'POST' }, { silencioso: true });
  } catch (_) {
    return;
  }
  carregarNotificacoes();
}

// --------------------------------------------------------------------------
// Renderizadores de Interface
// --------------------------------------------------------------------------

function renderDashboard(semana) {
  const container = document.getElementById('semanaAtualContainer');
  if (!container) return;

  if (!semana) {
    container.innerHTML = `
      <div class="glass-card text-center padding-30">
        <i class="ri-calendar-close-line" style="font-size: 48px; color: var(--text-muted);"></i>
        <h3 class="margin-top">Nenhuma Semana de Lista Ativa</h3>
        <p class="panel-subtitle">Uma nova semana precisa ser criada pelo Supervisor para começar o ciclo de suprimentos.</p>
      </div>
    `;
    return;
  }

  const badgeClass = getBadgeClassStatusSemana(semana.status);

  let locaisHtml = semana.locais.map(loc => `
    <div class="lista-local-card">
      <div class="lista-local-header">
        <h4><i class="ri-building-line"></i> ${escapeHtml(loc.nomeLocal || 'Local')}</h4>
        <span class="badge ${getBadgeClassStatusLista(loc.status)}">${loc.status}</span>
      </div>
      <div class="items-list">
        ${loc.itens.length === 0 ? '<p class="text-dim">Nenhum item adicionado.</p>' :
          loc.itens.map(item => `
            <div class="item-row">
              <span><strong>${escapeHtml(item.nomeMaterial || 'Material')}</strong> (${item.unidade})</span>
              <span>Solicitado: ${item.quantidadeSolicitada} | Separado: ${item.quantidadeSeparada ?? '-'}</span>
              <span class="badge ${item.statusItem === 'OK' ? 'badge-confirmado' : 'badge-pendente'}">${item.statusItem}</span>
            </div>
          `).join('')
        }
      </div>
    </div>
  `).join('');

  container.innerHTML = `
    <div class="glass-card">
      <div class="card-header space-between">
        <div>
          <h3>Semana ${formatarPeriodo(semana.dataInicio, semana.dataFim)}</h3>
          <p class="panel-subtitle">Criado em: ${new Date(semana.dataCriacao).toLocaleString('pt-BR')}</p>
        </div>
        <span class="badge ${badgeClass}">${semana.status}</span>
      </div>

      <h4 class="margin-top">Locais e Insumos (${semana.locais.length})</h4>
      ${semana.locais.length === 0 ? '<p class="panel-subtitle margin-top">Nenhum local vinculado a esta semana ainda.</p>' : locaisHtml}
    </div>
  `;
}

function renderSeparacao(semana) {
  const container = document.getElementById('separacaoContainer');
  if (!container) return;

  if (!semana || semana.status !== 'EnviadaParaSeparacao') {
    container.innerHTML = `
      <div class="glass-card text-center padding-30">
        <i class="ri-inbox-line" style="font-size: 48px; color: var(--text-muted);"></i>
        <h3 class="margin-top">Nenhuma Lista Pendente para Separação</h3>
        <p class="panel-subtitle">As listas aparecem aqui assim que o Supervisor enviar a semana para separação.</p>
      </div>
    `;
    return;
  }

  let html = semana.locais.map(loc => `
    <div class="glass-card margin-bottom">
      <div class="card-header space-between">
        <h3><i class="ri-map-pin-line"></i> ${escapeHtml(loc.nomeLocal)}</h3>
        <span class="badge ${getBadgeClassStatusLista(loc.status)}">${loc.status}</span>
      </div>

      <div class="table-responsive">
        <table class="data-table">
          <thead>
            <tr>
              <th>Material</th>
              <th>Solicitado</th>
              <th>Separado</th>
              <th>Observação</th>
              <th>Ação</th>
            </tr>
          </thead>
          <tbody>
            ${loc.itens.map(item => `
              <tr>
                <td><strong>${escapeHtml(item.nomeMaterial)}</strong> (${item.unidade})</td>
                <td>${item.quantidadeSolicitada}</td>
                <td>
                  <input type="number" id="separado_${item.id}" class="form-input" style="width: 80px;" min="0" value="${item.quantidadeSeparada ?? item.quantidadeSolicitada}">
                </td>
                <td>
                  <input type="text" id="obs_${item.id}" class="form-input" placeholder="Opcional..." value="${escapeHtml(item.observacao ?? '')}">
                </td>
                <td>
                  <button class="btn btn-sm btn-primary" onclick="handleRegistrarSeparacaoItem('${semana.id}', '${item.id}')">
                    <i class="ri-check-line"></i> Salvar
                  </button>
                </td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </div>
    </div>
  `).join('');

  container.innerHTML = html;
}

function renderRetirada(semana) {
  const container = document.getElementById('retiradaContainer');
  if (!container) return;

  if (!semana || semana.status === 'EmMontagem') {
    container.innerHTML = `<div class="glass-card text-center"><p>Nenhuma semana em separação ou retirada no momento.</p></div>`;
    return;
  }

  let html = semana.locais.map(loc => {
    const retirado = loc.status === 'Retirado';
    const podeConfirmar = loc.status === 'Separado';
    const podeRetirar = loc.status === 'Confirmado' || loc.status === 'ConfirmadoComPendencia';

    return `
      <div class="glass-card margin-bottom">
        <div class="card-header space-between">
          <h3><i class="ri-building-4-line"></i> ${escapeHtml(loc.nomeLocal)}</h3>
          <span class="badge ${getBadgeClassStatusLista(loc.status)}">${loc.status}</span>
        </div>

        ${retirado ? renderAssinatura(loc) : ''}

        ${podeConfirmar ? `
          <div class="form-group margin-top">
            <label>Confirmação de Recebimento</label>
            <div class="form-actions">
              <button class="btn btn-sm btn-success" onclick="handleConfirmarRecebimento('${semana.id}', '${loc.id}', false)">
                <i class="ri-check-double-line"></i> Confirmar Sem Pendências
              </button>
              <button class="btn btn-sm btn-secondary" onclick="handleConfirmarRecebimento('${semana.id}', '${loc.id}', true)">
                <i class="ri-alert-line"></i> Confirmar Com Pendências
              </button>
            </div>
          </div>
        ` : ''}

        ${podeRetirar ? `
          <div class="form-group margin-top">
            <label for="retiradoPor_${loc.id}">Assinatura: nome de quem retirou</label>
            <div class="form-actions">
              <input type="text" id="retiradoPor_${loc.id}" class="form-input" placeholder="ex: João da Silva">
              <button class="btn btn-sm btn-accent" onclick="handleRegistrarRetirada('${semana.id}', '${loc.id}')">
                <i class="ri-hand-coin-line"></i> Registrar Retirada
              </button>
            </div>
          </div>
        ` : ''}
      </div>
    `;
  }).join('');

  container.innerHTML = html;
}

function renderAssinatura(loc) {
  const valida = loc.assinaturaValida;
  return `
    <div class="assinatura-box ${valida ? '' : 'invalida'}">
      <strong><i class="ri-quill-pen-line"></i> Retirado por ${escapeHtml(loc.nomeRetiradoPor ?? '-')}</strong>
      <div>Em ${loc.dataRetirada ? new Date(loc.dataRetirada).toLocaleString('pt-BR') : '-'}</div>
      <div>${valida ? 'Assinatura digital válida' : 'Assinatura digital não confere'}</div>
      ${loc.assinaturaDigital ? `<code>${escapeHtml(loc.assinaturaDigital)}</code>` : ''}
    </div>
  `;
}

// Catálogo Tables
function renderTableMateriais(materiais) {
  const tbody = document.querySelector('#tableMateriais tbody');
  document.getElementById('countMateriais').textContent = materiais.length;

  tbody.innerHTML = materiais.map(m => `
    <tr>
      <td>${m.ordemExibicao}</td>
      <td><strong>${escapeHtml(m.nome)}</strong></td>
      <td><span class="badge badge-pendente">${m.unidade}</span></td>
      <td><span class="badge badge-confirmado">Ativo</span></td>
    </tr>
  `).join('');
}

function renderTableLocais(locais) {
  const tbody = document.querySelector('#tableLocais tbody');
  document.getElementById('countLocais').textContent = locais.length;

  tbody.innerHTML = locais.map(l => `
    <tr>
      <td>${l.ordemExibicao}</td>
      <td><strong>${escapeHtml(l.nome)}</strong></td>
      <td><span class="badge badge-enviado">${l.tipo}</span></td>
      <td><span class="badge ${l.ativo ? 'badge-confirmado' : 'badge-pendente'}">${l.ativo ? 'Ativo' : 'Inativo'}</span></td>
    </tr>
  `).join('');
}

// Gestor: totais por material da semana selecionada
async function handleCarregarResumo() {
  const semanaId = document.getElementById('selectSemanaGestor').value;
  const container = document.getElementById('resumoContainer');

  if (!semanaId) {
    showToast('Selecione uma semana.', 'error');
    return;
  }

  const resumo = await fetchAPI(`/semanas-lista/${semanaId}/resumo`);
  if (!resumo) return;

  const total = resumo.reduce((soma, linha) => soma + linha.saida, 0);

  container.innerHTML = `
    <div class="table-responsive">
      <table class="data-table">
        <thead>
          <tr>
            <th>MATERIAL</th>
            <th>UNIDADE</th>
            <th>SAÍDA</th>
          </tr>
        </thead>
        <tbody>
          ${resumo.map(linha => `
            <tr>
              <td>${escapeHtml(linha.material)}</td>
              <td>${linha.unidade}</td>
              <td>${linha.saida}</td>
            </tr>
          `).join('')}
          <tr>
            <td class="resumo-total">TOTAL</td>
            <td></td>
            <td class="resumo-total">${total}</td>
          </tr>
        </tbody>
      </table>
    </div>
  `;
}

// Gestor: baixa a planilha XLSX da semana selecionada
async function handleExportarPlanilha() {
  const semanaId = document.getElementById('selectSemanaGestor').value;
  if (!semanaId) {
    showToast('Selecione uma semana.', 'error');
    return;
  }

  const baixar = () => fetch(`${API_BASE}/semanas-lista/${semanaId}/planilha`, {
    headers: cabecalhoAutorizacao()
  });

  try {
    let response = await baixar();
    if (response.status === 401 && await renovarToken()) {
      response = await baixar();
    }

    if (!response.ok) {
      showToast(response.status === 403 ? 'Seu perfil não pode exportar planilhas.' : 'Não foi possível gerar a planilha.', 'error');
      return;
    }

    const blob = await response.blob();
    const nomeArquivo = nomeDoArquivo(response.headers.get('Content-Disposition')) || 'semana-lista.xlsx';

    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = nomeArquivo;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  } catch (err) {
    console.error('Erro ao exportar planilha:', err);
    showToast('Não foi possível gerar a planilha.', 'error');
  }
}

function nomeDoArquivo(contentDisposition) {
  const match = /filename="?([^";]+)"?/i.exec(contentDisposition || '');
  return match ? match[1] : null;
}

// Populate Selects
function populateSelectMateriais(materiais) {
  const select = document.getElementById('selectMaterialDoItem');
  if (!select) return;

  select.innerHTML = '<option value="">Selecione o material...</option>' +
    materiais.map(m => `<option value="${m.id}">${escapeHtml(m.nome)} (${m.unidade})</option>`).join('');
}

function populateSelectLocais(locais) {
  const select1 = document.getElementById('selectLocalAdicionar');
  if (select1) {
    select1.innerHTML = '<option value="">Selecione o local...</option>' +
      locais.map(l => `<option value="${l.id}">${escapeHtml(l.nome)} [${l.tipo}]</option>`).join('');
  }
}

function updateSelectSemanas(semana) {
  const select1 = document.getElementById('selectSemanaParaLocal');
  const select2 = document.getElementById('selectSemanaParaItem');

  const optionHtml = semana
    ? `<option value="${semana.id}">Semana ${formatarPeriodo(semana.dataInicio, semana.dataFim)} [${semana.status}]</option>`
    : '<option value="">Nenhuma semana ativa</option>';

  if (select1) select1.innerHTML = optionHtml;
  if (select2) select2.innerHTML = optionHtml;

  updateSelectLocaisDoItem();
}

function updateSelectLocaisDoItem() {
  const select = document.getElementById('selectLocalDoItem');
  if (!select) return;

  if (state.semanaAtual && state.semanaAtual.locais.length > 0) {
    select.innerHTML = '<option value="">Selecione o local...</option>' +
      state.semanaAtual.locais.map(l => `<option value="${l.localId}">${escapeHtml(l.nomeLocal)}</option>`).join('');
  } else {
    select.innerHTML = '<option value="">Nenhum local vinculado a esta semana</option>';
  }
}

// --------------------------------------------------------------------------
// Event Handlers (Submit Forms)
// --------------------------------------------------------------------------
async function handleCriarSemana(e) {
  e.preventDefault();
  const dataInicio = document.getElementById('inputDataInicio').value;
  const dataFim = document.getElementById('inputDataFim').value;

  const result = await fetchAPI('/semanas-lista', {
    method: 'POST',
    body: JSON.stringify({ dataInicio, dataFim })
  });

  if (result) {
    showToast('Semana de lista criada com sucesso!', 'success');
    loadAllData();
  }
}

async function handleAdicionarLocal(e) {
  e.preventDefault();
  const semanaId = document.getElementById('selectSemanaParaLocal').value;
  const localId = document.getElementById('selectLocalAdicionar').value;

  if (!semanaId || !localId) return;

  const result = await fetchAPI(`/semanas-lista/${semanaId}/locais`, {
    method: 'POST',
    body: JSON.stringify({ localId })
  });

  if (result) {
    showToast('Local vinculado com sucesso!', 'success');
    loadAllData();
  }
}

async function handleAdicionarItem(e) {
  e.preventDefault();
  const semanaId = document.getElementById('selectSemanaParaItem').value;
  const localId = document.getElementById('selectLocalDoItem').value;
  const materialId = document.getElementById('selectMaterialDoItem').value;
  const quantidadeSolicitada = parseInt(document.getElementById('inputQuantidadeItem').value, 10);

  if (!semanaId || !materialId || quantidadeSolicitada <= 0) return;

  const result = await fetchAPI(`/semanas-lista/${semanaId}/itens`, {
    method: 'POST',
    body: JSON.stringify({ localId, materialId, quantidadeSolicitada })
  });

  if (result) {
    showToast('Item adicionado à lista com sucesso!', 'success');
    loadAllData();
  }
}

async function handleEnviarSeparacao() {
  if (!state.semanaAtual) return;

  const result = await fetchAPI(`/semanas-lista/${state.semanaAtual.id}/enviar-separacao`, {
    method: 'POST'
  });

  if (result) {
    showToast('Semana enviada para separação com sucesso!', 'success');
    loadAllData();
  }
}

async function handleRegistrarSeparacaoItem(semanaId, itemId) {
  const qtdSeparada = parseInt(document.getElementById(`separado_${itemId}`).value, 10);
  const obs = document.getElementById(`obs_${itemId}`).value;

  const result = await fetchAPI(`/semanas-lista/${semanaId}/registrar-separacao`, {
    method: 'POST',
    body: JSON.stringify({
      itemId: itemId,
      quantidadeSeparada: qtdSeparada,
      observacao: obs
    })
  });

  if (result) {
    showToast('Separação do item registrada!', 'success');
    loadAllData();
  }
}

async function handleConfirmarRecebimento(semanaId, listaLocalId, possuiPendencias) {
  const result = await fetchAPI(`/semanas-lista/${semanaId}/confirmar-recebimento`, {
    method: 'POST',
    body: JSON.stringify({ listaLocalId, possuiPendencias })
  });

  if (result) {
    showToast('Recebimento confirmado com sucesso!', 'success');
    loadAllData();
  }
}

async function handleRegistrarRetirada(semanaId, listaLocalId) {
  const nomeRetiradoPor = document.getElementById(`retiradoPor_${listaLocalId}`).value.trim();

  if (!nomeRetiradoPor) {
    showToast('Informe o nome de quem retirou a lista.', 'error');
    return;
  }

  const result = await fetchAPI(`/semanas-lista/${semanaId}/registrar-retirada`, {
    method: 'POST',
    body: JSON.stringify({ listaLocalId, nomeRetiradoPor })
  });

  if (result) {
    showToast('Retirada registrada com assinatura digital!', 'success');
    loadAllData();
  }
}

// Catalogo
async function handleNovoMaterial(e) {
  e.preventDefault();
  const nome = document.getElementById('inputNomeMaterial').value;
  const unidade = document.getElementById('selectUnidadeMaterial').value;
  const ordemExibicao = parseInt(document.getElementById('inputOrdemMaterial').value, 10);

  const result = await fetchAPI('/materiais', {
    method: 'POST',
    body: JSON.stringify({ nome, unidade, ordemExibicao })
  });

  if (result) {
    showToast('Material cadastrado com sucesso!', 'success');
    document.getElementById('formNovoMaterial').reset();
    document.getElementById('formNovoMaterial').classList.add('hidden');
    loadMateriais();
  }
}

async function handleNovoLocal(e) {
  e.preventDefault();
  const nome = document.getElementById('inputNomeLocal').value;
  const tipo = document.getElementById('selectTipoLocal').value;
  const ordemExibicao = parseInt(document.getElementById('inputOrdemLocal').value, 10);

  const result = await fetchAPI('/locais', {
    method: 'POST',
    body: JSON.stringify({ nome, tipo, ordemExibicao })
  });

  if (result) {
    showToast('Local cadastrado com sucesso!', 'success');
    document.getElementById('formNovoLocal').reset();
    document.getElementById('formNovoLocal').classList.add('hidden');
    loadLocais();
  }
}

// --------------------------------------------------------------------------
// Utilities
// --------------------------------------------------------------------------
function escapeHtml(valor) {
  return String(valor ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function formatarPeriodo(dataInicio, dataFim) {
  const formatar = data => data.split('-').reverse().join('/');
  return `${formatar(dataInicio)} a ${formatar(dataFim)}`;
}

function getBadgeClassStatusSemana(status) {
  switch(status) {
    case 'EmMontagem': return 'badge-montagem';
    case 'EnviadaParaSeparacao': return 'badge-enviado';
    case 'SeparacaoConcluida': return 'badge-separado';
    case 'Finalizada': return 'badge-finalizada';
    default: return 'badge-pendente';
  }
}

function getBadgeClassStatusLista(status) {
  switch(status) {
    case 'Pendente': return 'badge-pendente';
    case 'Separado': return 'badge-separado';
    case 'Confirmado':
    case 'ConfirmadoComPendencia': return 'badge-confirmado';
    case 'Retirado': return 'badge-retirado';
    default: return 'badge-pendente';
  }
}

function showToast(message, type = 'success') {
  const container = document.getElementById('toastContainer');
  if (!container) return;

  const toast = document.createElement('div');
  toast.className = `toast ${type}`;
  toast.innerHTML = `
    <i class="${type === 'success' ? 'ri-checkbox-circle-fill' : 'ri-error-warning-fill'}"></i>
    <span>${escapeHtml(message)}</span>
  `;

  container.appendChild(toast);

  setTimeout(() => {
    toast.remove();
  }, 4000);
}
