# Etapa: Orçamento e pagamento básico (módulos 8/11 básico + portal)

Data: 2026-07-15 · Ordem recomendada da Fase 1, item 7 (`docs/fases_MVP.md`):
"Orçamento, aprovação simples e pagamento básico". Inclui a **trilha de
auditoria append-only de aprovação** exigida pela seção 16 do doc de stack
(diferencial do branding — quem, quando e o quê, nunca sobrescrita).

## Decisões aprovadas pelo usuário (2026-07-15)

1. **Composição**: mão de obra (editável, sugerida do preço base do serviço)
   + peças utilizadas (preço congelado no uso, automático) + desconto em R$.
   Mantém a separação serviço/peça (diferencial de modelagem).
2. **Aprovação binária no link de acompanhamento**: a página pública
   `/acompanhar/{slug}/{codigo}` exibe o orçamento com Aprovar/Recusar quando
   enviado; a loja também registra aprovação manual — tudo com trilha
   (canal Portal/Loja, usuário quando houver). Item a item é Fase 2.
3. **Pagamentos parciais com forma** (dinheiro, Pix, débito, crédito, outro):
   vários por OS; `StatusPagamento` passa a ser **derivado** (soma vs. total
   do orçamento) e sai da edição manual. Sem gateway na Fase 1.
4. **Só o envio move etapa**: enviar orçamento move a OS para
   "Aguardando aprovação" (com trilha de etapa); aprovar/recusar só atualizam
   o status — a loja decide o próximo passo no Kanban.

## Decisões técnicas (justificadas)

- **Módulo `Financeiro/`** (estrutura da seção 12 do doc de stack): Orcamento,
  OrcamentoEvento (trilha append-only) e Pagamento. PK `int` — orçamento e
  pagamento **não** estão no escopo offline (aprovação exige trilha, nunca
  last-write-wins; seção 4 do doc de stack).
- **Um orçamento por OS na Fase 1** (índice único); **o envio congela
  `ValorPecas`** — orçamento apresentado ao cliente é estável mesmo se a loja
  registrar mais peças depois. Editar após envio/resposta volta o status a
  Rascunho (e o da OS a Pendente) — a trilha preserva o histórico.
- **`StatusAprovacao` e `StatusPagamento` da OS viram derivados** dos fluxos
  reais e saem do PUT da OS (eram manuais desde a etapa de OS, como previsto).
  Sem orçamento, pagamento marca no máximo Parcial (não há total para quitar).
- **Pagamento pode ser removido** (erro de digitação) com recomputo do
  status — estorno formal fica para o financeiro da Fase 2/3.
- Endpoints públicos de aprovar/recusar sob o mesmo padrão do acompanhamento
  (slug fixa tenant + código opaco) e rate limiting "publico".

## Modelo de dados (`Modules/Financeiro/`)

- `Orcamento`: TenantId, OrdemServicoId (único), ValorMaoDeObra, Desconto,
  ValorPecas (congelado no envio; rascunho calcula ao vivo),
  Status (Rascunho|Enviado|Aprovado|Recusado), MotivoRecusa?, CriadoEm,
  EnviadoEm?, RespondidoEm?.
- `OrcamentoEvento` (append-only): OrcamentoId, Tipo (Enviado|Aprovado|
  Recusado), Canal (Loja|Portal), UsuarioId?, ValorTotal, Motivo?, CriadoEm.
- `Pagamento`: OrdemServicoId, Valor, Forma (Dinheiro|Pix|CartaoDebito|
  CartaoCredito|Outro), Observacao?, RegistradoPorUsuarioId?, CriadoEm.

## Endpoints

Internos: `GET/PUT api/ordens-servico/{id}/orcamento` (rascunho),
`POST .../orcamento/enviar|aprovar|recusar` (loja, com trilha),
`GET/POST/DELETE api/ordens-servico/{id}/pagamentos`.
Públicos: acompanhamento passa a incluir o orçamento (quando não-rascunho);
`POST api/publico/{slug}/acompanhar/{codigo}/orcamento/aprovar|recusar`.

## Front-end

- Detalhe da OS: seção Orçamento (mão de obra, desconto, peças, total,
  enviar/aprovar/recusar com trilha visível) e seção Pagamentos (lista,
  registrar valor+forma, remover, total pago e saldo); selects manuais de
  pagamento/aprovação saem da edição.
- Kanban: badge de aprovado/recusado no card.
- Portal `/acompanhar/{slug}/{codigo}`: box do orçamento com valores e
  botões Aprovar/Recusar quando enviado.

## Passos

1. Plano commitado.
2. Backend: entidades + DbContext + migração com RLS nas 3 tabelas; serviços,
   validadores, controllers (loja + público); derivação de status; testes.
3. Suíte verde no container; RLS verificado no Postgres.
4. Orval regen; front (OS, Kanban, portal); e2e com evidência.
5. Docs (`progresso.md`, README) e commits por passo.
