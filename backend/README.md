# 📦 LMaterial API — Sistema de Gestão de Listas Semanais de Materiais

API RESTful desenvolvida em **.NET 9** utilizando os princípios de **Domain-Driven Design (DDD)**, **Clean Architecture** e **SOLID** para gerenciamento, separação e conferência de materiais de limpeza/insumos por locais (Andares e Áreas Comuns).

---

## 🎯 O que o aplicativo faz

O **LMaterial** orquestra o ciclo completo de suprimento de materiais por local:

1. **Gestão de Materiais e Locais**: Cadastro centralizado de insumos (com unidades de medida `UN`, `CX`, `PCT` e ordem de exibição) e locais de entrega (Andares 1 a 16, Recepção, Refeitório, Ambulatório, etc.).
2. **Montagem da Semana de Lista**: O supervisor cria uma semana de lista e vincula os locais e materiais com suas respectivas quantidades solicitadas.
3. **Envio para Separação**: A semana é enviada para a equipe do almoxarifado, travando modificações de estrutura.
4. **Separação pelo Almoxarife**: O almoxarife registra a quantidade real separada por item (sinalizando itens pendentes ou completos).
5. **Confirmação e Retirada**: O responsável no local confirma o recebimento dos materiais e a retirada é registrada com o nome de quem retirou.
6. **Finalização Automática**: Quando todas as listas locais são concluídas, a semana de lista transita para o status `Finalizada`.

---

## 🏗️ Arquitetura da Solução

O projeto é estruturado em 4 camadas desacopladas:

```text
├── LMaterial.Domain            # Entidades, Enums, Exceções e Interfaces (DDD Puro)
├── LMaterial.Domain.UnitTests  # 41 Testes Unitários das Regras de Domínio (xUnit)
├── LMaterial.Infrastructure    # EF Core 9, Mapeamentos Npgsql PostgreSQL e Unit of Work
├── LMaterial.Application       # DTOs (Records), Services e Validadores (FluentValidation)
└── LMaterial.API               # Controllers REST, Swagger UI e Middleware Global de Erros
```

---

## 🗄️ Banco de Dados

- **SGBD**: PostgreSQL (v12+)
- **ORM**: Entity Framework Core 9 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Connection String** (configurada em `appsettings.json`):
  ```json
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=lmaterial_db;Username=postgres;Password=postgres"
  }
  ```
- **Mapeamento & Seed Data**: Mapeamentos Fluent API com carga inicial de 32 materiais e 23 locais pré-configurados.

---

## 👥 Perfis de Acesso

| Perfil | Login | O que faz |
|---|---|---|
| **Supervisor** | Sim | Cria a semana, define os locais, envia para separação e gerencia o catálogo de materiais e locais |
| **Coordenador** | Sim | Preenche os itens de cada andar, confirma o recebimento e registra a retirada com assinatura digital |
| **Almoxarife** | Sim | Recebe as listas enviadas e registra a quantidade separada de cada item |
| **Gestor** | Sim | Consulta o total separado por material e exporta a planilha semanal (XLSX) |
| Colaboradora | Não | Retira os materiais do seu andar; a retirada é registrada pelo Coordenador com nome e data/hora |

Ao final da separação, o Supervisor e o Coordenador recebem uma notificação dentro do sistema.

### Configuração de segredos e usuários iniciais

Segredos não ficam no repositório. Use `dotnet user-secrets` a partir da pasta `LMaterial.API`:

```bash
dotnet user-secrets set "JwtSettings:Secret" "<segredo com pelo menos 32 caracteres>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=lmaterial_db;Username=postgres;Password=<senha>"
dotnet user-secrets set "SeedUsuarios:supervisor:Password" "<senha>"
dotnet user-secrets set "SeedUsuarios:gestor:Password" "<senha>"
dotnet user-secrets set "SeedUsuarios:almoxarife:Password" "<senha>"
dotnet user-secrets set "SeedUsuarios:coordenador:Password" "<senha>"
```

Os usuários são criados na primeira inicialização. Usuários sem senha configurada são ignorados. Em ambiente de desenvolvimento, as migrations são aplicadas automaticamente ao iniciar a API.

## 🚀 Como Executar Localmente

### Pré-requisitos
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Instância ativa do PostgreSQL.

### Comandos

1. **Restaurar dependências**:
   ```bash
   dotnet restore
   ```

2. **Compilar a solução**:
   ```bash
   dotnet build
   ```

3. **Executar os Testes Unitários**:
   ```bash
   dotnet test
   ```

4. **Executar a API**:
   ```bash
   dotnet run --project LMaterial.API
   ```

---

## 🔗 Link de Acesso à Documentação (Swagger UI)

Após executar o comando `dotnet run --project LMaterial.API`, acesse a documentação interativa das rotas no navegador:

👉 **[http://localhost:5175/swagger](http://localhost:5175/swagger)**

---

## 📑 Principais Endpoints

### 🟢 Materiais (`/api/materiais`)
- `GET /api/materiais`: Lista todos os materiais por ordem de exibição.
- `GET /api/materiais/ativos`: Lista apenas materiais ativos.
- `GET /api/materiais/{id}`: Busca um material por ID.
- `POST /api/materiais`: Cadastra um novo material.
- `PUT /api/materiais/{id}`: Atualiza os dados de um material.

### 🔵 Locais (`/api/locais`)
- `GET /api/locais`: Lista todos os locais.
- `GET /api/locais/ativos`: Lista locais ativos.
- `GET /api/locais/{id}`: Busca um local por ID.
- `POST /api/locais`: Cadastra um novo local.
- `PUT /api/locais/{id}`: Atualiza um local (incluindo status ativo/inativo).

### 🟡 Semanas de Lista (`/api/semanas-lista`)
- `POST /api/semanas-lista`: Cria uma nova semana de lista (`EmMontagem`).
- `GET /api/semanas-lista/atual`: Retorna a semana ativa atual.
- `GET /api/semanas-lista/{id}`: Busca o detalhamento da semana pelo ID.
- `POST /api/semanas-lista/{id}/locais`: Adiciona um local à semana.
- `POST /api/semanas-lista/{id}/itens`: Adiciona materiais à lista do local.
- `POST /api/semanas-lista/{id}/enviar-separacao`: Altera o status da semana para `EnviadaParaSeparacao`.
- `POST /api/semanas-lista/{id}/registrar-separacao`: Registra os itens separados pelo almoxarife.
- `POST /api/semanas-lista/{id}/confirmar-recebimento`: Confirma o recebimento no local.
- `POST /api/semanas-lista/{id}/registrar-retirada`: Registra a retirada e finaliza a semana.

---

## ⚠️ Tratamento de Erros Padronizado

Todas as respostas de exceção da API seguem o contrato padronizado:

```json
{
  "error": true,
  "message": "Descrição detalhada do erro ou regra violada",
  "code": "DOMAIN_ERROR"
}
```
