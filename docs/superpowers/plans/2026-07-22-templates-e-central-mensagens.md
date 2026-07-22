# Etapa: Templates editáveis e central de mensagens (Fase 2)

Data: 2026-07-22 · 11º item web da Fase 2. Bloco comunicação/config (módulo 9).

## Diagnóstico / valor

Hoje o texto de **toda** notificação é string fixa no `ComunicacaoService`. A
loja não consegue ajustar tom, assinatura ou instruções próprias — e o doc pede
"templates editáveis por evento e por loja" na Fase 2.

Junto vai a **central de mensagens unificada**, que o doc descreve como "um
inbox mostrando, por cliente, todo o histórico de notificações enviadas — dá ao
lojista visibilidade do que já foi comunicado sem abrir o WhatsApp". O dado já
existe (`mensagens_enviadas`); falta a visão por cliente (hoje só há por OS).

## Decisões técnicas (justificadas)

- **Ausência = padrão** (mesmo padrão da matriz de preferências, que já provou
  ser bom): sem template salvo, vale o texto embutido. Zero seed, zero migração
  de dados, e uma loja nova continua com mensagens boas desde o primeiro dia.
- **Template por evento, não por evento × canal**: o despacho atual já usa um
  `assunto` (aplicado só no e-mail) e um `corpo` compartilhado. Duplicar por
  canal dobraria a tela de configuração sem ganho real — a loja quer ajustar o
  texto, não manter duas versões.
- **Placeholders validados na gravação, não no envio.** Cada evento declara as
  variáveis disponíveis (`{cliente}`, `{loja}`, `{servico}`, …). Salvar um
  template com variável inexistente é **400 com a lista do que é válido** — um
  erro de digitação nunca chega ao cliente final como texto quebrado.
- **Refatoração contida do `ComunicacaoService`**: cada gatilho passa a montar
  um dicionário de variáveis + o texto padrão e delegar a um `ComporAsync`. A
  composição fica num lugar só; os gates de LGPD/preferência e a auditoria não
  mudam.
- **Central de mensagens é leitura pura** derivada de `mensagens_enviadas` —
  sem tabela nova. Mostra também as **suprimidas/desativadas**, que é
  justamente o que responde "por que meu cliente não recebeu?".

## Modelo de dados

`TemplateMensagem` (int PK, tenant): TipoEvento (único por tenant), Assunto?,
Corpo, AtualizadoEm. Migração com RLS.

## Endpoints

- `GET /api/configuracoes/templates` — todos os eventos com o texto **efetivo**
  (personalizado ou padrão), flag `personalizado` e as variáveis disponíveis.
- `PUT /api/configuracoes/templates` — salva os personalizados; corpo vazio
  volta ao padrão (remove a personalização).
- `GET /api/clientes/{id}/mensagens` — histórico unificado do cliente.

## Front-end

- **Configurações**: aba/seção de templates por evento, com as variáveis
  disponíveis à vista e botão "voltar ao padrão".
- **Clientes**: histórico de mensagens no detalhe do cliente (canal, evento,
  status — incluindo suprimida/desativada — e data).

## Fora desta etapa (registrado)

- **Resposta automática a FAQ**: o próprio doc marca como dependente da API de
  WhatsApp escolhida — não entra até a stack de WhatsApp estar fechada.
- **Campanhas e reativação de inativos**: envio em massa tem implicações de
  LGPD/consentimento de marketing (hoje o consentimento é operacional). Merece
  etapa própria, junto com o consentimento separado já deferido.
- **Equipe com funções e permissões**: o doc pede desenhar junto com
  LGPD/auditoria — etapa própria, não remendo.

## Passos

1. Plano commitado.
2. Backend: entidade + renderer + `ComporAsync` no `ComunicacaoService` +
   endpoints + migração (RLS); testes (fallback ao padrão, personalização
   aplicada, variável inválida rejeitada, voltar ao padrão, central por cliente,
   isolamento).
3. Suíte verde; RLS conferido no Postgres.
4. Orval regen; front; e2e com evidência.
5. Docs (`progresso.md`, roadmap).
