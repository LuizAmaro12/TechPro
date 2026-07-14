# Etapa: OS e Kanban (módulo 3)

Data: 2026-07-14 · Ordem recomendada da Fase 1, item 5 (`docs/fases_MVP.md`).
Primeira etapa do **escopo offline do técnico** (seções 4 e 5 do doc de stack):
PK UUID, `updated_at`/`deleted_at`, endpoint de sync por delta e
`Idempotency-Key` estreiam aqui.

## Decisões aprovadas pelo usuário (2026-07-14)

1. **Escopo**: núcleo do módulo 3 (11 etapas, responsável técnico, prioridade,
   prazo estimado, status de pagamento e de aprovação como campos manuais até
   as etapas 6–7) + conversão automática do agendamento em OS no check-in +
   **histórico de etapas append-only** (alimenta linha do tempo e SLA da
   Fase 2). Diferidos (Fase 2, conforme doc de módulos): anexos/fotos (R2),
   comentários internos, SLA visual, reatribuição com histórico/motivo.
2. **Acompanhamento público da OS** (módulo 1, Fase 1): código opaco não
   enumerável por OS — rota `/acompanhar/{codigo}` sem login; base do QR
   compartilhável da Fase 2.
3. **@dnd-kit aprovado como dependência nova** (fora do doc de stack, decisão
   registrada): drag-and-drop do Kanban com boa experiência touch.
4. **Número da OS sequencial por empresa** (`numero` int único por tenant,
   "OS #124"); o UUID continua sendo a chave real.

## Decisões técnicas desta etapa (justificadas)

- **`IEntidadeSincronizavel`** (Id Guid, UpdatedAt, DeletedAt?): o DbContext
  carimba `UpdatedAt` automaticamente em todo INSERT/UPDATE dessas entidades
  (override de SaveChanges) — marca d'água de sync que não depende de ninguém
  lembrar de setar. Aplicada a `ordens_servico` e ao histórico de etapas
  (ambos no escopo offline do doc).
- **Idempotência na criação**: coluna `chave_idempotencia` (única por tenant,
  nullable) preenchida pelo header `Idempotency-Key`; repetição devolve a OS
  já criada (200) em vez de duplicar.
- **Sync por delta**: `GET /api/ordens-servico/sync?since=` devolve OS e
  histórico alterados desde a marca (incluindo soft-deletados) + `agora` do
  servidor para o próximo `since`.
- **Check-in cria a OS** (gancho previsto na etapa da agenda): nasce na etapa
  CheckInRealizado com cliente, serviço e snapshot do aparelho do agendamento.
  Agendamento avulso (sem cliente) usa o mesmo vínculo silencioso por telefone
  do portal; telefone inédito cria o cliente.
- **Coluna "Agendado" do Kanban mostra agendamentos** (ainda não são OS);
  arrastar para "Check-in realizado" faz o check-in e materializa a OS. As
  demais colunas mostram OS. Entregue/Cancelado ficam atrás de um filtro
  "mostrar finalizadas".
- **Movimentação de etapa livre** (qualquer → qualquer, correções permitidas);
  toda mudança grava histórico (de → para, usuário, instante). Cancelamento
  pede motivo.
- **Responsável técnico = qualquer usuário da empresa**: novo endpoint
  `GET /api/equipe` (usuários do tenant — tabela de plano de controle, filtro
  manual por TenantId + validação anti-IDOR ao atribuir).
- **Um serviço principal por OS na Fase 1** (orçamento item a item é Fase 2);
  aparelho: FK opcional para o CRM + snapshot marca/modelo (o que a conversão
  do agendamento tem).
- **Código de acompanhamento**: 16 chars aleatórios (RNG criptográfico),
  índice único global; a rota pública devolve apenas loja, número, serviço,
  etapa e prazo — nada de dados pessoais.

## Modelo de dados (`Modules/OrdensServico/`)

- `OrdemServico` (UUID, sync): TenantId, Numero (único/tenant), ClienteId,
  AparelhoId?, AparelhoMarca?, AparelhoModelo?, ServicoId, AgendamentoId?,
  Etapa (enum 11 valores, string no banco), Prioridade (Baixa|Normal|Alta),
  PrazoEstimado (DateOnly?), ResponsavelTecnicoId? (usuarios),
  StatusPagamento (NaoPago|Parcial|Pago),
  StatusAprovacao (Pendente|Aprovado|Recusado), DescricaoProblema?,
  Observacoes?, MotivoCancelamento?, CodigoAcompanhamento (único),
  ChaveIdempotencia?, CriadoEm, UpdatedAt, DeletedAt?.
- `OrdemServicoHistoricoEtapa` (UUID, sync): OrdemServicoId, DeEtapa?,
  ParaEtapa, UsuarioId?, Motivo?, CriadoEm, UpdatedAt, DeletedAt?.

## Endpoints

Internos: `GET /api/ordens-servico` (filtros etapa/busca/responsável/
incluirFinalizadas), `GET /{id}` (com histórico), `POST` (Idempotency-Key),
`PUT /{id}`, `POST /{id}/etapa` (move + histórico + motivo),
`GET /api/ordens-servico/sync?since=`, `GET /api/equipe`.
Público: `GET /api/publico/acompanhar/{codigo}` (rate limit "publico").

## Front-end

- `(empreendedor)/kanban`: colunas das etapas com @dnd-kit, cards (nº,
  cliente, serviço, prioridade, prazo, técnico, badges), coluna Agendado com
  agendamentos ativos, mover → POST etapa (motivo no Cancelado), filtro
  finalizadas. Link "Kanban" no nav.
- `(empreendedor)/ordens-servico`: listagem + criação manual (cliente
  obrigatório, serviço, prioridade, prazo, problema) + detalhe/edição
  (técnico, pagamento, aprovação, aparelho, observações) + link de
  acompanhamento copiável. Link "Ordens" no nav.
- `(portal-cliente)/acompanhar/[codigo]`: página pública de status no visual
  do guia.

## Passos de execução

1. Plano commitado.
2. Backend: entidades + `IEntidadeSincronizavel` + DbContext + migração com
   RLS nas 2 tabelas.
3. Backend: serviços/validadores/controllers + conversão no check-in +
   equipe + sync + rota pública; testes de integração.
4. Suíte completa verde no container; RLS verificado no Postgres.
5. Orval regen + validators Zod + `npm i @dnd-kit/core`.
6. Front-end: kanban, ordens-servico, acompanhar público, nav.
7. E2E navegador com evidência; screenshots.
8. Docs (`progresso.md`, README) e commits por passo.
