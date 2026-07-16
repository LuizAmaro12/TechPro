# Etapa: Configurações e equipe básica (módulo 13) — última lacuna da Fase 1

Data: 2026-07-16 · Lacuna identificada na auditoria de 2026-07-16. Fecha o
*escopo por módulo* da Fase 1.

## Decisões aprovadas pelo usuário (2026-07-16)

1. **Preferências de notificação: matriz evento × canal** (7 eventos × 2 canais
   = 14 toggles). *Escolha do usuário, diferente da recomendação (toggle só por
   evento).* O doc pede "preferências **básicas**" — a matriz vai além disso,
   mas dá controle fino (ex.: mandar lembrete por WhatsApp e não por e-mail).
   Custo: uma tabela e uma tela um pouco maiores; nenhum risco arquitetural.
2. **Contatos e políticas editáveis + visíveis no portal público**
   (`/agendar/{slug}` e `/acompanhar/{slug}/{codigo}`) — dado vivo, e reduz a
   pergunta "qual a garantia?".
3. **Conta do usuário: nome + troca de senha** (exigindo a senha atual). Troca
   de e-mail fica de fora: é o login, é único globalmente e trocar com segurança
   exige confirmação por e-mail — depende do provedor ainda não ligado.

## Fora de escopo (deferido, com justificativa)

- **Logo da loja**: upload depende do Cloudflare R2 (não provisionado). Mesmo
  motivo dos anexos de OS. Já registrado nas lacunas.
- **Equipe (adicionar membros, funções, permissões)**: o doc coloca
  explicitamente na Fase 2 do módulo 13.

## Modelo de dados

- `Empresa` ganha `Telefone?`, `Email?`, `Endereco?`, `Politicas?`
  (todos opcionais; a loja preenche quando quiser).
- Nova `PreferenciaNotificacao` (PK int, tenant): `TipoEvento`, `Canal`,
  `Ativo`. Índice único (TenantId, TipoEvento, Canal). **Ausência de linha =
  ativo** — tenant novo já nasce com tudo ligado, sem seed.
- Novo valor `StatusMensagem.Desativada`: registrado na auditoria quando a loja
  desligou aquele evento/canal (responde "por que meu cliente não recebeu?").
  Não precisa de migração — o enum é gravado como string sem constraint.

## Ordem dos gates no despacho (ComunicacaoService)

1. **Consentimento LGPD** do cliente → `Suprimida` (gate legal vem primeiro).
2. **Preferência da loja** (evento × canal) → `Desativada`.
3. Envia pelo adaptador do canal → `Enviada` / `Simulada` / `Falhou`.

## Endpoints

- `GET|PUT /api/configuracoes/loja` — nome, telefone, e-mail, endereço,
  políticas. (O **slug** e os **horários** seguem em `/api/agenda/*`, que já
  existem e funcionam; a tela de configurações linka para lá em vez de
  duplicar.)
- `GET|PUT /api/configuracoes/notificacoes` — a matriz completa (14 itens).
- `PUT /api/conta` (nome) e `POST /api/conta/senha` (senha atual + nova, via
  `UserManager.ChangePasswordAsync`). A leitura já existe em `/api/auth/me`.
- Público: `GET /api/publico/{slug}/info` e o acompanhamento passam a devolver
  os contatos e as políticas da loja.

## Front-end

- Nova página `(empreendedor)/configuracoes` com seções: **Dados da loja**
  (nome, contatos, políticas), **Notificações** (matriz evento × canal),
  **Conta** (nome + troca de senha), e um link para `/agenda/configuracoes`
  (horários e link público de agendamento).
- Link "Configurações" no nav.
- Portal público: bloco de contato/políticas da loja em `/agendar/{slug}` e
  `/acompanhar/{slug}/{codigo}`.

## Passos

1. Plano commitado.
2. Backend: colunas na Empresa + entidade de preferência + DbContext + migração
   (com RLS na tabela nova); serviços, validadores e controllers; gate de
   preferência no ComunicacaoService; DTOs públicos com contato/políticas.
3. Testes de integração (loja CRUD, matriz, gate desativada, senha, isolamento).
4. Suíte verde no container; RLS verificado no Postgres.
5. Orval regen; front (configurações + nav + portal); e2e com evidência.
6. Docs: `progresso.md` (fechar a lacuna do módulo 13 → **Fase 1 completa por
   módulo**), README. Commits por passo.
