# Etapa: Portal do técnico (web responsivo) — Fase 2

Data: 2026-07-22 · 14º item web da Fase 2. Módulo 4. **Último módulo funcional
web.**

## Escopo e restrição

Web **responsivo, mobile-first** — nunca app nativo. O doc é enfático na
"Cautela" do módulo 4: *"uma versão web responsiva otimizada para celular
resolve o mesmo problema com uma fração do esforço de manutenção"*. Isto **não**
inicia o escopo mobile; é uma tela web como as outras, só otimizada para o
celular do técnico na bancada.

## Por que agora

Até a etapa de equipe, o papel `tecnico` não existia na prática. Hoje ele entra
com permissões reais, mas cai nas mesmas telas de desktop do gestor. O portal é
o que dá propósito ao papel — o doc: *"o Kanban é a visão gerencial; o técnico
precisa de uma lista simples do que é dele para fazer agora"*.

## Decisões do usuário (AskUserQuestion)

- **Etapas: livres, como hoje.** O técnico move para qualquer etapa — **sem
  mudança de regra no backend**. (Consciente: ele pode marcar "Entregue" antes
  da retirada; o usuário aceitou o trade-off em favor da simplicidade.)
- **Escopo das OS: a bancada filtra, o resto continua.** A tela da bancada
  mostra só as OS do técnico logado (via `?responsavelId=`, que a API já
  aceita); Kanban e Ordens seguem mostrando a loja inteira. Restrição de foco,
  não de segurança — **sem permissão por objeto, sem tocar `/sync`**.
- **Home: não.** Todos continuam no dashboard; a bancada é mais um item de menu.

Resultado: **zero mudança de regra existente**. O único trabalho de backend é a
lacuna de dados abaixo.

## A única lacuna de dados: checklist técnico por OS

O doc lista "marcar checklist técnico" como essencial. Hoje existe só o
**template** do serviço (`servico_checklist_itens`); não há estado por OS.

- **Entidade nova `ItemChecklistOrdemServico`** no **escopo offline** (UUID +
  `UpdatedAt`/`DeletedAt` + no `/sync`): é trabalho de bancada, e o doc de stack
  manda tratar entidades do fluxo de campo assim **desde o primeiro migration**
  — mesmo padrão já aplicado a OS, histórico, peças usadas e comentários.
- **Snapshot da descrição** (não FK para o template): o serviço pode editar o
  checklist depois; a OS mantém o que tinha quando foi aberta.
- **Materialização na criação da OS** (ambos os caminhos: manual e conversão de
  agendamento), via helper compartilhado — mesmo lugar do `RegistrarHistorico`.
  OS anteriores a esta etapa ficam sem checklist (limite consciente: é recurso
  going-forward; backfill em tabela FORCE-RLS é a armadilha já conhecida).
- **Marcar/desmarcar** grava quem e quando; entra no detalhe da OS e no `/sync`.

## Endpoints

- `GET /api/ordens-servico/{id}/checklist` — itens com estado.
- `PUT /api/ordens-servico/{id}/checklist/{itemId}` — `{ concluido }`.
- `OrdemServicoDetalheResponse` ganha o checklist; `/sync` também.

## Front-end (`/bancada`, mobile-first)

- **Lista das OS do técnico logado**, ordenada por prioridade e prazo, com o SLA
  já calculado (cor do card) e o aparelho/serviço — cards grandes, alvo de
  toque, uma coluna no celular.
- **Detalhe enxuto**: avançar etapa, registrar peça usada (baixa automática),
  marcar checklist, comentário interno. **Sem** orçamento, pagamento, margem ou
  dados de cliente além do necessário para o reparo.
- **Nav**: "Bancada" para gestor e técnico (o atendente não vai à bancada).
- Responsividade real: testada em viewport de celular.

## Fora desta etapa (registrado, depende de infra externa)

- **Anexar foto da câmera** (doc, essencial): depende do **Cloudflare R2**, que
  ainda não foi provisionado. É o único item essencial do módulo que fica de
  fora, e só por dependência externa — não por escopo.
- Registro de tempo por etapa e atribuição por especialidade são **Fase 3** no
  próprio doc.

## Passos

1. Plano commitado.
2. Backend: entidade + materialização + endpoints + `/sync` + detalhe +
   migração (RLS); testes (materializa na criação, marca/desmarca com autor,
   entra no sync, isolamento).
3. Suíte verde; RLS conferido no Postgres.
4. Orval regen; front (`/bancada` + detalhe + nav); e2e em viewport móvel.
5. Docs (`progresso.md`, roadmap).
