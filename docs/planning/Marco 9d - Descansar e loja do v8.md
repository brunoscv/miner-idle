# Marco 9d — Descansar e loja do v8

> Plano de 2026-11-17, **aprovado no mesmo dia: P1…P7 com as recomendações (D-272…D-278)**. É o **Bloco C** da [Análise do GDD v8](Analise%20-%20GDD%20v8.md) §5, depois do [Marco 9c](Marco%209c%20-%20Andares%20e%20numeros%20do%20v8.md) (aprovado em 2026-11-17, D-271). Base: GDD v8.2 §4.2 (melhorias permanentes) e §7.3–7.4 (interface), D-248, D-252 e o pedido do Bruno na **D-265**.
>
> **O M9b E6 continua parado:** a sessão longa dele mede o combo, que sai no Bloco F.

## Objetivo

**Descansar vira uma pausa de verdade, e as permanentes só se compram nela.** Ao confirmar o descanso, aparece o **ZZZ** e o jogo **para**: o anão parado, nada correndo. Atrás fica a **tela preta**; na frente, o resumo do dia e a **loja das permanentes**, com **INICIAR** no fim. A run nova **só começa no INICIAR**. Durante a run, a aba das permanentes da LOJA serve só para **ver** o que se tem. Entram as duas permanentes novas do v8, o **Dedo Fogoso** e o **Atalho**, e a Promoção passa a pedir o Treinar no **nível 5**. O HUD fica com o do v8: lascas e lascas vermelhas, com o andar numa barra vertical à esquerda.

**Para você:** descansar no andar 12, ver o ZZZ, comprar a Memória e o Dedo Fogoso com o jogo parado (o anão não se mexe, o tempo do dia não anda), fechar o app no meio da compra e voltar na mesma tela, tocar INICIAR e ver a run nova começar só ali.

## O que já existe (e o que muda)

| Peça | Hoje (M8b, M9c) | No 9d (v8) |
|---|---|---|
| **Descansar** | botão à esquerda a partir do andar 10 (com as lascas vermelhas da run, D-156); modal CONTINUAR/DESCANSAR (D-153) | igual até o DESCANSAR |
| **Depois do DESCANSAR** | a run nova **já começa por trás** (D-154); o resumo do dia por cima, com PERMANENTES e COMEÇAR | **ZZZ** → **tela preta** com o resumo, a loja das permanentes e **INICIAR**; o jogo **parado** até o INICIAR (D-265) |
| **Loja das permanentes** | 2ª aba da LOJA, compra a qualquer hora, vale na hora (D-155) | só na tela do descanso; na LOJA, a aba vira **consulta** (nível e efeito, sem compra) |
| **Promoção do Estagiário** | pede o Treinar **no limite** (10) (D-169) | pede o Treinar no **nível 5** (§4.2, D-248) |
| **Dedo Fogoso** | não existe | **+0,5 no dano do crítico** por nível, até o 10, custo `10 + 2^(n−1)` (D-248) |
| **Atalho** | não existe | **−0,5 m** na distância entre cristais por nível, até o 5, custo `50 + 2^(n−1)` (D-252; ver P6) |
| **Distância entre cristais** | 1 m fixo (`minCrystalSpacing` = 2 unidades, D-58) | **3 m − 0,5 m × Atalho** (D-252; ver P6) |
| **Coração da montanha** (andar 100) | Descansar ao lado de CONTINUAR (D-157) | o mesmo caminho novo |
| **HUD** | lascas, dano, andar, velocidade | **lascas e lascas vermelhas**; o andar numa **barra vertical à esquerda** (§7.3; ver P7) |
| **Save** | v8 | **v9**: guarda o "descansando" (entre runs), para fechar e abrir na mesma tela |

Ficam **como estão**: o toque e o combo (Bloco F), a Bomba e a permanente dela (Bloco E), o raro por contador, o tier 2 e os 3 anúncios (Bloco D), a câmera de 60° (Bloco G) e a barra de vida do cristal (Bloco B).

## As regras (o que já está decidido)

- **D-265:** toda compra de permanentes é uma pausa. O anão parado, nenhum golpe, nada correndo. A run nova só começa no **INICIAR**, comprando algo ou nada.
- **§4.2 / D-248:** as permanentes **só se compram ao Descansar**, numa tela de fundo preto com a loja. Durante a run, o jogador só **olha** o que já comprou.
- **Custos e limites do v8** (já estão no `Balance_GDD_v8` desde a E1 do 9c, menos as duas novas): Memória 100, Sangue da Montanha 20, Coração Ardente 20 (teto de +1000%), Dedos Nervosos 10, Alma dos Anões 10, Treinar 10, todos `10 + 2^(n−1)`; Promoção 50, compra única.
- **Dedo Fogoso:** o dano do crítico vira `1,5 + 0,5 × Dedo Fogoso + excedente da chance acima de 100%` (D-249, que já previa somar com ele). Só o anão (o estagiário não tem crítico, D-167).
- **Atalho (D-252):** a distância começa em 3 m e cai 0,5 m por nível (3 → 2,5 → 2 → 1,5 → 1 → 0,5 m no nível 5).
- **Ordem na LOJA de permanentes (D-155):** as que ainda dá para comprar primeiro, as completas desabilitadas no fim.

## Perguntas para o Bruno

**P1 — O resumo do dia.** Hoje ele é uma tela própria (D-154). Com a loja na tela preta, são duas telas seguidas.
**Rec.:** o resumo vira o **cabeçalho da tela do descanso**: "Dia 3 terminou · Andar 25 · 1 h 12 · ◆ +24 · banco ◆ 31". Embaixo, a lista das permanentes; no fim, **INICIAR**. Uma tela só, e o jogador vê quanto tem na hora de comprar.

**P2 — O fundo: tela preta ou o jogo desfocado (D-265).** Você deixou para decidir vendo.
**Rec.:** **mockup das duas** na E0 (como o Próximo Andar na E2 do 9c). Se nenhuma se destacar, a **preta**, que é a do GDD.

**P3 — O ZZZ.**
**Rec.:** **1,5 s** sobre o preto: três "Z" na fonte pixel subindo e sumindo, um depois do outro, com um som baixo. Um toque pula. Ele aparece entre o DESCANSAR do modal e a tela da loja, e também no Descansar do coração da montanha.

**P4 — Fechar o jogo na tela do descanso.**
**Rec.:** abre de novo **na mesma tela**, com o que já foi comprado. **Sem offline:** o jogo está parado, nem o estagiário trabalha, e o tempo do dia não anda. O save guarda o "descansando" (save v9; um save v8 abre como hoje, numa run em andamento).

**P5 — A aba das permanentes na LOJA, durante a run.**
**Rec.:** a aba **continua**, mas **só para consulta**: cada linha com o nível e o efeito atual, sem preço nem botão, e uma linha no topo "Compre ao Descansar". O selo "!" da aba sai (não há o que comprar).

**P6 — A distância de 3 m entre cristais (Atalho).** Hoje os cristais ficam a 1 m. Com os 3 m da D-252, o anão anda o triplo entre um cristal e outro até comprar o Atalho: o começo fica mais lento, e o offline (que conta a corrida) rende menos. O Bloco G (câmera de 60°) ainda vai ajustar o valor fino.

| | Opção | Efeito |
|---|---|---|
| **a** | **3 m agora**, o Atalho tira 0,5 m por nível *(rec.)* | é a regra do GDD; o Atalho faz diferença desde a primeira compra |
| **b** | o Atalho entra agora, mas a base de 3 m só com a câmera (Bloco G) | sem o Atalho fazer diferença até lá (1 m − 0,5 m vira 0,5 m colado já no nível 1) |

**Rec.: a**, com um botão no DBG → Cálculos para comparar 1 m e 3 m enquanto você joga.

**P7 — O HUD do v8 e a barra do andar neste marco.** O v8 deixa no HUD só **lascas** e **lascas vermelhas**, e põe o andar numa **barra vertical à esquerda** (§7.3), junto da coluna dos bônus. Dano e velocidade saem do HUD; o âmbar do bônus (M9b E2) precisa de outro lugar.
**Rec.:** entra aqui, como a última etapa de tela, **com mockup antes** (E0): HUD com as duas lascas, a barra vertical do andar (o andar atual e o recorde marcados) e o dano e a velocidade na janela da LOJA, no topo, onde o jogador decide o que comprar.

## Etapas

### E0 — Mockups (P2, P7)

- A tela do descanso nas duas versões, **preta** e **desfocada**, com o resumo no topo, a lista das permanentes, as completas no fim e **INICIAR**.
- O HUD do v8: as lascas e as lascas vermelhas em cima e a barra vertical do andar à esquerda, com a coluna dos bônus.
- Você escolhe; a escolha vira decisão no registro.

**Como ficou (2026-11-17):** os mockups foram montados por cima de capturas reais do jogo (a LOJA de permanentes, o resumo do dia e o HUD, tiradas na cena Game), em `tools/ci/out/`:

- `m9d-e0-descanso.png`: o **ZZZ**, a tela **preta** e a **desfocada** lado a lado (as duas em tamanho cheio em `m9d-e0-descanso-preta.png` e `-desfocada.png`). O painel é o da LOJA: no título "DIA 1 ENCERRADO"; embaixo, o resumo (andar, tempo, lascas vermelhas ganhas e o banco), a lista das permanentes com o **Dedo Fogoso** (luvas vermelhas) e o **Atalho** (botas douradas), dois ícones do mesmo pacote dos outros, e a Promoção com "Pede o Treinar no 5"; no fim, **INICIAR**.
- `m9d-e0-hud-folha.png`: o HUD de hoje, o do v8 e a LOJA com o dano e a velocidade no topo (`m9d-e0-hud.png` e `-hud-loja.png` em tamanho cheio). A barra do andar à esquerda tem um trecho de cor por bioma (10 andares cada), o que já se desceu nesta run em âmbar, a seta com o andar de agora, um traço branco no recorde e tracinhos nos marcos 5, 10, 20 e 50. Os bônus passam para o lado da barra.
- **Uma observação:** com nove permanentes, o painel cobre quase a tela toda. Entre a preta e a desfocada, a diferença fica só nas bordas e na passagem do ZZZ para a loja. A preta é a do GDD e não custa um desfoque por quadro no celular.

### E1 — O núcleo do descanso e as permanentes novas

- **Entre runs:** depois do `Rest`, a simulação fica **descansando**. O `Tick` não anda nada (nem anão, nem estagiário, nem cartas, nem bônus), e o tempo do dia não conta. `StartRun()` (o INICIAR) monta a run nova com as permanentes compradas (Alma dos Anões, Promoção). Quem descansa pelo atalho do DBG passa pelo mesmo estado.
- **Compra só no descanso:** `PurchasePermanent` responde `Locked` fora do descanso (o DBG continua dando de graça).
- **Promoção com Treinar 5:** um campo novo no balanço (`internPromotionTrainingLevel`), no lugar do "Treinar no limite"; o setup põe 5 no `Balance_GDD_v8`.
- **Dedo Fogoso** e **Atalho:** duas linhas novas no `PermanentId` e no balanço, com os números do v8.2. O Atalho muda a distância da fila (`CrystalTrack`) e a corrida do offline.
- **Distância de 3 m** (se P6 = a): `minCrystalSpacing` vira a base de 3 m, e a fila e o offline usam `base − 0,5 m × Atalho`.
- **Save v9:** o "descansando" e o resumo do dia que estava na tela. Um save v8 abre numa run em andamento, como hoje.
- **DBG → Dia:** "Descansar agora" leva direto à tela do descanso (sem o modal e sem o ZZZ). Os "+10" e "+100 lascas vermelhas" que já existem continuam; entra "Atalho: n" (um toque sobe um nível, de 5 volta a 0).
- **Testes EditMode:** nada anda descansando; INICIAR monta a run com Alma e Promoção; comprar fora do descanso é `Locked`; Promoção com o Treinar 4 e 5; o crítico com o Dedo Fogoso e com o excedente; a distância com o Atalho de 0 a 5; o offline com o Atalho; o save v9 e o v8 abrindo.

**Como ficou (2026-11-17):**

- **A run parada:** o `MiningRunner.Rest` monta a run nova já com `IsResting` e grava. Descansando, a `MiningSimulation` não anda (`Tick`), o toque e a loja da run não valem, o offline não conta (D-275) e não dá para descansar de novo. O `StartRun()` (o INICIAR) monta **outra** run, com as permanentes de agora (Alma, Promoção e Atalho entram certos), grava e avisa (`RunStarted`).
- **A tela provisória:** até a E2, a tela do descanso é o **resumo do dia** de antes. Ele abre pelo evento `Rested` (pelo modal, pelo DBG e ao reabrir o jogo descansando); **COMEÇAR** faz o INICIAR; **PERMANENTES** abre a LOJA na aba delas e, ao fechar a loja ainda descansando, o resumo volta com o banco de agora.
- **Compra só no descanso:** `PurchasePermanent` é `Locked` fora dele, e a linha da LOJA mostra o preço sem acender; o selo "!" da LOJA também só acende por uma permanente descansando (D-276). Os atalhos do DBG continuam dando níveis de graça.
- **Promoção no Treinar 5:** `PermanentRules.PromotionTrainingLevel` (0 = o limite do Treinar, como no v7). O setup põe 5 no `Balance_GDD_v8`.
- **Dedo Fogoso:** +0,5 por nível no multiplicador do crítico (×1,5 → ×2,0 no 1), somado ao excedente da chance acima de 100% (D-249). Linha nova na LOJA ("Dano crítico +0% » +50%"), luvas em chamas.
- **Atalho:** a distância entre cristais é `3 m − 0,5 m × nível` (6 unidades no nível 0, 1 no 5), na fila e na corrida do offline. Linha nova na LOJA ("Distância −0 m » −0,5 m"), botas. O `minCrystalSpacing` do `Balance_GDD_v8` passou de 2 para **6** (3 m, D-277). Efeito visível no offline: a corrida por cristal no andar 1 vai de 0,5 s para 1,5 s, então 1 h fora rende 2.400 cristais em vez de 7.200 (até o Atalho).
- **LOJA → Permanentes rola:** com 9 linhas de 40 px (a moldura tem 38 px, não dá para encolher), a lista passou do VOLTAR. Ela foi para dentro de uma área com rolagem, no mesmo lugar; arrastar com o dedo mostra as de baixo.
- **Save v9:** guarda o resumo do dia que estava na tela; reabrir descansando volta ao resumo, sem offline. Um save v8 abre jogando.
- **DBG:** Dia → **Atalho 0/5** (um toque sobe um nível; do 5 volta ao 0) e o resumo do menu mostra o Fogoso e o Atalho; Cálculos → **Distância: 3 m** ⇄ 1 m (a de antes, para comparar). "Descansar agora" agora para no resumo (a run espera o COMEÇAR).
- **Testes (2026-11-17, todos verdes: EditMode 510, PlayMode 162):** `RestShopTests` (EditMode, novo) e os casos novos de save v9; os testes que compravam permanentes no meio da run passam a comprar descansando, e os PlayMode do descanso medem a run parada até o COMEÇAR.

### E2 — A tela do descanso

- O **DESCANSAR** do modal (e o do coração da montanha) mostra o **ZZZ** e depois a **tela do descanso** (o fundo escolhido na E0): o resumo do dia em cima (P1), a lista das permanentes com preço e compra, e **INICIAR** no fim. O resumo do dia antigo sai, e PERMANENTES e COMEÇAR saem com ele.
- Comprar atualiza o banco de lascas vermelhas no topo. INICIAR fecha a tela e a run nova começa ali.
- **Os dois fundos (D-279):** a preta e a do jogo desfocado, com um botão no DBG para trocar; o Bruno escolhe vendo. Até lá, a preta.
- **LOJA → Permanentes** vira consulta (P5).
- Textos em dados (`FeedbackTexts`), montado pelo `ProjectSetup`.
- **Testes PlayMode:** DESCANSAR → ZZZ → tela; o anão não se mexe com a tela aberta; comprar e INICIAR; fechar e abrir com a tela aberta volta nela; a aba da LOJA não compra; o coração da montanha passa pelo mesmo caminho.

**Como ficou (2026-11-17):**

- **A tela do descanso** (`RestShopScreen`, objeto `RestShop` na cena Game) abre pelo evento `Rested`. Pelo DESCANSAR do modal (e do coração da montanha, que passa pelo mesmo modal), primeiro o **ZZZ**: três Z âmbar que sobem e acendem em 1,5 s; um toque pula. Pelo "Descansar agora" do DBG e ao reabrir o jogo descansando, direto na tela, sem o ZZZ (`MiningRunner.RestedQuietly`).
- **O painel** tem a largura da LOJA e quase a altura da tela. Na barra de título, "DIA N ENCERRADO" e o banco de lascas vermelhas (que desce a cada compra). Embaixo, o quadro do resumo (o andar a que chegou, o tempo do dia e as vermelhas ganhas) e o aviso "Compre com o jogo parado. A run começa no INICIAR.". Depois, as 9 permanentes com preço e botão, numa lista que rola (as do limite vão para o fim), e **INICIAR**.
- **Os dois fundos (D-279):** a **preta** (padrão) e a **desfocada**, trocadas em DBG → Dia → **Fundo: preta / desfocada**; com a tela aberta, troca na hora (o DBG fica por cima da tela). A desfocada é uma foto da câmera do mundo tirada ao abrir, bem pequena e ampliada com filtro, escurecida. Tirar a foto uma vez basta, porque o jogo está parado, e o celular não paga desfoque a cada quadro. Diferença do mockup: a foto pega o corredor, mas não a interface (HUD e botões), que fica fora da câmera.
- **O resumo do dia antigo saiu**, com o COMEÇAR e o PERMANENTES.
- **LOJA → Permanentes em consulta (D-276):** durante a run, cada linha mostra o nível e o efeito de agora ("Dano +10%"), sem preço nem botão, e "Compre ao Descansar" no alto da lista. O "!" da LOJA não acende por elas.
- **Textos** em dados: `restShopNote` e `restShopStart` nos `FeedbackTexts` (e "Chegou ao andar" no lugar de "Chegou ao nível"); `permanentsConsultNote` no `ShopCatalog`.
- **Testes:** `RestShopScreenTests` (novo: o ZZZ passa sozinho e nada se mexe atrás; o botão do fundo troca com a tela aberta; a Promoção mostra o que pede); `RestFlowTests`, `PermanentShopTests`, `SaveFlowTests` e `LongSessionTests` passam pela tela nova.

### E3 — HUD do v8 (P7)

- O HUD com lascas e lascas vermelhas, a barra vertical do andar à esquerda e o dano e a velocidade na LOJA, como o mockup da E0.
- O âmbar dos bônus vai junto para o lugar novo.
- **Testes PlayMode:** o HUD atualiza com as lascas e as vermelhas; a barra do andar acompanha a descida e o recorde.

### E4 — Sessão longa e celular

- `LongSessionTests`: descansa pelo botão, espera o ZZZ, compra na tela do descanso (com o jogo parado: a foto antes e depois do tempo passar é a mesma), toca INICIAR e fecha e abre no meio do descanso.
- **Moto G75** com o roteiro abaixo (ou pulado, como na D-271, se você preferir).

## Como testar (Bruno) — ao fim de cada etapa

- **E0:** abrir os mockups (imagens no chat) e escolher.
- **E1:** sem tela nova ainda (a do descanso é o resumo do dia de antes). Antes, `Tools → Miners Idle → Projeto → Aplicar configuração do projeto`. Cena `Bootstrap`, Play:
  1. Logo de cara, os cristais estão a **3 m** um do outro (antes, 1 m). DBG → Cálculos → **Distância: 3 m** troca para 1 m e volta.
  2. DBG → Dia → **+100 vermelhas** e, na LOJA → Permanentes, tentar comprar: o preço aparece, mas o botão não acende (só no descanso).
  3. DBG → Dia → **Descansar agora**: abre o resumo do dia e **nada se mexe** atrás (o anão, os cristais, o tempo).
  4. No resumo, **PERMANENTES**: agora compra. Comprar o **Dedo Fogoso** e o **Atalho**, fechar a LOJA: o resumo volta, com o banco descontado.
  5. **COMEÇAR**: andar 1, a run anda, e os cristais vêm mais perto (2,5 m com o Atalho 1).
  6. DBG → Dia → **Atalho 0/5**: cada toque aproxima os cristais que nascem depois; no 5, quase colados; mais um toque volta a 3 m.
  7. Descansar de novo e fechar o jogo no resumo; abrir: volta no resumo, sem "Bem-vindo de volta".
  8. Com o Treinar no 5 (DBG → Estagiário ou comprando), a Promoção acende no descanso.
- **E2:** antes, `Tools → Miners Idle → Projeto → Aplicar configuração do projeto`. Cena `Bootstrap`, Play:
  1. LOJA → Permanentes: cada linha mostra o nível e o efeito de agora, sem preço nem botão, e "Compre ao Descansar" no alto.
  2. Chegar ao andar 10 (DBG → Andar), **Descansar** → **DESCANSAR**: o ZZZ (1,5 s; um toque pula) e a tela do descanso, de fundo preto.
  3. DBG → Dia → **Fundo: preta**: vira **desfocada** na hora, com o corredor atrás. Tocar de novo volta à preta. Comparar as duas e escolher.
  4. O anão parado atrás; DBG → Dia → **+100 vermelhas**, comprar a Memória e o Dedo Fogoso: o banco no alto desce; arrastar a lista mostra as de baixo.
  5. Fechar o jogo e abrir: a mesma tela, sem o ZZZ, com as compras.
  6. **INICIAR**: andar 1, a run começa.
  7. DBG → Dia → **Descansar agora**: a tela abre direto, sem o ZZZ.
- **E3:** o HUD novo, a barra do andar descendo junto com o Próximo Andar.
- **E4:** a sessão longa é automática; no celular, o roteiro abaixo.

### Roteiro do Moto G75 (E4)

1. **Save do 9c:** instalar por cima, sem desinstalar. Abre na run de antes.
2. **Descanso:** descansar no andar 10 ou mais: ZZZ, a tela, o anão parado.
3. **Fechar no descanso:** fechar pelo multitarefa, esperar 2 min e abrir: a mesma tela, sem "Bem-vindo de volta".
4. **Compras:** a Promoção (com o Treinar 5), INICIAR: a run começa com o estagiário.
5. **Atalho:** o Atalho no nível 1 ou mais: os cristais mais perto.
6. **Sessão curta:** 10 min em ×20 com dois descansos, sem travar.

## Critério de pronto

- O descanso para o jogo; a run nova só começa no INICIAR; fechar e abrir no descanso volta nele.
- As permanentes só se compram no descanso; a LOJA mostra a consulta.
- O Dedo Fogoso e o Atalho existem, com os números do v8.2; a Promoção pede o Treinar 5.
- O HUD é o do v8, com a barra do andar.
- Testes EditMode e PlayMode escritos e passando; o Moto G75 (ou a sua decisão de pular).
- README, Roadmap, Análise do v8 e decisões atualizados.

## Riscos

- **O começo mais lento (P6).** Com 3 m entre cristais e o v8 já mais duro por andar, o andar 1 pode ficar arrastado. O DBG → Cálculos compara 1 m e 3 m; se ficar ruim, a decisão é sua, com o número na mão.
- **"Nada correndo" toca em muita coisa.** Cartas do raro, boost do AD, transição de andar, autosave e o relógio do dia: cada um precisa respeitar o descanso. O teste "a foto antes e depois do tempo passar é a mesma" pega o que escapar.
- **Testes que supõem a run nova na hora** (`RestFlowTests`, `DaySummary`, a sessão longa, os do coração) quebram. Cada um é corrigido para medir o caminho novo, nunca apagado.
- **O HUD mexe no que o jogador olha o tempo todo.** Por isso o mockup antes, e a etapa separada.

## Fora deste marco

- A Bomba e a permanente dela (Bloco E, D-240, D-255).
- O tier 2 do raro (500 lascas vermelhas), o contador de 60 s e os 3 anúncios (Bloco D).
- O toque e o combo (Bloco F).
- A câmera de 60° e o ajuste fino da distância com ela (Bloco G).
- O cristal rachando (Bloco B).

## Esforço

| Etapa | Horas |
|---|---|
| E0 | 2–3 |
| E1 | 6–8 |
| E2 | 5–7 |
| E3 | 4–6 |
| E4 | 2–3 |
| **Total** | **~19–27 h** (cerca de uma semana do Bruno) |

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| Plano | ✅ aprovado em 2026-11-17: P1…P7 com as recomendações (D-272…D-278) | — |
| E0 Mockups | 🔄 mockups prontos em 2026-11-17; o fundo se escolhe no jogo, na E2 (D-279); o HUD aguarda a escolha | Ver "Como ficou" na E0 |
| E1 Núcleo do descanso e permanentes novas | ✅ aprovada pelo Bruno em 2026-11-17 | Ver "Como ficou" na E1 |
| E2 A tela do descanso | 🔄 pronta em 2026-11-17, aguardando o teste do Bruno e a escolha do fundo (D-279) | Ver "Como ficou" na E2 |
| E3 HUD do v8 | ⏳ | — |
| E4 Sessão longa e celular | ⏳ | — |
