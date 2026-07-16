# Etapa: Financeiro básico (módulo 8) — lacuna da Fase 1

Data: 2026-07-16 · Lacuna identificada na auditoria de 2026-07-16 (o módulo 8
tinha só o KPI de faturamento do mês no dashboard; a rota `financeiro/` prevista
na seção 13 do doc de stack não existia).

Objetivo do doc: *"mostrar 'quanto sobrou', não só 'quanto entrou'"*. A margem
em si é Fase 2 — a Fase 1 entrega a visão de caixa e a base já separada
(receita de serviço vs. custo de peça, congelados na OS).

## Decisões aprovadas pelo usuário (2026-07-16)

1. **Ticket médio = faturamento do período ÷ nº de OS distintas que receberam
   pagamento no período**. Coerente com o faturamento (caixa recebido).
2. **Pendentes (a receber) = OS com orçamento APROVADO e saldo em aberto.**
   Orçamento enviado sem resposta não conta — é proposta, não receita vendida.
3. **Incluir a projeção simples de fluxo de caixa**: aprovados aguardando
   pagamento + valor esperado dos agendamentos dos próximos 7 dias.

## Escopo (essenciais da Fase 1, módulo 8)

| Item | Como |
|---|---|
| Faturamento | Soma dos pagamentos no período |
| Receita por período | Seletor de período (presets + intervalo livre) |
| Transações | Lista dos pagamentos: data, OS, cliente, forma, valor |
| Pagamentos pendentes | OS com orçamento aprovado e saldo > 0 (lista + total) |
| Pagamentos concluídos | OS quitadas no período (total + contagem) |
| Ticket médio | Faturamento ÷ OS distintas pagas no período |
| Projeção de caixa | Aprovados a receber + agendamentos dos próximos 7 dias |

Fora de escopo (Fase 2, conforme o doc): lucro bruto, custo de peças, receita
por serviço, serviços mais lucrativos, margem média, relatórios exportáveis,
comissão por técnico. Split de pagamento é Fase 3 (cautela do doc).

## Decisões técnicas (justificadas)

- **Endpoint agregador** `GET /api/financeiro?de=&ate=` no módulo `Financeiro/`
  (já existe), ao lado do `FinanceiroService`. Leitura pura sob GQF, sem
  entidade nem migração — todo o dado já está em `pagamentos`, `orcamentos`,
  `ordens_servico` e `agendamentos`.
- **Período**: `DateOnly de/ate` (hora de parede da loja, como o resto);
  default = mês corrente. O filtro em `Pagamento.CriadoEm` usa
  `[de 00:00, ate+1d 00:00)` em UTC — consistente com o dashboard.
- **Somas de decimal em memória** (Sqlite dos testes não agrega decimal); os
  volumes por período são pequenos.
- **Valor esperado dos agendamentos** = soma do `PrecoBase` do serviço dos
  agendamentos com status `Agendado` nos próximos 7 dias. É estimativa (o
  orçamento real pode diferir) — a UI deixa isso explícito.
- **Reuso**: o total do orçamento e o saldo já têm regra no `FinanceiroService`
  (mão de obra + peças congeladas − desconto); o agregador segue a mesma
  fórmula para não divergir.

## Contrato (`GET /api/financeiro?de&ate`)

```
{
  de, ate,
  faturamento: decimal,            // pagamentos no período
  quantidadeOsPagas: int,
  ticketMedio: decimal,            // faturamento ÷ quantidadeOsPagas (0 se nenhuma)
  quantidadeTransacoes: int,
  transacoes: [ { pagamentoId, ordemServicoId, numero, clienteNome, forma, valor, criadoEm } ],
  porForma: [ { forma, total, quantidade } ],   // composição do caixa
  aReceber: decimal,               // total dos saldos aprovados em aberto
  pendentes: [ { ordemServicoId, numero, clienteNome, total, pago, saldo } ],
  projecao: {
    aprovadosAReceber: decimal,
    agendamentosProximos7Dias: decimal,
    total: decimal
  }
}
```

Listas limitadas (transações ~100, pendentes ~50) com o total sinalizado.

## Front-end

- `(empreendedor)/financeiro`: seletor de período (Hoje, 7 dias, Este mês, Mês
  passado, personalizado), KPIs (faturamento, ticket médio, nº de transações,
  a receber), bloco de projeção de caixa, composição por forma de pagamento,
  lista "A receber" (link para a OS) e tabela de transações.
- Link "Financeiro" no nav do layout; card de faturamento do dashboard passa a
  linkar para `/financeiro`.

## Passos

1. Plano commitado.
2. Backend: DTOs + `FinanceiroRelatorioService` + endpoint; testes de
   integração (faturamento/ticket/transações, pendentes só com aprovado,
   projeção, período, isolamento).
3. Suíte verde no container.
4. Orval regen; front (`/financeiro` + nav + link do dashboard); e2e com
   evidência.
5. Docs (`progresso.md` — remover a lacuna do módulo 8 —, README) e commits.
