# Progresso — TechPro

> Diário técnico do que está construído, decidido e pendente.
> Complementa (não substitui) os documentos de produto e de stack.

---

## Status geral

**Fundação do MVP concluída em 2026-07-05.** O critério de pronto da primeira
fase técnica foi atingido e verificado de ponta a ponta em navegador real:

> `docker compose up` → acessar o front-end → criar conta → fazer login →
> receber JWT válido com `tenant_id`.

Evidência da verificação (Playwright + Edge, 2026-07-05):

```json
{
  "raizRedirecionaPara": "/login",
  "cadastroLevaAoDashboard": true,
  "dashboardMostraEmpresa": true,
  "dashboardMostraPapel": true,
  "jwt": {
    "status": 200,
    "tenant_id": "eff7220b-07b1-4827-bd9b-b47bf90e6904",
    "role": "gestor",
    "iss": "TechPro",
    "expEmMinutos": 15
  },
  "dashboardMostraTenantId": true,
  "sairVoltaParaLogin": true,
  "loginLevaAoDashboard": true,
  "senhaErradaMostraErro": true
}
```

Durante a própria verificação o rate limiter respondeu `429` a partir da 11ª
chamada de auth no mesmo minuto — o limite de 10/min/IP funcionando ao vivo.

Suíte de testes do back-end: **15 testes xUnit verdes** (5 de Global Query
Filter, 2 de TokenService, 8 de fluxo completo de auth via
WebApplicationFactory + Sqlite em memória).

---

## Como rodar

```bash
docker compose up -d --build
```

| Serviço  | URL                                      |
|----------|------------------------------------------|
| Front-end | http://localhost:3000                    |
| API       | http://localhost:5080 (health: `/health`)|
| Swagger   | http://localhost:5080/swagger            |
| Postgres  | localhost:5432 (dev)                     |

- Variáveis locais opcionais: copiar `.env.example` para `.env` (nunca commitar `.env`).
- Migrations rodam automaticamente no startup **somente em Development**;
  em produção serão passo de deploy.
- Testes do back-end: `dotnet test backend/TechPro.slnx`.
  - ⚠ Se o Windows bloquear a DLL de teste (ver "Notas de ambiente"), rodar
    dentro do container do SDK:
    `docker run --rm -v "<repo>\backend:/repo:ro" -v techpro-nuget:/root/.nuget mcr.microsoft.com/dotnet/sdk:10.0 bash -c "cp -r /repo /work && cd /work && dotnet test TechPro.slnx"`
- Regenerar o cliente tipado do front após mudar contratos da API:
  `curl http://localhost:5080/swagger/v1/swagger.json -o frontend/openapi/swagger.json && cd frontend && npm run gerar-api`.

---

## Decisões aprovadas (2026-07-05)

1. **Front-end dentro do docker-compose** (node:22-alpine + volume), além de Postgres e API.
2. **UUID como PK** de `empresas` (o `tenant_id`) — não enumerável; viaja em JWT, URLs e RLS.
3. **Primeiro usuário cadastrado = papel `gestor`** da empresa recém-criada.
4. **Refresh token em cookie httpOnly/Secure** (`techpro_refresh`, SameSite=Lax, Path=/api/auth); **access token somente em memória** no front — nunca em localStorage.
5. **EFCore.NamingConventions** para snake_case no schema (tabelas do Identity renomeadas manualmente: `usuarios`, `papeis`, `usuario_papeis`, ...).
6. **Hangfire, Zustand e Recharts adiados** até o primeiro uso real (continuam decididos no doc de stack).
7. **orval** gera o cliente TypeScript tipado + hooks TanStack Query a partir do swagger.
8. Testes do back-end em `backend/tests/TechPro.Api.Tests` (xUnit).
9. `progesso.md` renomeado para `progresso.md`.
10. **E-mail globalmente único no MVP** (um e-mail = uma empresa); multi-vínculo fica para depois se houver demanda.

---

## O que está construído

### Multi-tenancy (defesa em profundidade)

- `tenant_id` em toda tabela relevante + **Global Query Filter** automático por
  convenção: toda entidade `ITenantEntity` é filtrada sem `.Where()` manual —
  fail-closed (sem tenant no contexto, nenhuma linha aparece).
- **RLS no Postgres** como segunda camada: `app.tenant_id` é setado por
  interceptor em toda conexão (`set_config`); app conecta como `techpro_app`
  (NOSUPERUSER, **NOBYPASSRLS**); `empresas` com ENABLE+FORCE RLS
  (SELECT isolado por tenant, INSERT público para o cadastro).
- `RlsHelper.AplicarIsolamentoTenant()` pronto para aplicar a política padrão
  em toda tabela de produto futura (OS, estoque, financeiro...).
- **Plano de controle deliberadamente fora do GQF/RLS**: `usuarios` e
  `refresh_tokens` são consultados por chave única *antes* de existir contexto
  de tenant (login por e-mail, refresh por token); o `AuthService` valida o
  vínculo explicitamente.

### Autenticação (primeira fatia vertical completa)

- ASP.NET Core Identity + JWT HS256 de 15 min com claims `sub`, `email`,
  `nome`, `tenant_id` e `role` (gestor/tecnico/atendente seedados).
- `POST /api/auth/registrar` — cria Empresa + gestor numa transação única (201).
- `POST /api/auth/login` — lockout após 5 falhas (5 min); resposta única para
  qualquer falha (sem oráculo de e-mails cadastrados).
- `POST /api/auth/refresh` — **rotação**: cada uso revoga o token e emite
  sucessor; reapresentar token já rotacionado revoga a família inteira
  (detecção de roubo). Só o hash SHA-256 vai ao banco.
- `POST /api/auth/logout`, `GET /api/auth/me` (empresa lida através do GQF+RLS).
- `RefreshToken.TipoCliente` (Web 7d / Mobile 90d) já modelado — seção 8 do doc
  de stack — para o app do técnico da Fase 2 não exigir migração.
- Rate limiting 10/min/IP nos endpoints de auth; CORS restrito ao front;
  FluentValidation com mensagens pt-BR.

### Front-end

- Next.js 16 (App Router, TS estrito, Tailwind 4, shadcn/ui sobre Radix,
  TanStack Query, RHF+Zod, motion instalado).
- Cliente da API gerado por orval (fetch + hooks TanStack Query); mutator
  injeta Bearer da memória e `credentials: include`.
- `AuthProvider` restaura a sessão no load via cookie de refresh e renova o
  access token 1 min antes de expirar.
- Páginas `/login` e `/cadastro` (grupo `(auth)`) e `/dashboard` protegido
  (grupo `(empreendedor)`) exibindo empresa, papel e tenant_id.
- Visual seguindo o guia de referência (docs/UI_UX-referencia.md): fundo
  branco, navy `#14162B`, corpo cinza, tag de seção rosa uppercase, CTA
  pílula preta única, glow gradiente atrás do card do dashboard.

### Infra local e CI

- `docker-compose.yml`: postgres:17-alpine (init script cria `techpro_app`
  sem BYPASSRLS), API (multi-stage Dockerfile .NET 10) e front (node:22).
- GitHub Actions (`.github/workflows/ci.yml`): job front (npm ci, lint,
  tsc --noEmit, build) + job back (restore, build Release, testes).

---

## Desvios e notas conscientes

- **`TechPro.slnx` em vez de `.sln`**: o .NET 10 gera o formato novo por
  padrão; mantido (suportado por CLI e IDEs atuais). Todos os comandos usam
  `backend/TechPro.slnx`.
- **Swagger sem security definitions** por ora: a API do Microsoft.OpenApi 2.x
  mudou e o orval só precisa de paths/schemas. Revisitar se o Swagger UI
  precisar de botão "Authorize".
- **Guard de rota é só UX**: a segurança real está na API (JWT + GQF + RLS).
  Quando houver páginas server-rendered com dados, considerar `proxy.ts`
  (novo nome do middleware no Next 16).
- **Bug corrigido do init do shadcn**: ele gerou `--font-sans: var(--font-sans)`
  (auto-referência) no `globals.css`, derrubando a UI para serif.

## Notas de ambiente (máquina de dev)

- **Smart App Control (Windows) em enforcement desde 2026-07-05 à noite**:
  bloqueia DLLs não assinadas compiladas localmente — `dotnet test` local
  falha com `0x800711C7`. Contorno adotado: rodar testes no container do SDK
  (mesmo ambiente do CI). Desligar o SAC resolve, mas é decisão irreversível
  do dono da máquina (Configurações → Segurança do Windows → Controle de
  aplicativos e navegador).
- **Bind mount Windows→Linux não propaga eventos de arquivo**: edições no
  host podem não disparar hot-reload do Next dentro do container — reiniciar
  o serviço (`docker compose restart frontend`) força a recompilação.

---

## Próximos passos sugeridos

1. Publicar o repositório no GitHub e ver o CI verde no primeiro push.
2. Fase 1 do produto (docs/fases_MVP.md): módulo de Ordens de Serviço —
   primeiro uso do `RlsHelper` em tabela de produto (PK UUID +
   `updated_at`/`deleted_at` desde já, groundwork do offline-first da Fase 2).
3. Confirmação de e-mail e recuperação de senha (Identity já suporta; falta
   provedor de e-mail — Resend, seção 7 do doc de stack).
4. Contas externas (checklist da seção 19): Cloudflare R2, Meta/WhatsApp,
   Resend, Render, Vercel, Sentry.
