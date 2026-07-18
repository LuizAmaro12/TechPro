# Etapa: OS/Kanban em profundidade (Fase 2)

Data: 2026-07-18 · 4º item web da Fase 2. Escopo web.

Bloco coeso de três entregas que o doc lista juntas em "OS, Kanban e técnico":
**SLA visual por etapa**, **comentários internos** e **reatribuição de técnico
com histórico e motivo**.

## Decisões já fechadas pelos docs (sem pergunta)

- **SLA é por tipo de serviço** e conta o tempo **naquela etapa** — o doc de
  módulos é explícito: "cada card muda de cor gradualmente (verde → amarelo →
  vermelho) conforme o tempo naquela etapa se aproxima de um limite
  configurável por tipo de serviço".
- **Comentários são internos** — nunca aparecem no portal do cliente.
- **Reatribuição exige motivo** — o valor do recurso é a rastreabilidade
  ("importante caso um cliente questione quem mexeu no aparelho").

## Decisões técnicas (justificadas)

- **Faixas de cor fixas** (não configuráveis): verde < 70% do limite, amarelo
  70–100%, vermelho > 100%. Configurar as faixas *além* do limite seria ajuste
  sobre ajuste sem ganho real. Serviço sem SLA → card neutro.
- **Tempo na etapa atual** vem do histórico (última transição para a etapa
  corrente), calculado no servidor e exposto como `horasNaEtapa` — o front não
  recalcula regra de negócio.
- **Comentários entram no escopo offline** (UUID + `updated_at`/`deleted_at` +
  no endpoint `/sync`): o técnico comenta em campo, e o doc de stack manda
  tratar entidades do fluxo de campo assim **desde o primeiro migration**, não
  como retrofit. Mesmo raciocínio já aplicado a OS, histórico e peças usadas.
- **Reatribuição é append-only** (`ordem_servico_reatribuicoes`), como a trilha
  de orçamento: registra de-quem → para-quem, motivo, autor e quando. Nunca
  sobrescrita. Fora do escopo offline (é ato de gestão, não de campo).
- Remoção de comentário = **soft delete** (lápide sincronizável), coerente com
  o resto do escopo offline.

## Modelo de dados

- `Servico.SlaHoras` (int?, nulo = sem SLA).
- `OrdemServicoComentario` (UUID, sync): OrdemServicoId, Texto, AutorUsuarioId?,
  CriadoEm, UpdatedAt, DeletedAt.
- `OrdemServicoReatribuicao` (int, tenant): OrdemServicoId, DeUsuarioId?,
  ParaUsuarioId?, Motivo, PorUsuarioId?, CriadoEm.
- Migração com RLS nas duas tabelas novas.

## Endpoints

- `GET|POST /api/ordens-servico/{id}/comentarios`, `DELETE .../{comentarioId}`.
- `POST /api/ordens-servico/{id}/responsavel` — `{ responsavelTecnicoId, motivo }`;
  valida o técnico no tenant (anti-IDOR, como já é feito no PUT da OS) e grava
  a reatribuição. O detalhe da OS passa a devolver o histórico.
- `OrdemServicoResponse` ganha `HorasNaEtapa` e `SlaHoras` (do serviço) para o
  Kanban colorir; o `/sync` passa a incluir os comentários.

## Front-end

- **Kanban**: borda/indicador de cor por SLA + tooltip com as horas na etapa.
- **Detalhe da OS**: seção de comentários internos (adicionar/remover) e
  reatribuição de técnico com motivo + histórico das trocas.
- **Catálogo de serviços**: campo "SLA por etapa (horas)".

## Passos

1. Plano commitado.
2. Backend: entidades + coluna + DbContext + migração (RLS) + serviços e
   endpoints + `HorasNaEtapa`; testes (SLA calculado, comentários CRUD + soft
   delete no sync, reatribuição com histórico e anti-IDOR, isolamento).
3. Suíte verde; RLS conferido no Postgres.
4. Orval regen; front (Kanban, OS, serviços); e2e com evidência.
5. Docs (`progresso.md`, roadmap).
