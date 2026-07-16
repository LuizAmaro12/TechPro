# Etapa: Onboarding guiado (módulo 0/13) — fecha a Fase 1

Data: 2026-07-16 · Ordem recomendada da Fase 1, item 10 (`docs/fases_MVP.md`).
Conforme a nota crítica do doc, o onboarding **encapsula os fluxos reais já
construídos** — reusa os endpoints existentes (horários, serviços, peças) e
adiciona só o mínimo: status/checklist, conclusão e dados de exemplo.

## Decisões aprovadas pelo usuário (2026-07-16)

1. **Dados de exemplo removíveis**: incluir agora, com coluna `Exemplo` de
   marcação (cliente, serviço e OS fictícios), com carregar e remover limpos.
2. **Wizard em `/bem-vindo`** com redirecionamento no primeiro acesso + **card
   de ativação** no dashboard (X de N passos) até tudo concluído.

## Fora de escopo (deferido, com justificativa)

- **Logo da loja**: upload depende de Cloudflare R2 (não configurado) — mesmo
  motivo dos anexos. O passo "dados da loja" cobre nome + endereço público
  (slug), que já existem. Logo fica anotado para quando o R2 existir.
- **Convite de equipe**: o próprio doc coloca "Equipe: adição de membros,
  funções e permissões" como Fase 2 (módulo 13). Exige fluxo de convite +
  aceite por e-mail, que não existe. Deferido; o passo é opcional no doc.

## Decisões técnicas (justificadas)

- **Checklist derivado dos dados** (sem estado novo, sempre exato): horários
  configurados, ao menos um serviço real, ao menos uma peça, ao menos um
  cliente real. `Empresa.OnboardingConcluidoEm` (nullable) controla só o
  redirecionamento (o dono pode "pular").
- **Wizard reusa endpoints existentes** (PUT horários, POST serviços/peças) —
  o backend do onboarding é só status + concluir + dados de exemplo.
- **Dados de exemplo criados direto no serviço** (sem passar pelos gatilhos de
  OS), para não disparar notificações fictícias. Coluna `Exemplo` em
  `clientes`, `servicos`, `ordens_servico`. Remoção respeita as FKs (OS antes
  de cliente/serviço).
- **Estado do wizard é local** (uma página, `useState`): o doc reserva Zustand
  para wizard multi-etapas, mas não há compartilhamento entre componentes/rotas
  aqui e o "cautela" pede não sofisticar. Zustand segue reservado para quando
  houver estado de UI realmente compartilhado.
- Sugestões de serviços comuns = lista estática no front (troca de tela,
  bateria, conector, limpeza, película), com preço/duração default editáveis
  antes de cadastrar.

## Modelo de dados

- `Empresa.OnboardingConcluidoEm` (DateTimeOffset?).
- `Cliente.Exemplo`, `Servico.Exemplo`, `OrdemServico.Exemplo` (bool, default
  false). Migração só adiciona colunas — RLS já existe nessas tabelas.

## Endpoints (`Modules/Onboarding/`)

- `GET /api/onboarding` → { onboardingConcluido, passos: { lojaConfigurada,
  horariosConfigurados, temServico, temPeca, temCliente }, passosConcluidos,
  totalPassos, temDadosExemplo }.
- `POST /api/onboarding/concluir` → marca `OnboardingConcluidoEm` (concluir/pular).
- `POST /api/onboarding/dados-exemplo` → cria cliente+serviço+OS de exemplo
  (idempotente: se já houver, não duplica).
- `DELETE /api/onboarding/dados-exemplo` → remove os registros `Exemplo`.

## Front-end

- `(empreendedor)/bem-vindo`: wizard passo a passo — boas-vindas + dados da
  loja (nome/slug) → horários → serviços (sugestões editáveis) → peças
  (opcional) → dados de exemplo + concluir. Barra de progresso; botão "pular".
- Layout `(empreendedor)`: no primeiro acesso (onboarding não concluído),
  redireciona para `/bem-vindo` (exceto se já estiver lá).
- Dashboard: card "Ativação (X de N)" com os passos e links, visível até
  concluir; ação de remover dados de exemplo quando houver.

## Passos

1. Plano commitado.
2. Backend: colunas + `OnboardingService` + DTOs + controller; migração;
   testes de integração (status/checklist, concluir, dados de exemplo
   carregar/remover, idempotência, isolamento).
3. Suíte verde no container; migração aplicada e verificada.
4. Orval regen; front (wizard, redirect, card); e2e com evidência.
5. Docs (`progresso.md`, README, checklist da seção 19 do doc de stack) e
   commits por passo. **Fase 1 completa.**
