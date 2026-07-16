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
4. **Agenda e portal de agendamento** — calendário dia/semana/mês, horários de
   funcionamento, bloqueios, capacidade por serviço e portal público sem login
   em `/agendar/{slug}` (módulo 2 + fluxo do módulo 1).
5. **OS e Kanban** — ordem de serviço com número por loja, trilha de etapas,
   Kanban drag-and-drop, conversão automática do agendamento no check-in e
   acompanhamento público em `/acompanhar/{slug}/{codigo}` (módulo 3, já com
   o schema offline-ready: UUID, sync por delta e idempotência).
6. **Estoque com baixa automática** — peças usadas na OS com custo/preço
   congelados, devolução ao remover, sugestão das peças padrão do serviço e
   avisos de estoque baixo/negativo (módulo 7 básico).
7. **Orçamento e pagamento básico** — orçamento da OS (mão de obra + peças +
   desconto) com trilha de auditoria, aprovação binária pela loja ou pelo
   cliente no portal, e pagamentos parciais com status derivado (módulos
   8/11 básico).
8. **Comunicação essencial** — notificações por WhatsApp e e-mail nos eventos
   do fluxo (agendamento, OS, orçamento, pronto para retirada) com provedor
   abstraído (adaptador simulado por padrão; Evolution/Resend por flag),
   respeito ao consentimento LGPD e lembrete via Hangfire (módulo 9).
9. **Dashboard essencial** — painel com KPIs da operação (OS abertas,
   agendamentos do dia, atrasos, bancada, prontos, faturamento do mês),
   "Radar do dia" acionável e comparativo mês a mês (módulo 12).

Suíte de testes do backend: 85/85 verdes. Próxima etapa da ordem recomendada
(`docs/fases_MVP.md`): **Onboarding guiado** (módulo 13) — fecha a Fase 1.
Detalhes, decisões e evidências em `docs/progresso.md`.

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
