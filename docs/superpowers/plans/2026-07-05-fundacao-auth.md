# TechPro — Fundação do Monorepo + Fatia Vertical de Autenticação — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bootstrap completo do monorepo TechPro com a primeira fatia vertical (auth multi-tenant): `docker-compose up` → front acessível → criar conta → login → JWT válido com `tenant_id`.

**Architecture:** Monolito modular .NET 10 (Web API pura, módulos por domínio) + Next.js 16 (App Router) + PostgreSQL 17 multi-tenant (coluna `tenant_id`, Global Query Filter no EF Core e Row-Level Security no Postgres como defesa em profundidade). Auth própria: ASP.NET Core Identity + JWT curto (claims `tenant_id` e `role`) + refresh token com rotação em cookie httpOnly.

**Tech Stack:** .NET 10 LTS, EF Core + Npgsql, ASP.NET Core Identity, FluentValidation, Serilog, Swashbuckle, xUnit · Next.js 16, React 19, TypeScript strict, Tailwind, shadcn/ui, TanStack Query, React Hook Form + Zod, motion, orval · Docker Compose (postgres:17 + api + frontend) · GitHub Actions.

## Global Constraints (vinculantes — docs/TechPro-stack-arquitetura-infraestrutura.md)

- Monolito modular: **um único projeto** `TechPro.Api`, pastas `Modules/` por domínio + `Shared/{Auth,Tenancy,Persistence}` — **não** Clean Architecture de 4 camadas, **não** Razor Views (seções 4 e 12).
- Toda entidade de tenant tem `tenant_id`; Global Query Filter automático + RLS no Postgres, fail-closed (seção 5).
- JWT carrega claims `tenant_id` e `role`; refresh token persistido com campo de tipo de cliente (web/mobile) desde já (seção 8).
- Front nunca acessa banco direto; API é o único ponto de aplicação de isolamento (seção 2).
- Estrutura de pastas do front conforme seção 13 (grupos de rota `(auth)`, `(empreendedor)`, etc.; `lib/api-client`, `lib/validators`, `components/ui`).
- CI mínimo: lint + build front, build + testes back (seção 14). Node 22 LTS no CI.
- Segredos só em variáveis de ambiente; CORS restrito; rate limiting em endpoints públicos (seção 15).
- Decisões aprovadas pelo usuário em 2026-07-05: frontend no compose; `tenant_id` UUID; primeiro usuário = `gestor`; refresh em cookie httpOnly (access token só em memória); pacote EFCore.NamingConventions; Hangfire/Zustand/Recharts adiados; orval; testes em `backend/tests/TechPro.Api.Tests`; renomear `progesso.md`→`progresso.md`; e-mail único global no MVP.
- Identifiers/rotas em pt-BR (`/api/auth/registrar`, `Empresa`, `Usuario`) seguindo o padrão dos docs (`OrdensServico`, `/api/ordens-servico`).

---

### Task 1: Fundação do monorepo e git

**Files:**
- Create: `.gitignore`, `.gitattributes`, `.editorconfig`, `README.md`
- Rename: `docs/progesso.md` → `docs/progresso.md`

**Steps:**
- [ ] `git init -b main` (a pasta `.git` existente está vazia/corrompida — o init a reinicializa)
- [ ] `.gitignore` cobrindo Node (`node_modules/`, `.next/`), .NET (`bin/`, `obj/`, `*.user`), env (`.env`, `.env*.local`), IDE
- [ ] `.gitattributes`: `* text=auto eol=lf` (+ exceções `.cmd/.bat` crlf)
- [ ] `.editorconfig`: indent 4 para C#, 2 para TS/JSON/YAML
- [ ] `README.md`: visão em 5 linhas + como rodar (`docker compose up`) + mapa de pastas (frontend/, backend/, mobile/ futura, docs/)
- [ ] `git mv`-equivalente do progesso.md e commit: `chore: fundacao do monorepo`

### Task 2: Esqueleto do back-end .NET 10

**Files:**
- Create: `backend/TechPro.sln`, `backend/src/TechPro.Api/TechPro.Api.csproj`, `backend/tests/TechPro.Api.Tests/TechPro.Api.Tests.csproj`, `backend/.config/dotnet-tools.json`, pastas `Modules/`, `Shared/Auth/`, `Shared/Tenancy/`, `Shared/Persistence/`

**Steps:**
- [ ] `dotnet new sln`, `dotnet new webapi --use-controllers`, `dotnet new xunit`, `dotnet sln add`
- [ ] Pacotes API: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `EFCore.NamingConventions`, `FluentValidation.DependencyInjectionExtensions`, `Serilog.AspNetCore`, `Swashbuckle.AspNetCore`
- [ ] Pacotes testes: ref ao projeto API + `Microsoft.EntityFrameworkCore.InMemory` + `Microsoft.AspNetCore.Mvc.Testing`
- [ ] `dotnet new tool-manifest && dotnet tool install dotnet-ef`
- [ ] `dotnet build` verde; commit `chore(backend): esqueleto do monolito modular .NET 10`

### Task 3: Fundação multi-tenant + primeira migration

**Files:**
- Create: `Shared/Tenancy/{Empresa.cs,ITenantEntity.cs,ITenantProvider.cs,HttpTenantProvider.cs,TenantSessionInterceptor.cs,RlsHelper.cs}`
- Create: `Shared/Auth/{Usuario.cs,RefreshToken.cs,Papeis.cs}`
- Create: `Shared/Persistence/{TechProDbContext.cs,DesignTimeDbContextFactory.cs}`
- Create: `Migrations/*_FundacaoMultiTenant.cs` (gerada + SQL de RLS)
- Test: `backend/tests/TechPro.Api.Tests/Tenancy/GlobalQueryFilterTests.cs`

**Interfaces (o que as tasks seguintes consomem):**
- `ITenantProvider { Guid? TenantId { get; } }`
- `ITenantEntity { Guid TenantId { get; set; } }` → GQF automático por convenção no `OnModelCreating`
- `TechProDbContext : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>` com `DbSet<Empresa> Empresas`, `DbSet<RefreshToken> RefreshTokens`
- `Papeis.Gestor/Tecnico/Atendente` (strings `"gestor"|"tecnico"|"atendente"`), papéis seedados via `HasData` com GUIDs fixos
- `RlsHelper.AplicarIsolamentoTenant(MigrationBuilder, string tabela)` para tabelas futuras

**Design fixado:**
- `Empresa { Guid Id; string Nome; DateTimeOffset CriadoEm }` — GQF estrito `e.Id == TenantIdAtual` (fail-closed; INSERT do cadastro não passa por query filter).
- `Usuario : IdentityUser<Guid> { Guid TenantId; string Nome; DateTimeOffset CriadoEm }` e `RefreshToken { Guid Id; Guid UsuarioId; Guid TenantId; string TokenHash; TipoCliente TipoCliente(Web|Mobile); CriadoEm; ExpiraEm; RevogadoEm?; Guid? SubstituidoPorId }` — **deliberadamente fora do GQF/RLS** (plano de controle: consultados por chave única antes de existir contexto de tenant — login por e-mail, refresh por token). Isolamento deles é checado explicitamente no AuthService. Documentar no código.
- Interceptor de conexão executa `SELECT set_config('app.tenant_id', <claim ou ''>, false)` em todo `ConnectionOpened` (pool do Npgsql faz DISCARD ALL na devolução).
- SQL da migration (após criação das tabelas):
```sql
ALTER TABLE empresas ENABLE ROW LEVEL SECURITY;
ALTER TABLE empresas FORCE ROW LEVEL SECURITY;
CREATE POLICY empresas_isolamento_leitura ON empresas FOR SELECT
  USING (id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
CREATE POLICY empresas_cadastro_publico ON empresas FOR INSERT WITH CHECK (true);
```
  (`NULLIF` evita erro de cast quando a variável está vazia → fail-closed sem exceção; `FORCE` aplica a policy até ao dono da tabela, já que `techpro_app` é dono e roda as migrations em dev.)

**Steps (TDD no que é lógica):**
- [ ] Teste falhando: `GlobalQueryFilterTests` — com InMemory + provider fake: tenant A não vê `Empresa` de B; provider nulo vê zero; entidade `ITenantEntity` de teste (contexto derivado no projeto de teste) é filtrada pela convenção
- [ ] Implementar entidades, provider, convenção GQF no `OnModelCreating`, interceptor
- [ ] `dotnet test` verde
- [ ] `dotnet ef migrations add FundacaoMultiTenant` + editar migration adicionando o SQL de RLS acima
- [ ] Commit `feat(backend): fundacao multi-tenant com tenant_id, GQF e esqueleto de RLS`

### Task 4: docker-compose (Postgres + API)

**Files:**
- Create: `docker-compose.yml` (raiz), `backend/Dockerfile`, `backend/db/init/01-app-role.sh`, `.env.example`

**Steps:**
- [ ] `01-app-role.sh`: cria `techpro_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS` + database `techpro OWNER techpro_app`
- [ ] Dockerfile multi-stage (sdk:10.0 → aspnet:10.0, porta 8080)
- [ ] compose: `postgres:17-alpine` (volume + healthcheck `pg_isready`) e `api` (porta host 5080, env: connection string com `techpro_app`, `Jwt__*`, `Cors__FrontendOrigin`)
- [ ] Migrate no startup quando `ASPNETCORE_ENVIRONMENT=Development`
- [ ] Verificar: `docker compose up -d --build` → `curl http://localhost:5080/health` = 200; tabelas com `rowsecurity=true` via psql
- [ ] Commit `feat(infra): docker-compose local com Postgres 17 e API`

### Task 5: Front-end Next.js 16

**Files:**
- Create: `frontend/` via `npx create-next-app@latest frontend --ts --tailwind --eslint --app --no-src-dir --import-alias "@/*" --use-npm --yes`
- Create: estrutura `app/(auth)/`, `app/(empreendedor)/`, `lib/{api-client,hooks,validators,auth}/`, `components/ui/`
- Modify: `docker-compose.yml` (serviço `frontend`: node:22-alpine, volume `./frontend:/app` + volume anônimo p/ node_modules, `npm install && npm run dev`, porta 3000)

**Steps:**
- [ ] create-next-app; conferir `strict: true` no tsconfig
- [ ] `npx shadcn@latest init` + `add button input label card form field sonner`
- [ ] `npm i @tanstack/react-query react-hook-form zod @hookform/resolvers motion` + `npm i -D orval`
- [ ] Provider de QueryClient em `app/providers.tsx`, importado no root layout
- [ ] Serviço `frontend` no compose; `npm run build` local verde
- [ ] Commit `feat(frontend): inicializacao Next.js 16 com shadcn, TanStack Query e RHF+Zod`

### Task 6: Fatia de auth — back-end

**Files:**
- Create: `Shared/Auth/{TokenService.cs,AuthService.cs,AuthController.cs,Dtos/*.cs,Validators/*.cs}`
- Modify: `Program.cs` (Serilog, Identity core, JwtBearer com `MapInboundClaims=false` e `RoleClaimType="role"`, CORS `frontend`, rate limiter `auth` 10/min/IP, Swagger com bearer, `/health`)
- Test: `Tests/Auth/TokenServiceTests.cs`, `Tests/Auth/AuthFluxoTests.cs` (WebApplicationFactory)

**Contratos (consumidos pela Task 7):**
- `POST /api/auth/registrar {nomeEmpresa,nome,email,senha}` → 201 `AuthResponse` + cookie `techpro_refresh`
- `POST /api/auth/login {email,senha}` → 200 `AuthResponse` + cookie
- `POST /api/auth/refresh` (cookie) → 200 `AuthResponse` + cookie rotacionado; reuse de token revogado → revoga a cadeia e 401
- `POST /api/auth/logout` → 204 + revoga e limpa cookie
- `GET /api/auth/me` [Authorize] → `{id,nome,email,papel,tenantId,empresa:{id,nome}}` (empresa lida sob GQF+RLS — prova a cadeia inteira)
- `AuthResponse = {accessToken, expiraEm, usuario:{id,nome,email,papel,tenantId}}`
- JWT: claims `sub`, `email`, `jti`, `nome`, `tenant_id`, `role`; HS256 `Jwt:Key`; 15 min. Refresh: 64 bytes aleatórios, SHA-256 no banco, 7d web / 90d mobile, rotação com `SubstituidoPorId`.
- Cookie: httpOnly, Secure, SameSite=Lax, `Path=/api/auth`.
- Registro: transação única — `Empresa` + `Usuario`(gestor, `EmailConfirmed=true` no MVP — confirmação por e-mail entra com Resend depois) + role.

**Steps:**
- [ ] Testes falhando: TokenService (claims tenant_id/role/sub, expiração 15min); fluxo registrar→login→me via WebApplicationFactory (Sqlite in-memory ou InMemory com transação ignorada)
- [ ] Implementar TokenService → AuthService → Controller → validators (senha ≥8, e-mail válido, nomes obrigatórios)
- [ ] `dotnet test` verde; smoke com curl no compose
- [ ] Commit `feat(auth): registro, login, refresh com rotacao e claims tenant_id/role`

### Task 7: Fatia de auth — front-end

**Files:**
- Create: `frontend/orval.config.ts`, `lib/api-client/mutator.ts` (+ gerado `lib/api-client/techpro.ts`), `lib/auth/auth-provider.tsx`, `lib/validators/auth.ts`
- Create: `app/(auth)/layout.tsx`, `app/(auth)/login/page.tsx`, `app/(auth)/cadastro/page.tsx`, `app/(empreendedor)/layout.tsx`, `app/(empreendedor)/dashboard/page.tsx`
- Modify: `app/page.tsx` (redirect por estado de auth)

**Design fixado:**
- Access token só em memória (módulo `lib/auth/token.ts`); bootstrap no mount do AuthProvider via `POST /refresh` (cookie); estados `carregando|autenticado|anonimo`; mutator injeta `Authorization` e `credentials:'include'`, e num 401 tenta 1 refresh antes de falhar.
- Visual (guia Handle): fundo branco, títulos navy `#14162B`, corpo cinza, um único CTA pílula preta por tela, card raio 12-16px com borda sutil.
- Dashboard exibe: nome da empresa (via `/me`, atravessando GQF+RLS), papel, e o `tenant_id` do JWT decodificado — evidência do critério de pronto.

**Steps:**
- [ ] `npm run generate:api` (orval apontando para swagger.json local; código gerado commitado)
- [ ] Validators Zod (mensagens pt-BR), páginas com RHF + hooks gerados
- [ ] Guard no layout `(empreendedor)`
- [ ] `npm run build` + `tsc --noEmit` verdes; commit `feat(frontend): fluxo de cadastro/login e dashboard com claims do JWT`

### Task 8: CI GitHub Actions

**Files:**
- Create: `.github/workflows/ci.yml`

**Steps:**
- [ ] Job `frontend` (Node 22, cache npm): `npm ci`, `npm run lint`, `npx tsc --noEmit`, `npm run build`
- [ ] Job `backend` (dotnet 10.0.x): `dotnet restore`, `dotnet build --no-restore -c Release`, `dotnet test --no-build -c Release`
- [ ] Commit `ci: pipeline minimo lint+build+testes`

### Task 9: Documentação

**Files:**
- Modify: `docs/progresso.md` (histórico da sessão, decisões 1-10 aprovadas e porquês, como rodar, próximos passos = Ordem da Fase 1 do fases_MVP.md)
- Modify: `docs/TechPro-stack-arquitetura-infraestrutura.md` (apenas ticks do checklist da seção 19 que se tornaram verdade)

**Steps:**
- [ ] Escrever progresso.md; atualizar checklist; commit `docs: registro da fundacao e decisoes da sessao`

### Task 10: Verificação end-to-end (critério de pronto)

**Steps:**
- [ ] `docker compose up -d --build` limpo
- [ ] Navegador: acessar http://localhost:3000 → cadastro de conta nova → login → dashboard mostra tenant_id/role
- [ ] Validar JWT retornado (claims `tenant_id` e `role` presentes, assinatura/exp corretos)
- [ ] Evidência registrada no progresso.md; commit final

## Self-Review (executado na escrita)

1. **Cobertura da spec:** monorepo ✔ (T1), front init ✔ (T5), back modular ✔ (T2), compose ✔ (T4+T5), migration com tenant_id+GQF+RLS ✔ (T3), auth vertical completa ✔ (T6+T7), CI ✔ (T8), docs/progresso ✔ (T9), critério de pronto ✔ (T10).
2. **Placeholders:** nenhum "TBD"; código crítico fixado aqui, boilerplate gerado por CLI tem comando exato.
3. **Consistência de tipos:** `ITenantProvider.TenantId: Guid?` usado por DbContext/interceptor; contratos de T6 consumidos textualmente em T7; `Papeis` strings idênticas em seed, claim e front.
