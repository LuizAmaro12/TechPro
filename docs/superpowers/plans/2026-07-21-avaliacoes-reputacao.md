# Etapa: Avaliações e reputação (Fase 2)

Data: 2026-07-21 · 10º item web da Fase 2. Módulo 10 do doc.

## Decisões do usuário (AskUserQuestion)

- **Escala: ambas** — estrelas 1–5 (experiência do reparo) + recomendação 0–10
  (NPS clássico). Atende o doc literalmente.
- **Escopo: núcleo + fechamento de loop + por técnico.**

## Objetivo (doc)

"Medir confiança e qualidade percebida, e fechar o loop quando ela falha."

## Decisões técnicas (justificadas)

- **Módulo próprio `Reputacao`**: a avaliação referencia a OS mas é um domínio
  distinto (reputação gerenciada), com resumo/NPS/loop próprios. Não incha o
  módulo de OS nem o de clientes.
- **Uma avaliação por OS** (índice único em `OrdemServicoId`): reavaliar não faz
  sentido — é o veredito daquele reparo.
- **Gatilho só após entrega** (doc, explícito): o pedido de avaliação sai no
  evento `Entregue` (não em "pronto para retirada"), reusando o
  `ComunicacaoService` — respeita consentimento LGPD e preferências da loja, e o
  link é o mesmo acompanhamento público.
- **Envio pelo link público** (slug + código opaco que já existe): o cliente
  avalia sem login, na mesma página que acompanha. Só aceita se a OS está
  `Entregue` e ainda não foi avaliada.
- **Snapshot de serviço e técnico** no momento da avaliação: a satisfação
  por técnico usa o responsável da OS na entrega (a reatribuição tem trilha, mas
  a avaliação congela quem estava responsável). Serviço idem.
- **"Negativa" (abre o loop) = `Nota <= 2` OU `Recomendacao <= 6`** (detrator):
  qualquer sinal forte de insatisfação vira pendência. Documentado.
- **Fechamento de loop**: avaliação negativa não resolvida é uma pendência;
  `resolver` grava nota de tratamento + quem + quando (histórico de reputação
  gerenciada, como o doc pede — "prova de qualidade de atendimento").
- **NPS**: promotores 9–10, neutros 7–8, detratores 0–6;
  `NPS = round((%promotores − %detratores))`. Cálculo em memória (Sqlite dos
  testes não agrega decimais no servidor — padrão já usado no Financeiro).

## Modelo de dados

`Avaliacao` (int PK, tenant): OrdemServicoId (Guid, único), ClienteId?,
ServicoId, ResponsavelTecnicoId?, Nota (1–5), Recomendacao (0–10), Comentario?,
Resolvida, ResolucaoNota?, ResolvidaEm?, ResolvidaPorUsuarioId?, CriadoEm.
Migração com RLS. Enum `TipoEventoComunicacao` ganha `PedidoAvaliacao`
(varchar — sem mudança de schema além da coluna já existente).

## Endpoints

- Público: `POST /api/publico/{slug}/acompanhar/{codigo}/avaliacao`
  ({ nota, recomendacao, comentario? }). O `GET` do acompanhamento passa a expor
  `podeAvaliar` e `jaAvaliada`.
- Interno: `GET /api/avaliacoes?apenasPendentes=`, `GET /api/avaliacoes/resumo`,
  `POST /api/avaliacoes/{id}/resolver` ({ nota }).

## Front-end

- **Acompanhamento público**: formulário de avaliação (estrelas + 0–10 +
  comentário) quando `podeAvaliar`; agradecimento quando `jaAvaliada`.
- **Nova tela `/avaliacoes`** (nav): resumo (média de estrelas + distribuição +
  NPS + por técnico), lista de avaliações e fechamento de loop (resolver
  negativa com nota). Card de pendências em destaque.

## Fora desta etapa (registrado)

- Avaliação **por serviço** no resumo (o doc cita "por serviço"): a avaliação já
  guarda o `ServicoId`; a quebra por serviço no resumo fica para quando houver
  demanda — a base de dados já existe.
- Campanhas / reativação / templates editáveis: outro item do roadmap.

## Passos

1. Plano commitado.
2. Backend: entidade + serviço + endpoints (público/interno) + gatilho na
   entrega + migração (RLS); testes (envio só após entrega, uma por OS, resumo
   média/NPS/por técnico, loop resolver, isolamento).
3. Suíte verde; RLS conferido no Postgres.
4. Orval regen; front (acompanhamento + tela de avaliações + nav); e2e.
5. Docs (`progresso.md`, roadmap).
