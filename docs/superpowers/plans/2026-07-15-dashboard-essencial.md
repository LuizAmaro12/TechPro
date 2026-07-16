# Etapa: Dashboard essencial (módulo 12)

Data: 2026-07-15 · Ordem recomendada da Fase 1, item 9 (`docs/fases_MVP.md`).
Etapa de **agregação read-only** dos dados que já existem — sem entidade nova,
sem migração, sem dependência externa.

## Decisões aprovadas pelo usuário (2026-07-15)

1. **Escopo**: 6 KPIs essenciais + "Radar do dia" (OS atrasadas + orçamentos
   pendentes há mais de 2 dias, com link direto) + comparativo de faturamento
   mês atual vs. anterior.
2. **Faturamento do mês** = soma dos **pagamentos recebidos no mês** (caixa
   real; casa com o módulo de pagamentos).
3. **Aparelhos em reparo** = **bancada inteira** (todas as etapas de trabalho
   ativo: NaFila, EmDiagnostico, AguardandoAprovacao, AguardandoPeca, EmReparo,
   EmTeste). Reflete "quantos aparelhos estão na oficina agora".

## Decisões técnicas (justificadas)

- **Um endpoint agregador** `GET /api/dashboard` (uma ida ao servidor, tudo
  calculado no banco sob GQF). Sem entidade nem tabela — só leitura.
- **"Hoje" e "mês" são hora de parede da loja** (data UTC do servidor, como o
  resto da agenda) — consistente com as decisões já registradas; fuso por loja
  fica para quando houver demanda.
- **Somas de decimal em memória** (Sqlite dos testes não agrega decimal): filtra
  os pagamentos do período no banco e soma no C# (Postgres idem, poucas linhas).
  Filtros de data traduzem — o conversor DateTimeOffset dos testes preserva
  ordem.
- **Radar limitado** ao que o dado suporta hoje: OS atrasadas e orçamentos
  pendentes. O terceiro item do doc ("peça que chegou libera reparo parado")
  depende de rastreio de chegada de peça, que não existe — fica anotado para
  quando o estoque tiver entradas/movimentação (Fase 2).
- **Módulo `Dashboard/`** (novo em `Modules/`), coerente com o monolito modular.

## Contrato (`GET /api/dashboard`)

```
{
  osAbertas: int,                 // não Entregue/Cancelado, não apagadas
  agendamentosHoje: int,          // Data == hoje, Status Agendado
  servicosEmAtraso: int,          // prazoEstimado < hoje e não finalizada
  aparelhosEmReparo: int,         // etapas de bancada
  prontosParaRetirada: int,       // etapa ProntoParaRetirada
  faturamentoMes: decimal,        // pagamentos do mês atual
  faturamentoMesAnterior: decimal,
  variacaoFaturamentoPct: decimal?, // null quando o mês anterior é zero
  radar: {
    osAtrasadas: [ { id, numero, clienteNome, servicoNome, prazoEstimado, diasAtraso } ],
    orcamentosPendentes: [ { id, numero, clienteNome, total, enviadoEm, diasAguardando } ]
  }
}
```

Listas do radar limitadas a ~10 itens, ordenadas pela maior urgência; contagem
total sinalizada quando houver corte.

## Front-end

- `/dashboard` deixa de mostrar só empresa/papel/tenant e passa a ser o painel:
  linha de KPIs (cards), bloco "Radar do dia" no topo (com link para a OS) e o
  comparativo de faturamento com seta de tendência. Cada card/link leva à tela
  correspondente (Kanban/OS/Agenda) com o filtro pertinente quando fizer sentido.

## Passos

1. Plano commitado.
2. Backend: `DashboardService` + DTOs + `DashboardController`; testes de
   integração (KPIs, faturamento com pagamentos, radar, isolamento).
3. Suíte verde no container.
4. Orval regen; front `/dashboard`; e2e com evidência.
5. Docs (`progresso.md`, README) e commits por passo.
