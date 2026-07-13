# Etapa: Agenda e portal de agendamento (módulo 2 + parte do módulo 1)

Data: 2026-07-13 · Ordem recomendada da Fase 1, item 4 (`docs/fases_MVP.md`).

## Decisões aprovadas pelo usuário

1. **Escopo**: agenda interna completa + rota pública de agendamento progressivo,
   sem dependências de módulos futuros. Diferidos conscientemente:
   - lembretes automáticos → etapa 8 (Comunicação, Hangfire + WhatsApp/Resend);
   - conversão automática em OS → etapa 5 (OS/Kanban) — o status
     `CheckInRealizado` já deixa o gancho pronto;
   - anexos no fluxo público → quando a conta Cloudflare R2 existir.
2. **Slug da loja**: gerado automaticamente do nome no cadastro (único global),
   editável depois na tela de configurações da agenda. URL pública:
   `/agendar/{slug}`.
3. **Identificação no portal público (sem login)**: vínculo silencioso — o
   agendamento guarda snapshot dos dados informados e o backend vincula ao
   cliente existente por telefone exato; nada do cadastro existente é exibido
   na tela pública (evita enumeração de telefones). Telefone inédito → cria
   cliente novo ao confirmar.
4. **Disponibilidade**: slots de 30 minutos dentro do horário de funcionamento
   por dia da semana (abertura/fechamento + intervalo opcional), menos
   bloqueios; um serviço ocupa `ceil(duração/30)` slots; a
   `CapacidadeSimultanea` do serviço limita sobreposições do mesmo serviço.

## Decisões técnicas desta etapa (justificadas)

- **RLS de `empresas` vira leitura pública** (`FOR SELECT USING (true)`,
  substituindo `empresas_isolamento_leitura`): a rota pública precisa resolver
  slug → empresa antes de existir contexto de tenant, e a tabela contém apenas
  dados de diretório (id, nome, slug, criado_em) — nenhum segredo de tenant.
  Os dados sensíveis continuam nas tabelas com RLS por tenant.
- **Tenant forçado por slug**: novo `TenantAmbiente` (scoped) consultado pelo
  `HttpTenantProvider` antes da claim JWT. O controller público resolve o slug
  e força o tenant; GQF + RLS passam a valer normalmente para o resto da
  requisição — o isolamento não é reimplementado à mão no fluxo público.
- **Datas locais da loja**: `Agendamento.Data` é `DateOnly` e horários são
  `TimeOnly` — agenda é conceito de horário de parede da loja; evita
  armadilhas de timezone com `timestamptz`.
- **Rate limiting** na rota pública (mesma infraestrutura da política "auth",
  política própria "publico"), conforme seção de segurança do doc de stack.
- **Concorrência de vaga**: revalidação de disponibilidade dentro da mesma
  transação do INSERT; corrida residual entre requisições simultâneas é risco
  aceito no MVP (documentado), sem lock pessimista.

## Modelo de dados (`Modules/Agendamentos/`, PK int + tenant)

- `HorarioFuncionamento`: DiaSemana (0=domingo..6), Abertura, Fechamento,
  IntervaloInicio?, IntervaloFim?, Ativo (dia fechado = Ativo=false).
  Único por tenant+dia.
- `BloqueioAgenda`: Data, HoraInicio, HoraFim, Motivo?.
- `Agendamento`: ClienteId? (FK), ServicoId (FK), Data, HoraInicio, HoraFim,
  Status (Agendado | CheckInRealizado | Cancelado), Origem (Manual | Portal),
  NomeContato, TelefoneContato, EmailContato?, DescricaoProblema?,
  AparelhoMarca?, AparelhoModelo?, CriadoEm, CanceladoEm?, MotivoCancelamento?,
  ReagendadoEm?.
- `Empresa.Slug` (novo, único) + policy pública de leitura.

## Endpoints

Internos (JWT + tenant): `GET/PUT api/agenda/horarios` (lote de 7 dias),
`GET/POST/DELETE api/agenda/bloqueios`, `GET/PUT api/agenda/configuracoes`
(slug), `GET api/agendamentos?inicio&fim` , `POST api/agendamentos`,
`PUT api/agendamentos/{id}` (reagendar/editar),
`POST api/agendamentos/{id}/checkin`, `POST api/agendamentos/{id}/cancelar`,
`GET api/agenda/disponibilidade?servicoId&data`.

Públicos (sem auth, rate-limited, prefixo `api/publico/{slug}`): `GET info`
(nome da loja + serviços com AgendavelOnline=true), `GET disponibilidade`,
`POST agendamentos` (Origem=Portal, vínculo silencioso por telefone).

## Front-end

- `(empreendedor)/agenda`: calendário próprio em Tailwind (dia/semana/mês),
  criação manual (cliente existente ou avulso), reagendar, cancelar, check-in;
  subpágina/aba de configurações (horários, bloqueios, slug com URL copiável).
  Link "Agenda" no nav do layout.
- `(portal-cliente)/agendar/[slug]`: wizard progressivo — identificação →
  aparelho → problema → serviço → data/horário (slots) → confirmação — no
  visual do guia Handle, sem login.

## Passos de execução

1. Plano (este arquivo) commitado.
2. Backend: slug + leitura pública de empresas (migração) e `TenantAmbiente`.
3. Backend: entidades + DbContext + migração com `RlsHelper` nas 3 tabelas.
4. Backend: serviços/validadores/controllers internos + testes de integração.
5. Backend: endpoints públicos + testes (isolamento, não-vazamento, vínculo
   silencioso, capacidade/bloqueio na disponibilidade).
6. Suíte completa verde no container; RLS verificado no Postgres real.
7. Orval regen + validators Zod.
8. Front-end empreendedor (agenda + configurações).
9. Front-end público (wizard).
10. E2E navegador com evidência; screenshots.
11. Docs (`progresso.md`, plano) atualizados; commits por passo.
