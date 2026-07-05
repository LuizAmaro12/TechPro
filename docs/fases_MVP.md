# TechPro — Roadmap por fases do MVP

Este roadmap consolida o escopo de `modules.md` em fases de entrega. A regra de corte é simples: a Fase 1 precisa entregar uma operação real de assistência técnica de ponta a ponta, sem depender de suporte manual do fundador para cada cliente novo. As fases seguintes ampliam margem, automação, reputação, inteligência operacional e maturidade SaaS.

## Princípios de priorização

- **Operação antes de sofisticação:** OS, agenda, Kanban, estoque, cliente, comunicação e financeiro básico precisam funcionar juntos antes de relatórios avançados ou automações comerciais.
- **Autoatendimento desde o início:** como a operação será tocada sem equipe de implantação, onboarding guiado não é enfeite; é redutor direto de suporte.
- **Separação estrutural entre serviço e peça:** serviço é mão de obra; peça tem custo, estoque e margem. Misturar os dois agora barateia o MVP, mas encarece todo o financeiro depois.
- **LGPD e multiempresa como fundação:** isolamento de dados entre empresas não é feature futura. Sem isso, o produto não é SaaS confiável.
- **Prometer apenas o que existe:** landing page, discurso comercial e demonstração precisam refletir a fase real do produto. Roadmap pode ser público; falsa maturidade destrói credibilidade.

---

## Fase 1 — MVP operacional vendável

### Objetivo da fase

Permitir que uma assistência técnica configure a loja, receba agendamentos, transforme atendimentos em OS, acompanhe o reparo no Kanban, controle peças usadas, comunique o cliente e enxergue os indicadores básicos do dia sem depender de planilhas.

### Escopo por módulo

#### 0. Onboarding e configuração inicial

- Wizard inicial: dados da loja, horários de funcionamento, primeiros serviços, primeiras peças e convite opcional de equipe.
- Sugestões pré-preenchidas de serviços comuns, editáveis pela loja.
- Dados de exemplo removíveis para demonstrar o fluxo sem exigir cadastro manual completo.
- Checklist de ativação com progresso visível.

#### 1. Portal do cliente

- Agendamento progressivo: identificação, aparelho, problema, serviço, data/horário, anexos e confirmação.
- Acompanhamento de status da OS.
- Visualização de orçamento e aprovação/recusa simples.
- Histórico de serviços anteriores do próprio cliente.

#### 2. Agendamento e calendário

- Rota personalizada de agendamento.
- Visualização por dia, semana e mês.
- Criação manual e via portal do cliente.
- Reagendamento, cancelamento, check-in e conversão automática em OS.
- Bloqueio de horários.
- Lembretes automáticos.
- Capacidade por tipo de serviço desde o início.

#### 3. Ordem de serviço e Kanban

- Etapas essenciais: Agendado, Check-in realizado, Na fila, Em diagnóstico, Aguardando aprovação, Aguardando peça, Em reparo, Em teste, Pronto para retirada, Entregue e Cancelado.
- Arrastar e soltar entre etapas.
- Responsável técnico.
- Prioridade.
- Prazo estimado.
- Status de pagamento.
- Status de aprovação de orçamento.
- Criação automática de OS a partir do agendamento.

#### 5. Clientes / CRM básico

- Cadastro, listagem e filtros básicos.
- Histórico de aparelhos, OS, pagamentos e mensagens por cliente.
- Estrutura de dados preparada para conta vinculada família/empresa. A interface completa pode ficar para a fase seguinte se ameaçar o prazo do MVP.

#### 6. Serviços e peças / catálogo

- Cadastro de serviços com preço base, duração estimada, categoria, prazo médio e marcação de disponibilidade para agendamento online.
- Cadastro separado de peças normalmente utilizadas por serviço.
- Checklist padrão por serviço, quando aplicável.
- Marcação de serviços que exigem diagnóstico.

#### 7. Estoque básico

- Cadastro de peças.
- Quantidade disponível.
- Custo unitário e preço de venda.
- Fornecedor.
- Estoque mínimo.
- Alerta de estoque baixo.
- Baixa automática ao usar peça em OS.

#### 8. Financeiro básico

- Faturamento.
- Receita por período.
- Transações.
- Pagamentos pendentes e concluídos.
- Ticket médio.
- Base de dados já preparada para separar receita de serviço, custo de peça e margem futura.

#### 9. Comunicação essencial

- Canais: WhatsApp e e-mail.
- Mensagens essenciais: confirmação e lembrete de agendamento, OS criada, orçamento disponível, orçamento aprovado/recusado, reparo concluído e pronto para retirada.
- Registro mínimo das mensagens enviadas para auditoria operacional.

#### 12. Dashboard inicial

- OS abertas.
- Agendamentos do dia.
- Serviços em atraso.
- Aparelhos em reparo.
- Serviços prontos para retirada.
- Faturamento do mês.

#### 13. Configurações e equipe básica

- Dados da loja: nome, logo, contatos, horários e políticas.
- Conta do usuário.
- Preferências básicas de notificação.
- Convite de equipe pode existir no onboarding, mas permissões granulares entram na Fase 2.

#### 14. Conformidade e dados

- Isolamento de dados entre empresas.
- Base mínima de consentimento para comunicações operacionais.
- Tratamento estrutural para separar dados de empresa, usuários internos e clientes finais.

### Fora do MVP, mesmo que pareça desejável

- App/Portal do técnico dedicado.
- Aprovação item a item de orçamento.
- Fotos antes/depois e evidência fotográfica completa no diagnóstico.
- Timeline visual detalhada da OS.
- Templates editáveis por loja.
- Campanhas, reativação e segmentação.
- Relatórios avançados e exportações completas.
- Automações de WhatsApp com resposta a perguntas frequentes.
- Permissões granulares por cargo.
- Administração SaaS completa.

### Critério de conclusão da Fase 1

A Fase 1 está pronta quando uma loja consegue se cadastrar, configurar serviços e peças, receber um agendamento, gerar uma OS, mover a OS até a entrega, baixar estoque, registrar pagamento, notificar o cliente e visualizar o estado básico da operação no dashboard sem intervenção manual do fundador.

---

## Fase 2 — Profundidade operacional e redução de suporte

### Objetivo da fase

Transformar o MVP em ferramenta de operação diária mais robusta: menos perguntas manuais, mais rastreabilidade, melhor margem, mais controle de equipe e mais transparência para o cliente.

### Entregas principais

#### Portal do cliente

- Linha do tempo visual da OS.
- Aprovação de orçamento item a item.
- Evidência fotográfica no diagnóstico.
- Link ou QR code compartilhável para acompanhamento.
- Estimativa de prazo dinâmica conforme mudança de status e disponibilidade de peça.

#### Agendamento

- Fila de espera quando não houver horário disponível.
- Sinalização ou bloqueio por indisponibilidade conhecida de peça.
- Histórico de comparecimento por cliente.

#### OS, Kanban e técnico

- SLA visual por etapa.
- Reatribuição de técnico com histórico e motivo.
- Comentários internos.
- Anexos técnicos.
- Fotos antes/depois.
- Checklist de qualidade por tipo de serviço.
- App/Portal do técnico em versão web responsiva, com lista de OS atribuídas, avançar etapa, registrar peça usada, anexar foto e marcar checklist.

#### Clientes e reputação

- Importação de contatos existentes por planilha/CSV.
- Conta vinculada família/empresa com interface completa.
- Indicador simples de risco de inadimplência com base no histórico interno.
- Avaliação por nota e comentário.
- Avaliação por serviço e por técnico.
- NPS.
- Pedido de avaliação apenas após entrega confirmada.
- Fechamento de loop para avaliação negativa.

#### Catálogo, estoque e financeiro

- Kits de serviço.
- Peça compatível/equivalente para múltiplos modelos.
- Histórico de movimentação de estoque.
- Peças mais utilizadas e com maior margem.
- Custo total em estoque.
- Previsão de reposição por consumo histórico.
- Lista de compra consolidada.
- Histórico de preço de compra por fornecedor.
- Lucro bruto.
- Custo de peças.
- Receita por serviço.
- Serviços mais lucrativos.
- Margem média por serviço.
- Comissão automática por técnico.
- Projeção simples de fluxo de caixa.
- Relatórios exportáveis básicos.

#### Comunicação, configurações e LGPD

- Templates editáveis por evento e por loja.
- Pedido automático de avaliação.
- Central de mensagens unificada com histórico de notificações automáticas.
- Equipe com funções e permissões: atendente, técnico e gestor.
- Histórico de ações.
- Exportação de dados do cliente sob solicitação.
- Exclusão de dados do cliente final com regra para preservar histórico operacional quando necessário.
- Consentimento separado para marketing e reativação.

### Dependências críticas

- A resposta automática por WhatsApp depende da escolha da API. Não deve ser prometida como entrega certa antes de fechar a stack de integração.
- Permissões e histórico de ações precisam ser desenhados junto com LGPD e auditoria; se forem adicionados como remendo, tendem a vazar responsabilidade entre telas.
- Portal do técnico deve ser web responsivo primeiro. Aplicativo nativo aumentaria custo de manutenção sem necessidade validada.

### Critério de conclusão da Fase 2

A fase está pronta quando a loja consegue operar equipe, aprovar orçamentos com mais detalhe, reduzir perguntas recorrentes de clientes, medir satisfação, controlar margem real e responder solicitações básicas de dados pessoais sem processo manual improvisado.

---

## Fase 3 — Automação comercial, conhecimento e inteligência operacional

### Objetivo da fase

Usar o histórico acumulado na Fase 1 e Fase 2 para melhorar venda, diagnóstico, produtividade e retenção. Esta fase depende de dados reais; construir antes disso criaria telas vazias ou conclusões fracas.

### Entregas principais

- Chat assíncrono no portal do cliente, se o uso real mostrar que clientes aceitariam conversar dentro do portal em vez de apenas responder pelo WhatsApp.
- Campanhas e segmentação de clientes.
- Reativação de clientes inativos.
- Automações de comunicação mais sofisticadas.
- Controle de garantia.
- Templates de mensagem mais avançados.
- Serviço com dependência, como diagnóstico obrigatório antes de certos orçamentos.
- RMA de peça defeituosa ao fornecedor.
- Split de pagamento, com desenho correto de conciliação e margem.
- Base de conhecimento técnico: sintoma relatado, causa identificada e serviço realizado.
- Apoio ao diagnóstico com causas frequentes baseadas no histórico da própria loja.
- Sugestão inicial de faixa de preço baseada em reparos semelhantes já concluídos.
- Registro de tempo gasto por etapa.
- Produtividade por técnico.
- Taxa de retrabalho por técnico.
- Atribuição sugerida por especialidade.
- Relatórios avançados e exportação completa.

### Critério de conclusão da Fase 3

A fase está pronta quando o produto deixa de apenas registrar a operação e passa a sugerir próximas ações com base em histórico: quem reativar, qual causa é mais provável, qual preço tende a fazer sentido, qual técnico está sobrecarregado e onde há retrabalho.

---

## Fase 4 — Plataforma SaaS, escala e integrações avançadas

### Objetivo da fase

Preparar a TechPro para operar como SaaS multiempresa em escala, com controle comercial, planos, limites, suporte, administração interna e integrações externas mais sensíveis.

### Entregas principais

- Administração SaaS completa.
- Overview de empresas.
- Usuários ativos.
- Planos e assinaturas.
- MRR, ARR, churn e trial conversion.
- Empresas inadimplentes.
- Uso por empresa.
- Limites por plano.
- Suporte administrativo.
- Logs administrativos.
- Gestão de trial.
- Bloqueio e suspensão de workspace.
- Multiunidade.
- API pública.
- Integrações fiscais avançadas, incluindo NF-e/NFS-e completas, sujeitas a validação jurídica municipal.
- Alerta de uso anômalo por empresa.
- Sugestão de precificação assistida por dados em versão madura.

### Critério de conclusão da Fase 4

A fase está pronta quando a TechPro consegue gerir várias empresas, planos, limites, suporte e integrações críticas com confiabilidade operacional e sem depender de acesso direto ao banco para resolver problemas comuns.

---

## Riscos de escopo e comunicação

- **Risco de MVP inchado:** se tudo que está em `modules.md` entrar como obrigatório na Fase 1, o produto demora demais para validar venda real. O MVP deve provar o fluxo operacional completo, não a visão final.
- **Risco de promessa comercial excessiva:** não comunicar Fase 3 ou Fase 4 como se já existisse. Use roadmap público, não falsa disponibilidade.
- **Risco de margem mal modelada:** financeiro sem separação entre mão de obra, peça e custo de estoque enfraquece o diferencial central do produto.
- **Risco de suporte manual:** sem onboarding guiado, dados de exemplo e checklist de ativação, cada novo cliente vira implantação manual.
- **Risco de WhatsApp mal escolhido:** automações avançadas dependem da API adotada. A decisão técnica impacta latência, custo, estabilidade, suporte a webhook e risco de bloqueio.
- **Risco de LGPD tardia:** isolamento multiempresa e consentimento não podem ser adicionados apenas no fim sem retrabalho de modelo de dados, permissões e auditoria.

## Ordem recomendada dentro da Fase 1

1. Fundação SaaS: empresas, usuários, isolamento de dados, autenticação e configurações básicas.
2. Catálogo: serviços, peças, relação serviço-peça e capacidade por tipo de serviço.
3. Clientes e aparelhos.
4. Agenda e portal de agendamento.
5. OS e Kanban.
6. Estoque com baixa automática por OS.
7. Orçamento, aprovação simples e pagamento básico.
8. Comunicação essencial.
9. Dashboard inicial.
10. Onboarding guiado e dados de exemplo, refinados sobre os fluxos reais já implementados.

**Nota crítica:** onboarding aparece como módulo de Fase 1, mas não deve ser o primeiro código do produto. Ele precisa guiar fluxos reais; portanto, a melhor ordem é construir a fundação operacional e depois encapsulá-la no wizard de ativação.
