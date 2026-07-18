# Etapa: LGPD visível — exportação e anonimização (módulo 14, Fase 2)

Data: 2026-07-17 · 2º item web da Fase 2. Escopo web.

## Objetivo (do doc)

Operacionalizar os dois direitos prometidos no branding:
- **Exportação** (portabilidade): um endpoint que serializa **todos os dados
  pessoais** do cliente final vinculados ao tenant em JSON.
- **Exclusão = anonimização** (seção 16 do doc de stack): trocar nome/telefone/
  e-mail por marcadores genéricos, **preservando o registro estrutural** da OS
  (histórico financeiro/estoque continua íntegro). Nunca hard delete.

## Decisões técnicas

- **Anonimização abrange todo dado pessoal ligado ao cliente**, não só a linha
  `clientes`: `aparelhos` (IMEI, senha de desbloqueio, observações),
  `agendamentos` (snapshots NomeContato/Telefone/Email) e `mensagens_enviadas`
  (Destino e Corpo contêm PII). Tudo sob GQF (impossível cruzar tenant).
- **Preserva o estrutural**: `ordens_servico`, `pagamentos`, `orcamentos`,
  histórico de etapas e de peças **ficam intactos** (integridade referencial e
  financeira). A OS continua existindo, apontando para o cliente anonimizado.
- **Marca `Cliente.AnonimizadoEm`** (nullable) + `Ativo=false` + consentimento
  zerado. Irreversível. Idempotente: reanonimizar não quebra (no-op).
- **Consentimento separado de marketing/reativação: DEFERIDO** — não há feature
  de marketing nem reativação; um flag agora seria campo sem consumidor (YAGNI).
  Anotado para quando a reativação existir.

## Modelo de dados

- `Cliente.AnonimizadoEm` (DateTimeOffset?). Migração só adiciona a coluna
  (RLS já existe em `clientes`).

## Endpoints (no módulo Clientes)

- `GET /api/clientes/{id}/dados-pessoais` — exportação: cadastro + aparelhos +
  agendamentos + OS (resumo estrutural) + mensagens (auditoria). 404 se não for
  do tenant.
- `POST /api/clientes/{id}/anonimizar` — anonimiza (irreversível). Devolve o
  cliente já anonimizado. 404 se não existir no tenant.
- `ClienteResponse`/`ClienteDetalheResponse` ganham `AnonimizadoEm` para a UI
  sinalizar e desabilitar a ação.

## Front-end

- Tela de clientes: no cliente aberto, duas ações LGPD — **Exportar dados**
  (baixa o JSON) e **Anonimizar** (confirmação explícita, irreversível). Cliente
  anonimizado mostra selo e some das ações; o nome vira o marcador genérico.

## Passos

1. Plano commitado.
2. Backend: coluna + `LgpdService` (exportar/anonimizar) + endpoints + DTOs;
   migração; testes (export com dados ligados, anonimização varrendo
   cliente/aparelho/agendamento/mensagem e preservando OS, idempotência,
   isolamento).
3. Suíte verde no container; RLS/coluna conferidos no Postgres.
4. Orval regen; front (exportar + anonimizar); e2e com evidência.
5. Docs (`progresso.md`, roadmap).
