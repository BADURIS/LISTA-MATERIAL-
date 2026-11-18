# Lista de Material — Normatel

Sistema de listas semanais de materiais de limpeza/insumos por local (andares e áreas comuns).

Junção dos dois projetos anteriores:
- **backend/** — API .NET 9 (DDD, PostgreSQL, login com JWT) vinda do projeto `L Material`
- **frontend/** — interface Next.js 15 + Tailwind vinda do projeto `LISTA MATERIAL`, adaptada à API acima

## Fluxo da semana

O cadastro de materiais (criar, editar, inativar e excluir) é do **Almoxarife**. Material que já apareceu em algum pedido não é apagado: é inativado para manter o histórico.

1. **Supervisor** cria a semana (já vem com todos os locais ativos) e faz o pedido de cada local, ou marca "sem pedido nesta semana". O **Coordenador** pode ajudar nos pedidos.
2. O envio ao almoxarifado só é liberado quando todos os locais estão prontos.
3. **Almoxarife** separa: as quantidades já vêm iguais ao pedido e ele só altera o que foi enviado a menos (a falta fica registrada). Pode salvar aos poucos.
4. Na entrega, o **Almoxarife** registra a retirada com o nome da colaboradora responsável (assinatura digital: nome + data/hora). O nome sai nos relatórios PDF e na aba "Retiradas" da planilha da semana.
5. Com todas as retiradas registradas a semana é finalizada e Supervisor, Coordenador e Gestor são avisados.
6. **Gestor** acompanha consumo (por material/local e evolução semanal), histórico e exporta planilhas Excel e relatórios em PDF.

Avisos dentro do sistema (sino no topo) são enviados em cada etapa.

## Como rodar localmente

Pré-requisitos: .NET 9 SDK, Node 20+, Docker (ou PostgreSQL 12+ instalado).

```bash
# 1. Banco
docker run -d --name lmaterial-pg -e POSTGRES_PASSWORD=<senha> -e POSTGRES_DB=lmaterial_db -p 5432:5432 postgres:16-alpine

# 2. Segredos da API (ficam fora do repositório)
cd backend/LMaterial.API
dotnet user-secrets set "JwtSettings:Secret" "<segredo com pelo menos 32 caracteres>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=lmaterial_db;Username=postgres;Password=<senha>"
dotnet user-secrets set "SeedUsuarios:supervisor:Password" "<senha>"   # idem para gestor, almoxarife, coordenador
# Trocar a senha depois: altere o valor e reinicie a API (o hash do usuário é atualizado na inicialização)

# 3. API (aplica as migrations e cria os usuários na primeira execução)
cd ..
dotnet run --project LMaterial.API --launch-profile http     # http://localhost:5175 (Swagger em /swagger)

# 4. Interface
cd ../frontend
npm ci
npm run build && npm start                                  # http://localhost:3000
```

O frontend repassa `/api` para a API (variável `API_URL`, padrão `http://localhost:5175`), então celulares
na mesma rede só precisam acessar `http://<ip-do-computador>:3000`.

## Testes

```bash
cd backend && dotnet test                         # testes das regras de negócio
SENHA_SUPERVISOR=<senha> SENHA_ALMOXARIFE=<senha> node scripts/popular-demo.mjs 4   # fluxo completo pela API; deixa 4 semanas de demonstração no banco
```

## Relatórios em PDF

Gerados no servidor com PDFsharp/MigraDoc (licença MIT): lista do local (com dados da retirada, assinatura digital
e espaço para assinatura em papel), levantamento da semana e consumo do período. Horários sempre em horário de Brasília.
Num servidor Linux é preciso ter uma fonte instalada (`apt-get install fonts-liberation`).
