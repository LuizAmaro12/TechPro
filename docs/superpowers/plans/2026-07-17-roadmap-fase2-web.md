# Roadmap da Fase 2 — recorte WEB (mobile fica para o fim)

Data: 2026-07-17 · Base: `docs/fases_MVP.md` (Fase 2) e
`docs/TechPro-Modulos-e-Funcionalidades.md`. A Fase 1 está completa por módulo.

## Regra de escopo desta fase de trabalho

Só **web**. O planejamento mobile (app nativo React Native/Expo, offline-first,
seção 4 do doc de stack) permanece **literalmente a última etapa** do projeto —
iniciado só após todo o web estar concluído, estável, testado e documentado.

Nota importante do próprio doc: o **Portal do técnico da Fase 2 é web
responsivo** ("app nativo aumentaria custo sem necessidade validada"). Ainda
assim, por ser a ferramenta de campo, tratamos ele como um item web **posterior**
nesta ordenação — priorizamos primeiro o que não toca o fluxo de campo.

## Itens da Fase 2 classificados

### Web, sem dependência externa (candidatos imediatos)

- **Linha do tempo visual da OS** (portal) — o histórico de etapas já existe
  (`ordem_servico_historico_etapas`); é o mesmo dado do Kanban exibido como
  jornada com data/hora. Diferencial de transparência, custo baixo.
- **LGPD visível**: exportação (JSON dos dados do cliente) e exclusão por
  **anonimização** (preserva o histórico operacional; seção 16 do doc de stack
  já desenhou isso). Estrutural, compliance.
- **Financeiro Fase 2 (margem)**: lucro bruto, custo de peças, receita por
  serviço, serviços mais lucrativos, margem média. Os custos/preços já ficam
  congelados na peça da OS — a base existe.
- **OS/Kanban profundidade** ✅ (concluído em 2026-07-18): SLA visual por etapa
  (limite configurável por serviço + cor do card por tempo na etapa),
  comentários internos, reatribuição de técnico com histórico e motivo.
- **Catálogo/estoque**: histórico de movimentação de estoque ✅ e lista de
  compra consolidada ✅ (concluídos em 2026-07-20; "kits de serviço" já existe
  como peças padrão do serviço). Restam: peça compatível/equivalente, previsão
  de reposição e histórico de preço por fornecedor — todos agora com base de
  dados para existir.
- **Agendamento**: fila de espera; histórico de comparecimento por cliente;
  sinalização por indisponibilidade de peça (liga com estoque).
- **Clientes/reputação**: importação CSV; conta vinculada família/empresa com
  UI completa; avaliações (nota/comentário, NPS, pedido após entrega,
  fechamento de loop); indicador de risco de inadimplência.
- **Comunicação/config**: templates editáveis por evento; central de mensagens
  unificada por cliente; equipe com funções e permissões + histórico de ações.
- **Portal do técnico (web responsivo)** — item web, mas de fluxo de campo;
  fica para depois dos itens acima.

### Web, mas dependem de infra externa (só quando a conta existir)

- Evidência fotográfica, fotos antes/depois, anexos técnicos, QR/link → **R2**.
- Resposta automática a perguntas frequentes → **API de WhatsApp**.
- Envio real de notificações/templates → Evolution + Resend (hoje modo `log`).

### Mobile (ÚLTIMA ETAPA — não iniciar agora)

- App nativo do técnico (React Native/Expo, offline-first + sync). O schema e o
  endpoint `/sync` por delta já estão prontos desde a etapa de OS; o app em si
  é a etapa final do projeto, depois de todo o web.

## Ordenação sugerida do web (a confirmar com o usuário)

1. **Linha do tempo visual da OS** (portal) — doc lista como 1º da Fase 2;
   menor risco, dado pronto, alto valor de transparência.
2. **LGPD visível** (exportar/anonimizar) — compliance, estrutural.
3. **Financeiro Fase 2** (margem) — o diferencial de "quanto sobrou".
4. **OS/Kanban profundidade** (SLA, comentários, reatribuição).
5. Demais itens web; por último o **portal do técnico web** e, só depois de
   todo o web, o **app mobile**.

Cada item segue o ritmo já estabelecido: plano → decisões em aberto → backend
com testes → RLS conferido → orval → front → e2e → docs.
