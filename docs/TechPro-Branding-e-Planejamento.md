# TechPro
## Branding, Posicionamento e Plano de Produto — (consolidado)

---

## Sumário executivo

A TechPro é uma plataforma SaaS vertical para assistências técnicas de celulares e eletrônicos portáteis, cobrindo agendamento, ordem de serviço, estoque de peças, comunicação automatizada com o cliente e controle financeiro.

Este documento trata de **branding, posicionamento e planejamento de produto**. Stack técnica, modelagem de banco de dados e arquitetura de sistema ficam fora de escopo aqui, por decisão explícita — devem ser tratadas em um documento técnico separado.

---

## 1. Visão geral da marca

A TechPro é uma plataforma SaaS voltada para assistências técnicas de celulares e eletrônicos portáteis — negócios que trabalham com ordens de serviço, agendamentos, estoque, atendimento ao cliente e controle financeiro.

A proposta central é transformar o processo tradicional de assistência técnica — manual, disperso entre WhatsApp, papel e planilhas — em uma operação digital, rastreável e automatizada.

O sistema conecta três frentes do negócio:

- **Cliente:** acompanha agendamentos, ordens de serviço, diagnósticos, orçamentos e notificações.
- **Empreendedor:** gerencia atendimentos, equipe, estoque, financeiro, clientes e comunicação.
- **Administrador SaaS:** acompanha empresas, planos, assinaturas e saúde do produto.

A TechPro não deve ser posicionada como "um sistema de ordem de serviço" — esse enquadramento já é ocupado por dezenas de concorrentes (ver seção 2) e não comunica por que alguém deveria trocar de fornecedor ou sair da planilha. O produto deve ser apresentado como uma **central operacional para assistências técnicas de celular**, com foco de nicho mais estreito do que a maioria dos concorrentes generalistas assume.

---

## 3. Posicionamento

### Posicionamento principal
TechPro é uma plataforma SaaS especializada em assistência técnica de celulares e eletrônicos portáteis, que centraliza agendamento, ordem de serviço, estoque, comunicação e financeiro — com foco de nicho mais estreito e execução mais precisa do que os concorrentes generalistas do setor.

### Posicionamento comercial
A plataforma ajuda assistências técnicas a atender melhor, vender mais, reduzir perdas de estoque, controlar custos reais por serviço e entregar uma experiência mais transparente ao cliente final — competindo por precisão de nicho, não por lista de funcionalidades.

### Posicionamento emocional
A TechPro transmite confiança, organização e transparência em um mercado onde o cliente final frequentemente sente insegurança ao deixar um aparelho para reparo.

### Posicionamento operacional
O sistema reduz a dependência de anotações manuais, conversas soltas no WhatsApp, planilhas desatualizadas e processos informais — com um fluxo de dados único por atendimento, do agendamento à entrega.

---

## 4. Proposta de valor

A TechPro entrega controle operacional completo para assistências técnicas de celular: o empreendedor acompanha cada etapa do serviço, do agendamento à entrega, com cálculo de custo, margem e lucro por atendimento — não apenas faturamento.

**Resumida:** Mais controle para a loja. Mais transparência para o cliente. Mais previsibilidade para o negócio.

**Transformação central:** de uma assistência técnica operacionalmente confusa (WhatsApp, papel, planilha, memória da equipe) para uma operação organizada, rastreável e com dados reais para decisão — sem prometer o que só sistemas genéricos de nicho amplo prometem, e sem tentar competir em lista de features contra players que já têm dez anos de mercado.

---

## 5. Diferenciais: o que é paridade e o que é defensável

Esta é a correção mais importante deste documento em relação à versão anterior. Nem todo recurso listado é um diferencial — a maioria é o preço de entrada da categoria.

### 5.1 Paridade de mercado (esperado, não vendável como diferencial)

| Recurso | Por que é paridade |
|---|---|
| Portal do cliente / acompanhamento de OS | Oferecido por Produttivo, AgoraOS, InforOS, Online OS |
| Kanban de ordens de serviço | Oferecido por praticamente todo concorrente direto e adjacente |
| Notificação automática por WhatsApp/e-mail | Já é padrão de categoria desde 2020+ |
| Controle de estoque vinculado à OS | Recurso central de todos os concorrentes citados |
| Emissão de NF-e/NFS-e | Presente em SIGE Lite, AgoraOS, InforOS, Oficina Integrada |
| Dashboard financeiro básico | Presente em todos os concorrentes listados |

Esses recursos continuam **obrigatórios no produto** — sem eles a TechPro não entra na conversa. Só não devem ser a mensagem central de venda.

### 5.2 Diferenciais defensáveis (onde vale investir a mensagem)

1. **Hiperfoco vertical em celular e eletrônicos portáteis.** A maioria dos concorrentes tenta abraçar "assistência técnica em geral", "oficinas", "facilities" ao mesmo tempo. Um produto desenhado só para celular pode ter checklists, terminologia, tipos de peça e fluxos de diagnóstico específicos do aparelho — profundidade que um sistema genérico não vai igualar sem descaracterizar o próprio produto.
2. **Separação estrutural entre serviço (mão de obra) e peça (custo/estoque) para margem real.** Vários concorrentes genéricos misturam os dois nos relatórios, distorcendo o cálculo de lucro. Manter essa separação como princípio de modelagem — e comunicá-la explicitamente — é um diferencial analítico real, não cosmético.
3. **LGPD e segurança como argumento comercial explícito, não nota de rodapé.** A InforOS já usa isso como prova de confiança na própria página de vendas. A TechPro deve fazer o mesmo desde o dia um — ver seção 7.
4. **Aprovação digital de orçamento com trilha de auditoria.** Reduz disputa sobre "o cliente autorizou ou não" e cria registro verificável — vai além do que a maioria oferece como simples notificação.
5. **(Fase futura, não MVP) Sugestão de precificação assistida por dados históricos.** Comparar preço cobrado, tempo gasto e margem de serviços semelhantes já realizados, para sugerir preço na abertura do orçamento. Nenhum concorrente pesquisado oferece isso hoje — mas é investimento de fase avançada, não de lançamento.

**Recomendação de comunicação:** o pitch deve nomear paridade rapidamente ("sim, temos OS, Kanban, estoque e WhatsApp, como qualquer sistema sério") e gastar o tempo de atenção do prospect nos itens da seção 5.2.

---

## 6. Público-alvo e persona

### Público principal
Assistências técnicas de celulares; lojas de manutenção de eletrônicos portáteis; técnicos autônomos em profissionalização; redes com múltiplos atendentes; lojas que vendem acessórios e também prestam serviço.

### Público secundário
Negócios com planos de suporte técnico recorrente; pequenas operações migrando de planilha para sistema.

### Persona: Empreendedor técnico
Dono ou gestor de assistência técnica. Entende bem do serviço, mas tem dificuldade em organizar atendimento, estoque, equipe, financeiro e relacionamento com clientes.

**Dores:** perde tempo respondendo status de serviço pelo WhatsApp; não sabe exatamente quais peças saíram do estoque; tem dificuldade para calcular margem real por serviço; perde clientes por falha de comunicação; não tem visão clara do dia, da semana e do mês; não mede produtividade da equipe.

**Desejos:** controle total do negócio; mais confiança percebida pelo cliente; evitar prejuízo com estoque; automatizar lembretes; aumentar lucro real (não apenas faturamento); profissionalizar a loja.

---

## 7. LGPD, privacidade e segurança de dados

Esta seção não existia na versão anterior do documento e precisa existir desde o MVP — não como adendo de "fase avançada" — porque a plataforma vai armazenar, para múltiplas empresas ao mesmo tempo, dados pessoais de clientes finais de terceiros: nome, telefone, e-mail, endereço, modelo e número de série/IMEI do aparelho, histórico de pagamento e, potencialmente, fotos do equipamento.

**Compromissos que devem estar no planejamento desde o início (como requisito de produto, não de implementação técnica):**

- **Isolamento de dados entre empresas.** Cada assistência técnica cliente da TechPro deve ver e acessar apenas os dados dos próprios clientes finais — nunca dados de outra empresa cadastrada na plataforma.
- **Base legal para comunicação.** Mensagens de marketing/reativação (diferente de notificações operacionais de uma OS em andamento) exigem consentimento explícito do cliente final, não apenas o cadastro dele na loja.
- **Direito de exclusão e portabilidade.** O cliente final deve poder solicitar a exclusão de seus dados, e a assistência técnica precisa de um caminho para atender esse pedido sem quebrar o histórico de outras OS.
- **Retenção definida.** Prazo claro de quanto tempo dados de clientes inativos ficam armazenados antes de anonimização ou exclusão.
- **Criptografia em trânsito e em repouso** para qualquer dado pessoal armazenado.

**Por que isso entra no planejamento de branding e não só no técnico:** tratar isso como diferencial comercial explícito (seção 5.2, item 3) exige que o compromisso seja real desde a primeira versão — não dá para prometer "seus dados estão seguros" na landing page se o modelo de dados do MVP não foi desenhado com isolamento entre empresas desde o começo.

---

## 8. Roadmap e MVP (prioridade sobre o catálogo completo)

O catálogo de módulos da seção 9 é extenso porque descreve a visão completa do produto — não porque tudo deve ser construído de uma vez. A ordem de leitura recomendada é: primeiro decidir o que entra no MVP, depois consultar o catálogo completo como backlog faseado.


---

## 9. Módulos do produto (catálogo completo, por fase)

### 9.1 Portal do cliente *(Fase 1, com evolução em Fase 2)*

O portal do cliente precisa ser tratado como uma experiência de atendimento digital, não como um formulário.

**Fluxo recomendado, progressivo:**
1. Identificação do cliente.
2. Dados do aparelho.
3. Problema apresentado.
4. Serviço desejado.
5. Data e horário.
6. Anexos e observações.
7. Confirmação.
8. Acompanhamento (status, orçamento, aprovação/recusa).

Funcionalidades completas (distribuídas entre Fase 1 e Fase 2): agendamento online; escolha de serviço, dia e horário; observações; envio de anexos/fotos; cadastro de dados pessoais e do aparelho; descrição do problema; acompanhamento de status; visualização e aprovação/recusa de orçamento (com trilha de auditoria — Fase 2); histórico de serviços; modificação/cancelamento de agendamento quando permitido; notificações; acesso a comprovante; avaliação do atendimento (Fase 2).

### 9.2 Dashboard do empreendedor *(versão essencial na Fase 1, indicadores avançados em Fase 2-3)*

O dashboard precisa responder perguntas de negócio, não só exibir números: o que precisa de atenção agora? Onde estou perdendo dinheiro? Quais serviços mais geram lucro (não faturamento)? Quais peças acabam com frequência? Quais ordens estão paradas?

**Indicadores essenciais (Fase 1):** ordens de serviço abertas; agendamentos do dia; serviços em atraso; aparelhos em reparo; serviços prontos para retirada; faturamento do mês.

**Indicadores avançados (Fase 2-3):** lucro estimado; custo de peças utilizadas; peças com estoque baixo; clientes novos/recorrentes; avaliação média; técnicos com maior volume; serviços mais solicitados; taxa de aprovação de orçamentos; tempo médio de conclusão.

### 9.3 Agendamentos e calendário *(Fase 1, com refinamento em Fase 2)*

Funcionalidades: visualização por dia/semana/mês; filtros por status, serviço, técnico e cliente; criação manual; agendamento via portal do cliente; reagendamento; cancelamento; check-in manual; conversão automática em OS; bloqueio de horários; lembretes automáticos.

**Recomendação:** capacidade por tipo de serviço (uma troca de tela consome mais tempo de agenda que uma limpeza ou diagnóstico) melhora a precisão desde o início — vale incluir já na Fase 1, é barato de modelar e caro de adicionar depois.

### 9.4 Kanban de ordens de serviço *(essencial na Fase 1, recursos avançados em Fase 2)*

**Etapas sugeridas:** Agendado, Check-in realizado, Na fila, Em diagnóstico, Aguardando aprovação, Aguardando peça, Em reparo, Em teste, Pronto para retirada, Entregue, Cancelado.

**Recursos Fase 1:** arrastar e soltar; responsável técnico; prioridade; prazo estimado; status de pagamento e de aprovação de orçamento.

**Recursos Fase 2:** etiquetas por tipo de serviço; alertas de atraso; comentários internos; anexos técnicos; fotos antes/depois; checklist de qualidade por tipo de serviço (uma troca de tela pode ter checklist diferente de troca de bateria).

### 9.5 Clientes *(Fase 1 básico, CRM completo em Fase 2-3)*

Funcionalidades: listagem completa; filtros por ativos/inativos/VIP/recorrentes; cadastro manual, importação e exportação; histórico de aparelhos, OS, pagamentos e mensagens; avaliações; observações internas; segmentação para campanhas (Fase 3).

**Classificação sugerida:** cliente novo, recorrente, VIP, inativo, com pendência, alto valor gasto, avaliação negativa — permite campanhas mais direcionadas quando a Fase 3 chegar.

### 9.6 Serviços predefinidos *(Fase 1)*

Funcionalidades: criar/editar/excluir serviço; preço base; duração estimada; categoria; peças normalmente utilizadas; checklist padrão; se exige diagnóstico; se pode ser agendado online; prazo médio.

**Princípio a manter desde o início (reforço da seção 5.2):** separar "serviço" (mão de obra) de "peça" (custo/estoque). Misturar os dois prejudica a análise de margem — este é um diferencial de modelagem, não apenas um detalhe de cadastro.

### 9.7 Estoque *(Fase 1 básico, indicadores avançados em Fase 2)*

Funcionalidades Fase 1: cadastro de peças; quantidade disponível; custo unitário; preço de venda; fornecedor; estoque mínimo; alertas de estoque baixo; baixa automática ao usar peça em OS.

Funcionalidades Fase 2: histórico de movimentação detalhado; peças mais utilizadas e com maior margem; custo total em estoque; relação entre peça e serviço.

### 9.8 Financeiro *(faturamento básico em Fase 1; margem e rentabilidade em Fase 2)*

O financeiro precisa mostrar rentabilidade, não só arrecadação — "quanto sobrou", não apenas "quanto entrou".

Funcionalidades Fase 1: faturamento; receita por período; transações; pagamentos pendentes/concluídos; ticket médio.

Funcionalidades Fase 2: lucro bruto; custo de peças; receita por serviço; serviços mais lucrativos; clientes com maior valor gasto; relatórios exportáveis; margem média por serviço.

### 9.9 Mensagens e comunicação *(essenciais em Fase 1, templates editáveis em Fase 2)*

**Canais:** WhatsApp, e-mail, notificações internas (SMS como possibilidade futura).

**Mensagens essenciais (Fase 1):** confirmação e lembrete de agendamento; OS criada; orçamento disponível/aprovado/recusado; reparo concluído; pronto para retirada.

**Fase 2:** templates editáveis por evento e por loja; pedido de avaliação; campanhas promocionais; reativação de clientes inativos.

### 9.10 Avaliações dos clientes *(Fase 2)*

Avaliação por nota e comentário; por serviço e por técnico; histórico de satisfação; NPS; alertas para avaliação negativa (pode gerar tarefa interna para o gestor entrar em contato).

### 9.11 Configurações do negócio *(Fase 1 básico, refinamento contínuo)*

**Informações da loja:** nome, logo, URL personalizada, contatos, endereço, redes sociais, cores da marca, horários de funcionamento, bloqueios de agenda, capacidade simultânea, políticas de cancelamento, mensagens padrão.

**Equipe (Fase 2):** adição de membros; funções e permissões (atendente, técnico, gestor não precisam acessar os mesmos dados); histórico de ações.

**Notificações:** preferências por canal e evento; templates; horários permitidos para envio.

### 9.12 Administração SaaS *(Fase 4)*

Overview de empresas; usuários ativos; planos e assinaturas; MRR/ARR; churn; trial conversion; empresas inadimplentes; uso por empresa; limites por plano; suporte; logs administrativos; gestão de trial; bloqueio/suspensão de workspace.

---

## 10. Planos e precificação

### Faixa de referência de mercado

Pesquisa de mercado indica que sistemas de gestão para assistência técnica no Brasil variam tipicamente entre **R$ 30 e R$ 300 por mês**, dependendo do conjunto de funcionalidades. Isso é uma referência de mercado, não uma definição de preço da TechPro — o preço final deve ser validado com custo de operação e testes de disposição a pagar (seção 14), não apenas ancorado na concorrência.

### Estrutura de planos sugerida

| Plano | Público | Módulos incluídos (por fase de disponibilidade) |
|---|---|---|
| **Inicial** | Técnicos autônomos, pequenas assistências | Agendamento, OS, Kanban básico, clientes, serviços, portal do cliente, notificações essenciais, dashboard básico *(Fase 1)* |
| **Profissional** | Lojas em crescimento | + Kanban completo, estoque, financeiro com margem, WhatsApp/e-mail com templates, avaliações, equipe, URL personalizada *(Fase 2)* |
| **Avançado** | Operações maiores | + Múltiplos usuários, permissões avançadas, indicadores avançados, campanhas, exportações, automações, controle avançado de estoque, integrações fiscais *(Fase 3)* |
| **Enterprise** | Redes ou operações personalizadas | + Multiunidade, suporte prioritário, API, integrações customizadas, relatórios personalizados, SLA comercial *(Fase 4)* |

**Nota de risco:** quatro planos com granularidade de recursos é mais complexidade de suporte e onboarding do que uma assistência técnica pequena, pouco sofisticada em software, costuma tolerar bem. Vale considerar iniciar comercialmente com dois planos (Inicial/Profissional) e só introduzir Avançado/Enterprise quando houver demanda real de clientes maiores — reduz decisão de compra complexa no momento de maior fricção (a primeira venda).

---

## 11. Tom de voz e identidade verbal

A TechPro deve comunicar segurança, clareza e profissionalismo. O produto lida com operação, dinheiro e dados de clientes — o tom não deve ser informal a ponto de parecer amador, nem tão técnico que afaste o dono de assistência que não é especialista em software.

**Características:** claro, objetivo, confiável, moderno, direto, próximo sem ser informal demais, focado em resultado.

**Evitar:** linguagem genérica ("sistema completo" sem explicar valor); promessas vagas; termos técnicos excessivos para o cliente final; aparência de ferramenta amadora; vender paridade de mercado como diferencial (seção 5).

**Exemplos de reformulação:**

| Genérico | Reformulado |
|---|---|
| Cadastre OS e veja seus clientes. | Centralize suas ordens de serviço, acompanhe cada etapa do reparo e mantenha seus clientes informados automaticamente. |
| Controle estoque. | Saiba exatamente quais peças foram usadas, quanto custaram e como impactaram o lucro de cada serviço. |
| Sistema de agendamento. | Permita que seus clientes agendem online com dados completos do aparelho, problema relatado e horário disponível. |

### Palavras-chave da marca
Controle, transparência, agilidade, organização, confiança, gestão, automação, rentabilidade, previsibilidade, profissionalização.

---

## 12. Mensagens-chave e slogan

**Opção principal:** Gestão inteligente para assistências técnicas de celular que querem vender, atender e controlar melhor.

**Alternativas (escolher uma linha, não misturar todas na comunicação):**
- Transforme sua assistência técnica em uma operação digital, organizada e lucrativa.
- Do agendamento à entrega, gerencie cada etapa do reparo.
- Menos improviso. Mais controle. Mais confiança no atendimento técnico.

**Nota:** a versão anterior trazia cinco variações de slogan e três pitches quase idênticos entre si. Manter uma única linha de mensagem principal por canal evita diluição de marca — variações servem para teste A/B, não para uso simultâneo.

---

## 13. Sugestões de melhoria de produto (backlog priorizado)

1. **Aprovação digital de orçamento com trilha de auditoria** *(Fase 2 — já tratado como diferencial na seção 5.2)*.
2. **Fotos antes e depois** *(Fase 2)* — aumenta confiança e reduz disputa.
3. **Assinatura digital de retirada** *(Fase 2)*.
4. **Checklist técnico por tipo de serviço** *(Fase 2)*.
5. **Status "aguardando peça"** *(Fase 1 — incluir desde o início, é onde reparos mais travam)*.
6. **Histórico por aparelho** (além de por cliente), incluindo modelo e número de série *(Fase 2)*.
7. **Controle de garantia** por serviço/peça, com validade *(Fase 3)*.
8. **Relatórios de margem** separados de faturamento *(Fase 2, já reforçado na seção 9.8)*.
9. **Templates de mensagem personalizáveis por loja** *(Fase 2)*.
10. **Permissões por função** *(Fase 2)*.
11. **Multiunidade** *(Fase 4)*.
12. **Integração fiscal (NFS-e/NF-e)** — tratar como projeto de integração fiscal e jurídica à parte, dependente de prefeitura, certificado digital e regras municipais. **Não prometer emissão automática de forma genérica antes de validar o escopo técnico e legal por município.** Este ponto já estava correto na versão anterior do documento e deve ser mantido como está.

---


## 14. Riscos e pontos de atenção

- **Escopo muito grande.** O catálogo completo (seção 9) é amplo; tentar construir tudo de uma vez antes de validar é o maior risco financeiro deste projeto — mais do que qualquer detalhe de branding.
- **Concorrência madura.** Vários concorrentes diretos têm mais de 5-10 anos de mercado e dezenas de milhares de clientes (seção 2). Entrar competindo por lista de funcionalidades é uma corrida perdida; competir por foco de nicho e execução (seção 5.2) é a única rota defensável identificada até agora.
- **Complexidade percebida.** Assistências pequenas precisam de um sistema simples. Interface progressiva — mostrar o básico primeiro, liberar recursos avançados conforme o plano ou a maturidade da loja — reduz esse risco.
- **LGPD tratada tarde.** Se o isolamento de dados entre empresas não for parte do desenho desde o MVP (seção 7), corrigir depois é caro e arriscado, especialmente já com dados reais de clientes em produção.
- **Excesso de granularidade de planos.** Quatro planos (seção 10) podem complicar a decisão de compra logo na largada — considerar simplificar para dois no lançamento.
- **Pular a validação.** Avançar direto para desenvolvimento completo do MVP sem as conversas da seção 14 significa apostar em hipóteses de diferenciação e preço não testadas, em um mercado que já tem players estabelecidos observando o mesmo público.

---

## 15. Estrutura de navegação

**Área do empreendedor:** Dashboard · Agenda · Ordens de Serviço · Kanban · Clientes · Serviços · Estoque · Financeiro · Mensagens · Avaliações · Relatórios · Equipe · Configurações

**Área do cliente:** Agendar serviço · Meus agendamentos · Minhas ordens de serviço · Acompanhar reparo · Orçamentos · Histórico · Perfil

**Área administrativa SaaS** *(Fase 4)*: Overview · Empresas · Usuários · Planos · Assinaturas · Pagamentos · Métricas · Suporte · Configurações

---

## 16. Pitch

**Curto:** A TechPro é uma plataforma SaaS especializada em assistência técnica de celulares e eletrônicos portáteis. O cliente acompanha o reparo pelo celular; o empreendedor controla agendamento, estoque, financeiro e comunicação em um só lugar, com visão real de margem por serviço — não apenas faturamento.

**Comercial:** A TechPro ajuda assistências técnicas de celular a sair do improviso sem prometer o que qualquer sistema genérico já promete. A diferença está no foco: hiperespecialização no fluxo de celular, separação real entre serviço e peça para calcular margem de verdade, e segurança/LGPD tratadas como compromisso desde o primeiro dia — não como nota de rodapé depois que o cliente perguntar.

---

## 17. Direção estratégica final

A TechPro deve ser construída e comunicada como uma solução vertical para um nicho específico — celular e eletrônicos portáteis — e não como um ERP genérico com uma vertical de "assistência técnica" colada. Essa é a vantagem competitiva real diante de um mercado que já tem generalistas maduros e bem estabelecidos (seção 2).

A força do produto está em conectar com precisão as etapas do fluxo real: cliente chega com problema → loja agenda → técnico diagnostica → orçamento é aprovado com trilha de auditoria → peça é consumida e seu custo rastreado → reparo é feito → aparelho é testado → cliente é avisado → pagamento é registrado com margem real calculada → histórico fica salvo com segurança e conformidade desde o primeiro dia.

### Essência da marca
A TechPro existe para profissionalizar assistências técnicas de celular, tornando atendimento, reparo, estoque e financeiro mais organizados, transparentes, seguros e lucrativos — competindo por foco e execução, não por lista de funcionalidades.

### Promessa central
Controle completo da operação, do primeiro agendamento à entrega final — com a margem real de cada serviço, não apenas o faturamento.

### Melhor definição do produto
SaaS de gestão operacional especializado em assistência técnica de celular e eletrônicos portáteis.

### Melhor mensagem comercial
Organize sua assistência técnica de celular, mantenha seus clientes informados e acompanhe o lucro real de cada serviço — em uma plataforma feita para o seu nicho, não adaptada de um sistema genérico.