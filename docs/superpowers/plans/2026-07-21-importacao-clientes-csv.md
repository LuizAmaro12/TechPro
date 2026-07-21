# Etapa: Importação de clientes por CSV (Fase 2)

Data: 2026-07-21 · 9º item web da Fase 2. Início do bloco clientes/reputação.

## Diagnóstico / valor

O doc pede "importação CSV" no bloco de clientes. É a **porta de entrada** do
produto: uma loja que já tem carteira (planilha, agenda do celular exportada,
outro sistema) não vai recadastrar centenas de contatos à mão. Sem importar, a
adoção começa do zero — atrito que derruba conversão.

## Decisões técnicas (justificadas)

- **Um passo com relatório por linha** (não preview + commit em duas fases): a
  importação só **adiciona** — nunca atualiza nem apaga —, então o risco é
  baixo. Importa as linhas válidas, **pula e reporta** duplicadas e inválidas.
  Re-importar depois de corrigir é seguro porque a deduplicação é por telefone.
  Duas fases dobrariam a superfície sem ganho proporcional aqui.
- **Deduplicação por telefone** (só dígitos), contra o banco **e** dentro do
  próprio arquivo. Telefone é a chave natural do cliente no resto do sistema
  (vínculo silencioso do portal/fila já usa isso). Duplicado = pulado, não
  atualizado: sobrescrever dado existente numa importação surpreende.
- **Cabeçalho por nome, tolerante**: aceita variações comuns
  (`nome`/`name`, `telefone`/`celular`/`whatsapp`/`fone`, `email`/`e-mail`,
  `cpf`, `endereço`, `observações`), sem acento e case-insensitive. Delimitador
  detectado entre `,` e `;` (Excel BR usa `;`). Campos entre aspas suportados.
- **`nome` e `telefone` são obrigatórios**: se o cabeçalho não os tiver, a
  importação inteira falha com mensagem clara (não adianta importar meio).
- **Consentimento = false** nos importados: importar uma lista **não** é
  consentimento LGPD. Coerente com o resto do sistema (a loja coleta consentimento
  no fluxo real). Registrado explicitamente.
- **Teto de linhas** (5000) para não virar vetor de abuso/memória.
- **Transacional**: as válidas entram num único `SaveChanges`; relatório é
  calculado antes de gravar.

## Modelo de dados

**Nenhuma tabela/coluna nova.** Cria `Cliente` existentes.

## Endpoints

- `POST /api/clientes/importar` — corpo `{ conteudoCsv }`; devolve
  `{ total, importados, duplicados, erros: [{ linha, motivo }] }`.

## Front-end

- **Clientes**: botão "Importar CSV" → área para colar/enviar o conteúdo (lê o
  arquivo no cliente e manda o texto), com dica do formato e exibição do
  relatório (importados / duplicados / erros por linha).

## Fora desta etapa (registrado)

- **Atualizar existentes na importação** (merge): decisão de produto arriscada;
  fica para quando houver demanda real.
- Avaliações/NPS, risco de inadimplência, UI completa de conta vinculada:
  próximos itens do bloco.

## Passos

1. Plano commitado.
2. Backend: parser CSV + `ImportarAsync` + endpoint + validação; testes
   (importa válidas, dedup banco+arquivo, cabeçalho ausente, delimitador `;`,
   aspas, teto, isolamento).
3. Suíte verde.
4. Orval regen; front (clientes); e2e com evidência.
5. Docs (`progresso.md`, roadmap).
