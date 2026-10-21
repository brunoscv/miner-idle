# Requisitos e Restrições

> Lista consolidada e rastreável (2026-10-20). **Fonte de verdade dos requisitos** até o GDD original ser formalmente
> substituído por decisão do Bruno. Cada linha aponta a origem. **Status:** `OK` = claro · `AMB` = ambíguo (ver C-xx no
> [Relatório de Descoberta](../planning/Descoberta%20-%20Relatorio.md)) · `CONF` = conflita com outro requisito.
> Tudo aqui é **[DOC]**, exceto onde marcado. Os IDs são usados no [Roadmap](../planning/Roadmap.md) para rastreabilidade.
> Abreviações de origem: **GDD** = `GDD_Idle_Miner.docx`; **RM** = `Roadmap_MVP_Idle_Miner_Unity_CSharp.docx`; **MK** = mockup embutido no GDD.

## A. Produto e plataforma

| ID | Requisito | Origem | Status |
|---|---|---|---|
| REQ-PLT-01 | Dispositivos móveis, **orientação vertical** | GDD Controle | OK |
| REQ-PLT-02 | Primeiro alvo: **Android** (APK de desenvolvimento durante o projeto; AAB final) | RM §1.2, §2 | OK |
| REQ-PLT-03 | **Uma tela principal** de mineração; interface legível com uma mão | GDD §1.2, §1.3 | OK |
| REQ-PLT-04 | Sessões curtas e recorrentes; mineração continua com loja/modal abertos | GDD §1.2, §2.3 | OK |
| REQ-PLT-05 | Safe Area (notch/recortes) | RM §2 | OK |
| REQ-PLT-06 | Nome do produto: **Miners Idle** | Bruno, 2026-10-21 (D-03) | OK (decidido) |
| REQ-BIZ-01 | Jogo **comercializado**, com **anúncios reais** (vídeo premiado no botão AD) na versão publicada; MVP mantém o AD simulado | Bruno, 2026-10-21 (D-18) | OK (novo; substitui o "sem anúncios reais" do GDD §12 para a versão publicada) |
| REQ-BIZ-02 | Idioma: **só pt-BR** no MVP, textos em dados | Bruno, 2026-10-21 (D-02) | OK (novo) |
| REQ-PLT-07 | Aparelho-alvo de teste: **Motorola Moto G75**; ~20 h/semana de desenvolvimento | Bruno, 2026-10-21 (D-20, D-17) | OK (novo) |

## B. Loop e mineração

| ID | Requisito | Origem | Status |
|---|---|---|---|
| REQ-LOOP-01 | Loop: atacar → quebrar → coletar → avançar → melhorar → descer | GDD §2.1 | OK |
| REQ-LOOP-02 | ~~Cinco cristais visíveis por lote, posições **aleatórias com espaçamento proporcional**, leitura vertical; ao quebrar o 5º, volta ao topo e gera novo lote~~ | GDD §2.2, §10 | **SUPERADO** (D-26 → REQ-MAP-01…03) |
| REQ-LOOP-03 | ~~Minerador começa no topo, fica visualmente acima do alvo sem sobrepô-lo e avança ao cristal disponível mais próximo~~ | GDD §2.2 | **SUPERADO** (D-26, D-27) |
| REQ-MAP-01 | O minerador **anda** pelo mapa visto de cima até o alvo (linha reta, D-31 em aberto) e bate quando está a uma distância ≤ alcance; o tempo de caminhada é regra do núcleo | Bruno, 2026-10-21 (D-26) | OK (novo) |
| REQ-MAP-02 | Grupos de cristais **nascem continuamente** dentro da área do mapa, com distância mínima entre si e do minerador, até um máximo na tela; cada grupo é um alvo (uma vida, uma recompensa) | Bruno, 2026-10-21 (D-26) | OK (novo; valores = protótipo, D-29) |
| REQ-MAP-03 | **Prioridade de alvo:** menor número de lote; no mesmo lote, o mais próximo do minerador; empate → o que nasceu primeiro. Cristais do início = lote 1, cada nascimento seguinte recebe o próximo lote. O alvo não muda até quebrar | Bruno, 2026-10-21 (D-27) | OK (novo) |
| REQ-LOOP-04 | Passar de caverna: quebrar uma "parede" que bloqueia a nova seção **(passo 6)** × botão "Passar de Caverna" **(§4.2, §7.3)** | GDD §2.1 × §4.2 | **CONF** (C-03) |
| REQ-MIN-01 | Golpe automático no intervalo atual: 5,0 s inicial, −0,1 s por nível de Velocidade, mínimo 0,5 s | GDD §4, §10 | OK |
| REQ-MIN-02 | Toque manual = **10% de uma picaretada atual** (com Picareta Dupla, considera as duas); alimenta o combo | GDD §3.2, §10 | AMB (C-06: área, taxa, crítico) |
| REQ-MIN-03 | Crítico: **5% por golpe**, dano ×2; mostra "CRÍTICO" com explosão em posição aleatória ao redor do minerador + som próprio | GDD §3.2, §9 | OK |
| REQ-MIN-04 | Combo: cada toque incrementa `xN` (pulsante, posição aleatória); sem toque por 1 s zera; a cada múltiplo de 10, animação especial | GDD §3.3 | OK |
| REQ-MIN-05 | Dano automático = Picareta × bônus Letrado aplicável × Picareta Dupla | GDD §3.2 | OK |

## C. Cristais, recompensa e penalidade

| ID | Requisito | Origem | Status |
|---|---|---|---|
| REQ-CRY-01 | Vida máxima = `10 × 10^(caverna−1)` (10 … 10¹⁰); mostrada **só como barra**, sem número | GDD §3.1 | OK (valor: C-01) |
| REQ-CRY-02 | Lascas básicas = `1 × 2^(caverna−1)` (1 … 512), antes de bônus de Mineração e da penalidade | GDD §3.1, §6.1 | OK (valor: C-01) |
| REQ-CRY-03 | Barra de vida só nos alvos atuais do principal e do estagiário | GDD §3.1 | OK |
| REQ-CRY-04 | Quebra: cristal desaparece, tela vibra levemente, lasca animada vai **lentamente** até o jogador, HUD atualiza | GDD §2.1, §9 | OK |
| REQ-CRY-05 | Penalidade: após **100 quebras do principal** na caverna, recompensa por cristal cai a 50%; removida ao entrar em nova caverna; ícone (seta vermelha) abaixo do minerador | GDD §3.1, §2.3, §10 | AMB (C-09) |
| REQ-CRY-06 | Quebras do estagiário **não** contam para a penalidade | GDD §5.2 | OK |

## D. Economia e melhorias (valores = GDD v1.0, sujeitos a C-01)

| ID | Requisito | Origem | Status |
|---|---|---|---|
| REQ-ECO-01 | **Única moeda:** lasca de cristal, obtida por principal e estagiário, gasta na loja | GDD §4 | OK |
| REQ-ECO-02 | Picareta: custo `50×4^(n−1)`, dano `0,5×2^(n−1)`, limite nível 20 | GDD §4 | AMB (C-04) |
| REQ-ECO-03 | Velocidade: custo `200×3^(n−1)`, −0,1 s/nível, mínimo 0,5 s | GDD §4 | AMB (C-04), valor (C-01) |
| REQ-ECO-04 | Mineração: custo `50×1,2^(n−1)`, +1% de lascas por nível, máximo +100% | GDD §4 | AMB (C-04) |
| REQ-ECO-05 | Estagiário: custo `500×5^(n−1)`, dano 10%×nível sobre uma picareta do principal, nível 10 = 100% | GDD §4, §5 | AMB (C-04) |
| REQ-ECO-06 | Letrado: custo `10×10^(n−1)`, 10 compras; dobra o dano **só** na caverna correspondente; **libera a passagem** (sem exigir 100 cristais); bônus não acumula entre cavernas | GDD §4, §4.2 | valor (C-01) |
| REQ-ECO-07 | Picareta Dupla: 5.000 lascas, compra única; dobra os efeitos da Picareta no ataque automático **e** manual do principal; uma picareta em cada mão | GDD §4, §4.1, §8.3 | OK |
| REQ-ECO-08 | Compra só conclui com lascas suficientes e limite não atingido; HUD atualiza imediatamente; **uma compra processada uma vez** | GDD §4.1; RM §3 sem. 4 | OK |
| REQ-ECO-09 | Linhas de melhoria: **sete** (com Crítico e "Pesquisador") × **seis** na tabela | GDD §1.3 × §4 | **CONF** (C-02) |
| REQ-ECO-10 | Números como `double`; arredondar só na apresentação; HUD com duas casas decimais; separador de milhares | RM §2; GDD §7.1, §7.4 | OK (formato: C-13) |

## E. Estagiário

| ID | Requisito | Origem | Status |
|---|---|---|---|
| REQ-INT-01 | 1ª compra cria um segundo minerador no cristal imediatamente à frente; compras seguintes só melhoram | GDD §5.1 | OK |
| REQ-INT-02 | Mesmo intervalo de golpe do principal (inclui nível de Velocidade) | GDD §5.1 | OK |
| REQ-INT-03 | Nunca ocupa o cristal do principal; barra de vida própria; recebe as mesmas lascas | GDD §5.2 | OK |
| REQ-INT-04 | Depois de quebrar seu alvo, **espera**; só avança quando o principal avança | GDD §5.2 | AMB (C-08) |
| REQ-INT-05 | Dano de referência: **uma** picareta do principal (Dupla não afeta); bônus de Letrado da caverna atual pode se aplicar antes do percentual | GDD §5.3 | OK |
| REQ-INT-06 | Silhueta semelhante à do principal, com variação de escala/cor/capacete | GDD §8.3 | OK |

## F. Cavernas e progressão

| ID | Requisito | Origem | Status |
|---|---|---|---|
| REQ-CAVE-01 | **10 cavernas**, cada uma com cristal, cor dominante, piso e "sensação" próprios (tabela GDD §6); a 10ª é vulcânica | GDD §6, §10 | AMB (C-05: um tipo × cinco cores) |
| REQ-CAVE-02 | Ao avançar: penalidade removida, visual atualizado, cinco cristais novos | GDD §2.3 | OK |
| REQ-CAVE-03 | Letrado nível *n* ↔ caverna *n*; botão "Passar de Caverna" habilita ao comprar o nível correspondente; **bloqueio após a caverna 10** | GDD §4.2; RM sem. 7 | OK |

## G. Boost de anúncio (simulado)

| ID | Requisito | Origem | Status |
|---|---|---|---|
| REQ-AD-01 | Botão AD (raio) abaixo da Loja; abre modal central com contagem de 5 s; depois habilita "Aprimorar" | GDD §7.2 | OK |
| REQ-AD-02 | Ao confirmar: modal fecha, botão em recarga de 30 s, intervalo ÷2,5 por 30 s, raio pulsante acima do minerador | GDD §2.3, §7.2, §9 | AMB (C-07) |
| REQ-AD-03 | Mineração continua durante o modal; **MVP sem rede de anúncios real** (anúncios reais entram no M12, atrás de uma interface; ver REQ-BIZ-01) | GDD §2.3, §12; D-18 | OK |
| REQ-AD-04 | `boostEndTime` em timestamp UTC; fechar/minimizar não duplica bônus | GDD §11; RM sem. 8 | OK |

## H. Interface

| ID | Requisito | Origem | Status |
|---|---|---|---|
| REQ-UI-01 | HUD superior com 4 indicadores com ícone e 2 casas decimais: lascas, dano, nível da mineração/caverna, velocidade (s) | GDD §7.1 | AMB (C-11) |
| REQ-UI-02 | Loja na lateral direita, **sobrepõe** os demais botões, comando de retorno, mostra custo/nível/efeito/estado do botão | GDD §4.1, §7.2 | OK |
| REQ-UI-03 | Botão central "Passar de Caverna" (livro): desabilitado até Letrado; **âmbar** quando habilitado | GDD §7.3 | OK |
| REQ-UI-04 | Alvos de toque grandes, contraste alto; loja/modal bloqueiam toques acidentais na mina sem pausar a mineração | GDD §7.4 | OK |
| REQ-UI-05 | Crítico, combo, penalidade e boost distinguíveis por **cor, forma e movimento** | GDD §9.1 | OK |
| REQ-UI-06 | Unity UI + TextMeshPro; itens de loja reutilizáveis | RM §2, §3 | OK |

## I. Arte e áudio

| ID | Requisito | Origem | Status |
|---|---|---|---|
| REQ-ART-01 | Pixel art no estilo do **mockup**: **fantasia, mina de madeira** (decidido em 2026-10-21, D-05). O texto "industrial/sci-fi, painéis metálicos" do GDD §8.1 fica superado; original, sem assets de terceiros reconhecíveis | Bruno D-05 (supera GDD §8.1) | OK (conflito C-05 resolvido) |
| REQ-ART-02 | Paleta-âncora: `#15191C #20262A #7D8A91` (mineração) · `#F2A900 #D84235` (segurança) · cristais `#D9FFFF #FFD529 #318AFF #44E480 #BB63FF #FF4141 #14E4D1 #FF6A19` · texto `#F5F7F8 #D9E0E3` | GDD §8.2 | OK |
| REQ-ART-03 | Anão bípede, barba visível, couro, frontal; picareta de madeira na direita, 2ª na esquerda após Dupla | GDD §8.3 | OK |
| REQ-ART-04 | Picareta: cabo de madeira + cabeça de aço; **brilho metálico cresce a cada nível** da Picareta (20 níveis) | GDD §8.4 | OK (RM §7.1 prevê reduzir) |
| REQ-ART-05 | A animação atinge o cristal sem o corpo do minerador o sobrepor | GDD §8.4 | OK |
| REQ-ART-06 | Cristais, chão, luz e paleta ficam mais intensos a cada caverna | GDD §1.1 | OK |
| REQ-ART-07 | Todo asset (inclusive IA) com registro de nome, autor, origem, licença, modificações, uso; IA = matéria-prima, com limpeza e padronização; Point filter | RM §5 | OK |
| REQ-AUD-01 | Sons: picaretada, crítico (destacado), quebra (opcional), toque (som normal), combo ×10 (pode reusar camadas); boost e penalidade sem exigência | GDD §9 | OK |
| REQ-AUD-02 | `AudioSource` + `AudioMixer` com controle de volumes | RM §2 | OK |

## J. Persistência, dados e técnica

| ID | Requisito | Origem | Status |
|---|---|---|---|
| REQ-TEC-01 | Unity **LTS**, C#, pipeline **URP 2D** | RM §2 | OK (C-15) |
| REQ-TEC-02 | Dados/balanceamento em **ScriptableObjects**, separados da lógica | RM §2 | OK |
| REQ-TEC-03 | Save em **JSON versionado** em `persistentDataPath`, com migração | RM §2, §3 sem. 8 | OK |
| REQ-TEC-04 | Estado persistente mínimo: `caveLevel, crystalShards, pickaxeLevel, speedLevel, miningLevel, internLevel, scholarLevel, doublePickaxe, mainBreakCount, penaltyActive, boostEndTime` | GDD §11.1 | OK (nota: o "mineração" 0–100 aqui = % de bônus) |
| REQ-TEC-05 | Eventos essenciais: `onAutoHit, onManualTap, onCritical, onCrystalBreak, onShardCollected, onUpgradePurchased, onInternUnlocked, onScholarPurchased, onCaveChanged, onAdOpened, onAdReady, onBoostStarted, onBoostEnded, onPenaltyStarted` | GDD §11.2 | OK |
| REQ-TEC-06 | Evitar um único `GameManager` onipotente; camadas Core/Economia/Mineração/Progressão/Interface/Persistência | RM §2.1 | OK |
| REQ-TEC-07 | Unity Test Framework: EditMode (fórmulas) desde a semana 2; PlayMode (integração) semanas 3–11 | RM §6 | OK |
| REQ-TEC-08 | Git + Git LFS; build Android frequente em aparelho real | RM §2 | OK |
| REQ-TEC-09 | Painel de debug para economia | RM §3 sem. 2 | OK |

## K. Processo e qualidade

| ID | Requisito | Origem | Status |
|---|---|---|---|
| REQ-PRO-01 | Marcos A (base técnica), B (vertical slice), C (sistemas completos), D (MVP candidato) com critérios de saída | RM §4 | OK |
| REQ-PRO-02 | Teste estatístico: 100.000 golpes ≈ 5% crítico; matriz do estagiário; playtest de 5–8 pessoas por 20–30 min | RM §6 | OK |
| REQ-PRO-03 | Bugs P0 (bloqueia), P1 (corrigir antes do RC), P2 (pós-MVP) | RM §6.1 | OK |
| REQ-PRO-04 | **Não reduzir:** testes das fórmulas, persistência, matriz do estagiário, penalidade, progressão até a caverna 10, mineração durante loja/modal, testes em aparelho real | RM §7.1 | OK |
| REQ-PRO-05 | Reduzível se houver atraso: variações de animação, partículas, fundos únicos para todas as cavernas, brilho para os 20 níveis de Picareta, sons extra, transições | RM §7.1 | OK |
| REQ-PRO-06 | Mudanças de balanceamento só após telemetria/playtest, **preservando uma versão-base** para comparação | GDD §13 | OK (conflita com C-01 — ver D-01) |

## L. Fora do escopo do MVP [DOC, GDD §12]

Prestígio/renascimento/meta-progressão além das 10 cavernas · dinheiro real, loja externa, rede de anúncios real · multiplayer, ranking, clãs, missões diárias, eventos sazonais · inventário de equipamentos, múltiplas moedas, fabricação · narrativa extensa, diálogos, chefes, combate contra criaturas · servidor, conta online, nuvem, social.

> **[INT]** Atenção: o pedido inicial do Bruno cita "clicar nos cristais" e "novas áreas/camadas"; a **mecânica real é bem mais específica** (automática + toque a 10%, lote de 5, estagiário, Letrado). Este projeto segue o GDD, não o resumo genérico.
