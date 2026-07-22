# Etapa: Equipe, permissões e histórico de ações (Fase 2)

Data: 2026-07-22 · 12º item web da Fase 2. Módulo 13 (avançado) + módulo 14.

## Diagnóstico (o que motivou a prioridade)

Três achados na análise, todos confirmados no código:

1. **`CreateAsync` só roda no cadastro da empresa** — não existe nenhuma forma de
   adicionar um segundo usuário. O produto é, na prática, **mono-usuário**.
2. **Os papéis `tecnico` e `atendente` estão seedados desde a fundação, mas
   nunca são atribuídos** — todo usuário nasce `gestor`.
3. **Nenhum endpoint usa `[Authorize(Roles=…)]`** — qualquer usuário autenticado
   pode tudo, inclusive financeiro, configurações e LGPD.

Consequência colateral: recursos já construídos (**reatribuição de técnico**,
**satisfação por técnico**, **SLA por técnico**, `responsavelTecnicoId` da OS)
só podem apontar para o próprio dono — foram feitos para um cenário
multi-usuário que ainda não é alcançável. Esta etapa destrava esse valor.

## Decisões do usuário (AskUserQuestion)

O doc exige que "atendente, técnico e gestor não acessem os mesmos dados", mas
não define **quais**. Decidido em conjunto:

- **Três níveis por função** (matriz abaixo).
- **Escopo completo**: membros + permissões + histórico de ações.

### Matriz de permissões aprovada

| Área | Gestor | Técnico | Atendente |
|---|---|---|---|
| OS / Kanban (ver e operar) | ✓ | ✓ | ✓ |
| Agenda e clientes (escrita) | ✓ | só ver | ✓ |
| Estoque (movimentar) | ✓ | ✓ | — |
| Custo/preço de peça | ✓ | ✓ | — |
| Registrar pagamento | ✓ | — | ✓ |
| Financeiro (margem, faturamento) | ✓ | — | — |
| Configurações / equipe | ✓ | — | — |
| LGPD (exportar / anonimizar) | ✓ | — | — |
| Avaliações (resolver loop) | ✓ | só ver | só ver |

## Decisões técnicas (justificadas)

- **Políticas nomeadas, não listas de papéis espalhadas.** `[Authorize(Policy =
  Politicas.Financeiro)]` em vez de repetir `Roles = "gestor"` em dezenas de
  lugares: quando a matriz mudar, muda num arquivo só, e o nome documenta a
  intenção.
- **Fail-closed por padrão**: endpoint sem política explícita continua exigindo
  autenticação; as políticas só **restringem**. Nenhum endpoint fica mais aberto
  do que hoje.
- **Desativar, nunca apagar** membro (coerente com o resto do sistema): o
  usuário tem histórico (OS, comentários, auditoria) que não pode sumir.
  `Usuario.Ativo` novo; o **login rejeita inativo**.
- **Guarda do último gestor**: não dá para rebaixar nem desativar o único
  gestor da loja — senão a loja se tranca para fora das configurações.
- **Senha inicial definida pelo gestor** (não convite por e-mail): envio real de
  e-mail ainda está em modo `log`, então um fluxo de convite por link ficaria
  quebrado na prática. Registrado como evolução quando o Resend estiver ligado.
- **Auditoria só onde ainda não há trilha.** OS (histórico de etapas),
  orçamento (trilha de eventos), estoque (razão) e reatribuição **já têm** as
  suas. Duplicar tudo numa tabela genérica seria redundância cara. A auditoria
  cobre o que hoje não deixa rastro e é sensível: **equipe, LGPD e
  configurações** — exatamente o que o doc pede junto de LGPD.
- **Snapshot do nome do autor** no registro de auditoria: o membro pode ser
  desativado depois, e a trilha precisa continuar legível.

## Modelo de dados

- `Usuario.Ativo` (bool, default true).
- `RegistroAuditoria` (int PK, tenant): UsuarioId?, UsuarioNome (snapshot),
  Acao, Entidade, EntidadeId?, Detalhe?, CriadoEm. Migração com RLS.

## Endpoints

- `POST /api/equipe` (gestor) — cria membro { nome, email, senha, papel }.
- `PUT /api/equipe/{id}` (gestor) — nome e papel.
- `DELETE /api/equipe/{id}` (gestor) — desativa.
- `GET /api/equipe` — passa a devolver papel e ativo.
- `GET /api/auditoria` (gestor) — histórico de ações.

## Front-end

- **Configurações**: seção Equipe (listar, adicionar, trocar função, desativar)
  e seção Histórico de ações — ambas só para gestor.
- **Navegação por papel**: esconder o que o papel não acessa (Financeiro,
  Configurações). Esconder não é segurança — o backend é a fonte —, é evitar
  levar o usuário a um 403.

## Fora desta etapa (registrado)

- **Convite por e-mail com link** — depende do Resend ligado.
- **Permissão por objeto** (ex.: técnico só vê as OS dele): a matriz aprovada é
  por área; refinar por objeto exige demanda real.

## Passos

1. Plano commitado.
2. Backend: `Ativo` + políticas + endpoints de equipe + auditoria + migração
   (RLS); testes (cada papel barrado onde deve, último gestor protegido, login
   de inativo, auditoria registrada, isolamento).
3. Suíte verde; RLS conferido no Postgres.
4. Orval regen; front (equipe, auditoria, nav por papel); e2e.
5. Docs (`progresso.md`, roadmap).
