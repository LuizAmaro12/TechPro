# TechPro

SaaS de gestão operacional especializado em assistência técnica de celular e eletrônicos portáteis: agendamento, ordem de serviço, Kanban, estoque, comunicação e financeiro — multi-tenant com isolamento por empresa desde a fundação (LGPD).

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
