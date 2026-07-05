# TechPro — Especificação de Módulos e Funcionalidades (v3)

## Como ler este documento


Cada módulo abaixo traz: **objetivo**, **funcionalidades essenciais**, **funcionalidades avançadas**, e uma seção **"Novo nesta rodada"** com features adicionais que agregam valor real — algumas marcadas com uma nota de cautela quando o custo de manutenção ou a complexidade merece reflexão antes de entrar no roadmap. Isso é proposital: liberdade para propor não significa empilhar feature sem crítica.

Dois módulos novos foram adicionados em relação à v2 — **Onboarding e configuração inicial** e **App/Portal do técnico** — porque, dado que você vai operar sozinho, sem equipe de sucesso do cliente ou de suporte, alguns módulos que pareceriam "nice to have" em uma operação com equipe são, aqui, redutores diretos da sua própria carga de trabalho.

---

## 0. Onboarding e configuração inicial *(novo módulo — Fase 1)*

### Por que este módulo existe
Sem equipe de implantação, cada nova empresa cliente precisa conseguir se configurar sozinha. Se a primeira experiência depender de você explicar manualmente cada campo por WhatsApp, o CAC (custo de aquisição) vira tempo seu, que é o recurso mais escasso do projeto agora.

### Funcionalidades essenciais (Fase 1)
- Assistente de configuração guiado (wizard) na primeira vez que a empresa acessa: nome/logo da loja → horários de funcionamento → cadastro dos primeiros serviços (com sugestões pré-preenchidas, ex.: "troca de tela", "troca de bateria", editáveis) → cadastro de peças básicas → convite da equipe (opcional).
- Dados de exemplo pré-carregados e removíveis (uma OS fictícia, um cliente fictício) para o dono da assistência "sentir" o produto sem precisar entender o modelo de adods primeiro.
- Checklist de ativação visível ("3 de 5 passos concluídos") — reduz abandono no primeiro acesso.

### Funcionalidades avançadas (Fase 2)
- Central de ajuda integrada (FAQ + vídeos curtos) dentro do próprio produto — reduz volume de dúvidas indo direto para você.
- Modo demonstração/sandbox: um ambiente de teste isolado que um lead pode explorar sem afetar dados reais, útil para venda sem processo comercial dedicado.

### Cautela
Não vale sofisticar demais o wizard antes de ter os primeiros clientes reais testando — é fácil gastar tempo desproporcional polindo a primeira tela em vez do núcleo do produto (OS, Kanban, estoque).

---

## 1. Portal do cliente *(Fase 1, com evolução em Fase 2)*

### Objetivo
Ser a razão pela qual o cliente final para de perguntar "e aí, já ficou pronto?" pelo WhatsApp.

### Funcionalidades essenciais (Fase 1)
- Fluxo progressivo de agendamento/reagendamento: identificação → dados do aparelho → problema → serviço → data/horário → anexos → confirmação.
- Acompanhamento de status da OS em tempo real.
- Visualização e aprovação/recusa simples de orçamento.
- Histórico de serviços anteriores do próprio cliente.

### Funcionalidades avançadas (Fase 2)
- **Linha do tempo visual da OS**, não apenas um status atual — mostrando cada etapa percorrida com data/hora (ex.: "Check-in 10/07 09h14 → Em diagnóstico 10/07 09h40 → Aguardando peça 10/07 11h20"). Isso é mais forte como prova de transparência do que um único rótulo de status, e reforça o diferencial de confiança sem custo de engenharia alto (é o mesmo dado do Kanban, só exibido de outro jeito).
- Aprovação de orçamento **item a item**, não apenas binária. O cliente pode aprovar a troca de tela e recusar a limpeza interna, por exemplo. Aumenta ticket médio sem forçar decisão tudo-ou-nada — e a trilha de auditoria (diferencial já definido no branding) se torna mais rica: fica registrado exatamente o que foi autorizado.
- Evidência fotográfica no diagnóstico: o técnico anexa foto/observação do problema identificado, visível ao cliente antes da aprovação do orçamento — reduz "por que vou pagar isso" na hora de aprovar.

### Novo nesta rodada
- **Link/QR code de acompanhamento compartilhável.** O cliente pode enviar o link da OS para um terceiro (familiar, sócio) acompanhar sem precisar logar. Baixo custo de implementação, alto valor percebido.
- **Estimativa de prazo dinâmica**, recalculada a cada mudança de status (não uma data fixa estimada na abertura, que quase sempre erra). Ex.: ao entrar em "aguardando peça", o prazo exibido passa a refletir a previsão de chegada da peça (ver módulo de Estoque).

### Cautela
Chat assíncrono dentro do portal (mensagens entre loja e cliente registradas no sistema, similar a um "inbox") é uma ideia forte para reduzir dependência do WhatsApp externo — mas é complexo de fazer bem (leitura, notificação, anexos) e compete diretamente com o hábito já consolidado do WhatsApp. Recomendo tratar como Fase 3, não antes: valide primeiro se o cliente realmente usaria um chat dentro do portal em vez de simplesmente responder no WhatsApp de sempre.

---

## 2. Agendamento e calendário *(Fase 1, refinamento em Fase 2)*

### Objetivo
Organizar a entrada de demanda sem depender de "vou olhar aqui e te falo".

### Funcionalidades essenciais (Fase 1)
- rota personalizada.
- Visualização por dia/semana/mês; criação manual e via portal do cliente; reagendamento; cancelamento; check-in; conversão automática em OS; bloqueio de horários; lembretes automáticos.
- **Capacidade por tipo de serviço** desde o início (uma troca de tela consome mais tempo de agenda que uma limpeza) — já identificado no branding como barato de modelar agora e caro depois.

### Funcionalidades avançadas (Fase 2)
- Histórico de comparecimento por cliente (sinaliza clientes que costumam faltar, sem bloquear, só informar).

### Novo nesta rodada
- **Fila de espera / lista de interesse** quando não há horário disponível na data desejada. Hoje essa demanda simplesmente se perde; capturá-la é receita que já existe e está sendo descartada.
- **Bloqueio automático de agenda por indisponibilidade de peça conhecida.** Se o estoque indica zero unidades de uma peça-chave para um serviço, o sistema pode sinalizar isso no momento do agendamento ("peça em falta, prazo estendido") em vez de prometer um prazo que não será cumprido — conecta agendamento com estoque de um jeito que nenhum concorrente pesquisado destacou.

### Cautela
"Conserto expresso" com sobretaxa por prioridade é uma ideia de monetização legítima, mas é uma decisão comercial da assistência técnica, não do software — o papel do produto aqui é permitir que a loja configure isso como um serviço com preço diferenciado (já suportado pelo cadastro de serviços), não construir um motor de precificação dinâmica para isso.

---

## 3. Ordem de serviço e Kanban *(essencial na Fase 1, avançado em Fase 2)*

### Objetivo
Ser o registro único e visual de tudo que acontece com o aparelho, do check-in à entrega.

### Funcionalidades essenciais (Fase 1)
- Etapas: Agendado, Check-in realizado, Na fila, Em diagnóstico, Aguardando aprovação, **Aguardando peça**, Em reparo, Em teste, Pronto para retirada, Entregue, Cancelado.
- Arrastar e soltar; responsável técnico; prioridade; prazo estimado; status de pagamento e de aprovação de orçamento.

### Funcionalidades avançadas (Fase 2)
- Etiquetas por tipo de serviço; comentários internos; anexos técnicos; fotos antes/depois; checklist de qualidade por tipo de serviço antes da entrega.

### Novo nesta rodada
- **SLA visual por etapa.** Cada card muda de cor gradualmente (verde → amarelo → vermelho) conforme o tempo naquela etapa se aproxima de um limite configurável por tipo de serviço. Torna o "alerta de atraso" (já previsto) visível sem precisar abrir um relatório — o Kanban já é olhado o dia todo, então é o lugar certo para esse sinal.
- **Reatribuição de técnico com histórico.** Se o técnico originalmente designado muda (folga, sobrecarga), a troca fica registrada com motivo — importante tanto para produtividade (módulo 12) quanto para rastreabilidade caso um cliente questione quem mexeu no aparelho.

---

## 4. App/Portal do técnico *(novo módulo — Fase 2)*

### Por que este módulo existe
O Kanban do módulo 3 é a visão gerencial — pensada para o dono/gestor enxergar tudo. O técnico na bancada (ou em campo) não precisa dessa visão completa; precisa de uma lista simples e mobile-first do que é dele para fazer agora. Misturar as duas visões numa única tela geralmente resulta em uma interface que não serve bem nem para um público nem para o outro.

### Funcionalidades essenciais (Fase 2)
- Lista de OS atribuídas ao técnico logado, ordenada por prioridade/prazo.
- Avançar etapa, registrar peça usada (com baixa automática no estoque), anexar foto direto da câmera do celular, marcar checklist técnico.
- Visualização somente dos dados necessários para executar o serviço (sem financeiro, sem dados de outros técnicos).

### Funcionalidades avançadas (Fase 3)
- Registro de tempo gasto por etapa (base para o indicador de produtividade por técnico no dashboard).
- Atribuição sugerida por especialidade (se o cadastro de equipe indicar que o técnico A faz placa e o técnico B faz tela, o sistema sugere o responsável ao criar a OS, mas o gestor sempre pode sobrescrever).

### Cautela
Não é necessário construir isso como aplicativo nativo — uma versão web responsiva otimizada para celular resolve o mesmo problema com uma fração do esforço de manutenção, especialmente relevante dado que você está construindo e mantendo isso sozinho.

---

## 5. Clientes (CRM) *(Fase 1 básico, completo em Fase 2-3)*

### Objetivo
Funcionar como um CRM especializado, não uma lista de contatos.

### Funcionalidades essenciais (Fase 1)
- Cadastro, listagem, filtros (ativos/inativos/VIP/recorrentes), histórico de aparelhos, OS, pagamentos e mensagens por cliente.

### Funcionalidades avançadas (Fase 2-3)
- Segmentação para campanhas; observações internas; avaliações vinculadas ao histórico do cliente.

### Novo nesta rodada
- **Importação de contatos existentes** (planilha/CSV, ou exportação do WhatsApp Business) no onboarding. Reduz diretamente a fricção de migração — a maior barreira de adoção identificada é justamente sair do "improviso" que já tem uma base de clientes em outro lugar.
- **Conta vinculada (família/empresa).** Permitir agrupar múltiplos aparelhos ou pessoas sob um cliente principal — útil para famílias que levam vários celulares, ou pequenas empresas que mandam funcionários com aparelhos corporativos.
- **Indicador simples de risco de inadimplência**, baseado em atraso de pagamento já registrado no próprio histórico da loja (não é score de crédito externo, é só "este cliente já atrasou X vezes") — ajuda a decisão de liberar reparo fiado ou exigir pagamento adiantado.

---

## 6. Serviços e peças (catálogo) *(Fase 1)*

### Objetivo
Padronizar a operação e manter a separação estrutural entre serviço (mão de obra) e peça (custo/estoque) — o diferencial de modelagem já definido no branding.

### Funcionalidades essenciais (Fase 1)
- Criar/editar/excluir serviço; preço base; duração estimada; categoria; peças normalmente utilizadas; checklist padrão; se exige diagnóstico; se pode ser agendado online; prazo médio.

### Novo nesta rodada
- **Kits de serviço (bundle).** Ex.: "Pacote manutenção preventiva" = diagnóstico + limpeza + troca de película, com preço de pacote menor que a soma individual. Cria caminho estruturado de upsell sem exigir que o dono da loja calcule manualmente cada combinação.
- **Peça compatível/equivalente.** Uma tela pode servir para vários modelos de aparelho — permitir vincular uma peça a múltiplos modelos evita cadastro duplicado e mantém o estoque preciso mesmo quando a mesma peça física atende vários serviços.

### Cautela
**Serviço com dependência** (ex.: "diagnóstico obrigatório antes de abrir orçamento de reparo de placa") é uma ideia legítima, mas exige um pequeno motor de regras que aumenta a complexidade de manutenção do cadastro. Vale como Fase 3, e só se o uso real mostrar que lojas estão abrindo orçamento sem diagnóstico de forma problemática — não construir preventivamente.

---

## 7. Estoque *(Fase 1 básico, avançado em Fase 2-3)*

### Objetivo
Estoque vinculado à OS, não uma planilha separada de contagem.

### Funcionalidades essenciais (Fase 1)
- Cadastro de peças; quantidade disponível; custo unitário; preço de venda; fornecedor; estoque mínimo; alertas de estoque baixo; baixa automática ao usar peça em OS.

### Funcionalidades avançadas (Fase 2)
- Histórico de movimentação; peças mais utilizadas e com maior margem; custo total em estoque.

### Novo nesta rodada
- **Previsão de reposição baseada em consumo histórico** (média de uso das últimas semanas), sugerindo quando comprar antes de zerar — mais útil que um alerta de mínimo estático, que reage tarde demais se o consumo acelerou.
- **Lista de compra consolidada.** Agrupa automaticamente todas as peças abaixo do mínimo em uma lista única, pronta para enviar ao fornecedor — pequena economia de tempo, mas recorrente (toda semana), o que faz o esforço valer a pena.
- **Histórico de preço de compra por fornecedor.** Sinaliza quando o custo de uma peça subiu, para o dono decidir se precisa reajustar o preço de venda — conecta diretamente com a promessa de "margem real" do módulo financeiro.

### Cautela
Um módulo de RMA (devolução de peça defeituosa ao fornecedor) é útil, mas é Fase 3 — não é algo que trava a operação diária no MVP, e adiciona um fluxo de estados extra que compete por atenção de desenvolvimento com módulos de maior impacto agora.

---

## 8. Financeiro *(faturamento em Fase 1, margem e rentabilidade em Fase 2)*

### Objetivo
Mostrar "quanto sobrou", não só "quanto entrou" — o diferencial central de modelagem do produto.

### Funcionalidades essenciais (Fase 1)
- Faturamento; receita por período; transações; pagamentos pendentes/concluídos; ticket médio.

### Funcionalidades avançadas (Fase 2)
- Lucro bruto; custo de peças; receita por serviço; serviços mais lucrativos; margem média por serviço; relatórios exportáveis.

### Novo nesta rodada
- **Comissão automática por técnico**, calculada por serviço concluído (percentual configurável sobre mão de obra, separado do custo de peça — mantendo a separação estrutural do módulo 6). Vários concorrentes pesquisados já oferecem isso; para lojas com equipe (não só autônomos), é quase paridade, não diferencial — mas sem isso, a TechPro fica atrás mesmo nos itens básicos para esse segmento de público.
- **Projeção simples de fluxo de caixa**: soma de orçamentos já aprovados aguardando pagamento + valor esperado dos agendamentos dos próximos dias. Não é contabilidade avançada, é uma visão de "quanto está para entrar", que a persona (que não tem visão clara da semana) valoriza diretamente.

### Cautela
**Split de pagamento** (parte na entrada, parte na entrega) é comum no setor, mas mexe com conciliação financeira e pode gerar confusão de relatório se não for bem modelado desde o início. Recomendo Fase 3, com um desenho cuidadoso de como isso aparece nos relatórios de margem — não implementar como um campo solto.

---

## 9. Comunicação *(essenciais em Fase 1, avançado em Fase 2-3)*

### Objetivo
Reduzir a dependência de responder manualmente "qual o status do meu aparelho" pelo WhatsApp — a dor mais citada da persona.

### Funcionalidades essenciais (Fase 1)
- Canais: WhatsApp e e-mail. Mensagens essenciais: confirmação/lembrete de agendamento, OS criada, orçamento disponível/aprovado/recusado, reparo concluído, pronto para retirada.

### Funcionalidades avançadas (Fase 2)
- Templates editáveis por evento e por loja; pedido de avaliação; campanhas promocionais; reativação de clientes inativos.

### Novo nesta rodada
- **Central de mensagens unificada.** Um inbox dentro do sistema mostrando, por cliente, todo o histórico de notificações automáticas enviadas — não substitui o WhatsApp do cliente, mas dá ao lojista (e a você, se precisar dar suporte) visibilidade do que já foi comunicado, sem precisar abrir o WhatsApp da loja e procurar a conversa.
- **Resposta automática a perguntas frequentes.** Um bot simples que reconhece um padrão de mensagem recebida (ex.: "status", "prazo", "pronto?") e responde automaticamente com o status atual da OS mais recente daquele número — ataca diretamente a dor mais citada da persona, e nenhum concorrente pesquisado destacou isso como recurso central.

### Cautela
O item acima depende inteiramente de qual API de WhatsApp você decidir usar (oficial vs. não oficial) — isso ainda está em aberto na conversa sobre stack. Um bot de resposta automática é bem mais simples de construir e mais barato de manter sobre a API oficial (que já tem webhook estruturado) do que sobre uma biblioteca não oficial. Vale tratar essa funcionalidade como dependente dessa decisão, não como certa para a Fase 2 até a stack estar fechada.

---

## 10. Avaliações e reputação *(Fase 2)*

### Objetivo
Medir confiança e qualidade percebida, e fechar o loop quando ela falha.

### Funcionalidades essenciais (Fase 2)
- Avaliação por nota e comentário; por serviço e por técnico; histórico de satisfação; NPS.

### Novo nesta rodada
- **Gatilho de solicitação apenas após confirmação de entrega bem-sucedida** — evitar pedir avaliação com o aparelho ainda não retirado (erro comum de automação genérica, que gera avaliação de contexto errado).
- **Fechamento de loop de avaliação negativa.** Quando uma avaliação negativa dispara uma tarefa interna (já previsto), o gestor deve poder marcar como "resolvida" com uma nota de como foi tratada — isso vira um histórico de reputação gerenciada, útil inclusive como prova de qualidade de atendimento se algum dia precisar disso formalmente.

---

## 11. Base de conhecimento técnico *(novo módulo — Fase 3)*

### Por que este módulo existe
É o primeiro passo, barato, em direção ao diferencial de "sugestão de precificação assistida por dados históricos" já apontado como fase futura no branding — mas sem exigir nada de machine learning caro. É estatística simples sobre o próprio histórico de reparos da loja.

### Funcionalidades propostas (Fase 3)
- Registro de "sintoma relatado → causa identificada → serviço realizado" a cada OS concluída (dado que já existe no sistema, só precisa ser agregado).
- Ao abrir uma nova OS com um sintoma já visto antes (ex.: "não liga"), o sistema mostra as causas mais frequentes já identificadas pela própria loja para sintomas parecidos — apoio ao diagnóstico, não substituição do técnico.
- Base disso, no médio prazo: sugestão de faixa de preço para um novo orçamento, baseada em serviços semelhantes já realizados (tempo gasto, peça usada, valor cobrado) — aqui sim entra a "sugestão de precificação assistida" do branding, mas como evolução natural deste módulo, não como projeto de IA separado.

### Cautela
Este módulo só faz sentido depois de a loja já ter um volume razoável de OS concluídas registradas — não adianta construir isso antes de haver dado histórico suficiente para ser útil. É Fase 3 por natureza, não por enfeite.

---

## 12. Dashboard e indicadores *(essencial na Fase 1, avançado em Fase 2-3)*

### Objetivo
Responder perguntas de negócio, não só exibir números.

### Funcionalidades essenciais (Fase 1)
- Ordens de serviço abertas; agendamentos do dia; serviços em atraso; aparelhos em reparo; serviços prontos para retirada; faturamento do mês.

### Funcionalidades avançadas (Fase 2-3)
- Lucro estimado; custo de peças; peças com estoque baixo; clientes novos/recorrentes; avaliação média; serviços mais solicitados; taxa de aprovação de orçamentos; tempo médio de conclusão.

### Novo nesta rodada
- **"Radar do dia"**: um bloco fixo no topo do dashboard priorizando o que precisa de atenção agora (OS atrasada, peça que chegou e libera um reparo parado, orçamento pendente de aprovação há mais de X dias) — em vez de só KPIs estáticos, que o dono olha e não sabe o que fazer com eles.
- **Comparativo automático mês atual vs. mês anterior** nos indicadores principais — transforma número absoluto em tendência, sem exigir que o dono monte essa comparação manualmente.
- **Produtividade e qualidade por técnico** (Fase 3): tempo médio de reparo e taxa de retrabalho (reparo que voltou por não ter sido bem resolvido) por técnico — nenhum concorrente pesquisado destacou taxa de retrabalho como métrica, e é exatamente o tipo de dado que só existe porque o produto já registra tudo no Kanban.

---

## 13. Configurações e equipe *(Fase 1 básico, refinamento contínuo)*

### Funcionalidades essenciais (Fase 1)
- Dados da loja (nome, logo, contatos, horários, políticas); conta do usuário; notificações básicas.

### Funcionalidades avançadas (Fase 2)
- Equipe: adição de membros, funções e permissões (atendente, técnico, gestor não acessam os mesmos dados); histórico de ações.


---

## 14. Conformidade e dados (LGPD operacionalizada) *(Fase 1 como requisito estrutural, funcionalidades visíveis em Fase 2)*

### Por que este módulo existe
O documento de branding já assumiu compromissos de LGPD como diferencial comercial (seção 7 e 5.2). Este módulo é onde esses compromissos viram funcionalidade concreta, não só texto de política de privacidade.

### Funcionalidades essenciais (Fase 1 — estrutural, não visível ao usuário)
- Isolamento de dados entre empresas (cada assistência só acessa seus próprios clientes finais).

### Funcionalidades avançadas (Fase 2 — visíveis ao usuário)
- **Exportação de dados do cliente** sob solicitação (um botão "exportar meus dados", operacionalizando o direito de portabilidade já prometido).
- **Exclusão de dados do cliente final** mediante solicitação, com tratamento definido para não quebrar o histórico de outras OS já concluídas.
- Consentimento explícito e separado para comunicação de marketing/reativação (diferente da notificação operacional de uma OS em andamento).

### Cautela
Não adianta prometer isso na landing page (como já sugerido no branding) sem essas duas funcionalidades existirem de fato até a Fase 2 — o risco reputacional de prometer e não entregar é maior do que o de não ter prometido nada.

---

## 15. Administração SaaS *(Fase 4)*

### Funcionalidades essenciais
- Overview de empresas; usuários ativos; planos e assinaturas; MRR/ARR; churn; trial conversion; empresas inadimplentes; uso por empresa; limites por plano; suporte; logs administrativos; gestão de trial; bloqueio/suspensão de workspace.

### Novo nesta rodada
- **Alerta de uso anômalo**: empresa gerando volume de OS muito acima do esperado para o plano contratado — sinal tanto de oportunidade de upgrade quanto de possível abuso/conta compartilhada indevidamente.

---

## Resumo de novidades desta rodada, por fase

| Fase | Novidades adicionadas |
|---|---|
| **Fase 1** | Onboarding guiado com wizard e checklist de ativação; dados de exemplo removíveis; conta vinculada família/empresa (base de dados); estrutura de isolamento LGPD |
| **Fase 2** | App/Portal do técnico; linha do tempo visual da OS; aprovação de orçamento item a item; evidência fotográfica no diagnóstico; QR/link compartilhável; estimativa de prazo dinâmica; fila de espera de agendamento; bloqueio por indisponibilidade de peça; SLA visual no Kanban; reatribuição de técnico com histórico; importação de contatos; indicador de risco de inadimplência; kits de serviço; peça compatível/equivalente; previsão de reposição de estoque; lista de compra consolidada; histórico de preço por fornecedor; comissão automática por técnico; projeção de fluxo de caixa; central de mensagens unificada; resposta automática a perguntas frequentes (dependente da API de WhatsApp escolhida); fechamento de loop de avaliação negativa; exportação e exclusão de dados (LGPD); especialidade por técnico |
| **Fase 3** | Chat assíncrono no portal; serviço com dependência; RMA de peça; split de pagamento; base de conhecimento técnico / diagnóstico assistido; produtividade e retrabalho por técnico; atribuição sugerida por especialidade |
| **Fase 4** | Alerta de uso anômalo no admin SaaS |

**Nota final de calibração:** esta lista é deliberadamente maior do que o que cabe em qualquer sprint único. O valor dela não é "construir tudo", é ter, quando chegar a hora de detalhar cada fase tecnicamente, um banco de ideias já filtrado por criticidade — cada item aqui já foi avaliado quanto a valor real vs. custo de manutenção para um desenvolvedor solo, não apenas listado por completude.