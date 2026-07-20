# Etapa: Sinalização de peça em falta na agenda (Fase 2)

Data: 2026-07-20 · 7º item web da Fase 2. Escopo web.

Segunda parte do bloco "agendamento" e o elo com o **estoque rastreável**
recém-feito.

## Diagnóstico / valor

O serviço já declara as "peças normalmente utilizadas" (`servico_pecas` com
`QuantidadePadrao`), e agora o estoque é confiável. Faltava juntar as duas
coisas no ponto em que a informação muda uma decisão: **a agenda**. Hoje a loja
só descobre que não tem a peça quando o cliente já está no balcão. Sinalizar
antes permite pedir a peça ou remarcar — exatamente o que o doc de módulos pede
em "sinalização por indisponibilidade de peça (liga com estoque)".

## Decisões técnicas (justificadas)

- **A regra mora no estoque, não na agenda.** `EstoqueService` ganha
  `FaltasPorServicoAsync(servicoIds)` → por serviço, a lista de peças padrão
  cujo saldo é menor que a quantidade necessária. A agenda apenas consome. Assim
  a mesma verdade serve OS e futuros consumidores sem duplicar lógica.
- **Cálculo em lote, nunca N+1**: uma consulta de `servico_pecas` (com o saldo
  da peça incluído) para todos os serviços da listagem, agrupada por serviço.
- **Só conta peça ativa** e com `QuantidadePadrao > 0`. Serviço sem peça padrão
  não sinaliza nada (não há o que faltar).
- **Sinal, não bloqueio.** Coerente com o resto do sistema (estoque negativo é
  permitido, a UI avisa): a falta nunca impede agendar nem dar check-in — apenas
  informa. A loja pode usar peça equivalente, encomendar, etc.
- **Reflete o estoque atual, não um "reservado".** Não desconto peças de outros
  agendamentos do mesmo dia: reserva de estoque é um problema maior (concorrência
  entre OS) que o doc não pede agora. Sinalizar contra o saldo real já entrega o
  valor e não inventa semântica que a loja não controla. Registrado como limite
  consciente.

## Modelo de dados

**Nenhuma tabela nem coluna nova.** Deriva de `servico_pecas` + `pecas`.

## Contrato

- `AgendamentoResponse` ganha `pecasEmFalta: [{ pecaId, pecaNome, necessario,
  emEstoque }]` (vazio quando o serviço está abastecido). Sem endpoint novo — o
  dado viaja na listagem e no detalhe que a agenda já consome.

## Front-end

- **Agenda**: selo "⚠ peça em falta" no card em aberto, com tooltip listando o
  que falta (peça, necessário × em estoque).

## Fora desta etapa (registrado)

- **Reserva de estoque entre OS** (descontar o que já está comprometido): maior,
  não pedido agora.
- **Mesmo sinal no detalhe da OS**: valioso para o técnico, mas é fluxo de OS;
  candidato a etapa própria — a lógica já fica pronta para reuso.
- **Fila de espera**: continua fora (UX própria).

## Passos

1. Plano commitado.
2. Backend: DTO + `FaltasPorServicoAsync` + integração na listagem/detalhe;
   testes (falta detectada, serviço abastecido não sinaliza, peça inativa
   ignorada, lote, isolamento).
3. Suíte verde.
4. Orval regen; front (agenda); e2e com evidência.
5. Docs (`progresso.md`, roadmap).
