# Marco 9c — Andares e números do v8

> Plano de 2026-11-16. **P1…P9 aprovadas no mesmo dia** (D-256…D-264); a P2 foi trocada pelo pedido de uma transição de verdade (D-266), cujo formato é a **P10, aguardando o Bruno**. É o **Bloco A** da [Análise do GDD v8](Analise%20-%20GDD%20v8.md) §5, a base dos outros blocos. Base: GDD v8.2 ([`docs/GDD_Idle_Miner v82.pdf`](../GDD_Idle_Miner%20v82.pdf), §3.1, §3.3, §4.1, §4.2, §4.3, §7.4; D-248) e as decisões **D-236** (andar = caverna), **D-238** (lascas), **D-239** (cristais por andar e Memória), **D-246** (offline conta na barra e passa de andar), **D-247** (sem simulador), **D-249** (crítico acima de 100%) e **D-253** (avanço automático).
>
> Um marco só começa com o anterior aprovado; a exceção da D-192 continua (M7 E4 e 8b E5 de fora). **O M9b E6 fica parado** até aqui: a sessão longa dele mede o combo, que sai no Bloco F.

## Objetivo

**O jogo passa a descer por andares, com os números do v8.** Some o Mapa, a trava dos 100 cristais e a parede. No canto de baixo à direita, o botão **Próximo Andar** enche como uma barra a cada cristal quebrado; cheio, um toque leva ao andar seguinte, onde a barra volta a 0%. Quanto mais fundo, mais cristais o andar pede (`andar × 10`, menos 5 por nível da Memória). Os marcos de andar valem para sempre: no **5** o avanço fica automático, no **10** os raros aparecem, no **20** o offline vai a 6 h. Vida, lascas, crítico, custo da Picareta e a Memória passam a ser os do v8.

**Para você:** começar no andar 1, ver a barra encher em 10 cristais, tocar e descer; chegar no andar 5 e ver o jogo descer sozinho; fechar o app com o estagiário e voltar alguns andares mais fundo.

## O que já existe (e o que muda)

| Peça | Hoje (v7) | No 9c (v8) |
|---|---|---|
| **Passagem** | Mapa na LOJA (custo zero, trava de 100 cristais − Memória), botão central **PASSAR DE CAVERNA**, **parede com vida** no corredor (D-98) | **Próximo Andar** com barra; nada na LOJA; sem parede |
| **Trava** | `MapLock = 100 − 10 × Memória`, `CaveBreaks` no estado | requisito = `max(0, 10 × andar − 5 × Memória)`; `CaveBreaks` vira os cristais do andar |
| **Vida** | `1 × 1,25^(nível−1)` | `1 × 1,5^(andar−1)` |
| **Lascas** | `vida × 1,2` | andar 1: `vida × 0,2`; depois `vida × 0,2 × (andar − 1)` (D-238) |
| **Crítico** | dano ×2 | ×1,5; chance acima de 100% soma +1 ponto no dano por 1% (D-249) |
| **Picareta (custo)** | 2,1 × dano da Picareta no nível atual (D-202) | **7** × dano da Picareta no nível atual (P5) |
| **Memória** | −10 cristais por nível, até 10 | −5 por nível, até **100** |
| **Estagiário** | compra a qualquer hora | a partir do **andar 5** (a Promoção continua dando ele desde o andar 1) |
| **Raro** | 15% desde o início | igual, mas só depois de chegar ao **andar 10** uma vez (o contador de 60 s é o Bloco D) |
| **Offline** | 6 h, não passa de nível, não conta na trava (D-189) | **4 h** (6 h depois do andar 20); conta na barra e, com o automático, passa de andar (D-246) |
| **Recorde** | não existe | **andar mais fundo** já alcançado, nas permanentes (sobrevive ao descanso) |
| **Balanço** | `Balance_GDD_v7` (e v5 no seletor do DBG) | `Balance_GDD_v8` |
| **Texto** | "CAVERNA" no HUD e nas telas | "ANDAR" |

Ficam **como estão** neste marco: o toque e o combo (saem no Bloco F, com a Bomba), as cartas do raro e o AD (Bloco D), a loja das permanentes e o Descansar (Bloco C), a câmera (Bloco G) e a barra de vida do cristal (Bloco B).

## As regras (o que já está decidido)

- **Requisito do andar:** `max(0, 10 × andar − 5 × nível da Memória)` cristais (D-239). Com 0, a barra já nasce cheia.
- **A barra:** cristais quebrados no andar ÷ requisito. Ao mudar de andar, volta a 0% (§7.4).
- **Offline:** os cristais que o estagiário quebra contam na barra; com o automático ligado, ele passa de andar e segue no andar novo, com a vida e as lascas de lá (D-246). Sem estagiário, nada acontece offline (D-189, que continua valendo nisso).
- **Marcos:** valem **para sempre** a partir da primeira vez (D-253): 5 = avanço automático (ligado por padrão, desliga nos AJUSTES), 10 = raros, 20 = offline base de 6 h.
- **Lascas vermelhas:** a tabela de hoje, por andar (D-236).

## Perguntas para o Bruno

**P1 — O seletor de balanço do DBG → Cálculos (D-150).** Hoje ele troca entre GDD v7 e v5. A passagem do v7 (Mapa, parede) não existe no v8, e manter as duas no código dobra o trabalho de cada bloco.
**Rec.:** o v8 vira o único balanço do jogo; **saem do código** o Mapa, o Letrado, a parede, a Picareta pela caverna (D-133) e os balanços v1/v5/v7 do seletor. Ficam no DBG → Cálculos a taxa e o incremento da Picareta (D-150), que continuam úteis para testar.

**P2 — O que se vê ao descer.** O v8 não descreve a passagem (a do v7 era a parede).
~~**Rec.:** escurecer rápido (≈0,4 s) e a fila nova nascer na tela.~~ **Trocada pela D-266:** o Bruno achou abrupto; o formato é a P10.

**P10 — A transição de andar (D-266).** Descer não pode ser "a fila troca e o anão já bate". Três formatos, do mais simples ao mais caro:

| | Formato | Como fica | Custo |
|---|---|---|---|
| **A** | **Desfoque e cartão** | a tela desfoca e escurece, "ANDAR 7" grande no centro, volta a focar e o jogo segue | ~2 h |
| **B** | **A câmera desce** *(rec.)* | o A, mais a câmera: ela **sobe** saindo do corredor, o cartão aparece no escuro e ela **desce do alto** até o anão, focando no caminho. É literalmente "descer um andar" | ~4–6 h |
| **C** | **A câmera vem pela caverna** | a câmera nasce lá no fundo do corredor e voa até o anão | ~6–8 h, e o Bloco G (câmera de 60°) obriga a refazer |

**Rec.: B**, em ~2,5 s:
1. **Saída (0,6 s):** o anão termina o golpe e para; a câmera sobe um pouco e a tela desfoca e escurece.
2. **Cartão (0,8 s):** no escuro, **"ANDAR 7"** na fonte pixel, com um som grave de descida e um tremor leve (obedece aos AJUSTES). A cada 10 andares, o nome do bioma novo embaixo ("SAFIRA").
3. **Chegada (1,0 s):** a câmera **desce do alto** até a posição de jogo, freando no fim, enquanto o desfoque some; a fila do andar novo já está lá.
4. **De volta:** o anão corre para o primeiro cristal.

Regras da B:
- **O código é dono do tempo:** a transição é um estado do núcleo com duração no balanço (2,5 s). Enquanto ela dura, **nada corre**: nenhum golpe (anão nem estagiário), nada nasce, o tempo dos bônus para. A animação só ilustra.
- **Igual no automático:** a mesma transição a cada andar; o DBG tem **Transição curta** (0,3 s) para testar rápido.
- **No offline não há transição:** é só a conta (D-191).
- **O desfoque:** se o URP do projeto não fizer um desfoque bonito no celular, o plano B é **pixelar** a tela (mosaico que vai engrossando e depois afina), que combina com a pixel art. A escolha sai do mockup.
- A câmera se move **relativa ao enquadramento atual**: quando o Bloco G trocar a câmera, o movimento continua valendo.

**P3 — O andar 100.** No v7, a última parede levava ao coração da montanha (fim do jogo, D-112).
**Rec.:** no andar 100, a barra cheia leva ao **coração da montanha** (a tela de hoje).

**P4 — Barra cheia e quebras a mais.** Com o automático desligado, o anão continua quebrando no andar.
**Rec.:** a barra fica em 100%; o que passar **não vai** para o andar seguinte (lá ela começa em 0%, §7.4).

**P5 — O custo da Picareta.** O v8 diz `Taxa Base (7) × Dano Base`. Pela leitura da tabela do Apellon (D-202), "Dano Base" no custo é o dano da Picareta no nível atual.
**Rec.:** a mesma leitura, trocando 2,1 por 7: 14; 15,4; 17,08… (nível 1, 2, 3).

**P6 — O avanço automático com o jogo aberto.**
**Rec.:** passa **na hora** em que a barra enche, com a mesma transição da P2; o botão mostra "AUTO". Nos AJUSTES, uma linha "Avanço automático" (ligado/desligado), travada com "Andar 5" até liberar. Fica no `settings.json` com os outros ajustes (D-200).

**P7 — O estagiário antes do andar 5.**
**Rec.:** a linha dele na LOJA aparece travada, com "Andar 5" no lugar do preço. Quem tem a Promoção começa com ele desde o andar 1 (é para isso que ela serve).

**P8 — O save de quem já joga (v7 → v8).** Um andar 30 do v7 tem vida 646; no v8 tem 127.834: o jogador ficaria preso.
**Rec.:** o save v8 abre um v7 com uma **run nova no andar 1**, mantendo as lascas vermelhas, as permanentes (os níveis da Memória passam a valer −5) e o dia. O **andar mais fundo** começa no andar a que a run antiga chegou (o que ele já alcançou continua alcançado). Só o pessoal de teste tem save hoje.

**P9 — "Caverna" no código.**
**Rec.:** os **textos** viram "ANDAR" já (dados); os nomes no código (`Cave`, `CaveBreaks`…) ficam como estão, para não misturar uma troca de nome enorme com a mudança de regra. Renomear depois, numa limpeza separada, se você quiser.

## Etapas

### E0 — As transições de andar no DBG (D-267, antes da E1)

O Bruno escolhe a transição **vendo as três no jogo**. Elas entram já, na passagem de caverna de hoje (quebrar a parede, DBG → Caverna → Próxima caverna), e a E1 só troca o gatilho.

- **Núcleo:** `MiningSimulation.FloorTransitionSeconds` (0 = sem transição, o de hoje). Ao mudar de caverna abre a transição: nada corre (golpe, estagiário, nascimento pelo relógio, tempo dos bônus, toque), a fila do nível novo é reposta na hora, e o tempo de jogo segue (o código é dono do tempo; a animação lê quanto falta).
- **C — a câmera vem pela caverna** (primeiro): a descida escurece; no escuro, o cartão "ANDAR n"; a câmera nasce lá na frente, baixa, e voa pelo corredor por cima da fila até o lugar de jogo, clareando no caminho. O corredor passa a ser desenhado também à frente da câmera, para o voo ter chão e paredes.
- **B — a câmera desce** (segunda): a sequência da P10 em 2,5 s (saída 0,6 · cartão 0,8 · chegada 1,1). A câmera sobe 1,2 m, desce de 6 m acima, sempre mirando o ponto do chão que a câmera de jogo mira (vale para o Bloco G). O **desfoque** é a descida desenhada numa textura até 6× menor: esticada com filtro suave fica desfocada; com filtro de pixel vira **mosaico** (o plano B da P10). As duas ficam no DBG para comparar. No cartão, um tremor leve e um "vum" grave sintetizado (`Sfx.FloorDown`), os dois desligáveis nos AJUSTES.
- **A — desfoque e cartão** (terceira), em 2,0 s (saída 0,5 · cartão 0,8 · volta 0,7): a câmera não sai do lugar; a descida desfoca (o mesmo desfoque da B) e escurece até 80%, sem chegar ao preto, para a caverna ficar borrada atrás do cartão; o bioma troca no ponto mais borrado; tudo volta a focar. No Inspector dá para escurecer até o preto ou trocar o desfoque pelo mosaico.
- **DBG → Caverna → "Transição: B"**: troca entre C → B → B mosaico → A → nenhuma. Não fica salvo. **Escolhida: B (D-268)**, a da cena.
- **Como testar (C):** cena `Bootstrap`, Play. DBG → Caverna → **Próxima caverna**: a descida escurece, o cartão "Nível 2 · …" aparece no meio da tela, a câmera nasce baixa lá na frente do corredor e voa por cima da fila até o lugar de sempre, clareando; o anão espera parado e só então corre. Tocar na tela durante a transição não faz nada. DBG → Caverna → **Transição: C** vira "Transição: nenhuma": a próxima caverna começa na hora, como antes. Com DBG → Tempo ×5, a transição acelera junto. Os números do voo (duração, de onde nasce, altura, inclinação) ficam no componente `FloorTransition`, no objeto `Effects/CaveBanner`, para ajustar no Inspector.
- **Como testar (B):** cena `Bootstrap`, Play. DBG → Caverna → **Transição: C** até ler "Transição: B". DBG → Caverna → **Próxima caverna**: a câmera sobe um pouco e a descida desfoca e escurece; no escuro, o cartão, um tremor leve e um "vum" grave; a câmera desce do alto até o lugar de sempre, focando e clareando; o anão só corre depois. Mais um toque no botão, "Transição: B mosaico": a mesma coisa com mosaico de pixels no lugar do desfoque. Com tremor ou som desligados nos AJUSTES, a B não treme ou não toca. Os números (tempos de cada parte, quanto sobe, de onde desce, quanto desfoca) ficam no mesmo componente, no grupo "B — a câmera desce".
- **Como testar (A):** a mesma coisa com "Transição: A": a câmera fica parada, a caverna desfoca e escurece sem sumir, o cartão aparece por cima, e a caverna nova volta a focar. Os números ficam no grupo "A — desfoque e cartão".

### E1 — O núcleo do v8: números e passagem por andar

- **`Balance_GDD_v8`** com os números do v8: vida 1,5ⁿ, lascas do D-238 (fórmula nova no `MiningRules`, não um crescimento fixo), crítico ×1,5, Picareta a 7 × (P5), Memória −5 até 100, offline 4 h / 6 h, 100 andares, 10 por bioma.
- **Crítico acima de 100%** (D-249): a chance que passar de 1 vira dano a mais (150% + 1 ponto por 1%).
- **Passagem:** `RequiredBreaks` (o requisito do andar), `FloorProgress` (0…1), `CanAdvance`, `AdvanceFloor()` (no lugar de `CanPassCave`/`PassCave`): abre a **transição** (P10: um estado com duração no balanço, em que nada corre), muda o andar, zera os cristais do andar, refaz a fila com a vida e as lascas novas e avisa por eventos (começo da transição, troca do andar, fim). No andar 100, leva ao coração (P3).
- **Sai** (P1): Mapa/Letrado, parede (`PlaceWall`, `WallHealthIn`, o estado de "parede de pé"), Picareta pela caverna, os balanços antigos do seletor.
- **Save v8** com a migração da P8.
- **DBG → Andar** (no lugar do DBG → Caverna): **Encher a barra**, **Próximo andar**, **Ir para o andar…** (1, 5, 10, 20, 50, 100). DBG → Cálculos sem o seletor de balanço.
- **Testes EditMode:** vida e lascas nos andares 1, 2, 10, 100; requisito com Memória 0, 2, 20 e negativo; crítico com 80%, 100% e 140%; custo da Picareta; passagem (zera a barra, refaz a fila, andar 100 → coração); migração v7 → v8. Os testes do Mapa e da parede **são corrigidos para medir a passagem nova** (nunca apagados só para passar).

**Como ficou (2026-11-17):**

- O botão de passagem de hoje (`CavePassButton`, "PASSAR") ficou como **provisório**: aparece com a barra cheia e chama o `AdvanceFloor()`. A E2 troca ele pelo `NextFloorButton` com a barra.
- **"Ir para o andar…"** virou **um botão só**, "Ir ao andar N", que vai ao próximo marco depois do andar atual (1 → 5 → 10 → 20 → 50 → 100 → 1). Junto dele: **Encher a barra**, **Próximo andar** (sem a barra), **+10 andares** e **Andar 100 (fim)** (vai ao 100 já com a barra cheia).
- A **fila do andar velho** fica na tela até o escuro da transição: os cristais do andar novo só aparecem quando ninguém vê.
- A **vibração longa** era a da parede; passou para a **descida de andar** (uma por andar).
- O **andar mais fundo** (`DeepestFloor`) entrou já na E1, nas permanentes e no save, porque a migração da P8 precisa dele; ele também liga o **teto de 6 h** do offline a partir do andar 20 (o modal mostra o teto certo). Os outros marcos de andar seguem na E3.
- A **Memória** na LOJA diz "Andar −N cristais" (o Mapa saiu); os textos "CAVERNA" → "ANDAR" seguem na E2.
- Os balanços v1, v5 e v7 seguem em `Data/` só para os testes que os usam; o jogo e o DBG usam só o `Balance_GDD_v8`.

### E2 — Próximo Andar na tela

- **Mockup** em 270×480 antes do código: o botão à direita, embaixo, com a barra que enche e o "x/y" de cristais; o Descansar fica à esquerda; o centro fica livre para a Bomba (Bloco E). E os **quadros da transição** (saída, cartão, chegada), com desfoque e com pixelado lado a lado.
- `NextFloorButton` (UI) no lugar do `CavePassButton`: apagado enchendo, aceso cheio, "AUTO" quando automático. Escuta o núcleo (eventos), não calcula nada.
- A **transição da P10** (Presentation): câmera, desfoque ou pixelado, cartão "ANDAR n" (texto em dados), som e tremor. Some a linha do Mapa da LOJA.
- Textos "CAVERNA" → "ANDAR" (P9) no HUD, no Descansar, no coração e no DBG.
- **Testes PlayMode:** quebrar até encher, tocar, ver o andar subir e a barra zerar; o botão não responde antes de encher.

**Como ficou (2026-11-17, D-269):**

- O Bruno escolheu a **opção c** do mockup: **o botão é a barra**. Enchendo, a madeira apagada acende da esquerda para a direita com os cristais quebrados, com "PRÓXIMO ANDAR" e "x/y"; o toque não faz nada. **Cheio**, o conteúdo vira o da opção b: a **setinha para baixo**, "DESCER" e "ANDAR n", com uma **borda que pulsa**; o toque desce e a barra zera. No último andar o alvo diz "CORAÇÃO"; depois do coração o botão some.
- `CavePassButton` virou `NextFloorButton` (o mesmo `.meta`); a cena se monta no `NextFloorSetup`, que também tira o PASSAR provisório da E1. A setinha é `Art/UI/icon_floor_down.png` (9×10).
- O **Descansar** ficou mais estreito (94 de largura, como o Próximo Andar), e o **centro fica livre** para a Bomba (Bloco E); um teste mede o vão.
- O **"AUTO"** não entrou: o avanço automático só existe na E3, e o selo entra junto com ele.
- Os **quadros da transição** não viraram mockup: a B já tinha sido escolhida vendo no jogo (E0, D-268). O cartão diz **"ANDAR n"** com o bioma embaixo, menor.
- Textos (P9): HUD "ANDAR"; Descansar, resumo do dia, offline e DBG → Dia dizem "andar"; o coração diz "Você desceu os 100 andares.". Os textos novos estão no `FeedbackTexts`; o setup migra os antigos.
- **DBG → Andar → Transição curta: sim/não** (0,3 s), para passar muitos andares sem esperar.

### E3 — Marcos de andar

- **Andar mais fundo** nas permanentes, no save: sobrevive ao descanso; o "Apagar save" dos AJUSTES zera, como tudo.
- **Andar 5:** avanço automático (P6) e a linha nos AJUSTES; o estagiário destrava na LOJA (P7).
- **Andar 10:** o raro de hoje (15%) só nasce depois dele.
- **Andar 20:** offline base de 6 h.
- **DBG → Andar:** **Recorde: 1 / 5 / 10 / 20** (põe o andar mais fundo direto) e **Auto liga/desliga**.
- **Testes EditMode:** cada marco liga só depois do recorde; o recorde sobrevive ao descanso; o automático passa sozinho com o ajuste ligado e não passa com ele desligado.

**Como ficou (2026-11-17):**

- Os três marcos estão no balanço (`Balance_GDD_v8`: avanço automático 5, estagiário 5, raro 10), ao lado do marco do offline (20), que já vinha da E1. Todos olham o **andar mais fundo** (`DeepestFloor`), que já sobrevivia ao descanso desde a E1.
- **Avanço automático** (P6): com o recorde no 5 e o ajuste ligado, a barra cheia desce **na hora**, com a mesma transição do toque; o botão mostra o selo verde **AUTO** no canto de cima (o do mockup da E2). Desligado, a barra fica cheia e espera o toque (P4); religado, desce. **O andar 100 espera o toque** mesmo com o automático: o coração da montanha é o fim da partida, e chegar nele sem querer seria estranho (confirmado pelo Bruno, D-270).
- **AJUSTES:** a linha **"Avanço automático"** depois da Vibração; antes do andar 5, apagada e com **"Andar 5"** no lugar do botão. Fica no `settings.json` com os outros ajustes (D-200); o `SettingsEffects` passa o ajuste ao `MiningRunner`, que o entrega a cada partida (descanso, reiniciar).
- **LOJA** (P7): o Estagiário mostra **"Andar 5"** no lugar do preço até o recorde chegar lá. A Promoção continua dando ele desde o andar 1.
- **Raro:** não nasce antes do andar 10, nem com a chance do balanço. O atalho DBG → Bônus → Raro agora continua valendo em qualquer andar.
- **DBG → Andar:** **"Recorde: n"** (um botão: 1 → 5 → 10 → 20 → 1) e **"Auto: ligado/desligado"** (o mesmo ajuste dos AJUSTES). Pôr o recorde **abaixo** do andar de agora leva a partida até ele, para o recorde nunca ficar acima de onde se está.
- Testes corrigidos para o comportamento novo: `InternShopTests` começa com o recorde no 5 (o teste é da contratação); `RestFlowTests` desliga o automático no andar 12 (o teste mede o toque com a barra cheia); `LongSessionTests` dá um tick sem tempo antes da foto da reabertura (um descanso no mesmo quadro, com a Memória, abre o andar 1 de barra cheia, e o automático desce no primeiro tick).

### E4 — Offline que desce andares

- `OfflineMining` em trechos por andar: em cada andar, quantos cristais faltam para a barra, o tempo de cada um com a vida de lá; com o automático, ao completar passa ao seguinte e continua; sem ele, fica no andar (a barra cheia, P4).
- O "Bem-vindo de volta" mostra também **os andares descidos** ("Andar 12 → 15"), com o texto em dados.
- As lascas vermelhas pendentes acompanham o andar a que se chegou (a tabela de hoje).
- **DBG → Offline** como hoje (Fora 1 min…8 h); o de 8 h mostra o corte em 4 h ou 6 h.
- **Testes EditMode:** offline sem estagiário (nada), com estagiário e automático desligado (barra enche e para), ligado (desce N andares, confere vida e lascas por andar), corte em 4 h e 6 h, a conta fechada igual à simulação golpe a golpe.

**Como ficou (2026-11-17):**

- A conta continua fechada (6 h custam o mesmo que 1 min): o laço de trechos que já existia para as cartas do raro ganhou o andar. Cada trecho tem as mesmas cartas e o mesmo andar; quando a barra enche com o automático, conta só até o cristal que enche, passa ao andar seguinte (sem gastar tempo, sem transição, D-191) e o tempo que sobra segue lá, com a vida e as lascas de lá. O andar 100 espera o toque (D-270).
- **Os cristais dele contam na barra** do andar, com ou sem automático (D-246). Até a E3 eles não contavam (D-189); o teste antigo foi corrigido para medir isso.
- Sem o automático (desligado, ou antes do recorde 5), a barra enche e fica cheia (P4); o anão desce no toque.
- Na volta, o jogo já está no andar novo, com a fila de lá e o recorde atualizado. Como o andar muda, as lascas vermelhas pendentes do Descansar acompanham sozinhas (são do andar).
- **"Bem-vindo de volta":** com descida, a linha **"Andares 12 » 15"** entre o "Contou" e os cristais, e a nota "Ele desceu do andar 12 ao 15 enquanto você esteve fora." (a nota do teto continua tendo prioridade). Duas voltas sem COLETAR mostram desde o andar da primeira. O andar de partida fica no save (`fromLevel`; um save de antes abre sem descida, sem mudar a versão).
- **Ordem na abertura:** o `GameBootstrap` agora liga os AJUSTES **antes** do save, para o offline da abertura já usar o avanço automático que o jogador escolheu (antes ele valeria o de fábrica, ligado).
- A conta é conferida, nos testes, contra uma de cristal em cristal (a corrida e cada golpe), com barra pela metade, com a Memória e de 1 min a 4 h.

### E5 — Sessão longa e celular

- `LongSessionTests` adaptado: sobe de andar pelo botão, liga o automático no 5, descansa, compra Memória, simula offlines.
- **Moto G75** com o roteiro abaixo.

**Como ficou (2026-11-17):**

- A sessão longa (2 h de jogo em ×20 na cena Game, ~1 min real) agora joga o v8: quando a barra enche, o robô **toca no botão do Próximo Andar** (o mesmo `Button` da tela) e confere que desceu um andar e a barra zerou. Até o recorde 5, **só o toque** pode mudar o andar: se o andar mudar no tempo sem o toque, o teste reprova. Do 5 em diante, o avanço automático (ligado, o de fábrica) desce sozinho, também no offline (a modal mostra a linha dos andares).
- O estagiário entra **pela compra na LOJA**, que respeita o andar 5 (antes o robô usava o atalho do DBG, que contrata de graça em qualquer andar, e o teste pegou isso). A cada descanso ele confere que o recorde sobreviveu e que a **Memória** encurta a barra do andar do recorde. A foto da reabertura agora inclui o recorde.
- Numa rodada: 11 andares pelo botão, 65 pelo automático e 35 no offline (recorde 38), estagiário contratado no recorde 7, 4 descansos, Memória 5, 4 reaberturas sem perder nada. O número exato varia um pouco entre rodadas (o tempo real entra no limite de toques), mas os mínimos do teste ficam bem abaixo.
- **Critério de pronto, conferido:** o Mapa, o Letrado e a parede saíram (E1); os números do v8 têm testes (`GddV8RulesTests`); os marcos 5/10/20 valem para sempre e o automático liga nos AJUSTES (E3); o offline conta na barra e desce (E4); o save v7 abre numa run nova no andar 1 (E1, P8); DBG → Andar funciona. Falta só o **Moto G75**.

## Como testar (Bruno) — ao fim de cada etapa

- **E1:** sem tela nova ainda; os testes cobrem (`bash tools/ci/run-tests.sh`, com o Editor fechado). Para ver, cena `Bootstrap`, Play:
  1. Um save antigo (v7) abre numa **run nova no andar 1**, com as lascas vermelhas, as permanentes e o dia (D-263).
  2. DBG → **Andar** → **Encher a barra**: aparece o **PASSAR** embaixo à direita (provisório até a E2).
  3. Tocar **PASSAR**: a transição B, o HUD vai para 2, os cristais novos só aparecem no escuro e o PASSAR some até a barra do andar 2 encher (20 cristais).
  4. DBG → Andar → **Ir ao andar 5** (cada toque vai ao próximo marco: 10, 20, 50, 100, 1): o andar muda direto; os cristais ficam mais duros e pagam mais.
  5. DBG → Andar → **Andar 100 (fim)**: andar 100 com a barra cheia; **PASSAR** abre a tela do coração da montanha.
  6. **LOJA**: sem o Mapa; a Picareta custa **14**. **PERMANENTES**: a Memória diz "Andar −0 » −5 cristais".
  7. DBG → **Cálculos**: sem o botão de balanço; o aviso diz "Cálculo: GDD v8".
  8. DBG → Estagiário → Contratar, DBG → Offline → **Fora 8 h**: conta **4 h**. Depois de DBG → Andar → Ir ao andar 20, Fora 8 h conta **6 h**.
- **E2:** cena `Bootstrap`, Play:
  1. Embaixo à direita, a barra **PRÓXIMO ANDAR** apagada, com **0/10**; à esquerda o Descansar, mais estreito; o centro vazio. O HUD diz **ANDAR 1**.
  2. A cada cristal quebrado a madeira acende um pedaço e o número sobe; tocar antes de encher não faz nada.
  3. Com 10/10: a setinha, **DESCER · ANDAR 2** e a borda pulsando. Tocar: a transição B, o cartão **ANDAR 2** com o bioma, e a barra volta a **0/20**.
  4. DBG → Andar → **Transição curta: sim**: a próxima descida leva 0,3 s (Encher a barra e tocar). Tocar de novo volta para "não".
  5. DBG → Andar → **Andar 100 (fim)**: o botão diz **DESCER · CORAÇÃO**; tocar abre o coração ("Você desceu os 100 andares."), e o botão some.
  6. Descansar: a tela diz "andar" no lugar de "nível".
- **E3:** cena `Bootstrap`, Play (um save que já passou do andar 5 abre com os marcos ligados; para ver do zero, DBG → Andar → **Recorde** até "Recorde: 1"):
  1. **AJUSTES**: a linha "Avanço automático" apagada, com **Andar 5**. **LOJA**: o Estagiário com **Andar 5** no lugar do preço.
  2. DBG → Andar → **Recorde: 5** (um toque). O botão ganha o selo **AUTO**; os AJUSTES mostram o botão ligado; a LOJA mostra o preço do Estagiário.
  3. Quebrar cristais até encher a barra: o jogo **desce sozinho**, com a transição, sem toque.
  4. Desligar o "Avanço automático" nos AJUSTES (ou DBG → Andar → **Auto: desligado**): o AUTO some, a barra enche e fica esperando o toque. Ligar de novo: desce.
  5. DBG → Andar → **Recorde: 10**: os raros voltam a aparecer. **Recorde: 20**: DBG → Estagiário → Contratar e DBG → Offline → Fora 8 h contam **6 h**.
  6. Descansar: na partida nova, no andar 1, o AUTO continua (os marcos valem para sempre).
  7. DBG → Andar → **Andar 100 (fim)**: com o AUTO, a barra cheia **espera o toque** para o coração.
- **E4:** cena `Bootstrap`, Play:
  1. DBG → Estagiário → **Contratar**; DBG → Andar → **Recorde** até "Recorde: 5" (o automático existe; a partida continua no andar 1).
  2. DBG → Offline → **Fora 1 h**: o "Bem-vindo de volta" mostra **Andares 1 » n** e "Ele desceu do andar 1 ao n…". Atrás dele, o HUD já está no andar n, sem transição.
  3. **COLETAR**: as lascas entram; a barra do andar n está com os cristais que ele quebrou lá.
  4. DBG → Andar → **Auto: desligado** e de novo **Fora 1 h**: o andar não muda, a modal não tem a linha dos andares e a barra fica **cheia**, esperando o toque.
  5. DBG → Offline → **Fora 8 h**: conta 4 h (ou 6 h com Recorde 20) e desce mais andares.
  6. Fechar o jogo com a modal aberta e abrir de novo: ela volta com o mesmo "1 » n".
- **E5:** a sessão longa é automática (`bash tools/ci/run-tests.sh PlayMode`, com o Editor fechado; a linha `[Sessão longa]` no `tools/ci/out/PlayMode.xml` conta os andares pelo botão, pelo automático e no offline). No Moto G75, com o celular no USB: `Tools → Miners Idle → Build → Rodar no celular (Android)` (ou, com o Editor fechado, `bash tools/build/run-android.sh` no Terminal do app) e o roteiro abaixo.

### Roteiro do Moto G75 (E5)

Anote ✅/❌ em cada passo.

1. **Save antigo:** instalar por cima do APK que está no celular (sem desinstalar). Abrir: andar 1 (ou o andar em que o save do v8 parou), lascas vermelhas e permanentes de antes, nada travado.
2. **Primeiros andares:** para ver do zero, DBG → Andar → **Recorde** até "Recorde: 1"; depois, sem outros atalhos, jogar até o andar 3: a madeira do botão acende a cada cristal (0/10, depois 0/20…), com a barra cheia aparece **DESCER · ANDAR n**, o toque desce com a transição e a barra zera. Antes de encher, o toque não faz nada.
3. **Automático:** DBG → Andar → **Recorde: 5**: o selo verde **AUTO** aparece no botão e o jogo desce sozinho a cada barra cheia. AJUSTES → "Avanço automático" desligado: o AUTO some e a barra cheia espera o toque. Fechar o jogo pelo multitarefa e abrir: continua desligado. Ligar de novo.
4. **Offline:** LOJA → comprar o **Estagiário** (com o recorde 5 ele tem preço), automático ligado, fechar pelo multitarefa, esperar 5 min, abrir: o "Bem-vindo de volta" mostra as lascas e, se deu, **Andares n » m**; atrás dele o HUD já está no andar novo. COLETAR.
5. **Descansar e voltar:** chegar ao andar 10 (DBG → Andar → Recorde: 10 e jogar, ou o atalho de andar) e descansar; na run nova, no andar 1: o AUTO continua, o Estagiário tem preço na LOJA (o recorde é 10), e os raros aparecem desde o andar 1.
6. **Sessão curta:** 10 min em ×20 (DBG) com compras: sem travar, sem tela preta, sem engasgo na transição de andar.

## Critério de pronto

- O jogo desce por andares com o botão Próximo Andar e a barra; o Mapa, o Letrado e a parede não existem mais.
- Vida, lascas, crítico (com o excedente), custo da Picareta e Memória são os do v8, conferidos por testes.
- Os marcos 5, 10 e 20 valem para sempre; o automático liga e desliga nos AJUSTES.
- O offline conta na barra e desce andares com o automático, até 4 h (6 h depois do 20).
- O save v7 abre como a P8 diz; os atalhos do DBG → Andar funcionam.
- Testes EditMode e PlayMode escritos e compilando; você roda a bateria no fim, como combinado. Teste no Moto G75.
- README, Roadmap, Análise do v8 e decisões atualizados.

## Riscos

- **O jogo pode travar fundo.** A vida cresce 1,5ⁿ e a Picareta 1,2ⁿ: no andar 30 o cristal tem 127.834 de vida e a Picareta no nível 50 dá ≈ 7.595; no 50, 4,25 × 10⁸. Sem o simulador (D-247), quem diz se fecha são os testes à mão com o DBG (Ir para o andar, +lascas, permanentes de graça). As permanentes novas e os bônus do v8 (Blocos C e D) entram depois e ajudam.
- **O começo fica mais lento.** No andar 1 cada cristal dá 0,2 lasca (10 cristais = 2 lascas) e a primeira Picareta custa 14: ela só sai no andar 3 (2 + 6 + 27 lascas acumuladas). É o que o v8 pede; fica registrado para o seu teste.
- **Tirar código vivo** (Mapa, parede, Picareta pela caverna, balanços antigos) quebra testes e cenas montadas pelo setup em código. Cada teste afetado é corrigido para a regra nova; o `ProjectSetup` reconstrói a cena.
- **Offline descendo muitos andares** pode render mais do que jogar, se o estagiário estiver forte. O teto (4 h / 6 h) limita; o DBG mostra a conta.

## Fora deste marco

- Toque, combo e Bomba (Blocos E e F).
- Raro por contador de 60 s, cartas do v8, tier 2 e os 3 anúncios (Bloco D).
- Permanentes só no Descansar, Dedo Fogoso, Atalho, Promoção com Treinar 5 (Bloco C).
- Cristal rachando no lugar da barra de vida (Bloco B).
- Câmera de 60° e distância entre cristais (Bloco G).
- HUD do v8 só com lascas e lascas vermelhas e a barra vertical do andar à esquerda (vão com o Bloco C, junto do resto da interface).

## Esforço

| Etapa | Horas |
|---|---|
| E1 | 8–10 |
| E2 (com o mockup e a transição B) | 9–13 |
| E3 | 4–6 |
| E4 | 5–7 |
| E5 | 3–4 |

Total: **~29–40 h**.

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| Plano | ✅ aprovado em 2026-11-16: P1…P9 (D-256…D-264); P10 → as três transições no DBG, C primeiro (D-266, D-267) | — |
| E0 Transições no DBG | ✅ aprovada em 2026-11-17: fica a **B** (D-268), a padrão da cena; C, B mosaico e A seguem no DBG | Testes: EditMode 438/438 (6 em `FloorTransitionTests`); PlayMode 152/153, com um teste por transição (`TenLevels_ChangeTheBiome_InTheDark` para a C, `CameraDrop_RisesBlursAndComesBackInFocus`, `BlurCard_BlursInPlace_AndComesBackInFocus`) e o do botão, que percorre a lista. Instáveis por tempo real, fora da transição, cada um falhou uma vez: `AllocationTests` (às vezes, quando uma quebra de cristal cai dentro da medição: 7 e 9 alocações num run, 1 no seguinte com a mesma quebra) e `TapInputTests.TappingBreaksCrystalsFaster_AndAutoTapRuns` |
| E1 Núcleo do v8 | ✅ aprovada pelo Bruno em 2026-11-17 | Testes: EditMode 463/463 (novo `GddV8RulesTests`; `CaveTests` reescrito para a barra; os do Mapa e da parede corrigidos para a passagem nova); PlayMode 153/153 (`CaveFlowTests` mede encher a barra → PASSAR → transição → andar 2 e o andar 100 → coração; `AllocationTests` mede a transição com a fila velha na tela; a vibração longa mede a descida de andar). Ver "Como ficou" na E1 |
| E2 Próximo Andar na tela | ✅ aprovada pelo Bruno em 2026-11-17 (opção c com o cheio da b, D-269) | Testes: EditMode 463/463; PlayMode 156/156 (novo `NextFloorFlowTests`: enche com os cristais, não responde antes, desce e zera, HUD "ANDAR", transição curta; `CaveFlowTests` e `RestFlowTests` corrigidos para o botão novo e o centro livre). Ver "Como ficou" na E2 |
| E3 Marcos de andar | ✅ aprovada pelo Bruno em 2026-11-17 (com a D-270) | Testes: EditMode 475/475 (novo `FloorMilestoneTests`: automático antes e depois do 5, ligado/desligado, com a transição, o andar 100 espera; Estagiário com "Andar 5"; raro só no 10; marcos depois do descanso; DBG Recorde); PlayMode 159/159 (`NextFloorFlowTests`: AUTO e descida sozinha, AJUSTES travado/liberado, LOJA). Ver "Como ficou" na E3 |
| E4 Offline que desce andares | ✅ aprovada pelo Bruno em 2026-11-17 | Testes: EditMode 490/490 (novo `OfflineFloorsTests`: sem estagiário, automático desligado enche e para, ligado desce conferindo de cristal em cristal, barra pela metade e Memória, andar 100 espera, teto 4 h/6 h, carta do raro, a volta pendente lembra o andar de partida; `OfflineMiningTests` corrigido: os cristais contam na barra; `SaveServiceTests`: o andar de partida no save); PlayMode 160/160 (`OfflineFlowTests`: Fora 1 h com o automático mostra "1 » n"). Ver "Como ficou" na E4 |
| E5 Sessão longa e celular | 🔄 sessão longa adaptada em 2026-11-17; aguardando o Moto G75 (roteiro acima) e a validação do Bruno | Testes: EditMode 490/490; PlayMode 160/160 (`LongSessionTests` do v8: o botão até o 5, o automático depois, o estagiário pela LOJA só no 5, a Memória encurta a barra, o recorde sobrevive ao descanso e à reabertura). Ver "Como ficou" na E5 |
