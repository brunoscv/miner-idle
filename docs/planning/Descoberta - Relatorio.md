# Descoberta — Relatório

> Fase 1 do planejamento de **Miners Idle**, escrita em 2026-10-20. **Só leitura:** nada do projeto, dos documentos
> originais ou dos projetos irmãos foi alterado. Convenção de rótulos usada em todos os documentos novos:
> **[DOC]** está escrito nos documentos originais · **[INT]** é interpretação minha · **[REC]** é recomendação minha,
> ainda sem decisão do Bruno.

## 1. Inventário do que foi inspecionado

### 1.1 Este projeto (`E:\Fabrica\Unity\miners-idle`)

| Item | Estado | Como foi inspecionado |
|---|---|---|
| `docs/GDD_Idle_Miner.docx` (1 MB) | Lido por inteiro (15 seções, ~540 parágrafos/linhas de tabela) | XML do `.docx` extraído para texto, com tabelas |
| `docs/Roadmap_MVP_Idle_Miner_Unity_CSharp.docx` (46 KB) | Lido por inteiro (10 seções) | Idem |
| Imagem embutida no GDD (`word/media/image1.png`, 605×1008) | **Vista** (única referência visual do projeto) | Aberta como imagem |
| Miniatura do Roadmap (`docProps/thumbnail.jpeg`) | Página em branco, sem informação | Aberta como imagem |
| Raiz do projeto | Contém **apenas** `.git` e `docs/` | `ls` |
| Git | 1 commit (`5651e96 adding first docs`), árvore limpa, branch `main` | `git log/status` |
| Projeto Unity | **Não existe.** Sem `Assets/`, `Packages/`, `ProjectSettings/`, `.gitignore`, `.gitattributes`, `CLAUDE.md`, `README.md` | `ls` |

**Arquivos citados.** O Roadmap (§10) lista como fontes de escopo `GDD.txt` ("especificação original das
mecânicas") e `Designer (2).png` ("referência visual da tela mobile"). **Atualização 2026-10-21:** o Bruno adicionou `docs/Designer (2).png`; conferi e é o mesmo mockup embutido no GDD. O `GDD.txt` original **continua não visto**: se ele contém
detalhes que o GDD consolidado omitiu, eles estão fora desta análise.

### 1.2 Projetos anteriores (acessíveis no mesmo disco, lidos em modo somente leitura)

| Projeto | O que foi lido |
|---|---|
| **Underworld Towers** (`E:\Fabrica\Unity\underworld-towers`) | `CLAUDE.md`, `README.md` (início), `docs/research/Lições do we-had-a-plan.md`, `docs/research/Boas práticas — Processo e Unity.md`, `docs/planning/Decisões em aberto.md`, `Roteiro — Marcos.md` (início), `Custos — Grátis e pago.md`, `Assets — Fontes e licenças.md`, `CREDITS.md`, `.gitignore`, `.gitattributes`, `Packages/manifest.json`, `ProjectVersion.txt`, árvore de `docs/`, `tools/`, `Assets/_Project` |
| **We Had a Plan** (`E:\Fabrica\Unity\we-had-a-plan`) | `CLAUDE.md` (processo de testes/CI), árvore de `docs/` e `tools/`, `ProjectVersion.txt`. O README (~490 KB) **não foi lido na íntegra**; o que se sabe dele vem das *Lições* já escritas no Underworld Towers |
| Memória do Claude desses dois projetos | Índices e as notas de feedback/usuário (`user-bruno`, `feedback-how-to-test`, `feedback-test-shortcuts`, `feedback-batch-sync-back`, `feedback_dev_flow`, `feedback_board_assignee`, `reference_github_board`) |

**Limites.** Não foram lidos os documentos de design de combate/3D desses jogos (não se aplicam a um idle 2D) nem
o código. O we-had-a-plan é do Thiago e o Underworld Towers é do Bruno; ambos continuam **só leitura** para este
projeto (regra herdada). Outras configurações do ambiente: Unity instalado em `C:\Program Files\Unity\Hub\Editor\6000.6.0f1`
com módulos **WebGL e Windows apenas — sem Android Build Support**; Git LFS 3.7.1 e GitHub CLI instalados; Android SDK
platform-tools no PATH; JDK 17 presente; **Aseprite, LibreSprite e Krita não encontrados** no PATH nem em `Program Files`.

## 2. O que os documentos exigem explicitamente

Lista completa, com IDs rastreáveis, em [Requisitos e Restrições](../design/Requisitos%20e%20Restricoes.md). Resumo [DOC]:

- **Produto:** idle/clicker de mineração, mobile, **orientação vertical**, pixel art, MVP com **uma tela principal**.
- **Jogo:** um anão minerador quebra **cinco cristais visíveis** por lote, coleta *lascas* (única moeda) e compra melhorias
  numa loja; mineração **automática + toque**, crítico (5%, ×2), combo, penalidade de −50% após 100 quebras, boost de
  anúncio ×2,5 por 30 s, estagiário, "Letrado" que libera a passagem, Picareta Dupla; **10 cavernas**, a última vulcânica.
- **Economia:** fórmulas fechadas para vida, recompensa, dano e custos (GDD §3–§6). "As fórmulas são requisitos do MVP."
- **Técnico (Roadmap §2):** Unity LTS + C# + URP 2D, Unity UI + TextMeshPro, ScriptableObjects, JSON versionado em
  `persistentDataPath`, Unity Test Framework, Git + LFS, AudioSource/AudioMixer, build Android (APK dev, depois AAB), `double`
  para números, relógio UTC para o boost, Safe Area.
- **Processo (Roadmap):** 11 semanas, ~420–460 h, 1 desenvolvedor; marcos A–D; testes Edit/Play/dispositivo/estatístico; bugs P0–P2;
  registro obrigatório de licença e origem de todo asset (inclusive IA); playtest com 5–8 pessoas.
- **Fora do escopo [DOC]:** prestígio, dinheiro real/rede de anúncios real, multiplayer/ranking/missões diárias/eventos, inventário,
  múltiplas moedas, narrativa/chefes, servidor/nuvem.

## 3. O que a referência visual mostra

Imagem única (mockup de celular, 605×1008) — descrição do que **está** nela:

1. **HUD superior:** 4 painéis com ícone — *Lascas* `1,234.00` (cristal roxo), *Dano* `12.50` (picareta), *Caverna* `3.00` (ícone de
   montanha/mina), *Velocidade* `2.30s` (cronômetro) — mais a engrenagem de configurações.
2. **Lateral direita:** botão **Loja** (barraca de feira) com **selo vermelho de notificação** e botão **AD** (raio amarelo).
3. **Centro:** anão de barba ruiva, capacete amarelo com lanterna e picareta, **de frente**, no topo de um corredor vertical;
   **cinco cristais empilhados em coluna**, cada um **de uma cor diferente** (branco, âmbar, azul-violeta, magenta, vermelho).
4. **Cenário:** mina de madeira (vigas, bancada, lanternas acesas), faixa vermelha com picaretas cruzadas, placa "MINA PROFUNDA",
   boca da caverna com céu e montanhas ao fundo, cristais azuis nas paredes, **cratera de lava** no canto inferior direito.
5. **Rodapé:** botão âmbar "PASSAR DE CAVERNA" com ícone de livro (estado habilitado).
6. **Textos em português**, mas **números em formato en-US** (`1,234.00`).

**Observação técnica [INT]:** a imagem é **arte de conceito gerada por IA** com aparência de pixel art: a grade de pixels não é
consistente (tamanhos de "pixel" diferentes entre elementos, gradientes e brilhos suaves). Serve como **direção de clima, cor
e composição**, não como especificação de sprite. Isso bate com a ressalva do próprio Roadmap (§5.2).

**O que o mockup NÃO mostra:** estagiário, barra de vida dos cristais, combo/crítico, selo de penalidade, indicador de boost,
loja aberta, modal de AD, lote com cristais em posições aleatórias, Picareta Dupla, estados desabilitados.

## 4. Experiência pretendida e identidade visual

- **Promessa central [DOC]:** "transformar cada toque e cada melhoria em uma sensação clara de poder crescente, enquanto a mina muda
  visualmente e conduz o jogador até uma caverna vulcânica."
- **Sessão [DOC]:** curta e recorrente, uma mão, mineração seguindo durante loja/modal.
- **Visual [DOC]:** pixel art, "linguagem industrial de ficção científica e mineração", inspirada em *Deep Rock Galactic* (execução
  original), painéis metálicos, amarelo de segurança, paleta definida (GDD §8.2). **Choque com o mockup:** ver §8, item C-05.
- **Áudio [DOC]:** exigência mínima só para golpe e crítico; combo/quebra/coleta opcionais; sem música especificada.

## 5. Loop de jogo esperado [DOC, GDD §2]

Atacar (auto + toque) → Quebrar (vibração, barra zera) → Coletar (lasca voa até o HUD) → Avançar (minerador vai ao próximo cristal;
após 5, novo lote) → Melhorar (loja) → Descer (comprar o Letrado da caverna atual habilita "Passar de Caverna").

## 6. Restrições técnicas já documentadas

Ver §2 e [Arquitetura Técnica](Arquitetura%20Tecnica.md). Restrições do **ambiente** (não documentadas, verificadas por mim): sem módulo
Android no Unity instalado; sem ferramenta de pixel art instalada; nenhum aparelho Android confirmado (a confirmar com o Bruno).

## 7. Decisões já tomadas × em aberto

Tabela completa em [Decisões e Riscos](Decisoes%20e%20Riscos.md). Em uma frase:

- **Tomadas [DOC]:** plataforma, stack, estrutura de dados, fórmulas, escopo, regras do estagiário/Letrado/AD/penalidade, os 10 temas de
  caverna, a paleta-âncora e o que está fora do escopo.
- **Em aberto:** a economia numérica (ver §8, C-01 — **bloqueia o desenvolvimento**), nome do produto, direção visual (industrial × fantasia),
  progressão offline, linha "Crítico", mecânica da "parede", regras do toque, versão exata da Unity, idioma/formatos, fonte de arte,
  rastreador de tarefas, disponibilidade de horas.

## 8. Contradições, ambiguidades, lacunas e riscos encontrados

Todos [INT] sobre os textos originais, com a seção de origem.

| ID | Tipo | Achado | Gravidade |
|---|---|---|---|
| **C-01** *(2026-10-21: rebaixado a **observação**; o Bruno decidiu balancear ao longo do desenvolvimento, e o jogo não precisa chegar à caverna 10 agora — D-01)* | Contradição numérica | **A economia do GDD, como escrita, não fecha.** O Letrado de nível *n* custa `10^n` lascas e é obrigatório para passar da caverna *n*, mas cada cristal da caverna *n* rende só `2^(n-1)` (até ×2 com Mineração, ×0,5 de penalidade). Para passar da caverna 9 seriam necessárias 10⁹ lascas com cristais que rendem ≤ 512. Além disso o dano máximo (Picareta 20 + Letrado + Dupla ≈ 1,05 M por golpe) mal cobre a vida da caverna 10 (10¹⁰) mesmo com golpes de 0,5 s, e o custo de Velocidade (`200×3^k`) chega a ~10²³ antes de atingir 0,5 s. Simulação de primeira ordem (§3 de [Economia e Progressão](../design/Economia%20e%20Progressao.md)): com as fórmulas literais, **a caverna 3 só é vencida após ~60–250 h e a 4 não é alcançada em 300–500 h.** | **Bloqueadora** |
| C-02 | Contradição | O escopo (§1.3) e o passo 5 do loop falam em **sete** linhas de melhoria, incluindo **Crítico** e **Pesquisador**; a tabela da economia (§4) tem **seis**, sem Crítico, e o item que libera a passagem se chama **Letrado**. A chance de crítico é fixa em 5% (§3.2). | Alta |
| C-03 | Ambiguidade | Passo 6 do loop: avançar "ao **quebrar a parede** que bloqueia a nova seção". §4.2/§7.3: basta o botão "Passar de Caverna". A parede não aparece em nenhuma outra seção (sem vida, estado ou arte). | Alta |
| C-04 | Ambiguidade | Custo "nível n": o primeiro nível comprado custa `50×4^0 = 50` ou `50×4^1`? "Nível 1 é o valor-base" sugere a primeira compra levar ao nível 2 — qual fórmula usa qual n não está dito. Mesma dúvida em todas as linhas. | Alta |
| C-05 | Contradição visual | Texto: arte **industrial/ficção científica**, painéis metálicos, carvão e aço. Mockup: **mina de fantasia medieval em madeira**, molduras marrom-alaranjadas, anão de barba ruiva. Também: o piso da caverna 1 no GDD é "areia clara"; no mockup é terra marrom. Os cinco cristais do mockup têm **cinco cores** num mesmo cenário, mas o GDD diz **um tipo de cristal por caverna**. | Alta |
| C-06 | Ambiguidade | **Toque:** em qualquer lugar da tela ou só no cristal? Há limite de toques por segundo? O toque pode ser crítico? (Só o "golpe" é citado.) Sem limite, toque a 3/s (0,3 golpe/s) supera o automático (0,1 golpe/s) no início e some no fim (0,5 s → 2 golpes/s): é uma transição natural, mas precisa ser **decidida**, não acidental. | Média |
| C-07 | Ambiguidade | **Velocidade × boost:** o intervalo mínimo é 0,5 s; o boost divide por 2,5. O mínimo vale com boost (0,2 s) ou não? Recarga do AD (30 s) começa na confirmação e dura o mesmo que o boost (30 s): o botão volta exatamente quando o boost acaba, ou seja, ~86% de uptime com 5 s de espera. É intencional? | Média |
| C-08 | Ambiguidade | **Estagiário** (§5): ordem de quebra dentro do lote de 5 com dois mineradores, quem fica sem alvo e como o lote se renova quando um deles termina antes (a matriz de testes da semana 6 existe, mas a **especificação** da máquina de estados não). | Alta |
| C-09 | Lacuna | **Penalidade:** a justificativa de design não está escrita (anti-grind? empurrar para o Letrado?). Pelas fórmulas, vale já a partir da caverna 3 (250 cristais para pagar o Letrado 3, mas a penalidade chega após 100 quebras) — vira a regra, não a exceção. | Média |
| C-10 | Lacuna | **Progressão offline** não é mencionada (nem incluída nem excluída). Em um idle, é decisão obrigatória. O Roadmap só cita "pausa e retomada" e UTC para o boost. | Média |
| C-11 | Ambiguidade | HUD "Nível da mineração" (GDD §7.1) × "Caverna" (mockup) × melhoria "Mineração" (+1% de lascas): três coisas com nome parecido; "duas casas decimais" para um número de caverna (`3.00`) não faz sentido. | Baixa |
| C-12 | Lacuna | **Nome do produto:** "Idle Miner" nos documentos, "Miners Idle" no repositório e no pedido. Define nome do pacote Android. | Baixa |
| C-13 | Contradição | Formatos: textos em pt-BR, números do mockup em en-US; o GDD manda "separador de milhares" sem dizer qual. | Baixa |
| C-14 | Risco de processo | Roadmap põe **toda a arte na semana 9**; a própria nota de risco (§5.2) diz que arte de IA é inconsistente. A decisão de estilo precisa vir **antes** do vertical slice. | Alta |
| C-15 | Risco de ambiente | Roadmap supõe "Unity LTS"; o Unity instalado (6000.6.0f1, usado nos outros dois jogos) **não tem módulo Android**, e **não confirmei** se essa versão é marcada LTS no Hub. | Média |
| C-16 | Lacuna | Sem plano de licenças monetizáveis: "AD simulado" no MVP, mas se haverá anúncios reais, loja de destino (Google Play?) e política de privacidade não estão definidos. | Baixa (MVP) |
| C-17 | Lacuna | Idioma da interface (só pt-BR?) e de documentos; acessibilidade (tremor de tela, vibração, daltonismo — a identidade das cavernas depende de cor) não citados. | Média |
| C-18 | Premissa | 40 h/semana (22+5+4+5+3+1) para 1 desenvolvedor por 11 semanas; o histórico do Bruno (aprendiz de Unity, 2 outros projetos em andamento) torna isso **não validado**. | Alta |

## 9. Premissas a validar

1. O Bruno tem (ou terá) um **aparelho Android físico** para teste (Roadmap, marco A) — e qual modelo/Android.
2. O **Unity 6000.6.0f1** serve (LTS?) e o **Android Build Support** será instalado.
3. O GDD v1.0 é **a especificação de intenção**; os números podem ser recalibrados **desde que a intenção** (progressão por Letrado, cinco
   cristais, passagem por caverna, 10 cavernas) **se preserve**. [Preciso de confirmação — D-01.]
4. O estilo visual definitivo vem do **mockup** (não do texto industrial) — D-05.
5. Ferramenta de pixel art: **Aseprite (pago, ~US$ 20 [verificar preço atual])** ou alternativas gratuitas — D-10.
6. Arte e áudio serão **gratuitos/licenciados/IA + retoque manual**, como diz o Roadmap, com registro de licença.
7. Distribuição inicial: **Android/Google Play**; iOS fora do MVP.
8. O Claude continua escrevendo o código com o Bruno revisando/testando (padrão dos outros dois projetos); as horas do Roadmap são **horas de trabalho
   focado**, não calendário.

## 10. Lições e práticas dos projetos anteriores que valem aqui

Lista longa em [Qualidade e Fluxo de Trabalho](Qualidade%20e%20Fluxo%20de%20Trabalho.md) §1. As que mais pesam:

| Prática (origem) | Para o Miners Idle |
|---|---|
| **Jogável antes de sistema** (we-had-a-plan → Underworld Towers) | Vertical slice da caverna 1 antes de loja, estagiário, arte final. Nada de gerador/framework antecipado. |
| **Plano primeiro, com critério de pronto verificável** | Cada marco ganha um plano em `docs/planning/` antes de codar. |
| **Valores de regra não mudam sem decisão registrada com data** | Vira ainda mais importante aqui: a economia é regra. |
| **Código dono do tempo e da regra; animação só ilustra** | O golpe/dano/recompensa é decidido no domínio; sprite e partícula reagem a eventos. |
| **Assembly Definitions desde o início; Input System desde o dia 1; texto em dados desde o 1º texto** | Idem. |
| **Testes que jogam pelos mesmos comandos do jogador; atalhos de debug em cada feature (F10)** | Painel de debug + testes de PlayMode via comandos; simulador econômico (novo, específico do idle). |
| **Config do projeto em código (`ProjectSetup.cs`), espelho de CI para o Editor aberto** | Reaproveitar a ideia de config reproduzível; o espelho de CI só se a fricção justificar (jogo 2D pequeno). |
| **Registro de licença de todo asset (`CREDITS.md`, tabela de fontes)** | Exigido também pelo Roadmap. |
| **Documentos em pt-BR, explicar o porquê das escolhas (Bruno aprende fazendo)** | Todos os documentos novos estão em pt-BR. |
| **O que NÃO trazer:** gerador procedural, arquitetura 3D/rede/cartas, rigs e Blender, "pronto para rede" | Nenhum se aplica; trazer rede seria excesso. |
| **Falhas passadas:** README gigante; reorganização total de assets por pasta bagunçada; teste intermitente por seed | Estrutura de pastas por papel desde o primeiro dia; README curto; RNG sempre por seed nos testes. |
