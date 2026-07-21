# Etapa: Fila de espera de agendamento (Fase 2)

Data: 2026-07-21 · 8º item web da Fase 2. Fecha o bloco de agendamento.

## Diagnóstico / valor

O doc de módulos é explícito: "fila de espera / lista de interesse quando não há
horário disponível na data desejada. **Hoje essa demanda simplesmente se perde;
capturá-la é receita que já existe e está sendo descartada**". Hoje o portal, ao
não achar vaga, é um beco sem saída — o cliente vai embora e a loja nem sabe que
ele existiu.

## Decisões técnicas (justificadas)

- **Entidade própria `EntradaFilaEspera`** (não um status de `Agendamento`): uma
  entrada de fila não ocupa horário, não tem `HoraInicio`, não vira OS. Forçá-la
  no `Agendamento` sujaria toda regra de disponibilidade/capacidade. São coisas
  diferentes.
- **Captura nas duas pontas**: portal (cliente entra sozinho quando não há vaga)
  e manual (a loja registra quem ligou). `Origem` distingue, como no agendamento.
- **Vínculo silencioso por telefone** na captura pública (reusa
  `VincularOuCriarPorTelefoneAsync`): o cliente já entra ligado ao CRM, então a
  loja vê quem é e o histórico acumula — mesmo padrão do agendamento público.
- **Converter reusa `CriarManualAsync`**: a loja escolhe data/hora quando abre
  vaga; a conversão cria o agendamento normal (com todas as validações de
  disponibilidade) e marca a entrada como `Convertida`, guardando o
  `AgendamentoId`. Zero duplicação da lógica de agendar.
- **Descartar é explícito** (com motivo opcional): a fila não cresce para sempre;
  `Descartada` tira da lista ativa sem apagar o registro (histórico de demanda).
- **Estados**: `Aguardando → Convertida | Descartada`. Terminal não reabre.
- **`DataPreferida` é só informativa** (a data que o cliente queria e não tinha):
  não reserva nada, não valida — orienta a loja sobre a urgência.
- **Fora do escopo offline** (int PK, sem `/sync`): é captação administrativa,
  não fluxo de campo do técnico.

## Modelo de dados

`EntradaFilaEspera` (int PK, tenant): ServicoId, ClienteId?, NomeContato,
TelefoneContato, EmailContato?, DataPreferida?, DescricaoProblema?,
AparelhoMarca?, AparelhoModelo?, Origem, Status, CriadoEm, ResolvidaEm?,
AgendamentoId?, MotivoDescarte?. Migração com RLS.

## Endpoints

- Público: `POST /api/publico/{slug}/fila-espera`.
- Interno: `GET /api/fila-espera?status=`, `POST /api/fila-espera` (manual),
  `POST /api/fila-espera/{id}/converter` ({ data, horaInicio }),
  `POST /api/fila-espera/{id}/descartar` ({ motivo? }).

## Front-end

- **Portal**: quando a disponibilidade da data volta sem horários, oferecer
  "entrar na fila de espera" com o mesmo formulário de contato.
- **Agenda (interno)**: painel "Fila de espera" com a contagem, converter (abre
  a escolha de data/hora) e descartar.

## Fora desta etapa (registrado)

- **Notificar automaticamente quando abre vaga**: depende de canal
  (WhatsApp/e-mail) e de detectar a vaga — sub-projeto próprio. A conversão
  manual já entrega o valor central (não perder a demanda).

## Passos

1. Plano commitado.
2. Backend: entidade + serviço + endpoints + migração (RLS); testes (captura
   pública com vínculo, captura manual, converter cria agendamento e marca
   convertida, descartar, isolamento).
3. Suíte verde; RLS conferido no Postgres.
4. Orval regen; front (portal + agenda); e2e com evidência.
5. Docs (`progresso.md`, roadmap).
