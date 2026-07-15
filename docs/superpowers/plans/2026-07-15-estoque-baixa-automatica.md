# Etapa: Estoque com baixa automática (módulo 7 básico)

Data: 2026-07-15 · Ordem recomendada da Fase 1, item 6 (`docs/fases_MVP.md`).
Cadastro, quantidade, custo/preço, fornecedor, mínimo e alerta já existem do
catálogo — esta etapa entrega o que falta do básico: **baixa automática ao
usar peça em OS**.

## Decisões aprovadas pelo usuário (2026-07-15)

1. **Peças usadas na OS com sugestão do catálogo**: seção "Peças utilizadas"
   no detalhe da OS — adicionar peça com quantidade (baixa imediata, custo e
   preço de venda **congelados no momento do uso**, para a margem real do
   financeiro), remover devolve ao estoque, e um botão aplica de uma vez as
   "peças normalmente utilizadas" do serviço. Entradas/ajustes continuam pela
   edição da peça no catálogo; histórico completo de movimentação é Fase 2.
2. **Estoque negativo permitido com aviso** (escolha do usuário, diferente da
   recomendação de bloquear — registrado): a baixa nunca é recusada por falta
   de saldo; a resposta sinaliza estoque abaixo do mínimo/negativo e a UI
   avisa. Reflete contagem física divergente; correção é editar a peça.

## Decisões técnicas (justificadas)

- **`OrdemServicoPeca` no escopo offline** (`IEntidadeSincronizavel`): o app
  do técnico da Fase 2 "registra peça usada" em campo (módulo 4) — UUID +
  `updated_at` + `deleted_at` desde já; remover peça da OS é soft-delete
  (lápide sincronizável) + devolução ao estoque.
- **Sync por delta estendido**: `GET /api/ordens-servico/sync` passa a
  devolver também as peças utilizadas (com lápides).
- **OS finalizada (Entregue/Cancelado) não recebe nem devolve peças** — o
  registro histórico fica estável.
- **Aplicar peças padrão é idempotente**: peça já presente na OS não duplica.
- RLS via `RlsHelper` na tabela nova; peça de outro tenant "não existe" (GQF
  → 400), OS de outro tenant → 404.

## Modelo de dados

- `OrdemServicoPeca` (UUID, sync): TenantId, OrdemServicoId (Guid FK),
  PecaId (int FK Restrict), Quantidade (>0), CustoUnitarioNoUso,
  PrecoVendaNoUso, CriadoEm, UpdatedAt, DeletedAt.

## Endpoints

- `GET /api/ordens-servico/{id}/pecas`
- `POST /api/ordens-servico/{id}/pecas` `{pecaId, quantidade}` → 201 + flags
  de estoque (restante, abaixo do mínimo, negativo)
- `POST /api/ordens-servico/{id}/pecas/aplicar-padrao` → aplica as peças
  padrão do serviço da OS (idempotente)
- `DELETE /api/ordens-servico/{id}/pecas/{pecaUsadaId}` → devolve estoque
- Detalhe da OS passa a incluir a lista de peças utilizadas; sync inclui o
  delta delas.

## Front-end

- Detalhe da OS (`/ordens-servico`): seção "Peças utilizadas" — select de
  peça (com estoque atual visível), quantidade, Adicionar, botão "Aplicar
  peças padrão do serviço", lista com remover e custo total; toasts de aviso
  quando a baixa deixa a peça abaixo do mínimo ou negativa.

## Passos

1. Plano commitado.
2. Backend: entidade + DbContext + migração com RLS; serviço + endpoints +
   sync; testes de integração (baixa/custo congelado, devolução com lápide,
   negativo com flag, aplicar padrão idempotente, OS finalizada, isolamento).
3. Suíte verde no container; RLS verificado no Postgres.
4. Orval regen; front na tela da OS; e2e com evidência.
5. Docs (`progresso.md`, README) e commits por passo.
