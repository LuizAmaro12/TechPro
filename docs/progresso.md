# Progresso — TechPro

> Diário técnico do que está construído, decidido e pendente.
> Complementa (não substitui) os documentos de produto e de stack.

---

## Status geral

**Fundação do MVP concluída em 2026-07-05.** O critério de pronto da primeira
fase técnica foi atingido e verificado de ponta a ponta em navegador real:

> `docker compose up` → acessar o front-end → criar conta → fazer login →
> receber JWT válido com `tenant_id`.

Evidência da verificação (Playwright + Edge, 2026-07-05):

```json
{
  "raizRedirecionaPara": "/login",
  "cadastroLevaAoDashboard": true,
  "dashboardMostraEmpresa": true,
  "dashboardMostraPapel": true,
  "jwt": {
    "status": 200,
    "tenant_id": "eff7220b-07b1-4827-bd9b-b47bf90e6904",
    "role": "gestor",
    "iss": "TechPro",
    "expEmMinutos": 15
  },
  "dashboardMostraTenantId": true,
  "sairVoltaParaLogin": true,
  "loginLevaAoDashboard": true,
  "senhaErradaMostraErro": true
}
```

Durante a própria verificação o rate limiter respondeu `429` a partir da 11ª
chamada de auth no mesmo minuto — o limite de 10/min/IP funcionando ao vivo.

Suíte de testes do back-end: **104 testes xUnit verdes** (GQF por convenção,
TokenService, fluxo de auth, catálogo, clientes, agenda, OS, estoque,
financeiro, comunicação, dashboard e onboarding — integração via
WebApplicationFactory + Sqlite em memória).

### Etapa Financeiro básico concluída em 2026-07-16 (fecha a lacuna do módulo 8)

Módulo 8 (essenciais da Fase 1). Plano e decisões em
`docs/superpowers/plans/2026-07-16-financeiro-basico.md`.
Evidência e2e (Playwright + Edge, 2026-07-16):

```json
{
  "dashboardLevaAoFinanceiro": true,
  "kpisFaturamentoETicket": true,
  "aReceberSoAprovado": true,
  "projecaoDeCaixa": true,
  "composicaoPorForma": true,
  "tabelaDeTransacoes": true,
  "filtroDePeriodoFunciona": true,
  "apiConfere": true
}
```

> **Ordem recomendada da Fase 1: 10/10 concluídos em 2026-07-16**, cada um com
> testes de integração, RLS verificado no Postgres real e evidência e2e no
> navegador. O **critério de conclusão** da Fase 1 (`docs/fases_MVP.md`) — "a
> loja se cadastra, configura serviços e peças, recebe um agendamento, gera uma
> OS, move até a entrega, baixa estoque, registra pagamento, notifica o cliente
> e vê o estado no dashboard, sem intervenção manual do fundador" — está
> atendido de ponta a ponta.
>
> **Fase 1 completa por módulo em 2026-07-17.** A auditoria de 2026-07-16
> encontrou duas lacunas no *escopo por módulo* (um resumo anterior tinha
> declarado a fase completa olhando só para a ordem recomendada — correção
> registrada). Ambas foram fechadas: **módulo 8 (Financeiro básico)** em
> 2026-07-16 e **módulo 13 (Configurações e equipe básica)** em 2026-07-17.
> Ficam fora, com registro e justificativa: logo da loja (depende do
> Cloudflare R2) e convite de equipe (o doc coloca equipe/permissões na
> Fase 2).

## Fase 2 (recorte WEB) — em andamento

O roadmap web da Fase 2 (separando o que é web do que é mobile/externo) está em
`docs/superpowers/plans/2026-07-17-roadmap-fase2-web.md`. **Mobile (app nativo)
permanece a última etapa do projeto — não iniciado.**

### Avaliações e reputação — concluída em 2026-07-21

10º item web da Fase 2 (módulo 10 do doc). Plano em
`docs/superpowers/plans/2026-07-21-avaliacoes-reputacao.md`.

**Decisões do usuário (AskUserQuestion)**: escala **dupla** (estrelas 1–5 para a
experiência + recomendação 0–10 para NPS) e escopo **núcleo + fechamento de loop
+ por técnico**. O doc lista "nota e comentário … NPS" sem fixar a escala — como
é foundational e difícil de trocar depois, foi decidido em conjunto em vez de
assumido.

- **Módulo próprio `Reputacao`**: a avaliação referencia a OS mas é domínio
  distinto (reputação gerenciada), com resumo/NPS/loop próprios.
- **Uma avaliação por OS** (índice único em `ordem_servico_id`) — é o veredito
  daquele reparo, não uma pesquisa recorrente.
- **Gatilho só após a entrega** (exigência explícita do doc, para não avaliar o
  contexto errado): novo evento `PedidoAvaliacao` sai no `Entregue`, reusando o
  `ComunicacaoService` — respeita o consentimento LGPD **e** a matriz de
  preferências (a loja pode desligar o pedido por canal; a matriz passou de 7×2
  para 8×2, o que quebrou — corretamente — um teste de Configurações que foi
  atualizado).
- **Envio pelo link público existente** (slug + código opaco): o cliente avalia
  sem login, na mesma página em que acompanha. O `GET` do acompanhamento passa a
  expor `podeAvaliar`/`jaAvaliada`.
- **Snapshot de serviço e técnico** no momento da avaliação: a satisfação por
  técnico usa quem estava responsável na entrega (a reatribuição tem trilha, mas
  a avaliação congela o responsável).
- **"Negativa" = `nota <= 2` OU `recomendação <= 6`** (detrator): qualquer sinal
  forte de insatisfação abre o loop. Resolver exige a **nota de tratamento** —
  sem ela não há "reputação gerenciada". Positiva não pode ser "resolvida".
- **NPS clássico**: promotores 9–10, neutros 7–8, detratores 0–6;
  `score = %promotores − %detratores`. Agregação em memória (Sqlite dos testes
  não agrega decimais no servidor — padrão já usado no Financeiro).
- **RLS `ENABLE`+`FORCE`** na tabela nova → **23/23 tabelas de tenant**.
- **Evidência**: 6 testes de integração → **163/163**; e2e **12/12** (gatilho na
  entrega, formulário só após entrega, uma por OS, média/NPS/por técnico na
  tela, pendência anunciada, loop fechado e persistido).
- **Registrado como fora desta etapa**: quebra do resumo **por serviço** (o dado
  já é gravado — `ServicoId` — falta só a agregação na tela); campanhas,
  reativação e templates editáveis são outro item do roadmap.

### Importação de clientes por CSV — concluída em 2026-07-21

9º item web da Fase 2 (início do bloco clientes/reputação). Plano em
`docs/superpowers/plans/2026-07-21-importacao-clientes-csv.md`.

**Motivação**: é a **porta de entrada** do produto. Uma loja com carteira
existente (planilha, contatos exportados) não recadastra centenas à mão —
importar reduz o atrito que derruba adoção.

- **Um passo com relatório por linha**: a importação só **adiciona** (nunca
  atualiza/apaga), então importa as válidas e **reporta** duplicadas e
  inválidas. Reimportar depois de corrigir é seguro — há teste de que reenviar
  o mesmo arquivo dá 100% duplicado.
- **Dedup por telefone (só dígitos)** contra o banco **e** dentro do próprio
  arquivo — telefone é a chave natural do cliente no resto do sistema.
- **Parser tolerante**: cabeçalho por nome sem acento/case
  (`nome`/`telefone`/`email`/`cpf`/`endereço`/`obs` e sinônimos), delimitador
  `,` ou `;` detectado (Excel BR usa `;`), campos entre aspas com vírgula
  interna. `nome` e `telefone` obrigatórios no cabeçalho, senão falha inteira
  com mensagem clara.
- **Consentimento LGPD = false** nos importados: importar uma lista não é
  consentimento. Registrado e testado.
- **Teto de 5000 linhas**; transacional (as válidas num único `SaveChanges`).
- **Sem tabela nova** — cria `Cliente` existentes.
- **Evidência**: 6 testes de integração → **157/157**; e2e **6/6** (relatório
  com números, linha de erro listada, importados existem sem consentimento, não
  duplica o existente, reimportar não duplica).
- **Registrado como fora desta etapa**: atualizar existentes na importação
  (merge — decisão de produto arriscada).

### Fila de espera de agendamento — concluída em 2026-07-21

8º item web da Fase 2 — **fecha o bloco de agendamento**. Plano em
`docs/superpowers/plans/2026-07-21-fila-de-espera.md`.

**Motivação (doc de módulos, textual)**: "fila de espera / lista de interesse
quando não há horário disponível na data desejada. Hoje essa demanda
simplesmente se perde; capturá-la é receita que já existe e está sendo
descartada." O portal, ao não achar vaga, era um beco sem saída.

- **Entidade própria `EntradaFilaEspera`**, não um status de `Agendamento`: uma
  entrada de fila não ocupa horário nem vira OS — forçá-la no agendamento
  sujaria disponibilidade/capacidade. São coisas diferentes.
- **Captura nas duas pontas**: portal (o cliente entra sozinho quando a data não
  tem vaga) e manual (a loja registra quem ligou). `Origem` distingue.
- **Vínculo silencioso por telefone** na captura pública (reusa
  `VincularOuCriarPorTelefoneAsync`): o cliente já entra ligado ao CRM.
- **Converter reusa `CriarManualAsync`**: a loja escolhe data/hora, a conversão
  cria o agendamento normal (com todas as validações) e marca a entrada como
  `Convertida` guardando o `AgendamentoId`. Horário indisponível **mantém** a
  entrada na fila (há teste). Zero duplicação da lógica de agendar.
- **Descartar preserva o registro** (histórico de demanda); estado terminal não
  reabre.
- **Fora do escopo offline** (int PK): captação administrativa, não fluxo de
  campo.
- **Fora desta etapa** (registrado): notificar automaticamente quando abre vaga
  (depende de canal + detecção de vaga — sub-projeto próprio). A conversão
  manual já entrega o valor central.
- **RLS `ENABLE`+`FORCE`** na tabela nova → **22/22 tabelas de tenant**.
- **Evidência**: 5 testes de integração → **151/151**; e2e **6/6** (portal +
  agenda) e **3/3** (UI do portal: botão aparece sem vaga, confirma, entrada
  chega na loja).

### Sinalização de peça em falta na agenda — concluída em 2026-07-20

7º item web da Fase 2 (segunda parte do bloco agendamento) e o **elo com o
estoque rastreável**. Plano em
`docs/superpowers/plans/2026-07-20-agenda-pecas-em-falta.md`.

- **A regra mora no estoque**: `EstoqueService.FaltasPorServicoAsync(servicoIds)`
  devolve, por serviço, as peças padrão (`servico_pecas`) cujo saldo não cobre a
  `QuantidadePadrao`. Uma consulta em lote (nunca N+1). A agenda consome; a
  mesma verdade fica pronta para a OS e outros reusarem.
- **`AgendamentoResponse.pecasEmFalta`** (vazio = abastecido) viaja na listagem
  e no detalhe — sem endpoint novo. A agenda mostra "⚠ peça em falta" no card em
  aberto, com tooltip do necessário × em estoque.
- **Só peça ativa e com `QuantidadePadrao > 0`**; serviço sem peça padrão não
  sinaliza nada. Há teste de que peça **inativa** zerada é ignorada.
- **Sinal, não bloqueio** (coerente com "estoque negativo permitido"): nunca
  impede agendar/dar check-in.
- **Reflete o saldo real, sem reserva entre OS**: não desconto o que outros
  agendamentos comprometem — reserva de estoque é problema maior (concorrência
  entre OS) que o doc não pede agora. Registrado como limite consciente; há
  teste de que repor a peça faz o sinal sumir.
- **Sem tabela nem coluna nova** — deriva de `servico_pecas` + `pecas`.
- **Evidência**: 6 testes de integração → **146/146**; e2e **7/7** (sinaliza
  falta, não sinaliza abastecido, peça inativa ignorada, cálculo em lote, selo
  no card certo, sinal some ao repor).

### Não comparecimento e histórico de comparecimento — concluída em 2026-07-20

6º item web da Fase 2 (primeira parte do bloco agendamento). Plano em
`docs/superpowers/plans/2026-07-20-agenda-comparecimento.md`.

**Motivação — lacuna de regra de negócio.** O agendamento só tinha
`Agendado / CheckInRealizado / Cancelado`. Um cliente que furou o horário ou
ficava preso em `Agendado` para sempre, ou era cancelado como se tivesse
avisado — as duas coisas mentem sobre o que aconteceu, e a loja não tinha como
saber quem falta.

- **Novo estado terminal `NaoCompareceu`**, distinto de `Cancelado`: cancelar é
  o cliente avisando, faltar é não aparecer. `POST /api/agendamentos/{id}/nao-compareceu`
  (só de `Agendado`, como check-in e cancelamento).
- **Histórico de comparecimento é derivado**, não uma tabela nova:
  `GET /api/clientes/{id}/comparecimento` agrega compareceu/faltou/cancelou dos
  próprios agendamentos. Zero migração de dados, zero denormalização a manter.
- **Sinal de risco onde a decisão é tomada**: a listagem devolve `clienteFaltas`
  por item (uma **consulta agregada em lote**, nunca N+1) e a agenda mostra
  "⚠ já faltou N×" no card em aberto; o detalhe do cliente traz o resumo com
  selo de risco.
- **Sem mudança de schema**: `Status` é `varchar` sem CHECK, então o novo valor
  não altera nada — confirmado gerando uma migração de prova com `Up()` **vazio**
  (descartada, não versionada).
- **Sem regressão nos outros módulos**: os usos de `StatusAgendamento` em
  Dashboard, Comunicação, Financeiro e Disponibilidade filtram por `Agendado`
  ou `Cancelado` específicos — o novo valor não cai em bucket errado. Um no-show
  continua ocupando o slot (evento passado, não se reagenda), o que é correto.
- **Evidência**: 5 testes de integração → **140/140**; e2e **7/7** (marca falta
  só de agendado, histórico agrega cada estado, selo de risco na agenda e no
  cliente, marca falta pela UI, isolamento entre empresas).
- **Registrado como fora desta etapa**: fila de espera (superfície de UX
  própria) e sinalização por indisponibilidade de peça (pertence ao fluxo de
  check-in/OS, liga com o estoque recém-feito).

### Estoque com movimentação rastreável — concluída em 2026-07-20

5º item web da Fase 2 (primeira parte do bloco catálogo/estoque). Plano em
`docs/superpowers/plans/2026-07-18-estoque-movimentacao.md`.

**Motivação — era um débito real, não um recurso novo.** O saldo era alterado
em três pontos sem registro nenhum (consumo na OS, estorno e edição do
catálogo), então não havia como responder "por que temos 3 telas se compramos
10?". Pior: **não existia entrada de estoque** — receber mercadoria era editar
o número na mão, o que também **apagava em silêncio** qualquer baixa que outro
usuário tivesse feito no intervalo.

- **`movimentacoes_estoque` append-only** com `Quantidade` **assinada** e
  `SaldoApos` gravado. `SUM(quantidade)` reconcilia com o saldo, então auditar
  virou consulta em vez de algoritmo — e é exatamente o que os testes asseguram
  depois de cada caminho (`AssertReconcilia`).
- **Funil único** `EstoqueService.Registrar`: as três mutações passam por ele.
  Um caminho novo que esqueça o razão vira erro de compilação, não bug mudo —
  mesmo raciocínio do `RegistrarHistoricoEtapa` no SLA.
- **A edição do catálogo continua aceitando o saldo**, mas grava um `Ajuste`
  com o delta. Zero regressão de UX e o buraco fechado.
- **Ajuste manual pede o saldo contado**, não o delta (é como a loja conta
  prateleira) e **exige motivo**.
- **Entrada com custo atualiza o custo da peça** — primeiro tijolo do histórico
  de preço por fornecedor, sem antecipar a tela dele.
- **`GET /api/estoque/lista-compra`**: peças no/abaixo do mínimo agrupadas por
  fornecedor (a loja compra por fornecedor), com sugestão e custo estimado.
  Peça sem fornecedor cai num grupo próprio em vez de sumir da lista.
- **Estoque negativo continua permitido** (decisão de 2026-07-15) — o razão só
  passa a registrar como se chegou lá.
- **Fora do escopo offline** de propósito: o razão é administrativo e derivado;
  o que o técnico gera em campo é a peça usada na OS, que já sincroniza.
- **Migração com saldo de abertura**: sem ele o razão nasceria devendo todo o
  estoque existente e a reconciliação falharia para dados anteriores. O
  `SELECT` em `pecas` (FORCE RLS, migração sem tenant na sessão) devolveria
  **zero linhas em silêncio** — o mesmo erro do backfill do slug —, então o RLS
  é desligado só durante o backfill e restaurado logo em seguida. Conferido no
  banco: **10 aberturas para 10 peças com saldo e 0 divergências** entre razão
  e saldo em toda a base.
- **RLS `ENABLE`+`FORCE`** na tabela nova → **21/21 tabelas de tenant**.
- **Evidência**: 11 testes de integração → **135/135**; e2e **14/14**
  (reconciliação, consumo amarrado à OS, saldo de abertura no extrato, entrada
  atualizando custo, ajuste bloqueado sem motivo, ajuste levando ao saldo
  contado, agrupamento por fornecedor, peça confortável fora da lista).
- **Registrado como fora desta etapa**: "kits de serviço" já existe de fato
  como peças padrão do serviço (`servico_pecas` + `aplicar-padrao`) — renomear
  sem demanda seria refatoração sem benefício. "Peça equivalente" e "previsão
  de reposição" dependem de histórico acumulado, que agora passa a existir.

### OS/Kanban em profundidade — concluída em 2026-07-18

4º item web da Fase 2 (módulo 3 avançado). Transforma o Kanban de quadro de
status em ferramenta de gestão diária. Plano em
`docs/superpowers/plans/2026-07-18-os-kanban-profundidade.md`.

- **SLA visual por etapa**: `Servico.SlaHoras` (nulo = sem SLA) + a nova
  `OrdemServico.EtapaDesde`. O servidor devolve `horasNaEtapa` e `slaHoras` já
  calculados; o card ganha faixa lateral verde/âmbar/vermelha (faixas fixas:
  atenção em 70% do limite) e só exibe tag quando há algo a avisar.
- **O relógio reinicia em toda transição** porque o carimbo mora no
  `RegistrarHistoricoEtapa`, ponto único por onde passam criação, mudança de
  etapa e o envio de orçamento (que vem do módulo Financeiro).
- **Etapa final não tem SLA**: OS `Entregue`/`Cancelado` volta o `slaHoras`
  nulo — parar ali é o fim esperado do fluxo, não atraso. Sem isso o quadro
  ficaria vermelho de OS concluídas.
- **`EtapaDesde` é anulável de propósito**: OS anteriores caem para `CriadoEm`,
  o que dispensa backfill — e backfill em tabela com FORCE RLS é justamente a
  armadilha que já nos custou uma migração (ver "Desvios e notas conscientes").
- **Comentários internos** (`ordem_servico_comentarios`) entram no **escopo
  offline** (UUID + `updated_at`/`deleted_at` + `/sync`), como o doc de stack
  manda para entidades do fluxo de campo — desde o primeiro migration, não como
  retrofit. Remoção é soft-delete: a lápide chega ao app do técnico.
- **São internos de verdade**: há teste de backend e asserção de e2e provando
  que não vazam no acompanhamento público.
- **Reatribuição de técnico** (`ordem_servico_reatribuicoes`, append-only) com
  **motivo obrigatório** — é o registro que responde "quem mexeu no aparelho".
  Valida o técnico contra o tenant à mão (`usuarios` não tem GQF): há teste
  anti-IDOR usando o dono de outra empresa.
- **RLS `ENABLE`+`FORCE` conferido no Postgres** nas duas tabelas novas →
  **20/20 tabelas de tenant**.
- **Evidência**: 10 testes de integração → **124/124**; e2e **16/16** (SLA
  verde/vermelho no card com OS envelhecida no banco, tooltip com o limite,
  comentário criado/removido, lápide no `/sync`, trilha de reatribuição, troca
  bloqueada sem motivo, comentário não vaza no portal).

### Financeiro Fase 2 — margem e rentabilidade — concluída em 2026-07-18

3º item web da Fase 2 (módulo 8 avançado). Entrega o **"quanto sobrou"** que o
branding define como diferencial central. Plano em
`docs/superpowers/plans/2026-07-18-financeiro-margem.md`.

- **`GET /api/financeiro/rentabilidade?de&ate`**: lucro bruto, custo de peças,
  receita, margem % e a quebra **por serviço** (mais lucrativos primeiro).
- **Base = OS entregues no período** (decisão do usuário): 1ª transição para
  `Entregue` dentro do período **e** a OS ainda está `Entregue` (se voltou
  atrás, sai da margem). É a margem *realizada*, sem ruído de OS em andamento.
- **Custo congelado**: usa `ordem_servico_pecas.CustoUnitarioNoUso`, então a
  margem é histórica — encarecer a peça no catálogo depois **não** altera o
  resultado passado. Há teste específico para isso.
- **Duas visões coexistem de propósito**: faturamento da Fase 1 é **caixa**
  (pagamentos recebidos); rentabilidade é **competência na entrega**. A UI diz
  isso explicitamente para não confundir os números.
- **OS entregue sem orçamento** entra com receita 0 (o custo de peça é real) e
  é sinalizada em `osSemOrcamento` — o número fica explicável e sugere à loja
  registrar o orçamento.
- **Deferidos com justificativa**: comissão automática por técnico (exige config
  de % por técnico — sub-projeto que mistura rentabilidade com folha);
  relatórios exportáveis (item próprio); split de pagamento (o doc põe na
  Fase 3, com cautela explícita).
- **Evidência**: 5 testes de integração → **114/114**; e2e 5/5 (KPIs, só
  entregues contam, ordenação por lucro, conferência via API).
- Sem entidade nova, sem migração — o dado já existia.

### LGPD visível (exportação + anonimização) — concluída em 2026-07-18

2º item web da Fase 2 (módulo 14). Operacionaliza os dois direitos prometidos
no branding. Plano em `docs/superpowers/plans/2026-07-17-lgpd-visivel.md`.

- **Exportação** (`GET /api/clientes/{id}/dados-pessoais`): JSON com cadastro,
  aparelhos, agendamentos, OS (resumo estrutural) e mensagens (auditoria). A UI
  baixa o arquivo.
- **Anonimização** (`POST /api/clientes/{id}/anonimizar`, `LgpdService`):
  substitui PII por marcadores no **cliente, aparelhos (IMEI/senha),
  agendamentos (snapshots de contato) e mensagens (destino/corpo)**; **preserva**
  OS/pagamentos/orçamentos (integridade referencial e financeira, seção 16 do
  doc de stack). Marca `Cliente.AnonimizadoEm`, desativa e zera consentimento.
  **Irreversível e idempotente**.
- **Deferido com justificativa**: consentimento separado de marketing/reativação
  — não há feature de reativação; seria campo sem consumidor (YAGNI).
- **Evidência**: 4 testes de integração (export com dados ligados, anonimização
  varrendo tudo e preservando a OS, idempotência, isolamento) → **109/109**;
  e2e 6/6 (exporta baixa JSON, anonimiza varre PII e marca o selo).
- Migração só adiciona `clientes.anonimizado_em` (RLS já existe na tabela).

### Linha do tempo visual da OS — concluída em 2026-07-17

1º item web da Fase 2 (portal do cliente). O acompanhamento público
(`/acompanhar/{slug}/{codigo}`) passou a mostrar a **jornada real** do aparelho:
cada etapa percorrida com data/hora, a atual em destaque, as futuras esmaecidas.
Reforça o diferencial de transparência com o dado que já existia
(`ordem_servico_historico_etapas`) — sem entidade nova, sem migração.

- **Backend**: `AcompanhamentoResponse.LinhaDoTempo` — projeção **client-safe**
  (`{ etapa, alcancadaEm }`), 1ª vez que cada etapa foi alcançada, **sem**
  usuário nem motivo internos. A loja continua vendo a trilha completa (com
  usuário/motivo) no detalhe da OS.
- **Evidência e2e** (Playwright + Edge, 2026-07-17): carimbo de hora presente
  nas etapas alcançadas, ausente nas futuras; API com 4 etapas e sem dado
  interno. **105/105 testes de back-end.**
- Sem RLS novo (só leitura do histórico já isolado por tenant).

## Auditoria de vulnerabilidades web (OWASP básico, 2026-07-17)

Varredura das 5 classes clássicas. **4 já estavam seguras; 1 lacuna corrigida.**

| # | Classe | Resultado |
|---|---|---|
| 1 | **SQL Injection** | ✅ Seguro. Zero SQL raw/interpolado no código de produção — tudo via EF Core (LINQ parametrizado). Migrations usam SQL literal fixo (RLS), sem input do usuário. |
| 2 | **XSS** | ✅ Seguro. Zero `dangerouslySetInnerHTML` no front; React escapa por padrão. O único HTML montado é no adaptador Resend (e-mail server-side, não é página web). |
| 3 | **CSRF** | ✅ Mitigado por design. Access token viaja em header `Authorization: Bearer` (o browser não o envia sozinho → imune a CSRF). Mutações não dependem de cookie. O cookie de refresh é `HttpOnly`+`Secure`+`SameSite=Lax`, `Path=/api/auth`. |
| 4 | **Validação de input** | ✅ Dupla camada. 7 validators FluentValidation (backend, fonte de verdade) + 10 telas com zodResolver (front). O backend valida sempre, independente do front. |
| 5 | **Headers de segurança** | ❌→✅ **Estavam ausentes; adicionados nesta etapa.** |

### Correção: headers de segurança

- **Front (Next `next.config.ts` → `headers()`):** `Content-Security-Policy`
  (default-src 'self'; connect-src libera a API; frame-ancestors 'none';
  base-uri/form-action 'self'; `unsafe-eval` só fora de produção por causa do
  Turbopack dev), `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`,
  `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy`
  (camera/mic/geo desligados). **Conferido ao vivo** na resposta de `/login`.
- **API (middleware no `Program.cs`):** mesmos headers, CSP enxuto
  (`default-src 'none'; frame-ancestors 'none'` em produção; afrouxado em
  Development só para o Swagger UI). A API só serve JSON.
- Sem regressão: front build de produção OK e backend 104/104 verdes.

## Auditoria pré-produção (2026-07-17)

Feita ao fechar a Fase 1, antes de qualquer deploy. **Nenhuma falha encontrada**
— os resultados abaixo são a verificação, não uma promessa.

### Isolamento entre empresas: 23/23 tabelas cobertas

Conferido no Postgres real (`pg_class` + `pg_policy`) contra as entidades
`ITenantEntity` do código: **todas as 23** tabelas de tenant têm
`relrowsecurity = t`, `relforcerowsecurity = t` e política ativa —
`agendamentos`, `aparelhos`, `avaliacoes`, `bloqueios_agenda`, `clientes`,
`fornecedores`,
`fila_espera`, `horarios_funcionamento`, `mensagens_enviadas`,
`movimentacoes_estoque`,
`orcamento_eventos`,
`orcamentos`, `ordem_servico_comentarios`, `ordem_servico_historico_etapas`,
`ordem_servico_pecas`, `ordem_servico_reatribuicoes`, `ordens_servico`,
`pagamentos`, `pecas`, `preferencias_notificacao`,
`servico_checklist_itens`, `servico_pecas`, `servicos`.

`usuarios` e `refresh_tokens` seguem fora por decisão documentada (plano de
controle: são consultados antes de existir tenant). **Como não têm rede de
proteção (nem GQF, nem RLS), auditei os 5 acessos a `db.Users` no código: todos
filtram por `TenantId` explicitamente** (FinanceiroService ×2,
OrdemServicoService ×2, OrdemServicoInteracaoService ×2, EstoqueService ×1,
EquipeController). Os testes cobrem a fronteira
(responsável técnico de outra empresa → 400; equipe isolada por tenant).
**Invariante frágil e consciente**: qualquer query nova em `usuarios` precisa
do filtro manual — não há compilador nem banco para avisar.

### Endurecimento para produção: já estava de pé (verificado)

Os guards de produção foram construídos desde a fundação e foram conferidos um
a um nesta auditoria — **nenhum ajuste crítico foi necessário**:

| Guard | Estado |
|---|---|
| Cookie de refresh | `HttpOnly` + `Secure` + `SameSite=Lax` (Secure fixo, não depende do ambiente) |
| Swagger | só em Development |
| `Database.Migrate()` no startup | só em Development (produção: passo de deploy) |
| Dashboard do Hangfire | só em Development |
| `Jwt:Key` ausente | a API **falha ao subir** (fail-closed), sem default silencioso |
| CORS | restrito à origem configurada |
| Rate limiting | políticas `auth` (10/min/IP) e `publico` (30/min/IP) |
| Segredos | só no `.env` (gitignored); nada versionado |

**Único ajuste feito (defesa em profundidade):** o filtro do dashboard do
Hangfire retornava `true` incondicionalmente. Era inofensivo hoje (o mount já é
condicional a Development), mas se alguém removesse essa condição o dashboard
ficaria aberto em produção. Agora o filtro é `DashboardSomenteEmDesenvolvimento`
e **nega fora de Development por conta própria** — duas guardas independentes.
Expor o dashboard de verdade continua exigindo um filtro que valide
autenticação + papel gestor (anotado como pendência).

### CI verificado localmente (passaria verde)

Os dois caminhos que o `.github/workflows/ci.yml` executa e que o dev server
não cobre foram rodados de verdade:

- **Front-end**: `npm run lint` (0 erros), `npx tsc --noEmit` (limpo) e
  **`npm run build` de produção** — 17 rotas geradas.
- **Back-end**: `dotnet build --configuration Release` (0 erros/warnings) e
  **`dotnet test --configuration Release` → 104/104**.

### Correção de documentação

O `progresso.md` listava "publicar o repositório no GitHub" como pendente. O
repositório **já existe** em `origin` (GitHub) — o que falta é dar push nos
commits locais (o branch está à frente). Item corrigido nos próximos passos.

---

### Etapa Configurações e conta concluída em 2026-07-17 (fecha o módulo 13 e a Fase 1)

Última lacuna do escopo por módulo. Plano e decisões em
`docs/superpowers/plans/2026-07-16-configuracoes-e-conta.md`.
Evidência e2e (Playwright + Edge, 2026-07-17):

```json
{
  "dadosDaLojaSalvos": true,
  "matrizNotificacoesSalva": true,
  "nomeDaContaAtualizado": true,
  "gateDesativaSoOCanalEscolhido": true,
  "auditoriaMostraDesativada": true,
  "contatoEPoliticasNoAgendar": true,
  "contatoEPoliticasNoAcompanhar": true,
  "trocaDeSenhaFunciona": true
}
```

O `gateDesativaSoOCanalEscolhido` prova a matriz de ponta a ponta: com "OS
criada" desligado só no e-mail, o WhatsApp sai (`Simulada`) e o e-mail fica
registrado como `Desativada`. A verificação da troca de senha exigiu aguardar
a janela do rate limiter de auth (10/min/IP) — o 429 no meio do e2e era o
limite funcionando, não um bug. RLS conferido: `preferencias_notificacao` com
`relrowsecurity = t` e `relforcerowsecurity = t`; fail-closed sem tenant.

### Etapa Onboarding guiado concluída em 2026-07-16

Módulo 0/13 — item 10 da ordem recomendada. O wizard encapsula os fluxos reais
já construídos (reusa horários/serviços/peças). Plano e decisões em
`docs/superpowers/plans/2026-07-16-onboarding-guiado.md`.
Evidência e2e (Playwright + Edge, 2026-07-16):

```json
{
  "redirecionaParaWizardNoPrimeiroAcesso": true,
  "servicosCadastradosNoWizard": true,
  "finalizaEVaiParaDashboard": true,
  "cardAtivacaoEDadosExemploVisiveis": true,
  "osExemploNoKanban": true,
  "removeDadosExemplo": true,
  "onboardingConcluido": true,
  "semDadosExemploAposRemover": true,
  "servicoContaComoPasso": true
}
```

### Etapa Dashboard essencial concluída em 2026-07-16

Módulo 12 — item 9 da ordem recomendada da Fase 1. Agregação read-only dos
dados que já existem (sem entidade, sem migração). Plano e decisões em
`docs/superpowers/plans/2026-07-15-dashboard-essencial.md`.
Evidência e2e (Playwright + Edge, 2026-07-16):

```json
{
  "kpisCorretos": true,
  "radarMostraAtrasada": true,
  "faturamentoVisivel": true,
  "kpiLevaAoKanban": true,
  "apiConfere": true
}
```

### Etapa Comunicação essencial concluída em 2026-07-15

Módulo 9 — item 8 da ordem recomendada da Fase 1. Abordagem (a): provedor
abstraído com adaptador **log como padrão** (dev/teste sem envio real),
Evolution (WhatsApp) e Resend (e-mail) selecionáveis por flag — "pronto para
plugar". Plano e decisões em
`docs/superpowers/plans/2026-07-15-comunicacao-essencial.md`.
Evidência e2e (Playwright + Edge, 2026-07-15, modo simulado):

```json
{
  "notificacoesRegistradasNaOs": true,
  "modoSimulacaoVisivel": true,
  "supressaoPorConsentimentoVisivel": true,
  "osComConsentimentoTemQuatroMensagens": true,
  "todasSimuladas": true,
  "semConsentimentoSuprimida": true
}
```

Verificado também contra o Postgres real: RLS ENABLE+FORCE em
`mensagens_enviadas` (fail-closed sem tenant), Hangfire criou suas 12 tabelas
no schema próprio, e o smoke test confirmou o registro por canal (WhatsApp +
e-mail) ao criar uma OS.

### Etapa Orçamento e pagamento básico concluída em 2026-07-15

Módulos 8/11 básico — item 7 da ordem recomendada da Fase 1 ("orçamento,
aprovação simples e pagamento básico"). Inclui a **trilha de auditoria
append-only de aprovação** (seção 16 do doc de stack, diferencial do
branding). Plano e decisões em
`docs/superpowers/plans/2026-07-15-orcamento-e-pagamento.md`.
Evidência e2e (Playwright + Edge, 2026-07-15):

```json
{
  "rascunhoSalvoComTotal": true,
  "orcamentoEnviado": true,
  "etapaMudouParaAguardando": true,
  "pagamentoParcialRegistrado": true,
  "aprovadoNoPortal": true,
  "trilhaComEnvioEAprovacaoPortal": true,
  "statusAprovacaoDerivado": true,
  "quitadoStatusPago": true
}
```

O `trilhaComEnvioEAprovacaoPortal` prova a trilha de ponta a ponta: envio pela
loja + aprovação pelo cliente no portal, cada evento com seu canal. RLS
conferido: `orcamentos`, `orcamento_eventos` e `pagamentos` com
`relrowsecurity = t` e `relforcerowsecurity = t`; fail-closed sem tenant.

### Etapa Estoque com baixa automática concluída em 2026-07-15

Módulo 7 básico — item 6 da ordem recomendada. Cadastro/quantidade/custo/
mínimo/alerta já existiam do catálogo; a etapa entregou a **baixa automática
ao usar peça em OS**. Plano e decisões em
`docs/superpowers/plans/2026-07-15-estoque-baixa-automatica.md`.
Evidência e2e (Playwright + Edge, 2026-07-15):

```json
{
  "pecasPadraoAplicadas": true,
  "baixaComAvisoDeMinimo": true,
  "estoqueTelaBaixou": true,
  "estoqueColaBaixou": true,
  "negativoPermitidoComAviso": true,
  "totalDePecasCorreto": true,
  "devolucaoAoRemover": true
}
```

RLS conferido: `ordem_servico_pecas` com `relrowsecurity = t` e
`relforcerowsecurity = t`; fail-closed confirmado sem `app.tenant_id`.

### Etapa OS e Kanban concluída em 2026-07-15

Módulo 3 (ordem de serviço e Kanban) + acompanhamento público de status do
módulo 1 — item 5 da ordem recomendada da Fase 1. Primeira etapa do **escopo
offline do técnico** (UUID, `updated_at`/`deleted_at`, sync por delta e
Idempotency-Key). Plano e decisões aprovadas em
`docs/superpowers/plans/2026-07-14-os-e-kanban.md`.
Evidência e2e (Playwright + Edge, 2026-07-15):

```json
{
  "osManualCriada": true,
  "detalheComLinkETrilha": true,
  "checkinNoKanbanCriaOs": true,
  "moverPorSelectFunciona": true,
  "dragAndDropFunciona": true,
  "cancelamentoComMotivo": true,
  "acompanhamentoPublicoFunciona": true,
  "codigoInvalido404": true
}
```

O `checkinNoKanbanCriaOs` prova a conversão automática de ponta a ponta: o
card do agendamento arrastado/clicado em check-in vira a OS #2 na coluna
seguinte. RLS conferido no banco: `ordens_servico` e
`ordem_servico_historico_etapas` com `relrowsecurity = t` e
`relforcerowsecurity = t`; fail-closed confirmado sem `app.tenant_id`.

### Etapa Agenda e portal de agendamento concluída em 2026-07-14

Módulo 2 (agendamento e calendário) + o fluxo de agendamento do módulo 1
(portal do cliente) — item 4 da ordem recomendada da Fase 1 — de ponta a
ponta: API + RLS verificado no Postgres + cliente orval + telas + portal
público sem login. Plano e decisões aprovadas em
`docs/superpowers/plans/2026-07-13-agenda-e-portal-agendamento.md`.
Evidência e2e (Playwright + Edge, 2026-07-14):

```json
{
  "navAgendaFunciona": true,
  "agendamentoManualCriado": true,
  "checkinFunciona": true,
  "configuracoesCarregam": true,
  "slotOcupadoSomeDoPortal": true,
  "portalAgendamentoConcluido": true,
  "portalApareceNaAgendaDaLoja": true,
  "clienteCriadoPeloPortal": true
}
```

O `slotOcupadoSomeDoPortal` prova a capacidade simultânea de ponta a ponta:
um agendamento manual às 14:00 fez o horário sumir da grade pública.
RLS conferido no banco: `agendamentos`, `bloqueios_agenda` e
`horarios_funcionamento` com `relrowsecurity = t` e `relforcerowsecurity = t`;
fail-closed confirmado (`count(*) = 0` sem `app.tenant_id` na sessão).

### Etapa Clientes e aparelhos concluída em 2026-07-12

Módulo 5 (CRM básico) — item 3 da ordem recomendada da Fase 1 — de ponta a
ponta: API + RLS verificado no Postgres + cliente orval + tela. Evidência
e2e (Playwright + Edge, 2026-07-12):

```json
{
  "navClientesFunciona": true,
  "clienteCriado": true,
  "aparelhoAdicionado": true,
  "badgeVipNaTabela": true,
  "contagemAparelhos": true,
  "vinculoCriadoComBadge": true,
  "filtroVipEsconde": true,
  "buscaPorTelefone": true,
  "desativarEsconde": true,
  "inativoApareceComFiltro": true
}
```

RLS conferido no banco: `clientes` e `aparelhos` com `relrowsecurity = t`
e `relforcerowsecurity = t`.

### Etapa Catálogo concluída em 2026-07-08

Módulo 6 (serviços e peças) — item 2 da ordem recomendada da Fase 1 — de
ponta a ponta: API + RLS verificado no Postgres + cliente orval + telas.
Evidência e2e (Playwright + Edge, 2026-07-08):

```json
{
  "cadastroLevaAoDashboard": true,
  "navPecasFunciona": true,
  "pecaCriadaAparaceNaTabela": true,
  "fornecedorVinculado": true,
  "semAlertaEstoqueBaixo": true,
  "servicoCriadoComChecklistEPeca": true,
  "edicaoRecarregaChecklist": true,
  "edicaoRecarregaPeca": true,
  "desativarRemoveDaListagemPadrao": true,
  "inativoApareceComFiltro": true
}
```

RLS conferido direto no banco: as 5 tabelas novas (`fornecedores`, `pecas`,
`servicos`, `servico_pecas`, `servico_checklist_itens`) com
`relrowsecurity = t` e `relforcerowsecurity = t`.

---

## Como rodar

```bash
docker compose up -d --build
```

| Serviço  | URL                                      |
|----------|------------------------------------------|
| Front-end | http://localhost:3000                    |
| API       | http://localhost:5080 (health: `/health`)|
| Swagger   | http://localhost:5080/swagger            |
| Postgres  | localhost:5432 (dev)                     |

- Variáveis locais opcionais: copiar `.env.example` para `.env` (nunca commitar `.env`).
- Migrations rodam automaticamente no startup **somente em Development**;
  em produção serão passo de deploy.
- Testes do back-end: `dotnet test backend/TechPro.slnx`.
  - ⚠ Se o Windows bloquear a DLL de teste (ver "Notas de ambiente"), rodar
    dentro do container do SDK:
    `docker run --rm -v "<repo>\backend:/repo:ro" -v techpro-nuget:/root/.nuget mcr.microsoft.com/dotnet/sdk:10.0 bash -c "cp -r /repo /work && cd /work && dotnet test TechPro.slnx"`
- Regenerar o cliente tipado do front após mudar contratos da API:
  `curl http://localhost:5080/swagger/v1/swagger.json -o frontend/openapi/swagger.json && cd frontend && npm run gerar-api`.

---

## Decisões aprovadas (2026-07-05)

1. **Front-end dentro do docker-compose** (node:22-alpine + volume), além de Postgres e API.
2. **UUID como PK** de `empresas` (o `tenant_id`) — não enumerável; viaja em JWT, URLs e RLS.
3. **Primeiro usuário cadastrado = papel `gestor`** da empresa recém-criada.
4. **Refresh token em cookie httpOnly/Secure** (`techpro_refresh`, SameSite=Lax, Path=/api/auth); **access token somente em memória** no front — nunca em localStorage.
5. **EFCore.NamingConventions** para snake_case no schema (tabelas do Identity renomeadas manualmente: `usuarios`, `papeis`, `usuario_papeis`, ...).
6. **Hangfire, Zustand e Recharts adiados** até o primeiro uso real (continuam decididos no doc de stack).
7. **orval** gera o cliente TypeScript tipado + hooks TanStack Query a partir do swagger.
8. Testes do back-end em `backend/tests/TechPro.Api.Tests` (xUnit).
9. `progesso.md` renomeado para `progresso.md`.
10. **E-mail globalmente único no MVP** (um e-mail = uma empresa); multi-vínculo fica para depois se houver demanda.

---

## O que está construído

### Multi-tenancy (defesa em profundidade)

- `tenant_id` em toda tabela relevante + **Global Query Filter** automático por
  convenção: toda entidade `ITenantEntity` é filtrada sem `.Where()` manual —
  fail-closed (sem tenant no contexto, nenhuma linha aparece).
- **RLS no Postgres** como segunda camada: `app.tenant_id` é setado por
  interceptor em toda conexão (`set_config`); app conecta como `techpro_app`
  (NOSUPERUSER, **NOBYPASSRLS**); `empresas` com ENABLE+FORCE RLS.
- **Policies de `empresas` (revisadas em 2026-07-13)**: leitura **pública**
  (a rota de agendamento resolve slug → empresa antes de existir tenant e a
  tabela é só diretório — id, nome, slug, criado_em), INSERT público para o
  cadastro e UPDATE restrito à própria empresa (edição de slug). A policy de
  UPDATE **não existia** até então — o FORCE RLS negaria o UPDATE em silêncio;
  descoberta registrada em "Desvios e notas".
- **Tenant fixado fora do JWT** (`TenantAmbiente`): a rota pública resolve o
  slug e fixa o tenant da requisição; `HttpTenantProvider` o consulta antes da
  claim — GQF e RLS passam a valer normalmente, sem isolamento reimplementado
  à mão no fluxo público.
- `RlsHelper.AplicarIsolamentoTenant()` pronto para aplicar a política padrão
  em toda tabela de produto futura (OS, estoque, financeiro...).
- **Plano de controle deliberadamente fora do GQF/RLS**: `usuarios` e
  `refresh_tokens` são consultados por chave única *antes* de existir contexto
  de tenant (login por e-mail, refresh por token); o `AuthService` valida o
  vínculo explicitamente.

### Autenticação (primeira fatia vertical completa)

- ASP.NET Core Identity + JWT HS256 de 15 min com claims `sub`, `email`,
  `nome`, `tenant_id` e `role` (gestor/tecnico/atendente seedados).
- `POST /api/auth/registrar` — cria Empresa + gestor numa transação única (201).
- `POST /api/auth/login` — lockout após 5 falhas (5 min); resposta única para
  qualquer falha (sem oráculo de e-mails cadastrados).
- `POST /api/auth/refresh` — **rotação**: cada uso revoga o token e emite
  sucessor; reapresentar token já rotacionado revoga a família inteira
  (detecção de roubo). Só o hash SHA-256 vai ao banco.
- `POST /api/auth/logout`, `GET /api/auth/me` (empresa lida através do GQF+RLS).
- `RefreshToken.TipoCliente` (Web 7d / Mobile 90d) já modelado — seção 8 do doc
  de stack — para o app do técnico da Fase 2 não exigir migração.
- Rate limiting 10/min/IP nos endpoints de auth; CORS restrito ao front;
  FluentValidation com mensagens pt-BR.

### Catálogo (módulo 6 — primeira etapa de produto)

- **Serviços** (`/api/servicos` + tela `/servicos`): preço base, categoria
  (texto livre com sugestões), duração, prazo médio, exige diagnóstico,
  agendável online, **capacidade simultânea** (consumida pela agenda depois),
  **checklist padrão ordenado** (tabela própria — a Fase 2 marca item a item
  na OS) e **peças normalmente utilizadas** com quantidade padrão.
- **Peças** (`/api/pecas` + tela `/pecas`): custo, preço de venda, quantidade,
  estoque mínimo com **alerta de estoque baixo** e fornecedor.
- **Fornecedores** (`/api/fornecedores`, entidade mínima): a Fase 2 precisa de
  histórico de preço por fornecedor — campo texto viraria migração de dados
  reais. Fornecedor com peça vinculada não pode ser removido (409).
- **Exclusão = desativação** (`ativo=false`): serviço/peça podem estar
  referenciados por OS futuras; listagem padrão esconde inativos
  (`incluirInativos=true` os revela).
- **Isolamento testado na API**: empresa B não lista, não lê, não altera nem
  desativa itens de A (404 via GQF), e **não consegue vincular peça de A a um
  serviço seu** (400 — o teste anti-IDOR `ServicoNaoAceitaPecaDeOutraEmpresa`).
- Primeiro uso real do `RlsHelper` em tabelas de produto; PK `int` identity
  (UUID + `updated_at`/`deleted_at` seguem exclusivos do escopo offline do
  técnico — seção 5 do doc de stack).

### Clientes e aparelhos (módulo 5 — CRM básico)

- **Clientes** (`/api/clientes` + tela `/clientes`): cadastro completo por
  decisão aprovada em 2026-07-12 (nome e telefone obrigatórios; e-mail, CPF,
  endereço e observações opcionais), flag **VIP manual** ("recorrente" será
  derivado quando OS existir), busca por nome/telefone/CPF e filtros
  ativos/inativos/VIP.
- **Consentimento LGPD** (módulo 14, Fase 1): checkbox de comunicação
  operacional com carimbo de data na concessão; revogar limpa o carimbo.
- **Conta vinculada família/empresa**: FK `cliente_principal_id` com regra de
  **1 nível** (sem auto-vínculo, sem cadeia, principal com dependentes não
  vira vinculado) + UI mínima (select "Vinculado a" e badge na listagem).
- **Aparelhos** como sub-recurso (`/api/clientes/{id}/aparelhos`): marca,
  modelo, IMEI/nº de série, senha de desbloqueio e observações, gerenciados
  na própria tela do cliente.
- **Isolamento testado**: empresa B não lista/lê/altera clientes de A (404),
  não vincula cliente de A como principal (400 anti-IDOR) e não adiciona
  aparelho em cliente de A (404).
- Exclusão = desativação; a anonimização LGPD (seção 16) entra na Fase 2 sem
  migração destrutiva.

### Agenda e portal de agendamento (módulo 2 + fluxo do módulo 1)

- **Horários de funcionamento** (`/api/agenda/horarios` + tela
  `/agenda/configuracoes`): um registro por dia da semana (abertura,
  fechamento, intervalo opcional); dia sem registro ou inativo = fechado
  (fail-closed). Salvo em lote (os 7 dias num PUT).
- **Bloqueios pontuais** (`/api/agenda/bloqueios`): data + faixa de horário +
  motivo; somem da grade de disponibilidade. Exclusão física permitida —
  bloqueio é configuração operacional, não registro de negócio.
- **Disponibilidade em slots de 30 min** (`/api/agenda/disponibilidade`):
  grade dentro do horário do dia, menos intervalo e bloqueios; um serviço
  ocupa `ceil(duração/30)` slots e a **capacidade simultânea** do serviço
  (campo criado no catálogo) limita sobreposições por sub-slot. Aritmética em
  minutos inteiros — `TimeOnly.AddMinutes` dá a volta na meia-noite.
- **Agendamentos** (`/api/agendamentos` + tela `/agenda`): criação manual
  (cliente vinculado ou contato avulso — snapshot de nome/telefone),
  reagendamento (marca `reagendado_em`; só no status Agendado), **check-in**
  (gancho da conversão em OS na etapa 5) e cancelamento com motivo.
  Calendário próprio em Tailwind com visões dia/semana/mês (sem lib nova,
  conforme o doc de stack).
- **Slug público por empresa** (`empresas.slug`, único global): gerado do nome
  no cadastro (`GeradorDeSlug` — minúsculas, sem acentos, hífens), editável em
  configurações com URL copiável; conflito responde 409.
- **Rota pública `/api/publico/{slug}`** (sem login, rate limit próprio
  30/min/IP): `info` (nome + serviços com `agendavel_online=true`),
  `disponibilidade` e criação de agendamento. **Vínculo silencioso por
  telefone** (decisão aprovada 2026-07-13): telefone que bate com cliente
  ativo (comparação só por dígitos) vincula sem expor nada do cadastro;
  telefone inédito cria cliente novo no CRM.
- **Portal `/agendar/{slug}`** (grupo `(portal-cliente)`, sem guard): wizard
  progressivo — identificação → aparelho/problema → serviço → data/horário →
  confirmação — no visual do guia. Anexos entram quando o R2 existir.
- **Isolamento testado também no fluxo público**: slug da loja B + serviço da
  loja A → 400 em disponibilidade e criação (GQF com tenant fixado); B não
  lista nem faz check-in em agendamento de A (404); enums viajam como string
  no JSON (`JsonStringEnumConverter`).

### OS e Kanban (módulo 3 — primeira etapa do escopo offline)

- **OrdemServico** (`/api/ordens-servico` + telas `/ordens-servico` e
  `/kanban`): 10 etapas (a coluna "Agendado" do Kanban mostra agendamentos que
  ainda não viraram OS), responsável técnico, prioridade, prazo estimado,
  status de pagamento e de aprovação (campos manuais até as etapas 6–7),
  problema, observações e snapshot de aparelho (FK opcional para o CRM).
- **Escopo offline estreou** (seções 4 e 5 do doc de stack):
  `IEntidadeSincronizavel` (UUID + `updated_at` carimbado automaticamente
  pelo DbContext no SaveChanges + `deleted_at` como lápide),
  `GET /api/ordens-servico/sync?since=` (delta com lápides e `agora` do
  servidor) e `Idempotency-Key` na criação (coluna única por tenant —
  reenvio devolve a mesma OS).
- **Número sequencial por empresa** (decisão 2026-07-14): "OS #124" único por
  tenant, sem vazar volume entre empresas; o UUID segue como chave real.
- **Trilha de etapas append-only** (`ordem_servico_historico_etapas`): toda
  mudança grava de → para, usuário e motivo — alimenta a linha do tempo e o
  SLA visual da Fase 2. Movimentação livre entre etapas (correções
  permitidas); cancelar exige motivo.
- **Conversão automática no check-in**: o check-in do agendamento cria a OS
  na mesma transação (cliente, serviço, snapshot do aparelho e problema);
  agendamento avulso usa o vínculo silencioso por telefone (helper movido
  para o `ClienteService`, compartilhado com o portal).
- **Acompanhamento público** (decisão 2026-07-14): código opaco de 16 chars
  (RNG criptográfico) por OS; rota `GET /api/publico/{slug}/acompanhar/{codigo}`
  reusa o padrão de tenant fixado por slug (sem afrouxar RLS) e expõe só
  loja, número, serviço, etapa e prazo. Página `/acompanhar/{slug}/{codigo}`
  com régua do fluxo.
- **Kanban com @dnd-kit** (dependência nova aprovada em 2026-07-14): drag
  entre colunas + select "mover" por card como fallback touch; arrastar
  agendamento para "Check-in realizado" faz o check-in; Entregue/Cancelado
  atrás do filtro "mostrar finalizadas".
- **`GET /api/equipe`**: usuários da empresa para o select de responsável
  (plano de controle sem GQF — filtro por tenant explícito + validação
  anti-IDOR ao atribuir responsável, coberta por teste).
- **Isolamento testado**: B não lista/lê/move OS de A (404), não cria OS com
  cliente de A (400); código de acompanhamento certo no slug errado → 404.

### Estoque com baixa automática (módulo 7 básico)

- **Peças utilizadas na OS** (`/api/ordens-servico/{id}/pecas` + seção no
  detalhe da OS): adicionar baixa o estoque na hora e **congela custo e preço
  de venda no momento do uso** (`ordem_servico_pecas` — a margem real do
  financeiro nasce daqui); remover devolve ao estoque via **soft-delete**
  (lápide sincronizável); total em peças exibido na OS.
- **Estoque negativo permitido com aviso** (decisão do usuário 2026-07-15,
  diferente da recomendação de bloquear): a baixa nunca é recusada; a resposta
  traz flags (restante, abaixo do mínimo, negativo) e a UI avisa por toast.
  Correção de contagem é editar a peça no catálogo.
- **Aplicar peças padrão do serviço** (idempotente): um clique registra as
  "peças normalmente utilizadas" do catálogo, pulando as já presentes.
- **OS finalizada (Entregue/Cancelado) não recebe nem devolve peças** — o
  registro histórico fica estável.
- **Sync por delta estendido**: as peças utilizadas (com lápides) entram no
  `GET /api/ordens-servico/sync` — o app do técnico da Fase 2 registra peça
  usada offline (módulo 4).
- Entradas/ajustes de estoque continuam pela edição da peça; histórico
  completo de movimentação, previsão de reposição e lista de compra são
  Fase 2 (doc de módulos).
- **Isolamento testado**: peça de A não entra em OS de B (400); OS de B "não
  existe" para A (404).

### Orçamento e pagamento (módulos 8/11 básico + portal)

- **Orçamento da OS** (`/api/ordens-servico/{id}/orcamento` + seção no detalhe):
  mão de obra editável (sugerida do preço base) + peças utilizadas (preço
  **congelado no envio** — o que o cliente vê não muda se a loja registrar
  mais peças depois) − desconto. Um orçamento por OS na Fase 1 (item a item é
  Fase 2). Editar um orçamento já respondido volta o status a Rascunho,
  preservando a trilha.
- **Trilha de auditoria append-only** (`orcamento_eventos`, seção 16 do doc de
  stack — diferencial do branding): cada envio/aprovação/recusa grava tipo,
  **canal** (Loja/Portal), usuário (quando loja), valor total e motivo, nunca
  sobrescrita.
- **Aprovação binária** pela loja (registro manual "aprovou pelo WhatsApp") e
  pelo cliente no portal `/acompanhar/{slug}/{codigo}` sem login. **Só o envio
  move etapa** (para Aguardando aprovação, com histórico); aprovar/recusar só
  atualizam o status — a loja decide o próximo passo no Kanban.
- **Pagamentos parciais** (`/api/ordens-servico/{id}/pagamentos`): vários por
  OS com forma (dinheiro/Pix/débito/crédito/outro); podem ser removidos (erro
  de digitação). **`StatusPagamento` e `StatusAprovacao` da OS agora derivados**
  dos fluxos reais — saíram do PUT manual da OS (eram campos manuais desde a
  etapa de OS). Sem orçamento, pagamento marca no máximo Parcial.
- **Acompanhamento público** passou a incluir o orçamento (só depois de
  enviado — rascunho é interno) e os endpoints de aprovar/recusar, sob o mesmo
  padrão de tenant fixado por slug + código opaco + rate limiting "publico".
- **Fora do escopo offline** (PK `int`, sem sync): aprovação exige trilha
  append-only, nunca last-write-wins (seção 4 do doc de stack) — decisão
  consciente que separa o financeiro do fluxo de campo do técnico.
- **Isolamento testado**: orçamento/pagamento de A "não existem" para B (404).

### Comunicação essencial (módulo 9 — provedor abstraído)

- **Provedor abstraído** (`ICanalNotificacao`): adaptadores `LogWhatsAppCanal`/
  `LogEmailCanal` (padrão, só registram), `EvolutionWhatsAppCanal` e
  `ResendEmailCanal` selecionados por flag `Comunicacao:{Whatsapp,Email}:Provedor`
  (`log`|`evolution`|`resend`). Default `log` mantém dev/e2e determinístico.
- **ComunicacaoService**: disparo **automático e síncrono** por evento; respeita
  o **consentimento LGPD** (cliente sem consentimento → mensagem `Suprimida`,
  registrada para auditoria); envia em **todos os canais disponíveis** (WhatsApp
  sempre; e-mail se houver); falha de provedor externo vira `Falhou` e **nunca
  derruba a ação** que disparou (`ProtegerAsync`). Um registro `MensagemEnviada`
  por canal (RLS ENABLE+FORCE) — o "registro mínimo para auditoria" da Fase 1 e
  base do inbox unificado da Fase 2.
- **Eventos**: agendamento confirmado (criar), OS criada (criar + conversão do
  check-in), orçamento disponível (enviar), orçamento aprovado/recusado
  (responder — loja e portal), pronto para retirada (mudança de etapa).
- **Hangfire** (Postgres) para o **lembrete temporizado** (~3h antes; não agenda
  se já passou). Ligado só com `Comunicacao:Hangfire:Habilitado=true` (docker);
  sem a flag, `IAgendadorDeLembretes` é no-op — testes/`dotnet run` puro não
  dependem de Postgres/Hangfire. O `LembreteJob` roda fora do HTTP e fixa o
  tenant via `TenantAmbiente` (padrão das rotas públicas); só envia se o
  agendamento ainda estiver `Agendado` (cancelado/check-in → não envia).
- **Endpoint** `GET /api/ordens-servico/{id}/mensagens` (auditoria) + seção
  "Notificações enviadas" no detalhe da OS (canal, evento, status, horário).
- **Isolamento testado**: mensagens de A não aparecem para B (GQF).

### Dashboard essencial (módulo 12 — agregação read-only)

- **`GET /api/dashboard`** (módulo `Dashboard/`, sem entidade/migração): 6 KPIs
  da Fase 1 — OS abertas (não finalizadas), agendamentos do dia, serviços em
  atraso (prazo vencido e não finalizada), **aparelhos em reparo = bancada
  inteira** (NaFila→EmTeste), prontos para retirada, e **faturamento do mês =
  pagamentos recebidos no mês** (caixa real).
- **Comparativo** faturamento mês atual vs. anterior + variação % (null quando
  o anterior é zero).
- **"Radar do dia"**: OS atrasadas e orçamentos pendentes há mais de 2 dias
  (com link para a OS), listas limitadas a 10 com o total sinalizado. O
  terceiro item do doc ("peça que chegou libera reparo parado") ficou de fora —
  depende de rastreio de chegada de peça, que só existe com entradas de estoque
  (Fase 2). Anotado.
- Leitura pura sob GQF; somas de decimal em memória (Sqlite dos testes);
  "hoje"/"mês" são a data UTC do servidor (hora de parede da loja).
- **Front `/dashboard`** deixou de exibir empresa/papel/tenant e virou o painel:
  radar no topo, KPIs clicáveis (levam a Kanban/Agenda/OS) e faturamento com
  tendência. Isolamento testado (dashboard de A zerado para B).

### Configurações e conta (módulo 13)

- **Dados da loja** (`GET|PUT /api/configuracoes/loja` + tela `/configuracoes`):
  nome editável, telefone, e-mail, endereço e políticas (texto livre). Os
  **contatos e políticas aparecem para o cliente final** nas páginas públicas
  `/agendar/{slug}` e `/acompanhar/{slug}/{codigo}` (decisão 2026-07-16 — dado
  vivo, não campo morto).
- **Preferências de notificação: matriz evento × canal** (decisão do usuário
  2026-07-16, *diferente da recomendação de toggles só por evento*; o doc pede
  "preferências básicas", a matriz vai além mas dá controle fino, ex.: lembrete
  por WhatsApp sim, por e-mail não). Tabela `preferencias_notificacao`
  (RLS ENABLE+FORCE) com **ausência de linha = ativo** — tenant novo já nasce
  notificando tudo, sem seed.
- **Gate de preferência no despacho** (`ComunicacaoService`), na ordem:
  1) consentimento LGPD → `Suprimida`; 2) preferência da loja → **novo status
  `Desativada`**; 3) envio pelo adaptador. O registro fica na auditoria da OS
  ("desativada nas configurações") — responde "por que meu cliente não recebeu?".
- **Conta do usuário**: editar o próprio nome (`PUT /api/conta`) e trocar a
  senha (`POST /api/conta/senha`, via `UserManager.ChangePasswordAsync`, que já
  exige a senha atual). **Troca de e-mail ficou de fora**: é o login, é único
  globalmente e exige confirmação por e-mail — depende do provedor não ligado.
- **Slug e horários seguem em `/api/agenda/*`** (já existiam e funcionam); a
  tela de configurações linka para lá em vez de duplicar a regra.
- Isolamento testado (dados/preferências de A não vazam para B).

### Financeiro básico (módulo 8 — visão de caixa)

- **`GET /api/financeiro?de&ate`** + tela **`/financeiro`** (a rota prevista na
  seção 13 do doc de stack): leitura pura sob GQF, sem entidade nem migração.
- **Faturamento por período** (presets Hoje/7 dias/Este mês/Mês passado +
  intervalo livre; default = mês corrente), **transações** (data, OS, cliente,
  forma, valor), **composição por forma de pagamento** e **ticket médio =
  faturamento ÷ nº de OS distintas pagas** (decisão 2026-07-16 — coerente com o
  faturamento, que é caixa recebido).
- **A receber = OS viva com orçamento APROVADO e saldo em aberto** (decisão
  2026-07-16): orçamento só enviado é proposta, não receita vendida, e OS
  cancelada sai da conta. Ambos cobertos por teste.
- **Projeção de caixa** ("quanto está para entrar", item novo do doc): a receber
  + valor esperado dos agendamentos dos próximos 7 dias (estimado pelo preço
  base do serviço — a UI deixa explícito que o orçamento final pode diferir).
- "A receber" e a projeção são **visão atual**, não filtradas pelo período —
  coberto por teste para evitar interpretação errada.
- Margem, lucro bruto, receita por serviço e relatórios exportáveis seguem na
  Fase 2 (doc); a base para eles (custo x preço congelados na peça da OS) já
  existe desde a etapa de estoque.

### Onboarding guiado (módulo 0 — encapsula os fluxos reais)

- **Wizard `/bem-vindo`** (5 passos): dados da loja (nome + slug editável),
  horários (setup rápido: dias abertos + um horário), serviços com **sugestões
  pré-preenchidas editáveis** (troca de tela/bateria/conector/limpeza/película),
  peças opcionais, e dados de exemplo + conclusão. Cada passo chama os
  endpoints já existentes — o backend do onboarding é só o entorno.
- **Redirecionamento no primeiro acesso**: `Empresa.OnboardingConcluidoEm`
  (nulo) → o dashboard leva ao wizard; "pular" ou concluir marca o carimbo e
  não redireciona mais.
- **Checklist de ativação derivado dos dados** (sempre exato, sem estado novo):
  loja, horários, serviço, peça, cliente — "X de 5". Card no dashboard com os
  passos pendentes (links) até completar.
- **Dados de exemplo removíveis** (decisão 2026-07-16): coluna `Exemplo` em
  `clientes`/`servicos`/`ordens_servico`; carregar cria um cliente + serviço +
  OS fictícios (direto, sem disparar notificações), remover limpa respeitando
  as FKs. Idempotente. Não contam como passos reais do checklist.
- **Deferidos com registro**: logo da loja (depende do Cloudflare R2) e convite
  de equipe (o doc coloca como Fase 2 no módulo 13; exige fluxo de convite).

### Front-end

- Next.js 16 (App Router, TS estrito, Tailwind 4, shadcn/ui sobre Radix,
  TanStack Query, RHF+Zod, motion instalado).
- Cliente da API gerado por orval (fetch + hooks TanStack Query); mutator
  injeta Bearer da memória e `credentials: include`.
- `AuthProvider` restaura a sessão no load via cookie de refresh e renova o
  access token 1 min antes de expirar.
- Páginas `/login` e `/cadastro` (grupo `(auth)`) e `/dashboard` protegido
  (grupo `(empreendedor)`) exibindo empresa, papel e tenant_id.
- Visual seguindo o guia de referência (docs/UI_UX-referencia.md): fundo
  branco, navy `#14162B`, corpo cinza, tag de seção rosa uppercase, CTA
  pílula preta única, glow gradiente atrás do card do dashboard.

### Infra local e CI

- `docker-compose.yml`: postgres:17-alpine (init script cria `techpro_app`
  sem BYPASSRLS), API (multi-stage Dockerfile .NET 10) e front (node:22).
- GitHub Actions (`.github/workflows/ci.yml`): job front (npm ci, lint,
  tsc --noEmit, build) + job back (restore, build Release, testes).

---

## Desvios e notas conscientes

- **`TechPro.slnx` em vez de `.sln`**: o .NET 10 gera o formato novo por
  padrão; mantido (suportado por CLI e IDEs atuais). Todos os comandos usam
  `backend/TechPro.slnx`.
- **Swagger sem security definitions** por ora: a API do Microsoft.OpenApi 2.x
  mudou e o orval só precisa de paths/schemas. Revisitar se o Swagger UI
  precisar de botão "Authorize".
- **Guard de rota é só UX**: a segurança real está na API (JWT + GQF + RLS).
  Quando houver páginas server-rendered com dados, considerar `proxy.ts`
  (novo nome do middleware no Next 16).
- **Bug corrigido do init do shadcn**: ele gerou `--font-sans: var(--font-sans)`
  (auto-referência) no `globals.css`, derrubando a UI para serif.
- **Sem camada Repository** (o doc de stack a cita na estrutura de pastas):
  o DbContext + GQF já cumprem o papel; a camada extra entra apenas quando
  houver query complexa reutilizada. Padrão Controller fino + Service.
- **Kits de serviço e peça compatível/equivalente ficam para a Fase 2**
  (fases_MVP.md os lista lá, apesar de o doc de módulos citá-los no módulo 6).
- **Migrations agora também rodam no container do SDK** (Smart App Control):
  copiar o repo para `/work`, gerar lá e copiar `Migrations/*.cs` de volta —
  comando registrado no plano da etapa (docs/superpowers/plans/).
- **Senha de desbloqueio do aparelho em texto** (decisão aprovada 2026-07-12):
  a loja precisa lê-la, então hash não serve; candidata a criptografia de
  campo se surgir exigência — a criptografia em repouso do provedor cobre o
  disco. Tratar como dado sensível em qualquer exportação futura.
- **Diferimentos da etapa Agenda (aprovados 2026-07-13)**: lembretes
  automáticos → etapa 8 (Comunicação, Hangfire + WhatsApp/Resend); conversão
  automática em OS → etapa 5 (o status `CheckInRealizado` é o gancho); anexos
  no fluxo público → quando a conta Cloudflare R2 existir.
- **Datas da agenda são "hora de parede" da loja** (`DateOnly`/`TimeOnly`,
  sem timezone). O bloqueio de data passada no portal usa o dia UTC com
  tolerância de 1 dia — sem ela, uma loja UTC-3 não agendaria "hoje" à noite.
  Fuso explícito por loja só se surgir demanda real.
- **Corrida residual de vaga aceita no MVP**: a disponibilidade é revalidada
  imediatamente antes do INSERT, mas duas requisições simultâneas no mesmo
  slot podem passar (sem lock pessimista). Baixíssima probabilidade no volume
  esperado; revisitar com constraint/lock se aparecer na prática.
- **Lição de migração com RLS FORCE**: o backfill de `empresas.slug` foi
  bloqueado em silêncio pelo RLS (migração roda como `techpro_app`, sem
  policy de UPDATE e sem tenant na sessão) e o índice único falhou nos slugs
  vazios. Correção: a migração desliga/religa o RLS da tabela em volta do
  UPDATE (o dono da tabela pode). **Todo backfill futuro em tabela com FORCE
  RLS precisa disso** — o UPDATE não dá erro, só afeta zero linhas.
- **@dnd-kit/core adicionado ao front** (decisão aprovada 2026-07-14): única
  dependência fora da lista do doc de stack — drag-and-drop do Kanban com
  suporte a touch; o fallback por select em cada card cobre onde drag não
  opera.
- **Rota pública de acompanhamento leva o slug**
  (`/acompanhar/{slug}/{codigo}` em vez de só `{codigo}`, refinando o plano
  da etapa): o slug resolve o tenant pelo padrão já existente e o código é
  buscado sob GQF+RLS — sem criar exceção de RLS nova para busca global de
  código.
- **Sqlite dos testes não traduz DateTimeOffset** (comparação nem ORDER BY):
  o DbContext aplica `DateTimeOffsetToBinaryConverter` (workaround documentado
  da Microsoft) **somente quando o provider é Sqlite** — Postgres segue com
  `timestamptz` nativo; o filtro `since` do sync exigiu isso.
- **Número sequencial da OS = max+1 na transação**: corrida entre duas
  criações simultâneas no mesmo tenant faria a segunda falhar no índice único
  (erro, nunca duplicidade). Volume esperado torna isso raríssimo; retry
  automático fica anotado como melhoria se aparecer na prática.
- **Status de pagamento/aprovação da OS agora derivados** (etapa de
  orçamento): o `StatusPagamento`/`StatusAprovacao` deixaram de ser campos
  manuais no PUT da OS e passaram a ser recalculados pelo `FinanceiroService`
  (soma dos pagamentos vs. total do orçamento; status do orçamento). Os enums
  na entidade OS continuam existindo — são a projeção materializada que o
  Kanban e a listagem já consomem, agora sempre coerente com o financeiro.
- **Financeiro deliberadamente fora do escopo offline** (PK `int`, sem
  `updated_at`/`deleted_at`/sync): a seção 4 do doc de stack manda aprovação
  de orçamento usar trilha append-only em vez de last-write-wins, então
  orçamento e pagamento não sincronizam com o app do técnico — ficam só no
  portal web, como os demais módulos não-campo.
- **Somas em memória no financeiro**: o Sqlite dos testes não agrega `decimal`
  no servidor; as somas de peças/pagamentos por OS trazem poucas linhas e são
  feitas no cliente (Postgres continua eficiente com o mesmo código LINQ).
- **WhatsApp via Evolution API, não Meta Cloud API** (decisão do usuário
  2026-07-15): desvio da seção 7 do doc de stack, que agora traz a nota do
  desvio. A cautela do doc contra libs não oficiais (Baileys → risco de
  banimento do número) é reconhecida; mitigação é a abstração de provedor —
  troca-se para a Cloud API sem tocar no resto se preciso. WhatsApp segue em
  modo `log` até haver uma instância Evolution configurada.
- **Notificações imediatas são síncronas** (dentro da request; log/teste
  determinístico e resiliente por `ProtegerAsync`). Movê-las para jobs de
  background (Hangfire) é melhoria de resiliência/latência da Fase 2 — só o
  lembrete temporizado usa Hangfire hoje.
- **Segredo do Resend só no `.env`** (gitignored), dormente até
  `EMAIL_PROVEDOR=resend`. A key foi compartilhada no chat → **recomendada a
  rotação**. Sem domínio verificado no Resend, só `onboarding@resend.dev`
  envia, e apenas para o e-mail do dono da conta.
- **Dashboard do Hangfire** (`/hangfire`) só em Development, com filtro
  permissivo local — **produção exige um filtro de autorização real** (anotado
  no código).

## Notas de ambiente (máquina de dev)

- **Smart App Control (Windows) em enforcement desde 2026-07-05 à noite**:
  bloqueia DLLs não assinadas compiladas localmente — `dotnet test` local
  falha com `0x800711C7`. Contorno adotado: rodar testes no container do SDK
  (mesmo ambiente do CI). Desligar o SAC resolve, mas é decisão irreversível
  do dono da máquina (Configurações → Segurança do Windows → Controle de
  aplicativos e navegador).
- **Bind mount Windows→Linux não propaga eventos de arquivo**: edições no
  host podem não disparar hot-reload do Next dentro do container — reiniciar
  o serviço (`docker compose restart frontend`) força a recompilação.
- **Turbopack no container às vezes materializa uma pasta com o caminho
  Windows sanitizado** (ex.: `frontend/C:ProjetosPessoalTechProfrontend/`)
  cheia de chunks de dev — é artefato inofensivo, ignorado no
  `eslint.config.mjs`; pode ser apagado à vontade.

---

## Próximos passos sugeridos

**A Fase 1 está completa por módulo (2026-07-17).** As lacunas dos módulos 8 e
13 foram fechadas; o código de produto da fase acabou. O que segue é operação
e, depois, Fase 2.

### 1. Operação e produção (fora do código de produto)

- **Push dos commits locais** para o `origin` (o repositório já existe no
  GitHub; o branch local está à frente) e conferir o CI verde. A auditoria de
  2026-07-17 rodou os mesmos passos do workflow localmente e todos passaram.
- Provisionar produção conforme o doc de stack: Render (API + Postgres),
  Vercel (front). Migrations como passo de deploy, nunca automático.
- Contas externas (checklist da seção 19): Cloudflare R2, Meta/WhatsApp,
  Resend, Render, Vercel, Sentry.
- Ligar a comunicação de verdade: instância Evolution +
  `WHATSAPP_PROVEDOR=evolution`; domínio verificado no Resend +
  `EMAIL_PROVEDOR=resend`. Hoje ambos rodam em modo `log`.
- **Rotacionar a API key do Resend** (foi compartilhada em chat).
- Dashboard do Hangfire (`/hangfire`): hoje tem duas guardas fail-closed (mount
  só em Development + filtro que nega fora de Development). **Se um dia for
  exposto em produção**, precisa antes de um filtro que exija autenticação e
  papel gestor — o dashboard não é coberto pelo JWT bearer da API (navegação de
  browser não manda o header), então exigiria cookie de sessão ou proxy próprio.

### 2. Fase 2 (doc de módulos)

App/Portal do técnico (React Native/Expo, offline-first — o schema/sync já
está pronto), financeiro com margem e rentabilidade, avaliações e reputação,
aprovação de orçamento item a item, linha do tempo visual da OS, evidência
fotográfica, importação de contatos, LGPD visível (exportação/anonimização),
central de mensagens unificada, kits de serviço, previsão de reposição.

### 3. Melhorias anotadas ao longo da Fase 1

- Radar "peça que chegou libera reparo parado" (depende de entradas de estoque).
- Notificações imediatas em background via Hangfire (hoje são síncronas).
- Confirmação de e-mail e recuperação de senha (Identity já suporta; depende do
  provedor de e-mail ligado).
- Logo da loja e convite de equipe no onboarding (R2 / fluxo de convite).
- Retry no número sequencial da OS se a corrida aparecer na prática.
