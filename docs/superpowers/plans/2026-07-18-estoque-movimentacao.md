# Etapa: Estoque com movimentação rastreável e lista de compra (Fase 2)

Data: 2026-07-18 · 5º item web da Fase 2. Escopo web.

Primeira parte do bloco "catálogo/estoque" do roadmap. Ataca antes de tudo a
**integridade do saldo**, que hoje é um débito real.

## Diagnóstico (o que motivou esta etapa)

O `QuantidadeEmEstoque` é alterado em **três** pontos, nenhum deles registrado:

1. `OrdemServicoPecaService` dá baixa ao usar peça na OS (`-= quantidade`);
2. o mesmo serviço estorna ao remover a linha (`+= quantidade`);
3. `PecaService.Aplicar` sobrescreve o campo inteiro na edição do catálogo.

Consequências concretas:

- **O saldo não é explicável.** Não há como responder "por que temos 3 telas se
  compramos 10?" — informação que a persona usa para brigar com fornecedor.
- **Não existe entrada de estoque.** Receber mercadoria hoje significa editar o
  número na mão, o que também **sobrescreve silenciosamente** qualquer baixa
  concorrente feita por outro usuário entre o carregamento e o salvamento.
- Sem histórico, "previsão de reposição" e "histórico de preço por fornecedor"
  (itens seguintes do roadmap) não têm base de dados para existir.

## Decisões técnicas (justificadas)

- **Razão (ledger) append-only** `movimentacoes_estoque`, com `Quantidade`
  **assinada** (entrada +, saída −). Assim `SUM(quantidade)` reconcilia com o
  saldo e a auditoria é uma consulta, não um algoritmo.
- **`SaldoApos` gravado na linha**: o histórico fica legível sem soma corrida e
  qualquer divergência futura entre razão e saldo vira evidência, não mistério.
- **Funil único**: todas as três mutações passam a chamar
  `EstoqueService.RegistrarAsync`. Um caminho novo que esqueça o razão vira
  exceção de compilação, não um bug silencioso — é o mesmo raciocínio do
  `RegistrarHistoricoEtapa` no SLA.
- **A edição do catálogo continua aceitando o campo**, mas passa a gravar um
  movimento de `Ajuste` com o delta. Zero regressão de UX e o saldo fica
  explicável. Ajuste **exige motivo** (o resto do sistema já trata motivo como
  o que dá sentido a um registro).
- **Fora do escopo offline** (int PK, sem `/sync`): o razão é administrativo e
  derivado. O que o técnico gera em campo é a peça usada na OS, que **já**
  sincroniza — o movimento de consumo é consequência dela, não entrada dupla.
- **Estoque negativo continua permitido** (decisão de 2026-07-15): a UI avisa,
  não bloqueia. O razão apenas passa a registrar como se chegou lá.
- **Entrada carrega `CustoUnitario` opcional** e, quando informado, atualiza o
  custo da peça. É o primeiro tijolo do "histórico de preço por fornecedor" sem
  antecipar a tela dele.

## Modelo de dados

`MovimentacaoEstoque` (int PK, tenant): PecaId, Tipo
(`Entrada|Saida|Ajuste|ConsumoOs|EstornoOs`), Quantidade (assinada), SaldoApos,
CustoUnitario?, Motivo?, OrdemServicoId?, UsuarioId?, CriadoEm.
Migração com RLS.

## Endpoints

- `GET /api/pecas/{id}/movimentacoes` — extrato da peça.
- `POST /api/pecas/{id}/movimentacoes` — entrada/saída/ajuste manual.
- `GET /api/estoque/lista-compra` — peças no/abaixo do mínimo agrupadas por
  fornecedor, com sugestão de quantidade e custo estimado da compra.

## Front-end

- **Peças**: ação "Movimentar" (entrada/saída/ajuste com motivo) e extrato da
  peça com saldo após cada linha.
- **Lista de compra**: seção agrupada por fornecedor com total estimado.

## Fora desta etapa (registrado, não esquecido)

- **Kits de serviço**: já existem de fato como peças padrão do serviço
  (`servico_pecas` + `aplicar-padrao`). Renomear/expandir sem demanda real
  seria refatoração sem benefício.
- **Peça compatível/equivalente** e **previsão de reposição**: dependem de
  histórico acumulado — agora passam a ter a base para existir.

## Passos

1. Plano commitado.
2. Backend: entidade + `EstoqueService` + funil nos 3 pontos + endpoints;
   testes (razão bate com saldo, consumo/estorno registram, ajuste exige
   motivo, lista de compra, isolamento).
3. Suíte verde; RLS conferido no Postgres.
4. Orval regen; front; e2e com evidência.
5. Docs (`progresso.md`, roadmap).
