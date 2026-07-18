# Etapa: Financeiro Fase 2 — margem e rentabilidade

Data: 2026-07-18 · 3º item web da Fase 2 (módulo 8, funcionalidades avançadas).
Entrega o "quanto sobrou" que o branding define como diferencial central.

## Decisões aprovadas pelo usuário (2026-07-18)

1. **Base = OS entregues no período** (margem *realizada*): só entra OS que
   chegou a `Entregue`. Serviço concluído, peça de fato consumida — sem ruído
   de OS em andamento ou que ainda pode ser cancelada.
2. **Comissão automática por técnico: DEFERIDA.** Exige tela de configuração de
   percentual por técnico/loja e abatimento no cálculo — sub-projeto próprio,
   melhor isolado (mistura rentabilidade com folha). Anotado.

## Como a margem é calculada

Usa a **separação estrutural serviço/peça** e o **custo já congelado** no
momento do uso (`ordem_servico_pecas.CustoUnitarioNoUso`) — por isso a margem
é histórica e não muda se o preço da peça mudar depois no catálogo.

Por OS entregue no período:
- **Receita** = total do orçamento (mão de obra + peças − desconto).
- **Custo de peças** = Σ(`CustoUnitarioNoUso` × quantidade) das peças da OS.
- **Lucro bruto** = receita − custo de peças.
- **Margem %** = lucro ÷ receita.

Agrupado pelo **serviço principal da OS** → receita, custo, lucro, margem e
quantidade por serviço; ordenado por lucro = "serviços mais lucrativos".

### Detalhes que a UI precisa deixar claros

- **Data de entrega** = 1ª transição para `Entregue` no histórico; a OS precisa
  estar `Entregue` hoje (se voltou atrás, não conta).
- **OS sem orçamento**: entra com receita 0 (o custo de peça é real). O total de
  `osSemOrcamento` é exposto para o número ser explicável — sinaliza à loja que
  faltou registrar orçamento.
- Esta visão é **competência (entrega)**, diferente do faturamento da Fase 1 que
  é **caixa (pagamentos)**. As duas coexistem de propósito — o doc separa
  "faturamento" (Fase 1) de "margem/rentabilidade" (Fase 2).

## Contrato (`GET /api/financeiro/rentabilidade?de&ate`)

```
{ de, ate, quantidadeOs, osSemOrcamento,
  receitaTotal, custoPecas, lucroBruto, margemPercentual,
  porServico: [ { servicoId, servicoNome, quantidadeOs,
                  receita, custoPecas, lucroBruto, margemPercentual } ] }
```

Leitura pura sob GQF; **sem entidade nova, sem migração**. Somas de decimal em
memória (Sqlite dos testes não agrega decimal; volumes por período são baixos).

## Front-end

- `/financeiro` ganha a seção **Rentabilidade** com o mesmo seletor de período:
  cards (lucro bruto, receita, custo de peças, margem %) e tabela por serviço
  ordenada por lucro, com aviso quando houver OS sem orçamento.

## Fora de escopo (deferido, com justificativa)

- **Comissão por técnico** (decisão acima).
- **Relatórios exportáveis**: item próprio; a tela já mostra os números.
- **Split de pagamento**: o doc coloca em Fase 3 (cautela explícita).

## Passos

1. Plano commitado.
2. Backend: DTOs + método no `FinanceiroRelatorioService` + endpoint; testes
   (margem com custo congelado, agrupamento por serviço, OS não entregue fora,
   período, isolamento).
3. Suíte verde no container.
4. Orval regen; front (seção Rentabilidade); e2e com evidência.
5. Docs (`progresso.md`, roadmap).
