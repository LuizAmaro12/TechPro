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

Suíte de testes do back-end: **97 testes xUnit verdes** (GQF por convenção,
TokenService, fluxo de auth, catálogo, clientes, agenda, OS, estoque,
financeiro, comunicação, dashboard e onboarding — integração via
WebApplicationFactory + Sqlite em memória).

### Etapa Financeiro básico concluída em 2026-07-16 (fecha a lacuna do módulo 8)

Módulo 8 (essenciais da Fase 1). Plano e decisões em
`docs/superpowers/plans/2026-07-16-financeiro-basico.md`.
Evidência e2e (Playwright + Edge, 2026-07-16):

```json
{
  "dashboardLevaAoFinanceiro": true,
  "kpisFaturamentoETicket": true,
  "aReceberSoAprovado": true,
  "projecaoDeCaixa": true,
  "composicaoPorForma": true,
  "tabelaDeTransacoes": true,
  "filtroDePeriodoFunciona": true,
  "apiConfere": true
}
```

> **Ordem recomendada da Fase 1: 10/10 concluídos em 2026-07-16**, cada um com
> testes de integração, RLS verificado no Postgres real e evidência e2e no
> navegador. O **critério de conclusão** da Fase 1 (`docs/fases_MVP.md`) — "a
> loja se cadastra, configura serviços e peças, recebe um agendamento, gera uma
> OS, move até a entrega, baixa estoque, registra pagamento, notifica o cliente
> e vê o estado no dashboard, sem intervenção manual do fundador" — está
> atendido de ponta a ponta.
>
> **Correção registrada em 2026-07-16:** um resumo anterior declarou "Fase 1
> completa". Isso valia para a *ordem recomendada* (10 itens), mas **não** para
> o *escopo por módulo* da Fase 1, que tinha duas lacunas reais. O **módulo 8
> (Financeiro básico)** foi fechado em 2026-07-16 (ver etapa abaixo); resta o
> **módulo 13 (Configurações e equipe básica)**. A Fase 1 **não** está fechada
> enquanto ele não for entregue.

## Lacunas conhecidas da Fase 1 (a fazer)

Levantadas na auditoria de 2026-07-16, conferindo o *escopo por módulo* de
`docs/fases_MVP.md` contra o código. **Módulo 8 resolvido em 2026-07-16.**

### Módulo 13 — Configurações e equipe básica (majoritariamente pendente)

| Item do escopo | Status |
|---|---|
| Horários da loja | ✅ em `/agenda/configuracoes` |
| Endereço público (slug) | ✅ em `/agenda/configuracoes` |
| Dados da loja: nome editável, contatos, políticas | ❌ nome só é definido no cadastro |
| Logo da loja | ❌ bloqueado por Cloudflare R2 (não provisionado) |
| Conta do usuário (perfil, troca de senha) | ❌ |
| Preferências básicas de notificação | ❌ |
| Convite de equipe | ❌ deferido (o doc põe permissões granulares na Fase 2) |

### Etapa Onboarding guiado concluída em 2026-07-16 (fecha a Fase 1)

Módulo 0/13 — item 10 da ordem recomendada. O wizard encapsula os fluxos reais
já construídos (reusa horários/serviços/peças). Plano e decisões em
`docs/superpowers/plans/2026-07-16-onboarding-guiado.md`.
Evidência e2e (Playwright + Edge, 2026-07-16):

```json
{
  "redirecionaParaWizardNoPrimeiroAcesso": true,
  "servicosCadastradosNoWizard": true,
  "finalizaEVaiParaDashboard": true,
  "cardAtivacaoEDadosExemploVisiveis": true,
  "osExemploNoKanban": true,
  "removeDadosExemplo": true,
  "onboardingConcluido": true,
  "semDadosExemploAposRemover": true,
  "servicoContaComoPasso": true
}
```

### Etapa Dashboard essencial concluída em 2026-07-16

Módulo 12 — item 9 da ordem recomendada da Fase 1. Agregação read-only dos
dados que já existem (sem entidade, sem migração). Plano e decisões em
`docs/superpowers/plans/2026-07-15-dashboard-essencial.md`.
Evidência e2e (Playwright + Edge, 2026-07-16):

```json
{
  "kpisCorretos": true,
  "radarMostraAtrasada": true,
  "faturamentoVisivel": true,
  "kpiLevaAoKanban": true,
  "apiConfere": true
}
```

### Etapa Comunicação essencial concluída em 2026-07-15

Módulo 9 — item 8 da ordem recomendada da Fase 1. Abordagem (a): provedor
abstraído com adaptador **log como padrão** (dev/teste sem envio real),
Evolution (WhatsApp) e Resend (e-mail) selecionáveis por flag — "pronto para
plugar". Plano e decisões em
`docs/superpowers/plans/2026-07-15-comunicacao-essencial.md`.
Evidência e2e (Playwright + Edge, 2026-07-15, modo simulado):

```json
{
  "notificacoesRegistradasNaOs": true,
  "modoSimulacaoVisivel": true,
  "supressaoPorConsentimentoVisivel": true,
  "osComConsentimentoTemQuatroMensagens": true,
  "todasSimuladas": true,
  "semConsentimentoSuprimida": true
}
```

Verificado também contra o Postgres real: RLS ENABLE+FORCE em
`mensagens_enviadas` (fail-closed sem tenant), Hangfire criou suas 12 tabelas
no schema próprio, e o smoke test confirmou o registro por canal (WhatsApp +
e-mail) ao criar uma OS.

### Etapa Orçamento e pagamento básico concluída em 2026-07-15

Módulos 8/11 básico — item 7 da ordem recomendada da Fase 1 ("orçamento,
aprovação simples e pagamento básico"). Inclui a **trilha de auditoria
append-only de aprovação** (seção 16 do doc de stack, diferencial do
branding). Plano e decisões em
`docs/superpowers/plans/2026-07-15-orcamento-e-pagamento.md`.
Evidência e2e (Playwright + Edge, 2026-07-15):

```json
{
  "rascunhoSalvoComTotal": true,
  "orcamentoEnviado": true,
  "etapaMudouParaAguardando": true,
  "pagamentoParcialRegistrado": true,
  "aprovadoNoPortal": true,
  "trilhaComEnvioEAprovacaoPortal": true,
  "statusAprovacaoDerivado": true,
  "quitadoStatusPago": true
}
```

O `trilhaComEnvioEAprovacaoPortal` prova a trilha de ponta a ponta: envio pela
loja + aprovação pelo cliente no portal, cada evento com seu canal. RLS
conferido: `orcamentos`, `orcamento_eventos` e `pagamentos` com
`relrowsecurity = t` e `relforcerowsecurity = t`; fail-closed sem tenant.

### Etapa Estoque com baixa automática concluída em 2026-07-15

Módulo 7 básico — item 6 da ordem recomendada. Cadastro/quantidade/custo/
mínimo/alerta já existiam do catálogo; a etapa entregou a **baixa automática
ao usar peça em OS**. Plano e decisões em
`docs/superpowers/plans/2026-07-15-estoque-baixa-automatica.md`.
Evidência e2e (Playwright + Edge, 2026-07-15):

```json
{
  "pecasPadraoAplicadas": true,
  "baixaComAvisoDeMinimo": true,
  "estoqueTelaBaixou": true,
  "estoqueColaBaixou": true,
  "negativoPermitidoComAviso": true,
  "totalDePecasCorreto": true,
  "devolucaoAoRemover": true
}
```

RLS conferido: `ordem_servico_pecas` com `relrowsecurity = t` e
`relforcerowsecurity = t`; fail-closed confirmado sem `app.tenant_id`.

### Etapa OS e Kanban concluída em 2026-07-15

Módulo 3 (ordem de serviço e Kanban) + acompanhamento público de status do
módulo 1 — item 5 da ordem recomendada da Fase 1. Primeira etapa do **escopo
offline do técnico** (UUID, `updated_at`/`deleted_at`, sync por delta e
Idempotency-Key). Plano e decisões aprovadas em
`docs/superpowers/plans/2026-07-14-os-e-kanban.md`.
Evidência e2e (Playwright + Edge, 2026-07-15):

```json
{
  "osManualCriada": true,
  "detalheComLinkETrilha": true,
  "checkinNoKanbanCriaOs": true,
  "moverPorSelectFunciona": true,
  "dragAndDropFunciona": true,
  "cancelamentoComMotivo": true,
  "acompanhamentoPublicoFunciona": true,
  "codigoInvalido404": true
}
```

O `checkinNoKanbanCriaOs` prova a conversão automática de ponta a ponta: o
card do agendamento arrastado/clicado em check-in vira a OS #2 na coluna
seguinte. RLS conferido no banco: `ordens_servico` e
`ordem_servico_historico_etapas` com `relrowsecurity = t` e
`relforcerowsecurity = t`; fail-closed confirmado sem `app.tenant_id`.

### Etapa Agenda e portal de agendamento concluída em 2026-07-14

Módulo 2 (agendamento e calendário) + o fluxo de agendamento do módulo 1
(portal do cliente) — item 4 da ordem recomendada da Fase 1 — de ponta a
ponta: API + RLS verificado no Postgres + cliente orval + telas + portal
público sem login. Plano e decisões aprovadas em
`docs/superpowers/plans/2026-07-13-agenda-e-portal-agendamento.md`.
Evidência e2e (Playwright + Edge, 2026-07-14):

```json
{
  "navAgendaFunciona": true,
  "agendamentoManualCriado": true,
  "checkinFunciona": true,
  "configuracoesCarregam": true,
  "slotOcupadoSomeDoPortal": true,
  "portalAgendamentoConcluido": true,
  "portalApareceNaAgendaDaLoja": true,
  "clienteCriadoPeloPortal": true
}
```

O `slotOcupadoSomeDoPortal` prova a capacidade simultânea de ponta a ponta:
um agendamento manual às 14:00 fez o horário sumir da grade pública.
RLS conferido no banco: `agendamentos`, `bloqueios_agenda` e
`horarios_funcionamento` com `relrowsecurity = t` e `relforcerowsecurity = t`;
fail-closed confirmado (`count(*) = 0` sem `app.tenant_id` na sessão).

### Etapa Clientes e aparelhos concluída em 2026-07-12

Módulo 5 (CRM básico) — item 3 da ordem recomendada da Fase 1 — de ponta a
ponta: API + RLS verificado no Postgres + cliente orval + tela. Evidência
e2e (Playwright + Edge, 2026-07-12):

```json
{
  "navClientesFunciona": true,
  "clienteCriado": true,
  "aparelhoAdicionado": true,
  "badgeVipNaTabela": true,
  "contagemAparelhos": true,
  "vinculoCriadoComBadge": true,
  "filtroVipEsconde": true,
  "buscaPorTelefone": true,
  "desativarEsconde": true,
  "inativoApareceComFiltro": true
}
```

RLS conferido no banco: `clientes` e `aparelhos` com `relrowsecurity = t`
e `relforcerowsecurity = t`.

### Etapa Catálogo concluída em 2026-07-08

Módulo 6 (serviços e peças) — item 2 da ordem recomendada da Fase 1 — de
ponta a ponta: API + RLS verificado no Postgres + cliente orval + telas.
Evidência e2e (Playwright + Edge, 2026-07-08):

```json
{
  "cadastroLevaAoDashboard": true,
  "navPecasFunciona": true,
  "pecaCriadaAparaceNaTabela": true,
  "fornecedorVinculado": true,
  "semAlertaEstoqueBaixo": true,
  "servicoCriadoComChecklistEPeca": true,
  "edicaoRecarregaChecklist": true,
  "edicaoRecarregaPeca": true,
  "desativarRemoveDaListagemPadrao": true,
  "inativoApareceComFiltro": true
}
```

RLS conferido direto no banco: as 5 tabelas novas (`fornecedores`, `pecas`,
`servicos`, `servico_pecas`, `servico_checklist_itens`) com
`relrowsecurity = t` e `relforcerowsecurity = t`.

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
  (NOSUPERUSER, **NOBYPASSRLS**); `empresas` com ENABLE+FORCE RLS.
- **Policies de `empresas` (revisadas em 2026-07-13)**: leitura **pública**
  (a rota de agendamento resolve slug → empresa antes de existir tenant e a
  tabela é só diretório — id, nome, slug, criado_em), INSERT público para o
  cadastro e UPDATE restrito à própria empresa (edição de slug). A policy de
  UPDATE **não existia** até então — o FORCE RLS negaria o UPDATE em silêncio;
  descoberta registrada em "Desvios e notas".
- **Tenant fixado fora do JWT** (`TenantAmbiente`): a rota pública resolve o
  slug e fixa o tenant da requisição; `HttpTenantProvider` o consulta antes da
  claim — GQF e RLS passam a valer normalmente, sem isolamento reimplementado
  à mão no fluxo público.
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

### Catálogo (módulo 6 — primeira etapa de produto)

- **Serviços** (`/api/servicos` + tela `/servicos`): preço base, categoria
  (texto livre com sugestões), duração, prazo médio, exige diagnóstico,
  agendável online, **capacidade simultânea** (consumida pela agenda depois),
  **checklist padrão ordenado** (tabela própria — a Fase 2 marca item a item
  na OS) e **peças normalmente utilizadas** com quantidade padrão.
- **Peças** (`/api/pecas` + tela `/pecas`): custo, preço de venda, quantidade,
  estoque mínimo com **alerta de estoque baixo** e fornecedor.
- **Fornecedores** (`/api/fornecedores`, entidade mínima): a Fase 2 precisa de
  histórico de preço por fornecedor — campo texto viraria migração de dados
  reais. Fornecedor com peça vinculada não pode ser removido (409).
- **Exclusão = desativação** (`ativo=false`): serviço/peça podem estar
  referenciados por OS futuras; listagem padrão esconde inativos
  (`incluirInativos=true` os revela).
- **Isolamento testado na API**: empresa B não lista, não lê, não altera nem
  desativa itens de A (404 via GQF), e **não consegue vincular peça de A a um
  serviço seu** (400 — o teste anti-IDOR `ServicoNaoAceitaPecaDeOutraEmpresa`).
- Primeiro uso real do `RlsHelper` em tabelas de produto; PK `int` identity
  (UUID + `updated_at`/`deleted_at` seguem exclusivos do escopo offline do
  técnico — seção 5 do doc de stack).

### Clientes e aparelhos (módulo 5 — CRM básico)

- **Clientes** (`/api/clientes` + tela `/clientes`): cadastro completo por
  decisão aprovada em 2026-07-12 (nome e telefone obrigatórios; e-mail, CPF,
  endereço e observações opcionais), flag **VIP manual** ("recorrente" será
  derivado quando OS existir), busca por nome/telefone/CPF e filtros
  ativos/inativos/VIP.
- **Consentimento LGPD** (módulo 14, Fase 1): checkbox de comunicação
  operacional com carimbo de data na concessão; revogar limpa o carimbo.
- **Conta vinculada família/empresa**: FK `cliente_principal_id` com regra de
  **1 nível** (sem auto-vínculo, sem cadeia, principal com dependentes não
  vira vinculado) + UI mínima (select "Vinculado a" e badge na listagem).
- **Aparelhos** como sub-recurso (`/api/clientes/{id}/aparelhos`): marca,
  modelo, IMEI/nº de série, senha de desbloqueio e observações, gerenciados
  na própria tela do cliente.
- **Isolamento testado**: empresa B não lista/lê/altera clientes de A (404),
  não vincula cliente de A como principal (400 anti-IDOR) e não adiciona
  aparelho em cliente de A (404).
- Exclusão = desativação; a anonimização LGPD (seção 16) entra na Fase 2 sem
  migração destrutiva.

### Agenda e portal de agendamento (módulo 2 + fluxo do módulo 1)

- **Horários de funcionamento** (`/api/agenda/horarios` + tela
  `/agenda/configuracoes`): um registro por dia da semana (abertura,
  fechamento, intervalo opcional); dia sem registro ou inativo = fechado
  (fail-closed). Salvo em lote (os 7 dias num PUT).
- **Bloqueios pontuais** (`/api/agenda/bloqueios`): data + faixa de horário +
  motivo; somem da grade de disponibilidade. Exclusão física permitida —
  bloqueio é configuração operacional, não registro de negócio.
- **Disponibilidade em slots de 30 min** (`/api/agenda/disponibilidade`):
  grade dentro do horário do dia, menos intervalo e bloqueios; um serviço
  ocupa `ceil(duração/30)` slots e a **capacidade simultânea** do serviço
  (campo criado no catálogo) limita sobreposições por sub-slot. Aritmética em
  minutos inteiros — `TimeOnly.AddMinutes` dá a volta na meia-noite.
- **Agendamentos** (`/api/agendamentos` + tela `/agenda`): criação manual
  (cliente vinculado ou contato avulso — snapshot de nome/telefone),
  reagendamento (marca `reagendado_em`; só no status Agendado), **check-in**
  (gancho da conversão em OS na etapa 5) e cancelamento com motivo.
  Calendário próprio em Tailwind com visões dia/semana/mês (sem lib nova,
  conforme o doc de stack).
- **Slug público por empresa** (`empresas.slug`, único global): gerado do nome
  no cadastro (`GeradorDeSlug` — minúsculas, sem acentos, hífens), editável em
  configurações com URL copiável; conflito responde 409.
- **Rota pública `/api/publico/{slug}`** (sem login, rate limit próprio
  30/min/IP): `info` (nome + serviços com `agendavel_online=true`),
  `disponibilidade` e criação de agendamento. **Vínculo silencioso por
  telefone** (decisão aprovada 2026-07-13): telefone que bate com cliente
  ativo (comparação só por dígitos) vincula sem expor nada do cadastro;
  telefone inédito cria cliente novo no CRM.
- **Portal `/agendar/{slug}`** (grupo `(portal-cliente)`, sem guard): wizard
  progressivo — identificação → aparelho/problema → serviço → data/horário →
  confirmação — no visual do guia. Anexos entram quando o R2 existir.
- **Isolamento testado também no fluxo público**: slug da loja B + serviço da
  loja A → 400 em disponibilidade e criação (GQF com tenant fixado); B não
  lista nem faz check-in em agendamento de A (404); enums viajam como string
  no JSON (`JsonStringEnumConverter`).

### OS e Kanban (módulo 3 — primeira etapa do escopo offline)

- **OrdemServico** (`/api/ordens-servico` + telas `/ordens-servico` e
  `/kanban`): 10 etapas (a coluna "Agendado" do Kanban mostra agendamentos que
  ainda não viraram OS), responsável técnico, prioridade, prazo estimado,
  status de pagamento e de aprovação (campos manuais até as etapas 6–7),
  problema, observações e snapshot de aparelho (FK opcional para o CRM).
- **Escopo offline estreou** (seções 4 e 5 do doc de stack):
  `IEntidadeSincronizavel` (UUID + `updated_at` carimbado automaticamente
  pelo DbContext no SaveChanges + `deleted_at` como lápide),
  `GET /api/ordens-servico/sync?since=` (delta com lápides e `agora` do
  servidor) e `Idempotency-Key` na criação (coluna única por tenant —
  reenvio devolve a mesma OS).
- **Número sequencial por empresa** (decisão 2026-07-14): "OS #124" único por
  tenant, sem vazar volume entre empresas; o UUID segue como chave real.
- **Trilha de etapas append-only** (`ordem_servico_historico_etapas`): toda
  mudança grava de → para, usuário e motivo — alimenta a linha do tempo e o
  SLA visual da Fase 2. Movimentação livre entre etapas (correções
  permitidas); cancelar exige motivo.
- **Conversão automática no check-in**: o check-in do agendamento cria a OS
  na mesma transação (cliente, serviço, snapshot do aparelho e problema);
  agendamento avulso usa o vínculo silencioso por telefone (helper movido
  para o `ClienteService`, compartilhado com o portal).
- **Acompanhamento público** (decisão 2026-07-14): código opaco de 16 chars
  (RNG criptográfico) por OS; rota `GET /api/publico/{slug}/acompanhar/{codigo}`
  reusa o padrão de tenant fixado por slug (sem afrouxar RLS) e expõe só
  loja, número, serviço, etapa e prazo. Página `/acompanhar/{slug}/{codigo}`
  com régua do fluxo.
- **Kanban com @dnd-kit** (dependência nova aprovada em 2026-07-14): drag
  entre colunas + select "mover" por card como fallback touch; arrastar
  agendamento para "Check-in realizado" faz o check-in; Entregue/Cancelado
  atrás do filtro "mostrar finalizadas".
- **`GET /api/equipe`**: usuários da empresa para o select de responsável
  (plano de controle sem GQF — filtro por tenant explícito + validação
  anti-IDOR ao atribuir responsável, coberta por teste).
- **Isolamento testado**: B não lista/lê/move OS de A (404), não cria OS com
  cliente de A (400); código de acompanhamento certo no slug errado → 404.

### Estoque com baixa automática (módulo 7 básico)

- **Peças utilizadas na OS** (`/api/ordens-servico/{id}/pecas` + seção no
  detalhe da OS): adicionar baixa o estoque na hora e **congela custo e preço
  de venda no momento do uso** (`ordem_servico_pecas` — a margem real do
  financeiro nasce daqui); remover devolve ao estoque via **soft-delete**
  (lápide sincronizável); total em peças exibido na OS.
- **Estoque negativo permitido com aviso** (decisão do usuário 2026-07-15,
  diferente da recomendação de bloquear): a baixa nunca é recusada; a resposta
  traz flags (restante, abaixo do mínimo, negativo) e a UI avisa por toast.
  Correção de contagem é editar a peça no catálogo.
- **Aplicar peças padrão do serviço** (idempotente): um clique registra as
  "peças normalmente utilizadas" do catálogo, pulando as já presentes.
- **OS finalizada (Entregue/Cancelado) não recebe nem devolve peças** — o
  registro histórico fica estável.
- **Sync por delta estendido**: as peças utilizadas (com lápides) entram no
  `GET /api/ordens-servico/sync` — o app do técnico da Fase 2 registra peça
  usada offline (módulo 4).
- Entradas/ajustes de estoque continuam pela edição da peça; histórico
  completo de movimentação, previsão de reposição e lista de compra são
  Fase 2 (doc de módulos).
- **Isolamento testado**: peça de A não entra em OS de B (400); OS de B "não
  existe" para A (404).

### Orçamento e pagamento (módulos 8/11 básico + portal)

- **Orçamento da OS** (`/api/ordens-servico/{id}/orcamento` + seção no detalhe):
  mão de obra editável (sugerida do preço base) + peças utilizadas (preço
  **congelado no envio** — o que o cliente vê não muda se a loja registrar
  mais peças depois) − desconto. Um orçamento por OS na Fase 1 (item a item é
  Fase 2). Editar um orçamento já respondido volta o status a Rascunho,
  preservando a trilha.
- **Trilha de auditoria append-only** (`orcamento_eventos`, seção 16 do doc de
  stack — diferencial do branding): cada envio/aprovação/recusa grava tipo,
  **canal** (Loja/Portal), usuário (quando loja), valor total e motivo, nunca
  sobrescrita.
- **Aprovação binária** pela loja (registro manual "aprovou pelo WhatsApp") e
  pelo cliente no portal `/acompanhar/{slug}/{codigo}` sem login. **Só o envio
  move etapa** (para Aguardando aprovação, com histórico); aprovar/recusar só
  atualizam o status — a loja decide o próximo passo no Kanban.
- **Pagamentos parciais** (`/api/ordens-servico/{id}/pagamentos`): vários por
  OS com forma (dinheiro/Pix/débito/crédito/outro); podem ser removidos (erro
  de digitação). **`StatusPagamento` e `StatusAprovacao` da OS agora derivados**
  dos fluxos reais — saíram do PUT manual da OS (eram campos manuais desde a
  etapa de OS). Sem orçamento, pagamento marca no máximo Parcial.
- **Acompanhamento público** passou a incluir o orçamento (só depois de
  enviado — rascunho é interno) e os endpoints de aprovar/recusar, sob o mesmo
  padrão de tenant fixado por slug + código opaco + rate limiting "publico".
- **Fora do escopo offline** (PK `int`, sem sync): aprovação exige trilha
  append-only, nunca last-write-wins (seção 4 do doc de stack) — decisão
  consciente que separa o financeiro do fluxo de campo do técnico.
- **Isolamento testado**: orçamento/pagamento de A "não existem" para B (404).

### Comunicação essencial (módulo 9 — provedor abstraído)

- **Provedor abstraído** (`ICanalNotificacao`): adaptadores `LogWhatsAppCanal`/
  `LogEmailCanal` (padrão, só registram), `EvolutionWhatsAppCanal` e
  `ResendEmailCanal` selecionados por flag `Comunicacao:{Whatsapp,Email}:Provedor`
  (`log`|`evolution`|`resend`). Default `log` mantém dev/e2e determinístico.
- **ComunicacaoService**: disparo **automático e síncrono** por evento; respeita
  o **consentimento LGPD** (cliente sem consentimento → mensagem `Suprimida`,
  registrada para auditoria); envia em **todos os canais disponíveis** (WhatsApp
  sempre; e-mail se houver); falha de provedor externo vira `Falhou` e **nunca
  derruba a ação** que disparou (`ProtegerAsync`). Um registro `MensagemEnviada`
  por canal (RLS ENABLE+FORCE) — o "registro mínimo para auditoria" da Fase 1 e
  base do inbox unificado da Fase 2.
- **Eventos**: agendamento confirmado (criar), OS criada (criar + conversão do
  check-in), orçamento disponível (enviar), orçamento aprovado/recusado
  (responder — loja e portal), pronto para retirada (mudança de etapa).
- **Hangfire** (Postgres) para o **lembrete temporizado** (~3h antes; não agenda
  se já passou). Ligado só com `Comunicacao:Hangfire:Habilitado=true` (docker);
  sem a flag, `IAgendadorDeLembretes` é no-op — testes/`dotnet run` puro não
  dependem de Postgres/Hangfire. O `LembreteJob` roda fora do HTTP e fixa o
  tenant via `TenantAmbiente` (padrão das rotas públicas); só envia se o
  agendamento ainda estiver `Agendado` (cancelado/check-in → não envia).
- **Endpoint** `GET /api/ordens-servico/{id}/mensagens` (auditoria) + seção
  "Notificações enviadas" no detalhe da OS (canal, evento, status, horário).
- **Isolamento testado**: mensagens de A não aparecem para B (GQF).

### Dashboard essencial (módulo 12 — agregação read-only)

- **`GET /api/dashboard`** (módulo `Dashboard/`, sem entidade/migração): 6 KPIs
  da Fase 1 — OS abertas (não finalizadas), agendamentos do dia, serviços em
  atraso (prazo vencido e não finalizada), **aparelhos em reparo = bancada
  inteira** (NaFila→EmTeste), prontos para retirada, e **faturamento do mês =
  pagamentos recebidos no mês** (caixa real).
- **Comparativo** faturamento mês atual vs. anterior + variação % (null quando
  o anterior é zero).
- **"Radar do dia"**: OS atrasadas e orçamentos pendentes há mais de 2 dias
  (com link para a OS), listas limitadas a 10 com o total sinalizado. O
  terceiro item do doc ("peça que chegou libera reparo parado") ficou de fora —
  depende de rastreio de chegada de peça, que só existe com entradas de estoque
  (Fase 2). Anotado.
- Leitura pura sob GQF; somas de decimal em memória (Sqlite dos testes);
  "hoje"/"mês" são a data UTC do servidor (hora de parede da loja).
- **Front `/dashboard`** deixou de exibir empresa/papel/tenant e virou o painel:
  radar no topo, KPIs clicáveis (levam a Kanban/Agenda/OS) e faturamento com
  tendência. Isolamento testado (dashboard de A zerado para B).

### Financeiro básico (módulo 8 — visão de caixa)

- **`GET /api/financeiro?de&ate`** + tela **`/financeiro`** (a rota prevista na
  seção 13 do doc de stack): leitura pura sob GQF, sem entidade nem migração.
- **Faturamento por período** (presets Hoje/7 dias/Este mês/Mês passado +
  intervalo livre; default = mês corrente), **transações** (data, OS, cliente,
  forma, valor), **composição por forma de pagamento** e **ticket médio =
  faturamento ÷ nº de OS distintas pagas** (decisão 2026-07-16 — coerente com o
  faturamento, que é caixa recebido).
- **A receber = OS viva com orçamento APROVADO e saldo em aberto** (decisão
  2026-07-16): orçamento só enviado é proposta, não receita vendida, e OS
  cancelada sai da conta. Ambos cobertos por teste.
- **Projeção de caixa** ("quanto está para entrar", item novo do doc): a receber
  + valor esperado dos agendamentos dos próximos 7 dias (estimado pelo preço
  base do serviço — a UI deixa explícito que o orçamento final pode diferir).
- "A receber" e a projeção são **visão atual**, não filtradas pelo período —
  coberto por teste para evitar interpretação errada.
- Margem, lucro bruto, receita por serviço e relatórios exportáveis seguem na
  Fase 2 (doc); a base para eles (custo x preço congelados na peça da OS) já
  existe desde a etapa de estoque.

### Onboarding guiado (módulo 0 — encapsula os fluxos reais)

- **Wizard `/bem-vindo`** (5 passos): dados da loja (nome + slug editável),
  horários (setup rápido: dias abertos + um horário), serviços com **sugestões
  pré-preenchidas editáveis** (troca de tela/bateria/conector/limpeza/película),
  peças opcionais, e dados de exemplo + conclusão. Cada passo chama os
  endpoints já existentes — o backend do onboarding é só o entorno.
- **Redirecionamento no primeiro acesso**: `Empresa.OnboardingConcluidoEm`
  (nulo) → o dashboard leva ao wizard; "pular" ou concluir marca o carimbo e
  não redireciona mais.
- **Checklist de ativação derivado dos dados** (sempre exato, sem estado novo):
  loja, horários, serviço, peça, cliente — "X de 5". Card no dashboard com os
  passos pendentes (links) até completar.
- **Dados de exemplo removíveis** (decisão 2026-07-16): coluna `Exemplo` em
  `clientes`/`servicos`/`ordens_servico`; carregar cria um cliente + serviço +
  OS fictícios (direto, sem disparar notificações), remover limpa respeitando
  as FKs. Idempotente. Não contam como passos reais do checklist.
- **Deferidos com registro**: logo da loja (depende do Cloudflare R2) e convite
  de equipe (o doc coloca como Fase 2 no módulo 13; exige fluxo de convite).

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
- **Sem camada Repository** (o doc de stack a cita na estrutura de pastas):
  o DbContext + GQF já cumprem o papel; a camada extra entra apenas quando
  houver query complexa reutilizada. Padrão Controller fino + Service.
- **Kits de serviço e peça compatível/equivalente ficam para a Fase 2**
  (fases_MVP.md os lista lá, apesar de o doc de módulos citá-los no módulo 6).
- **Migrations agora também rodam no container do SDK** (Smart App Control):
  copiar o repo para `/work`, gerar lá e copiar `Migrations/*.cs` de volta —
  comando registrado no plano da etapa (docs/superpowers/plans/).
- **Senha de desbloqueio do aparelho em texto** (decisão aprovada 2026-07-12):
  a loja precisa lê-la, então hash não serve; candidata a criptografia de
  campo se surgir exigência — a criptografia em repouso do provedor cobre o
  disco. Tratar como dado sensível em qualquer exportação futura.
- **Diferimentos da etapa Agenda (aprovados 2026-07-13)**: lembretes
  automáticos → etapa 8 (Comunicação, Hangfire + WhatsApp/Resend); conversão
  automática em OS → etapa 5 (o status `CheckInRealizado` é o gancho); anexos
  no fluxo público → quando a conta Cloudflare R2 existir.
- **Datas da agenda são "hora de parede" da loja** (`DateOnly`/`TimeOnly`,
  sem timezone). O bloqueio de data passada no portal usa o dia UTC com
  tolerância de 1 dia — sem ela, uma loja UTC-3 não agendaria "hoje" à noite.
  Fuso explícito por loja só se surgir demanda real.
- **Corrida residual de vaga aceita no MVP**: a disponibilidade é revalidada
  imediatamente antes do INSERT, mas duas requisições simultâneas no mesmo
  slot podem passar (sem lock pessimista). Baixíssima probabilidade no volume
  esperado; revisitar com constraint/lock se aparecer na prática.
- **Lição de migração com RLS FORCE**: o backfill de `empresas.slug` foi
  bloqueado em silêncio pelo RLS (migração roda como `techpro_app`, sem
  policy de UPDATE e sem tenant na sessão) e o índice único falhou nos slugs
  vazios. Correção: a migração desliga/religa o RLS da tabela em volta do
  UPDATE (o dono da tabela pode). **Todo backfill futuro em tabela com FORCE
  RLS precisa disso** — o UPDATE não dá erro, só afeta zero linhas.
- **@dnd-kit/core adicionado ao front** (decisão aprovada 2026-07-14): única
  dependência fora da lista do doc de stack — drag-and-drop do Kanban com
  suporte a touch; o fallback por select em cada card cobre onde drag não
  opera.
- **Rota pública de acompanhamento leva o slug**
  (`/acompanhar/{slug}/{codigo}` em vez de só `{codigo}`, refinando o plano
  da etapa): o slug resolve o tenant pelo padrão já existente e o código é
  buscado sob GQF+RLS — sem criar exceção de RLS nova para busca global de
  código.
- **Sqlite dos testes não traduz DateTimeOffset** (comparação nem ORDER BY):
  o DbContext aplica `DateTimeOffsetToBinaryConverter` (workaround documentado
  da Microsoft) **somente quando o provider é Sqlite** — Postgres segue com
  `timestamptz` nativo; o filtro `since` do sync exigiu isso.
- **Número sequencial da OS = max+1 na transação**: corrida entre duas
  criações simultâneas no mesmo tenant faria a segunda falhar no índice único
  (erro, nunca duplicidade). Volume esperado torna isso raríssimo; retry
  automático fica anotado como melhoria se aparecer na prática.
- **Status de pagamento/aprovação da OS agora derivados** (etapa de
  orçamento): o `StatusPagamento`/`StatusAprovacao` deixaram de ser campos
  manuais no PUT da OS e passaram a ser recalculados pelo `FinanceiroService`
  (soma dos pagamentos vs. total do orçamento; status do orçamento). Os enums
  na entidade OS continuam existindo — são a projeção materializada que o
  Kanban e a listagem já consomem, agora sempre coerente com o financeiro.
- **Financeiro deliberadamente fora do escopo offline** (PK `int`, sem
  `updated_at`/`deleted_at`/sync): a seção 4 do doc de stack manda aprovação
  de orçamento usar trilha append-only em vez de last-write-wins, então
  orçamento e pagamento não sincronizam com o app do técnico — ficam só no
  portal web, como os demais módulos não-campo.
- **Somas em memória no financeiro**: o Sqlite dos testes não agrega `decimal`
  no servidor; as somas de peças/pagamentos por OS trazem poucas linhas e são
  feitas no cliente (Postgres continua eficiente com o mesmo código LINQ).
- **WhatsApp via Evolution API, não Meta Cloud API** (decisão do usuário
  2026-07-15): desvio da seção 7 do doc de stack, que agora traz a nota do
  desvio. A cautela do doc contra libs não oficiais (Baileys → risco de
  banimento do número) é reconhecida; mitigação é a abstração de provedor —
  troca-se para a Cloud API sem tocar no resto se preciso. WhatsApp segue em
  modo `log` até haver uma instância Evolution configurada.
- **Notificações imediatas são síncronas** (dentro da request; log/teste
  determinístico e resiliente por `ProtegerAsync`). Movê-las para jobs de
  background (Hangfire) é melhoria de resiliência/latência da Fase 2 — só o
  lembrete temporizado usa Hangfire hoje.
- **Segredo do Resend só no `.env`** (gitignored), dormente até
  `EMAIL_PROVEDOR=resend`. A key foi compartilhada no chat → **recomendada a
  rotação**. Sem domínio verificado no Resend, só `onboarding@resend.dev`
  envia, e apenas para o e-mail do dono da conta.
- **Dashboard do Hangfire** (`/hangfire`) só em Development, com filtro
  permissivo local — **produção exige um filtro de autorização real** (anotado
  no código).

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
- **Turbopack no container às vezes materializa uma pasta com o caminho
  Windows sanitizado** (ex.: `frontend/C:ProjetosPessoalTechProfrontend/`)
  cheia de chunks de dev — é artefato inofensivo, ignorado no
  `eslint.config.mjs`; pode ser apagado à vontade.

---

## Próximos passos sugeridos

### 1. Fechar a última lacuna da Fase 1 (código de produto)

- **Módulo 13 — Configurações e equipe básica**: dados da loja editáveis
  (nome, contatos, políticas), conta do usuário (perfil + troca de senha) e
  preferências básicas de notificação. Logo depende do R2.
- ~~Módulo 8 — Financeiro básico~~ ✅ concluído em 2026-07-16.

### 2. Operação e produção (fora do código de produto)

- Publicar o repositório no GitHub e ver o CI verde no primeiro push
  (job front + back já configurados).
- Provisionar produção conforme o doc de stack: Render (API + Postgres),
  Vercel (front). Migrations como passo de deploy, nunca automático.
- Contas externas (checklist da seção 19): Cloudflare R2, Meta/WhatsApp,
  Resend, Render, Vercel, Sentry.
- Ligar a comunicação de verdade: instância Evolution +
  `WHATSAPP_PROVEDOR=evolution`; domínio verificado no Resend +
  `EMAIL_PROVEDOR=resend`. Hoje ambos rodam em modo `log`.
- **Rotacionar a API key do Resend** (foi compartilhada em chat).
- Dashboard do Hangfire (`/hangfire`) precisa de filtro de autorização real
  antes de ir a produção (hoje é permissivo e só sobe em Development).

### 3. Fase 2 (doc de módulos)

App/Portal do técnico (React Native/Expo, offline-first — o schema/sync já
está pronto), financeiro com margem e rentabilidade, avaliações e reputação,
aprovação de orçamento item a item, linha do tempo visual da OS, evidência
fotográfica, importação de contatos, LGPD visível (exportação/anonimização),
central de mensagens unificada, kits de serviço, previsão de reposição.

### 4. Melhorias anotadas ao longo da Fase 1

- Radar "peça que chegou libera reparo parado" (depende de entradas de estoque).
- Notificações imediatas em background via Hangfire (hoje são síncronas).
- Confirmação de e-mail e recuperação de senha (Identity já suporta; depende do
  provedor de e-mail ligado).
- Logo da loja e convite de equipe no onboarding (R2 / fluxo de convite).
- Retry no número sequencial da OS se a corrida aparecer na prática.
