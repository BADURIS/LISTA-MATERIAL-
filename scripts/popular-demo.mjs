// Percorre o fluxo completo pela API e deixa semanas finalizadas no banco, para testar telas e gráficos de consumo.
// Também serve como teste de ponta a ponta: para no primeiro erro.
//
// Uso (com a API rodando):  node scripts/popular-demo.mjs [semanas] [senha]
//   API_URL=http://localhost:5175 (padrão)

const API = process.env.API_URL || 'http://localhost:5175';
const SEMANAS = Number(process.argv[2] || 3);
const SENHA = process.argv[3] || process.env.SENHA_DEMO;

if (!SENHA) {
  console.error('Informe a senha dos usuários de teste: node scripts/popular-demo.mjs 3 <senha>');
  process.exit(1);
}

// Sequência pseudo-aleatória fixa: o resultado é sempre o mesmo
let semente = 42;
const aleatorio = () => ((semente = (semente * 1103515245 + 12345) % 2 ** 31) / 2 ** 31);
const entre = (min, max) => min + Math.floor(aleatorio() * (max - min + 1));

async function chamar(token, metodo, caminho, corpo) {
  const resp = await fetch(`${API}${caminho}`, {
    method: metodo,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: corpo === undefined ? undefined : JSON.stringify(corpo),
  });
  const texto = await resp.text();
  if (!resp.ok) throw new Error(`${metodo} ${caminho} -> ${resp.status}: ${texto}`);
  return texto ? JSON.parse(texto) : null;
}

async function login(usuario) {
  const r = await chamar(null, 'POST', '/api/auth/login', { username: usuario, password: SENHA });
  return r.accessToken;
}

const iso = (d) => d.toISOString().slice(0, 10);
function segundaDaSemana(offsetSemanas) {
  const d = new Date();
  d.setUTCHours(0, 0, 0, 0);
  d.setUTCDate(d.getUTCDate() - ((d.getUTCDay() + 6) % 7) + offsetSemanas * 7);
  return d;
}

const supervisor = await login('supervisor');
const almoxarife = await login('almoxarife');
const coordenador = await login('coordenador');

const atual = await fetch(`${API}/api/semanas-lista/atual`, { headers: { Authorization: `Bearer ${supervisor}` } });
if (atual.status === 200) {
  console.error('Já existe uma semana em andamento no banco. Este script só roda com o banco sem semana aberta.');
  process.exit(1);
}

const materiais = (await chamar(supervisor, 'GET', '/api/materiais/ativos?pageSize=100')).items;

for (let s = SEMANAS; s >= 1; s--) {
  const inicio = segundaDaSemana(-s);
  const fim = new Date(inicio);
  fim.setUTCDate(fim.getUTCDate() + 4);

  let semana = await chamar(supervisor, 'POST', '/api/semanas-lista', { dataInicio: iso(inicio), dataFim: iso(fim) });

  // Pedido de cada local: alguns ficam sem pedido, os demais pedem de 3 a 8 materiais
  for (const [idx, lista] of semana.locais.entries()) {
    if (idx % 9 === 4) {
      await chamar(supervisor, 'PUT', `/api/semanas-lista/${semana.id}/locais/${lista.localId}/sem-pedido`, { semPedido: true });
      continue;
    }
    const escolhidos = [...materiais].sort(() => aleatorio() - 0.5).slice(0, entre(3, 8));
    const itens = escolhidos.map((m) => ({ materialId: m.id, quantidadeSolicitada: entre(1, 6) + (SEMANAS - s) }));
    await chamar(supervisor, 'PUT', `/api/semanas-lista/${semana.id}/locais/${lista.localId}/itens`, { itens });
  }

  semana = await chamar(supervisor, 'POST', `/api/semanas-lista/${semana.id}/enviar-separacao`);

  // Separação: de vez em quando falta material
  const itensSeparados = semana.locais.flatMap((l) =>
    l.itens.map((i) => {
      const falta = aleatorio() < 0.1 ? entre(1, i.quantidadeSolicitada) : 0;
      return { itemId: i.id, quantidadeSeparada: i.quantidadeSolicitada - falta, observacao: falta ? 'Sem estoque' : null };
    })
  );
  semana = await chamar(almoxarife, 'POST', `/api/semanas-lista/${semana.id}/registrar-separacao`, { itens: itensSeparados });

  // Recebimento e retirada em cada local com pedido
  for (const lista of semana.locais.filter((l) => !l.semPedido)) {
    await chamar(coordenador, 'POST', `/api/semanas-lista/${semana.id}/confirmar-recebimento`, { listaLocalId: lista.id });
    semana = await chamar(coordenador, 'POST', `/api/semanas-lista/${semana.id}/registrar-retirada`, {
      listaLocalId: lista.id,
      nomeRetiradoPor: `Colaboradora ${lista.nomeLocal}`,
    });
  }

  const comPedido = semana.locais.filter((l) => !l.semPedido);
  const assinaturasValidas = comPedido.every((l) => l.assinaturaValida);
  console.log(
    `Semana ${iso(inicio)}: ${semana.status} | ${comPedido.length} locais retirados, ` +
      `${semana.locais.length - comPedido.length} sem pedido | assinaturas válidas: ${assinaturasValidas}`
  );
  if (semana.status !== 'Finalizada' || !assinaturasValidas) throw new Error('Fluxo não terminou como esperado.');
}

const consumo = await chamar(supervisor, 'GET', '/api/consumo/semanal?semanas=8');
console.log(`Consumo semanal: ${consumo.semanas.map((s) => s.quantidadeTotal).join(' -> ')} itens`);
