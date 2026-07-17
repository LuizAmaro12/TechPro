# Etapa: Linha do tempo visual da OS (Fase 2 — portal do cliente)

Data: 2026-07-17 · 1º item web da Fase 2 (`docs/fases_MVP.md`). Escopo web.

## Objetivo

O portal público de acompanhamento passa a mostrar a **jornada real** do
aparelho — cada etapa que a OS percorreu com data/hora — em vez de só a régua
estática do fluxo. É "o mesmo dado do Kanban exibido de outro jeito" (doc de
módulos), reforçando o diferencial de transparência sem custo alto.

## Decisões técnicas (sem pergunta ao usuário — escolhas óbvias/seguras)

- **Dado já existe**: `ordem_servico_historico_etapas` grava cada transição
  (DeEtapa, ParaEtapa, UsuarioId, Motivo, CriadoEm). Sem entidade nova, sem
  migração.
- **Projeção client-safe**: a resposta pública expõe só `{ etapa, alcancadaEm }`
  por etapa percorrida — **nunca** nome de usuário nem motivo (podem conter nota
  interna). A loja já vê a trilha completa (com usuário/motivo) no detalhe da OS.
- **Primeira vez que alcançou**: se uma etapa foi revisitada (correção), usa o
  `CriadoEm` mais antigo daquela `ParaEtapa`.
- **Front**: a régua do fluxo já existente ganha o carimbo de hora real em cada
  etapa alcançada (✓ + data/hora); etapa atual destacada; futuras esmaecidas.
  OS cancelada segue tratada como hoje (estado "Cancelado"), com a linha do
  tempo mostrando o que percorreu.

## Backend

- `AcompanhamentoResponse` ganha `List<EtapaAlcancadaResponse> LinhaDoTempo`
  (`EtapaAlcancadaResponse(EtapaOrdemServico Etapa, DateTimeOffset AlcancadaEm)`).
- `AcompanhamentoController.Obter` popula a partir do histórico da OS (sob o
  tenant fixado pelo slug), agrupando por `ParaEtapa` com `min(CriadoEm)`,
  ordenado por tempo.
- Teste: a linha do tempo traz as etapas percorridas em ordem e **não** expõe
  nome de usuário (o tipo simplesmente não tem o campo — garante por
  construção); isolamento já coberto pelo padrão do acompanhamento.

## Front-end

- `/acompanhar/{slug}/{codigo}`: a régua passa a usar a linha do tempo — cada
  etapa alcançada mostra a data/hora real; a atual fica em destaque.

## Passos

1. Roadmap + plano commitados.
2. Backend: DTO + controller + teste (105 total).
3. Suíte verde no container.
4. Orval regen; front no /acompanhar; e2e com evidência.
5. Docs (`progresso.md`).
