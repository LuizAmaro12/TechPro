# TechPro — Stack, Arquitetura e Infraestrutura (v1)

## 0. Contexto, premissas e o que já ficou decidido

Este documento assume o que já foi levantado nas conversas anteriores:

---

## 1. Princípios de arquitetura (resumo executivo)

| Decisão | Escolha | Por quê |
|---|---|---|
| Estilo de arquitetura | **Monolito modular** (não microsserviços) | Microsserviços resolvem problema de escala de equipe, não de escala de tráfego. Para uma pessoa só, cada serviço extra é mais um deploy, mais um log, mais uma coisa que pode cair às 2h da manhã. |
| Multi-tenancy | **Banco compartilhado com `tenant_id`** + isolamento reforçado por Row-Level Security no Postgres | Banco por empresa (database-per-tenant) só se paga em escala grande ou exigência regulatória específica — no seu estágio, é custo de operação sem benefício correspondente. |
| Banco de dados | **PostgreSQL** (não SQL Server) | Open source, sem custo de licença, ótimo suporte a multi-tenancy via RLS, plenamente suportado pelo Entity Framework Core via Npgsql, e disponível em praticamente toda hospedagem barata. |
| Front-end/back-end | **Separados**, comunicando via API REST/JSON tipada | Já é sua base. Mitigo o custo com geração automática de cliente tipado (seção 3). |
| Autenticação | **ASP.NET Core Identity + JWT própria** (não Auth0/Clerk) | Provedores terceirizados cobram por usuário ativo — isso cresce junto com o seu SaaS e vira custo recorrente crescente. Construir uma vez com Identity é mais trabalho no início e zero custo marginal depois. |
| Comunicação com cliente | **WhatsApp Cloud API oficial, direto da Meta** (sem BSP intermediário) | Sem mensalidade de plataforma, sem markup por mensagem. Mais integração manual, mas você é o próprio integrador. |
| Armazenamento de arquivos | **Cloudflare R2** | Sem cobrança de egress (dado que fotos serão visualizadas com frequência pelo portal do cliente, isso importa), free tier generoso. |
| App do técnico (Fase 2, ainda não construído) | **React Native + Expo**, offline-first | O técnico opera em campo com conectividade instável (oficina, casa do cliente, área sem sinal) — web/PWA não resolve isso de forma confiável para o fluxo de OS/Kanban/fotos. A decisão é tomada agora, e algumas escolhas de schema e API (seções 4 e 5) já entram na Fase 1, porque são baratas de fazer desde o primeiro migration e caras de retrofitar depois de existirem dados reais — mesma lógica já aplicada ao `tenant_id` + RLS. |

---

## 2. Visão geral da arquitetura

```
Next.js 16 (Vercel)                              ASP.NET Core 10 Web API
App Router + RSC          <---- HTTPS/JSON ---->  (Render, container Docker)
TanStack Query + Zod       cliente tipado          Módulos por domínio (seção 4)
                           (OpenAPI -> TS)                    |
                                                              |
                              -------------------------------+-------------------------------
                              |                               |                              |
                     PostgreSQL (Render)             Cloudflare R2                  WhatsApp Cloud API
                     tenant_id + RLS                 fotos/anexos                   (Meta, direto)
                                                                                     Resend (e-mail)
```

O front-end nunca acessa o banco, o R2 ou o WhatsApp diretamente — tudo passa pela API .NET, que é o único ponto que aplica as regras de isolamento entre empresas (seção 16).

**Nota sobre o app mobile do técnico (Fase 2, ainda não construído):** o diagrama acima é o da Fase 1. Quando o app nativo (React Native/Expo) for construído, ele vai se conectar à mesma API .NET — nunca direto ao banco ou ao R2 — com a diferença de que vai manter uma cópia local (SQLite) dos dados do fluxo de campo (OS, Kanban, fotos) e sincronizar por um endpoint de sync dedicado (seção 4). As decisões de schema que isso exige (seção 5) já entram no primeiro migration, mesmo sem o app existir ainda.

---

## 3. Front-end

### Núcleo
- **Next.js 16** (App Router, Turbopack como bundler padrão, Node.js 22 LTS). A versão 16 trouxe Cache Components (cache explícito via `"use cache"`, em vez do cache implícito das versões anteriores) — vale adotar o modelo explícito desde o início, é mais previsível para depurar sozinho.
- **React 19** (já embutido no Next 16).
- **TypeScript** em modo estrito (`strict: true`) — não negociável dado que você é o único revisor do próprio código; o compilador vira seu par de revisão.

### UI e estilo
- **Tailwind CSS** — já na sua base.
- **Radix UI (primitives)** — já na sua base. Recomendo adotar por cima o **shadcn/ui**: ele não é uma dependência de runtime, é um gerador de componentes que já combina Radix + Tailwind com acessibilidade e estilos prontos, copiados para o seu próprio repositório (você é dono do código, não de mais uma dependência externa para atualizar). Para um desenvolvedor solo, isso poupa semanas de construção de componentes de formulário, modal, tabela, combobox etc.
- **`motion`** (sucessor do `framer-motion`, mesma equipe) — já na sua base, para transições e microinterações. Usar com moderação: animação é polimento, não correção de UX — não deixe isso consumir tempo desproporcional no MVP.
- **Ícones:** `lucide-react` — leve, tree-shakeable, e o conjunto mais usado hoje em combinação com Radix/shadcn.
- **Gráficos:** **Recharts** — já na sua base, adequado para os dashboards do módulo 12 do documento de produto.

### Dados e estado
- **TanStack Query (React Query)** para chamadas à API .NET: cache, revalidação, estados de loading/erro — evita reinventar isso manualmente em cada tela.
- **Zustand** para estado de UI local que não vem da API (ex.: estado de um wizard multi-etapas do onboarding) — deliberadamente mais simples que Redux; Redux é ferramenta de equipe grande coordenando estado complexo, overhead desnecessário aqui.
- **React Hook Form + Zod** para formulários e validação — Zod também gera o tipo TypeScript a partir do schema, então validação e tipo nunca ficam dessincronizados.

### Integração tipada com o back-end (crítico dado o custo do stack dividido)
Gerar um **cliente TypeScript tipado automaticamente a partir do OpenAPI/Swagger** que o ASP.NET Core já expõe nativamente (ferramentas: `openapi-typescript` + um wrapper fino de fetch, ou `orval` que já gera hooks de TanStack Query prontos). Isso significa: sempre que você mudar um contrato no back-end C#, o front-end aponta erro de compilação imediatamente se não acompanhar a mudança — em vez de descobrir em produção que um campo mudou de nome.

### PWA (proposta de baixo custo, alto retorno)
Já que o app nativo não está no MVP, adicionar um **manifest PWA + service worker leve** (via `next-pwa` ou configuração manual) permite que o cliente "instale" o portal no celular com ícone próprio, e dá uma sensação de app sem o custo de manter React Native/Flutter. Baixo esforço, plausível para Fase 2.

---

## 4. Back-end (.NET)

### Núcleo
- **.NET 10** — é a versão LTS atual (lançada em novembro de 2025, suporte até novembro de 2028). Evite .NET 9: é STS (suporte curto, até novembro de 2026), errado para um projeto que você vai manter sozinho por anos.
- **ASP.NET Core Web API** — atenção a um ponto de clareza: "estrutura MVC" no seu base inicial deve significar **Web API com controllers no padrão MVC** (Controller → Action → retorno JSON), não Razor Views server-side. As Views MVC tradicionais renderizam HTML no servidor, o que seria redundante já que o Next.js é quem cuida de toda a UI. Vou assumir Web API puro; me avise se a intenção era outra.

### Padrão de arquitetura interna: monolito modular por domínio
Em vez de Clean Architecture "de livro" (4 projetos separados, portas e adaptadores, inversão de dependência em todas as direções) — que paga dividendos em equipes grandes mas é cerimônia excessiva para uma pessoa só — recomendo um **monolito modular**: um único projeto ASP.NET Core, organizado em pastas por módulo de negócio, espelhando exatamente os módulos do documento de produto (`OrdensServico`, `Estoque`, `Financeiro`, `Clientes`, etc.). Cada módulo tem seu próprio Controller, Service e Repository; regras de negócio ficam no Service, nunca no Controller.

Isso dá 90% do benefício de organização da Clean Architecture com uma fração da cerimônia — e ainda deixa a porta aberta para, se um módulo específico crescer descontroladamente (ex.: o financeiro virar complexo o bastante para merecer seu próprio serviço), extraí-lo depois. Arquitetura modular bem feita é o que torna essa extração possível sem reescrever tudo.

### Bibliotecas e ferramentas
- **Entity Framework Core** + **Npgsql** (provider PostgreSQL) — ORM e migrations.
- **FluentValidation** — validação de DTOs de entrada, mais expressivo e testável que Data Annotations.
- **Hangfire** (com armazenamento no próprio Postgres) — jobs em background: lembretes agendados, cálculo de previsão de reposição de estoque, verificação de SLA do Kanban. Evita precisar de uma fila gerenciada separada (custo e complexidade extra que seu orçamento não pede ainda).
- **Serilog** — logging estruturado (ver seção 9).
- **Swagger/Swashbuckle** — já vem quase pronto no ASP.NET Core, é a base para o cliente tipado do front-end (seção 3).

### Testes (conecta diretamente com o que você já sabe)
Você já tem familiaridade com ferramentas de teste (JUnit, PyTest, Selenium, JMeter, SonarQube) e trabalhou especificamente com **Playwright** no seminário acadêmico. Isso se aplica bem aqui:
- **xUnit** para testes unitários e de integração do back-end .NET (é o padrão de fato no ecossistema .NET, papel equivalente ao JUnit).
- **Playwright** para testes end-to-end do front-end Next.js — você já tem essa base pronta do seminário, é só redirecionar para o próprio produto em vez de um estudo de caso acadêmico.
- Dado que é só você mantendo o projeto, não vale perseguir cobertura de teste alta em tudo — prioridade para os fluxos que envolvem dinheiro (financeiro, aprovação de orçamento) e isolamento entre empresas (multi-tenant), que são os pontos onde um bug é caro ou embaraçoso.

### Preparação para o app mobile do técnico (Fase 2 — offline-first, ainda não implementado)
O app nativo do técnico (React Native/Expo, módulo 4 do documento de produto) só entra na Fase 2, e o escopo de dados offline é deliberadamente restrito ao fluxo de campo: Ordens de Serviço, status de Kanban, fotos/anexos de diagnóstico, e consulta (somente leitura) a itens de Estoque. Financeiro, Clientes e os demais módulos continuam apenas online, via portal web.

O ponto desta subseção não é construir o app agora — é decidir, hoje, um punhado de coisas na API e no schema que são baratas de fazer desde o primeiro migration e caras de adicionar depois de o app já existir e ter dados reais sincronizando:

- **Chave primária em UUID (não `serial`/identity) nas entidades do escopo offline.** Um app offline-first precisa criar registros (ex.: nova OS, novo status de Kanban) sem round-trip ao servidor — isso só funciona sem colisão de ID se a chave for gerada no cliente. Adotar UUID como PK desde a Fase 1 nessas entidades evita uma migração de tipo de coluna (e de toda FK que aponta pra ela) quando a Fase 2 chegar.
- **Colunas de sincronização em toda entidade do escopo offline:** `updated_at` (timestamp setado pelo servidor em toda escrita, usado como "marca d'água" da sincronização) e `deleted_at` (soft-delete — o app offline só descobre que um registro foi apagado se existir uma "lápide" para sincronizar, não um `DELETE` físico).
- **Um endpoint de sincronização por delta, além do CRUD normal** (ex.: `GET /api/ordens-servico/sync?since={timestamp}`), retornando tudo que mudou (incluindo apagados) desde o último `since`. Desenhar o contrato agora evita ter que encaixar esse padrão depois em cima de uma API que só pensou em request/response síncrono.
- **Suporte a `Idempotency-Key` nos endpoints de escrita** dessas entidades — uma mutação feita offline e reenviada pelo app quando a conexão volta (por timeout, retry automático etc.) não pode ser aplicada duas vezes.
- **Política de resolução de conflito:** *last-write-wins* por `updated_at` como padrão para campos de status/andamento de campo; qualquer coisa que toque aprovação de orçamento já tem trilha de auditoria append-only (seção 16) e continua exigindo essa trilha em vez de sobrescrita silenciosa.
- **Refresh token com política própria para o cliente mobile** (seção 8) — o técnico pode passar horas ou dias sem conexão em campo; o token de longa duração do app não pode expirar no meio de uma janela offline como expiraria o de uma sessão web comum.
- **Notificação push via Expo Push Notifications** (canal separado de WhatsApp/Resend) para acordar o app e disparar sincronização quando a conectividade volta — a decidir/implementar junto com o app em si, mas já mapeado aqui para não virar surpresa de custo/integração na Fase 2.

Nada disso muda o que é entregue na Fase 1 (não há app, não há tela de sync) — é só schema e contrato de API desenhados de um jeito que a Fase 2 encaixa por cima, em vez de exigir migração de dados reais de cliente.

---

## 5. Banco de dados e multi-tenancy

### Escolha: PostgreSQL
Sem custo de licença (diferente de SQL Server em alguns cenários de hospedagem), suporte maduro a Row-Level Security, e disponível em praticamente qualquer provedor de hospedagem barato.

### Estratégia de isolamento entre empresas — LGPD aplicada (módulo 14 do doc de produto)
Duas camadas, deliberadamente redundantes ("defesa em profundidade"):

1. **Camada de aplicação:** toda entidade que pertence a uma empresa tem uma coluna `tenant_id`. O Entity Framework Core aplica um **Global Query Filter** automaticamente em todo `DbContext`, filtrando por `tenant_id` do usuário autenticado — nenhuma query no código de negócio precisa lembrar de filtrar manualmente, o que elimina a categoria mais comum de bug de vazamento de dado entre empresas (esquecer um `.Where()`).
2. **Camada de banco:** **Row-Level Security (RLS) nativo do Postgres** como segunda trava. Mesmo que um bug no código de aplicação esqueça o filtro (ou uma query bruta seja escrita sem passar pelo EF Core), o próprio banco recusa devolver linhas de outro tenant. Isso é o que transforma "prometemos isolamento" (seção 7 do documento de branding) em algo verificável, não apenas uma promessa de código de aplicação.

Isso é mais barato de montar agora, no início do schema, do que adicionar depois de já existirem dados reais de clientes — reforça o ponto já levantado no documento de produto (módulo 14).

### Migrations
EF Core Migrations, versionadas no mesmo repositório do back-end. Nunca editar schema diretamente em produção sem migration correspondente — para uma pessoa só, é fácil "só ajustar rapidinho" e perder rastreabilidade; a disciplina de migration é o que evita isso.

### Schema pronto para o app mobile offline-first (Fase 2, seção 4)
Nas entidades do escopo de campo do técnico (`OrdensServico`, status/histórico de Kanban, anexos/fotos de diagnóstico) — e só nelas — o primeiro migration já usa `uuid` como tipo de chave primária (gerada no cliente ou no servidor, em vez de `serial`/identity) e inclui as colunas `updated_at` e `deleted_at` desde o início. O restante do schema (Financeiro, Clientes, Estoque, etc.) segue como PK `serial`/identity normal, sem essas colunas — não há caso de uso offline para eles, então não há razão para pagar esse custo de modelagem ali.

---

## 6. Armazenamento de arquivos

**Cloudflare R2** para fotos de diagnóstico, evidência antes/depois, anexos de OS (módulos 1, 3, 9 do documento de produto):
- Compatível com API S3 (qualquer SDK/lib S3 funciona sem adaptação).
- **Zero cobrança de egress** — relevante porque fotos são lidas com frequência pelo portal do cliente, e egress é justamente o item que mais pesa em provedores como AWS S3.
- Free tier: 10GB de armazenamento, 1 milhão de operações de escrita e 10 milhões de operações de leitura por mês — confortável para o volume esperado no MVP e provavelmente por bastante tempo depois.

---

## 7. Comunicação (WhatsApp e e-mail)

### WhatsApp: API oficial da Meta (Cloud API), direto, sem BSP
Contexto que mudou desde meados de 2025: a Meta passou de cobrança por conversa (24h) para **cobrança por mensagem entregue**, com quatro categorias — marketing, utilidade (utility), autenticação e serviço. **Mensagens de serviço, respondendo dentro de uma janela de 24h aberta pelo próprio cliente, continuam gratuitas.** Mensagens de utilidade (que é a categoria da maior parte das suas notificações — confirmação de agendamento, OS criada, orçamento disponível, pronto para retirada) têm custo baixo por mensagem, e ficam ainda mais baratas dentro da janela de 24h.

**Implicação prática de design:** desenhe os fluxos de notificação para, sempre que possível, responder dentro da janela de 24h de uma interação iniciada pelo cliente (ex.: cliente confirma agendamento pelo portal, o que conta como iniciar a janela) em vez de sempre disparar mensagens novas fora dela. Isso não é só economia — é a diferença entre pagar por praticamente toda notificação ou pagar por uma fração pequena delas.

**Por que direto da Meta e não via um BSP (Twilio, 360dialog, Wati etc.):** BSPs cobram mensalidade de plataforma e/ou markup por mensagem em cima da tarifa da Meta. Para orçamento apertado e um desenvolvedor que já vai construir a integração de qualquer forma, ir direto no Cloud API elimina essa camada de custo — a única desvantagem é que você mesmo lida com a integração e a aprovação de templates de mensagem junto à Meta, o que é aceitável dado que não há equipe para terceirizar isso de qualquer forma.

**Cautela:** essas tarifas mudam com frequência (a própria Meta já fez ajustes regionais em janeiro e julho de 2025, e mais em janeiro de 2026) — trate o custo de WhatsApp como uma linha de orçamento variável a ser monitorada mensalmente, não como um valor fixo. Não use bibliotecas não oficiais (ex. Baileys) em produção: violam os termos de uso do WhatsApp e o número pode ser banido — aceitável para prototipagem interna, nunca para clientes reais, dado que a comunicação automatizada é um dos diferenciais centrais do produto (não vale o risco de ficar sem esse canal de repente).

### E-mail transacional: Resend
Free tier de 3.000 e-mails/mês (limite de 100/dia), API simples, integra bem com templates em JSX/React (React Email) — o que é natural já que o front-end é React. Para o volume esperado no MVP (confirmações, lembretes, recuperação de senha), o free tier cobre com folga; migrar para o plano pago (a partir de US$20/mês) só quando o volume justificar.

---

## 8. Autenticação e autorização

- **ASP.NET Core Identity** como base (gestão de usuário, hash de senha já seguro por padrão, confirmação de e-mail, recuperação de senha).
- **JWT** (access token de vida curta + refresh token) para autenticar as chamadas do front-end à API — modelo padrão para uma API separada do front-end.
- O **`tenant_id`** e a **role** (atendente/técnico/gestor, do módulo 13 do documento de produto) entram como claims no JWT — assim, cada requisição já carrega a informação necessária para o Global Query Filter (seção 5) e para as regras de autorização por permissão, sem consulta extra ao banco a cada request.
- Autorização por política (`[Authorize(Policy = "...")]`) do ASP.NET Core, mapeando as combinações de módulo × role já definidas no documento de produto.
- **Política de refresh token diferenciada por tipo de cliente** (web vs. o futuro app mobile do técnico, seção 4): o app precisa de um refresh token de vida mais longa e tolerante a ficar sem uso por horas/dias (técnico em campo sem sinal), enquanto o refresh token da sessão web pode continuar com uma janela mais curta. Modelar o refresh token já com um campo indicando a origem/tipo de cliente evita ter que migrar a tabela de tokens quando o app existir.

---

## 9. Observabilidade e confiabilidade

Dado que dois runtimes rodam separados, vale investir no mínimo necessário para não operar às cegas:

- **Serilog** (back-end) com output estruturado (JSON), enviado para um destino centralizado.
- **Sentry** (free tier) para rastreamento de erro — tem SDK tanto para .NET quanto para Next.js/React, permitindo ver erros dos dois lados da stack no mesmo lugar, o que compensa parte do custo de ter dois runtimes separados.
- **Health check endpoint** nativo do ASP.NET Core (`/health`), monitorado por um serviço gratuito de uptime (ex. UptimeRobot ou Better Stack free tier) que avisa por e-mail/WhatsApp se a API cair.
- Quando o app mobile (Fase 2, seção 4) existir, o Sentry tem SDK para React Native/Expo — mantendo os três lados (API, web, mobile) no mesmo painel de erro, sem precisar adotar outra ferramenta de observabilidade específica de mobile.

Não é necessário nada além disso no MVP — uma stack de observabilidade tipo Datadog/New Relic tem custo e complexidade que não se justificam ainda.

---

## 10. Infraestrutura e hospedagem

### Onde hospedar cada peça, e por quê

| Componente | Serviço | Custo estimado |
|---|---|---|
| Front-end (Next.js) | **Vercel** (plano Hobby/free) | R$0 |
| Back-end (.NET, container Docker) | **Render** (Web Service, plano Starter) | ~US$7/mês (~R$36) |
| Banco de dados (Postgres) | **Render Postgres** (plano inicial) | ~US$7/mês (~R$36) |
| Armazenamento de arquivos | **Cloudflare R2** (free tier) | R$0 até 10GB |
| E-mail transacional | **Resend** (free tier) | R$0 até 3.000 e-mails/mês |
| Rastreamento de erro | **Sentry** (free tier) | R$0 |
| Domínio | Registro.br ou similar | ~R$3-5/mês (anualizado) |
| WhatsApp Cloud API | Meta (variável, por mensagem) | Variável — orçar à parte, não incluído no teto de R$100 |
| **Total infraestrutura fixa** | | **≈ R$75-80/mês** |

Câmbio de referência: ~R$5,20/US$1 (verifique a cotação do dia ao orçar — isso oscila). Isso deixa uma margem de ~R$20-25 dentro do teto de R$100/mês antes mesmo de contar o custo variável do WhatsApp, que deve ser monitorado separadamente desde o primeiro mês real de uso.

**Custo futuro, fora do teto atual:** quando a Fase 2 (app do técnico, seção 4) começar, entra o custo de **EAS (Expo Application Services)** para build e distribuição do app nas lojas — o free tier cobre bem o início, mas deve ser orçado à parte quando essa fase se aproximar, assim como já se faz com o WhatsApp.

### Por que Render em vez de Railway (a alternativa mais óbvia)
Railway é frequentemente a recomendação padrão para desenvolvedores solo por causa da experiência de uso mais simples — mas a pesquisa de mercado feita para este documento encontrou registros de instabilidade operacional relevante em 2026, incluindo uma interrupção de toda a plataforma por horas quando o provedor de nuvem por trás do Railway suspendeu a conta de produção deles. Para um SaaS que vai lidar com a operação diária de clientes reais (ordens de serviço, agendamentos), esse é um risco real, não teórico. Render tem preço muito próximo, documentação mais madura e um histórico mais estável — a diferença de alguns reais por mês não compensa o risco de indisponibilidade para um produto que se vende com a promessa de "controle e confiança".

**Se o orçamento apertar:** Railway continua sendo uma opção válida e mais barata (~US$10-15/mês para back-end + banco juntos) — é uma troca legítima de robustez por custo, não um erro, desde que a decisão seja consciente.

---

## 11. Estrutura de repositórios

Recomendo **um único repositório (monorepo)**, não dois separados — mesmo com linguagens diferentes nas duas metades. Para uma pessoa só, ter front-end e back-end no mesmo repositório significa uma única fonte de verdade para issues, um único histórico de commits para correlacionar "essa mudança de contrato de API quebrou o front", e um único lugar para configurar CI. Como as duas metades não compartilham grafo de build (são linguagens diferentes), não há necessidade de ferramentas de monorepo como Turborepo/Nx — uma estrutura de pastas simples resolve:

```
TechPro/
  frontend/                 (Next.js)
    app/
    components/
    lib/
    package.json
  backend/                  (ASP.NET Core)
    src/
      TechPro.Api/
    TechPro.sln
  mobile/                   (Fase 2 - React Native/Expo, ainda nao criada)
  docs/                     (documentos de branding, produto e este)
  .github/
    workflows/              (CI/CD, secao 14)
  docker-compose.yml        (ambiente local: API + Postgres)
```

A pasta `mobile/` já entra no desenho do monorepo por antecipação — quando o app do técnico (seção 4) for construído, ele entra como um terceiro membro deste mesmo repositório, e não como um repositório separado, pelo mesmo motivo de fonte única de verdade que já vale para `frontend/` e `backend/`.

---

## 12. Estrutura de pastas do back-end (.NET)

```
backend/src/TechPro.Api/
  Program.cs
  appsettings.json
  Modules/
    OrdensServico/
      OrdensServicoController.cs
      OrdemServicoService.cs
      OrdemServicoRepository.cs
      Dtos/
    Agendamentos/
    Clientes/
    ServicosEPecas/
    Estoque/
    Financeiro/
    Comunicacao/
    Avaliacoes/
    Administracao/          (Fase 4)
  Shared/
    Auth/                   (Identity, JWT, politicas de autorizacao)
    Tenancy/                 (resolucao de tenant, Global Query Filter)
    Persistence/              (DbContext, configuracoes EF Core)
    Jobs/                       (Hangfire)
  Migrations/
```

Cada pasta em `Modules/` corresponde diretamente a um módulo do documento de produto — isso é intencional: quando você for implementar a Fase 2 de um módulo, o lugar onde mexer no código é óbvio sem precisar caçar em uma estrutura genérica por camada técnica.

---

## 13. Estrutura de pastas do front-end (Next.js App Router)

```
frontend/app/
  (portal-cliente)/          (rotas do portal do cliente final)
    agendar/
    acompanhar/[osId]/
    layout.tsx
  (empreendedor)/              (area logada do dono/equipe)
    dashboard/
    ordens-servico/
    kanban/
    clientes/
    estoque/
    financeiro/
    layout.tsx
  (tecnico)/                     (app/portal do tecnico - modulo 4 do doc de produto)
  (admin)/                        (administracao SaaS - Fase 4)
  api/                              (apenas rotas auxiliares do proprio Next.js, se necessario)
components/
  ui/                            (componentes shadcn/ui)
  [dominio]/                      (componentes especificos de cada modulo)
lib/
  api-client/                    (cliente TypeScript gerado a partir do OpenAPI do back-end)
  hooks/                          (hooks de TanStack Query por modulo)
  validators/                      (schemas Zod)
```

Os grupos de rota entre parênteses `(portal-cliente)`, `(empreendedor)`, `(tecnico)`, `(admin)` mantêm layouts e regras de acesso separados sem duplicar estrutura de URL — recurso nativo do App Router.

**Sobre `(tecnico)` e o futuro app mobile (Fase 2, seção 4):** este grupo de rotas continua existindo como acesso web do técnico (útil como fallback com conexão, ou para quem prefere não instalar o app) — ele não é substituído pelo app nativo, e sim complementado por ele. O app React Native/Expo é quem assume o caso de uso offline-first em campo; o Next.js nunca precisou (nem deve) resolver isso sozinho.

---

## 14. CI/CD

- **GitHub Actions** (gratuito dentro dos limites generosos para repositórios pessoais/pequenos).
- Pipeline mínimo por push: lint + build do front-end, build + testes xUnit do back-end.
- Deploy do front-end: automático via integração nativa Vercel ↔ GitHub (preview em cada PR, produção no merge para `main`).
- Deploy do back-end: build da imagem Docker e deploy automático no Render a cada merge em `main` (Render suporta isso nativamente conectando o repositório).
- `docker-compose.yml` na raiz do repositório reproduz localmente a mesma dupla API + Postgres que roda em produção — isso importa especialmente aqui, porque reduz o risco do problema clássico de stack dividido ("funciona na minha máquina, mas o ambiente de produção é diferente").

---

## 15. Segurança — boas práticas gerais

- HTTPS em todas as camadas (Vercel e Render já fornecem TLS automático).
- Segredos (connection string, chaves de API do WhatsApp/Resend/R2) em variáveis de ambiente, nunca commitados — usar os cofres de secret nativos de Vercel/Render/GitHub Actions.
- CORS restrito: a API .NET só aceita requisições da origem do front-end (não deixar `AllowAnyOrigin` em produção).
- Rate limiting nativo do ASP.NET Core nos endpoints públicos (especialmente o portal do cliente, que não exige login completo para agendar).
- Validação de entrada em toda a borda da API (FluentValidation) — nunca confiar em validação só do lado do front-end.
- Dependabot (nativo do GitHub, gratuito) para alertar sobre dependências com vulnerabilidade conhecida, tanto no `package.json` quanto no `.csproj`.

---

## 16. LGPD aplicada à arquitetura (implementação técnica do módulo 14 do documento de produto)

- Isolamento por `tenant_id` + RLS (seção 5) — a base técnica do compromisso já assumido no documento de branding.
- **Exclusão de dados do cliente final:** implementar como **anonimização**, não exclusão física (hard delete) — trocar nome/telefone/e-mail por marcadores genéricos, mantendo o registro estrutural da OS intacto (necessário para o histórico financeiro e de estoque da empresa continuar consistente). Isso atende ao direito de exclusão sem quebrar integridade referencial.
- **Exportação de dados:** um endpoint que serializa todos os dados pessoais do cliente final vinculados àquele `tenant_id` em um JSON — operacionalizando o botão "exportar meus dados" do módulo 14.
- **Tabela de auditoria** para a trilha de aprovação de orçamento (diferencial definido no documento de branding, seção 5.2): toda mudança de status de aprovação registra quem, quando e o quê, de forma append-only (nunca sobrescrita).
- Criptografia em repouso: Render Postgres e Cloudflare R2 já criptografam por padrão — confirmar isso na documentação de cada provedor no momento da contratação, já que política de provedor pode mudar.

---

## 17. Roadmap técnico por fase (alinhado ao documento de produto)

- **Fase 1 (MVP):** schema com `tenant_id` + RLS desde o primeiro dia; nas entidades do fluxo de campo do técnico (OS, Kanban, anexos), PK em UUID + colunas `updated_at`/`deleted_at` + endpoint de sync já desenhados (seções 4 e 5), mesmo sem o app mobile existir ainda; módulos de Agendamento, OS/Kanban básico, Clientes, Serviços/Peças, Estoque básico, Dashboard essencial, Comunicação essencial (WhatsApp utility + e-mail), autenticação própria, onboarding guiado.
- **Fase 2:** app nativo do técnico (React Native/Expo, offline-first, construído sobre as decisões de schema/API já tomadas na Fase 1), financeiro com margem, avaliações, aprovação de orçamento com trilha de auditoria, exportação/exclusão de dados (LGPD visível), central de mensagens unificada.
- **Fase 3:** base de conhecimento técnico, produtividade por técnico, automações mais ricas.
- **Fase 4:** administração SaaS completa, multiunidade, integrações fiscais.

---

## 18. Riscos técnicos e mitigação

| Risco | Mitigação |
|---|---|
| Bus factor 1 (só você mantém tudo) | Documentação viva (este conjunto de documentos), `docker-compose.yml` que reproduz o ambiente sem depender de configuração manual lembrada de cabeça. |
| Sobrecarga operacional de dois runtimes | Cliente de API tipado automático, monorepo, observabilidade unificada via Sentry, `docker-compose` único para desenvolvimento local. |
| Instabilidade de provedor de hospedagem | Escolha de Render em vez de Railway pelos motivos da seção 10; uso de containers Docker no back-end mantém portabilidade para outro provedor se necessário. |
| Mudança de tarifa/política do WhatsApp | Tratar como linha de orçamento variável, revisada mensalmente, não fixa. |
| Vazamento de dado entre empresas (tenant) | Dupla camada — Global Query Filter (aplicação) + Row-Level Security (banco), seção 5. |
| Custo de infraestrutura ultrapassar o teto | Tabela de custo da seção 10 já deixa margem; monitorar uso mensalmente, especialmente WhatsApp. |
| Retrofit caro de offline-first quando o app do técnico (Fase 2) começar | PK em UUID, colunas `updated_at`/`deleted_at`, endpoint de sync e `Idempotency-Key` já desenhados no schema/API desde a Fase 1 (seção 4/5) — evita migração de dados reais de cliente depois. |

---

## 19. Checklist antes de começar a escrever código

- [x] Confirmar que "estrutura MVC" no back-end significa Web API (controllers), não Razor Views — presumido neste documento, mas vale confirmar.
- [ ] Criar conta Cloudflare (R2), Meta for Developers (WhatsApp Cloud API), Resend, Render, Vercel, Sentry.
- [x] Definir nome de domínio e registrar.
- [x] Montar o `docker-compose.yml` local (API + Postgres) antes de escrever a primeira feature.
- [x] Desenhar o schema inicial com `tenant_id` em toda tabela relevante e políticas de RLS já no primeiro migration — não como retrofit.
- [ ] No primeiro migration das entidades do fluxo de campo do técnico (OS, Kanban, anexos), já usar PK em UUID e incluir `updated_at`/`deleted_at` — groundwork para o app mobile offline-first da Fase 2 (seção 4), não como retrofit depois.
- [x] Configurar o pipeline de CI mínimo (lint + build + testes) antes do primeiro merge em `main`.

Este documento assume que o próximo passo é detalhar o schema de banco de dados (entidades, relacionamentos) módulo a módulo — posso montar isso em seguida, se fizer sentido.