# TechPro

SaaS de gestão operacional especializado em assistência técnica de celular e eletrônicos portáteis: agendamento, ordem de serviço, Kanban, estoque, comunicação e financeiro — multi-tenant com isolamento por empresa desde a fundação (LGPD).

## Estado atual (Fase 1 em andamento)

Concluído e verificado de ponta a ponta (backend com testes de integração, RLS
validado no Postgres real e fluxo e2e no navegador):

1. **Fundação** — multi-tenancy (Global Query Filter + RLS), auth JWT com
   refresh token httpOnly, cadastro/login, CI.
2. **Catálogo** — serviços, peças e fornecedores (módulo 6).
3. **Clientes e aparelhos** — CRM com vínculo familiar (1 nível), consentimento
   LGPD e aparelhos por cliente (módulo 5).

Suíte de testes do backend: 37/37 verdes. Próxima etapa da ordem recomendada
(`docs/fases_MVP.md`): **Agenda e portal de agendamento** (módulo 2). Detalhes,
decisões e evidências em `docs/progresso.md`.

## Como rodar (desenvolvimento local)

Pré-requisitos: Docker Desktop.

```bash
cp .env.example .env   # ajuste os segredos se quiser
docker compose up -d --build
```

- Front-end: http://localhost:3000
- API: http://localhost:5080 (Swagger em `/swagger` no ambiente Development)
- PostgreSQL: localhost:5432

## Estrutura do monorepo

```
frontend/   Next.js 16 (App Router, TypeScript estrito, Tailwind, shadcn/ui)
backend/    ASP.NET Core 10 Web API — monolito modular por domínio
mobile/     (Fase 2 — React Native/Expo, ainda não criada)
docs/       Branding, produto, stack/arquitetura, fases do MVP e progresso
.github/    Workflows de CI
```

As decisões de stack e arquitetura são vinculantes e estão em
`docs/TechPro-stack-arquitetura-infraestrutura.md`. O histórico de execução
fica em `docs/progresso.md`.
