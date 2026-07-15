# Etapa: Comunicação essencial (módulo 9)

Data: 2026-07-15 · Ordem recomendada da Fase 1, item 8 (`docs/fases_MVP.md`).
Primeira etapa com **infraestrutura externa** (WhatsApp, e-mail) e **Hangfire**.
Abordagem escolhida pelo usuário: **(a) provedor abstraído com adaptador
fake/log como padrão** — tudo testável e "pronto para plugar" as credenciais.

## Decisões aprovadas pelo usuário (2026-07-15)

1. **Disparo automático no evento** (atende o "sem intervenção manual" do
   critério de pronto da Fase 1). Em dev/teste o adaptador log só registra —
   nada sai de verdade.
2. **Respeitar o consentimento LGPD** (`cliente.consentiuComunicacoes`):
   cliente sem consentimento não recebe; a tentativa vira log "Suprimida".
3. **Trazer Hangfire agora** (Postgres) para o lembrete temporizado de
   agendamento e futuros jobs (SLA, reposição).
4. **Todos os canais disponíveis**: WhatsApp (telefone é obrigatório) +
   e-mail quando houver e-mail cadastrado.

## Desvio de documentação registrado

- **WhatsApp via Evolution API** (decisão do usuário), no lugar da **Meta
  Cloud API** que o doc de stack (seção 7) fixa. O mesmo doc adverte contra
  bibliotecas não oficiais (Baileys, base da Evolution) por risco de
  **banimento do número** — "nunca para clientes reais". Mitigação: a
  abordagem (a) isola o provedor atrás de `ICanalNotificacao`; se o número for
  banido, troca-se o adaptador para a Cloud API sem tocar no resto. O doc de
  stack será atualizado para refletir a decisão e manter o risco visível.
- **E-mail via Resend** (já previsto no doc). A API key fica **só no `.env`
  local (gitignored)** — nunca versionada; recomendada a rotação após o uso.

## Arquitetura (provedor abstraído)

- `ICanalNotificacao { CanalNotificacao Canal; Task<ResultadoEnvio>
  EnviarAsync(destino, assunto?, corpo) }`.
- Adaptadores selecionados por **flag de provedor explícita** (default `log`):
  - WhatsApp: `LogWhatsAppCanal` (default) | `EvolutionWhatsAppCanal`.
  - E-mail: `LogEmailCanal` (default) | `ResendEmailCanal`.
  - Seleção por `Comunicacao:Whatsapp:Provedor` / `Comunicacao:Email:Provedor`
    (`log`|`evolution`|`resend`). Default `log` mantém dev/e2e determinístico;
    a key do Resend fica no `.env` dormente até virar `EMAIL_PROVEDOR=resend`.
- `ComunicacaoService`: compõe a mensagem (templates pt-BR por evento), checa
  consentimento, despacha nos canais disponíveis (try/catch — falha externa
  nunca derruba a ação que disparou) e grava um registro por canal.
- Disparo **síncrono** dos eventos imediatos (dentro da request; log/again
  determinístico para testes). Só o **lembrete** usa Hangfire. Mover os
  imediatos para jobs de background é melhoria de resiliência da Fase 2.

## Modelo de dados (`Modules/Comunicacao/`)

- `MensagemEnviada` (PK int, tenant; fora do escopo offline): ClienteId?,
  OrdemServicoId?, AgendamentoId?, Canal (WhatsApp|Email), Destino,
  TipoEvento, Assunto?, Corpo, Status (Enviada|Simulada|Suprimida|Falhou),
  Erro?, IdExterno?, CriadoEm. É o "registro mínimo para auditoria" (fases_MVP
  item 9) e a base do inbox unificado da Fase 2.

## Eventos notificados

| Evento | Gatilho |
|--------|---------|
| AgendamentoConfirmado | criar agendamento (manual e portal) |
| AgendamentoLembrete | job Hangfire agendado p/ ~3h antes |
| OrdemServicoCriada | criar OS (manual e conversão do check-in) |
| OrcamentoDisponivel | enviar orçamento |
| OrcamentoAprovado / OrcamentoRecusado | responder orçamento (loja/portal) |
| ProntoParaRetirada | OS muda para etapa Pronto para retirada |

Consentimento: verificado pelo `ClienteId` quando existir; agendamento avulso
sem cliente usa o contato informado (consentimento implícito do próprio
pedido). OS sempre tem cliente.

## Hangfire

- Pacotes `Hangfire.AspNetCore` + `Hangfire.PostgreSql` (validar conflito com
  Npgsql 10 no build do container; pinar se preciso).
- Ligado só com `Comunicacao:Hangfire:Habilitado=true` (docker-compose). Sem a
  flag (testes/`dotnet run` puro), registra `IAgendadorDeLembretes` no-op — os
  testes não dependem de Postgres/Hangfire.
- `IAgendadorDeLembretes.AgendarLembrete(agendamentoId, quando, tenantId)`:
  impl Hangfire agenda `LembreteJob`; o job fixa o tenant via `TenantAmbiente`
  (mesmo padrão das rotas públicas — sem HttpContext), carrega o agendamento,
  e só envia se ainda estiver `Agendado` (cancelado/check-in → não envia).
- Dashboard `/hangfire` só em Development (filtro permissivo local; prod exige
  auth real — anotado).

## Endpoints

- `GET /api/ordens-servico/{id}/mensagens` — auditoria das notificações da OS
  (loja vê o que foi enviado; base do inbox Fase 2).

## Config e segredos

- docker-compose (api): flags de provedor (default `log`), `RESEND_API_KEY`,
  `Comunicacao__Hangfire__Habilitado=true`, placeholders Evolution.
- `.env` (gitignored, criado nesta etapa): `RESEND_API_KEY=...` (real, dormente
  até `EMAIL_PROVEDOR=resend`). `.env.example` ganha placeholders comentados.

## Front-end

- Detalhe da OS: seção "Notificações enviadas" (lista do log com canal, tipo,
  status e horário; indica "modo simulação" quando Status=Simulada).

## Passos

1. Plano commitado.
2. Backend: entidade + adaptadores + ComunicacaoService + templates +
   MensagemEnviada + endpoint; Hangfire + agendador + job; gatilhos nos
   services (agendamento, OS, financeiro); DbContext + migração com RLS.
3. Config: docker-compose, `.env`, `.env.example`.
4. Suíte verde no container; RLS verificado; build com Hangfire OK.
5. Orval regen; front (seção de notificações na OS); e2e com evidência
   (modo simulação — mensagens logadas por evento + consentimento suprimindo).
6. Docs: atualizar doc de stack (Evolution + risco), `progresso.md`, README.
