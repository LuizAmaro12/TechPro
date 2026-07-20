# Etapa: Não comparecimento e histórico de comparecimento (Fase 2)

Data: 2026-07-20 · 6º item web da Fase 2. Escopo web.

Primeira parte do bloco "agendamento" do roadmap. Ataca a lacuna de **regra de
negócio** mais concreta do módulo.

## Diagnóstico

O `Agendamento` só tem três estados: `Agendado`, `CheckInRealizado`,
`Cancelado`. Não existe **não comparecimento**. Consequências:

- Um cliente que furou o horário fica preso em `Agendado` para sempre ou é
  cancelado como se tivesse avisado — as duas coisas mentem sobre o que
  aconteceu.
- A loja não tem como saber **quais clientes faltam**, embora o doc de módulos
  peça "histórico de comparecimento por cliente" justamente para isso.
- Métricas futuras (taxa de comparecimento, risco de falta) não têm o dado.

## Decisões técnicas (justificadas)

- **Novo estado terminal `NaoCompareceu`**, distinto de `Cancelado`: cancelar é
  o cliente avisando; faltar é não aparecer. Misturar os dois apaga a
  informação que dá valor ao recurso.
- **Transição só a partir de `Agendado`**, como check-in e cancelamento. Marcar
  falta grava `CanceladoEm`? Não — é evento próprio: reaproveito o timestamp
  existente `CanceladoEm` **não**; adiciono a semântica no status e uso um
  campo dedicado só se necessário. O `Status` + `ReagendadoEm`/`CanceladoEm`
  atuais bastam; falta não precisa de motivo (o "motivo" é a própria ausência).
- **Histórico de comparecimento é derivado, não uma tabela nova**: os próprios
  agendamentos já são a fonte. `GET /api/clientes/{id}/comparecimento` agrega
  `compareceu` (CheckInRealizado), `faltou` (NaoCompareceu) e `cancelou`
  (Cancelado) + a lista recente. Zero migração de dados, zero denormalização a
  manter sincronizada.
- **Sinal de risco onde a decisão é tomada**: na agenda, um agendamento cujo
  cliente já faltou antes ganha um aviso discreto. O cálculo é **em lote** (uma
  consulta agregada por cliente dos agendamentos do dia), nunca N+1.
- **Só conta cliente vinculado**: agendamento de portal sem `ClienteId` não tem
  histórico — e vincular por telefone já acontece no check-in, então clientes
  recorrentes acumulam histórico naturalmente.

## Modelo de dados

Apenas o enum `StatusAgendamento` ganha `NaoCompareceu`. **Sem tabela nova, sem
coluna nova** — logo, sem migração de schema (só o `HasConversion<string>` do
status, que já existe, passa a aceitar o novo valor). Confirmar no migration
que nenhuma coluna muda.

## Endpoints

- `POST /api/agendamentos/{id}/nao-compareceu` — marca a falta (de `Agendado`).
- `GET /api/clientes/{id}/comparecimento` — resumo + histórico recente.
- `GET /api/agendamentos?...` passa a devolver, por item, `clienteFaltasAnteriores`
  (0 quando não há cliente vinculado) para a agenda colorir.

## Front-end

- **Agenda**: ação "Não compareceu" no agendamento agendado; selo discreto de
  "já faltou N×" quando o cliente tem histórico.
- **Clientes**: resumo de comparecimento (compareceu / faltou / cancelou) no
  detalhe/linha do cliente.

## Fora desta etapa (registrado, não esquecido)

- **Fila de espera**: superfície de UX própria (encaixe quando abre vaga) —
  merece etapa dedicada.
- **Sinalização por indisponibilidade de peça**: liga com o estoque recém-feito,
  mas pertence ao fluxo de check-in/OS; melhor tratada junto dele.

## Passos

1. Plano commitado.
2. Backend: enum + serviço + endpoints + agregação de faltas na listagem;
   testes (marca falta só de Agendado, histórico agrega certo, faltas na
   agenda, isolamento).
3. Suíte verde; migração conferida como no-op de schema.
4. Orval regen; front (agenda + clientes); e2e com evidência.
5. Docs (`progresso.md`, roadmap).
