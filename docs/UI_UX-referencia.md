# Guia Estético de UI/UX — Referência: usehandle.ai

> Revisado a partir dos prints reais enviados. Tudo abaixo é observação direta da página, não mais inferência de gênero.

## 1. Paleta de cores (confirmada nos prints)

| Uso | Cor aproximada | Observação |
|---|---|---|
| Fundo principal | Branco (`#FFFFFF`) | Predominante em ~70% da página |
| Fundo alternado (seções de destaque) | Cinza muito claro (`#F5F5F6`–`#F7F7F8`) | Usado para alternar ritmo: hero branco → problema cinza → solução branco → agentes cinza → FAQ cinza |
| Texto de título | Navy quase preto (`#14162B`–`#1A1B2E`) | Não é preto puro — tem leve matiz azulado, mais suave que `#000000` |
| Texto de corpo | Cinza médio (`#6B7280`–`#8B8D98`) | Contraste baixo proposital, o peso visual fica todo no título |
| Botão CTA | Preto sólido, texto branco, formato pílula | Único botão de ação em toda a página — nunca há dois CTAs competindo na mesma dobra |
| Tag/badge de seção | Texto rosa/vermelho (`#E8536B`-ish) sobre fundo rosa claríssimo | Pequeno, uppercase, letras espaçadas — usado antes de cada headline de seção ("CURRENT REALITY", "FEATURES", "AGENT 01", "USE CASES") |
| Check / X de comparação | Verde para ✓, vermelho/rosa para ✗ | Nas listas "processo manual vs. com Handle" |
| **Gradiente decorativo** | Laranja → rosa → roxo → azul, bem saturado | **É o elemento de assinatura visual da página** — aparece como um blur/glow colorido *atrás* dos mockups de produto (dashboard), nunca em texto ou fundo de seção inteira. Cria contraste vibrante numa página que, fora isso, é quase monocromática. |
| Headline bicolor | Navy + gradiente laranja-rosa na mesma frase | Ex.: "**Quoting** *Agent*" — a primeira palavra em navy sólido, a segunda em gradiente. Usado em todo título de agente/seção de destaque. |

## 2. Tipografia

- Sans-serif geométrico moderno, provavelmente da família Inter/Söhne/Neue Montreal — peso 600-700 nos títulos, 400-450 no corpo.
- Títulos grandes e confiantes (hero: ~56-64px), subtítulo bem menor e mais leve (~16-18px) — contraste de peso forte entre os dois.
- Tags de seção: fonte minúscula (~11px), uppercase, tracking largo, sempre coloridas (rosa/vermelho) — funcionam como "olho mágico" antes de cada bloco novo.
- Números de estatística: grandes, bold, alguns em cor de destaque (laranja/âmbar) — tratados quase como ilustração, não só como dado.

## 3. Layout e espaçamento

- Coluna de conteúdo centralizada, largura confortável (~1100-1200px), bastante margem lateral em telas maiores.
- Seções separadas por mudança de fundo (branco/cinza), não por linhas divisórias — o espaço em branco generoso entre blocos (~80-120px) é o que cria a sensação "premium/calma".
- Cards de produto (dashboard mockups) sempre dentro de um card arredondado (raio ~12-16px), borda de 1px bem sutil, sombra quase imperceptível — a profundidade vem do contraste de fundo, não de sombra pesada.
- Ícones de linha fina (stroke, não preenchidos), um por card, monocromáticos em cinza/navy — usados nos grids de "problema" e "casos de uso" (linha em queda, gráfico de barras, ondas concêntricas, formas 3D wireframe).

## 4. Componentes-chave observados

| Componente | Onde aparece | Papel |
|---|---|---|
| Badge de autoridade | Topo do hero ("Backed by a16z") | Credibilidade antes do produto |
| Carrossel de logos de clientes | Logo abaixo do hero | Prova social, baixo contraste (logos acinzentados) |
| Barra de menções de imprensa | Logo abaixo do carrossel | Texto pequeno, links separados por barra vertical |
| Contador de estatística | Seção "impacto" | Números grandes, alguns coloridos, label pequeno embaixo |
| Card ✗ / ✓ | Seção de cada agente | Comparação direta processo manual vs. com produto |
| **Mockup com glow gradiente atrás** | Toda screenshot de produto | Assinatura visual: dashboard limpo (branco) sobre um blur colorido — é o único lugar da página onde a cor "explode" |
| Stepper numerado horizontal | Fluxo de cada agente | Etapas do processo automatizado, com linha conectora e estado ("Pending") |
| Card de "camada" com ilustração abstrata | Seção "Two layers" | Ondas concêntricas (SOR) vs. formas 3D wireframe (Agentes) — ilustração conceitual, não fotografia nem ícone literal |
| Grid horizontal scrollável de casos de uso | Seção "Use Cases" | Ícone de linha único por card + título + descrição curta |
| Grid de integrações com categoria acima do logo | Seção "Integrations" | Pequeno label cinza ("CRM", "Email") acima de cada logo/nome |
| Acordeão de FAQ | Final da página | Pergunta em navy + ícone "+", divisórias finas, fundo cinza claro |

## 5. Tom de copy

- Frases curtas, sem jargão de marketing vazio.
- Números concretos substituindo adjetivo ("de 4 dias para 3 horas", "99% de precisão").
- Um único CTA repetido ("Contact us") — nunca CTA duplo competindo.
- Headline de cada seção sempre precedida por uma tag pequena e colorida que nomeia a seção.

## 6. Aplicação sugerida à TechPro

- **O glow gradiente atrás de mockups** é a técnica mais transferível e de maior impacto: mostrar o Kanban ou o dashboard financeiro da TechPro como um card branco limpo sobre um blur colorido, numa página por padrão monocromática — cria um ponto focal forte sem precisar colorir a página inteira.
- **Headline bicolor** (navy + gradiente) funciona bem para nomear os diferenciais reais definidos no branding (seção 5.2): ex. "**Margem** *real*", "**Aprovação** *auditável*".
- **Card ✗/✓** encaixa diretamente na seção de problema já escrita no branding ("WhatsApp, papel e planilha" ✗ vs. "fluxo digital rastreável" ✓).
- **Tag pequena colorida antes de cada headline de seção** é barata de implementar e dá o mesmo ritmo editorial da página de referência — vale adotar como padrão para todas as seções da landing page da TechPro.
- **Um único CTA repetido**, reforçando o que já foi observado na v1 deste guia: reconsiderar o CTA duplo (principal + secundário) que a seção 18 do documento de branding ainda sugere.