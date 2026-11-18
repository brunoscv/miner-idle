# Marco 9g — Bomba e o fim do toque

> Plano de 2026-11-18, **aguardando as respostas do Bruno (P1…P10)**. São os **Blocos E e F** da [Análise do GDD v8](Analise%20-%20GDD%20v8.md) §5, juntos de propósito: o toque sai quando a Bomba entra, para a tela nunca ficar sem interação. Vem depois do [Marco 9f](Marco%209f%20-%20Cristal%20rachando.md) (aprovado em 2026-11-18, D-306). Segue o processo do v8 (D-298): as melhorias vistas nos testes vão para a [lista de ajustes](Ajustes%20do%20v8%20-%20lista.md). Base: GDD v8.2 §2.1, §3.4, §7.4 e as decisões D-237, D-240 e D-255.

## Objetivo

**Tocar no cristal deixa de bater; quem bate agora é a Bomba.** No centro da barra de baixo, entre o Descansar e o Próximo Andar, fica a bomba, carregando em **5 s**. Cheia, o jogador a **arrasta para cima** e ela voa até o cristal, tirando uma **% fixa da vida máxima**: a Pequena tira 10%, o Barril tira 100%. A bomba não acaba (D-240); é só esperar carregar de novo. As bombas maiores se compram no descanso, com lascas vermelhas. Saem o dano do toque, o combo e os bônus do combo (D-237).

**Para você:** jogar alguns andares só com a bomba, arrastando para cima a cada 5 s, e ver o cristal rachar de uma vez; comprar a Pequena Vermelha no descanso e ver os 20%; tocar no corredor e ver que não bate mais.

## As regras (o que já está decidido)

- **D-237:** saem o toque de 33% (D-86), o combo e os bônus dele (x10…x100, D-210…D-217). Tocar no cristal não dá dano. A interação passa a ser a Bomba, com *swipe up*.
- **D-240:** 8 bombas. Tier 1: Pequena 10%, Pequena Vermelha 20%, Normal 30%, Normal Vermelha 40%. Tier 2: Dinamite 50%, Pacote 60%, Caixa 70%, Barril 100%. Carga **sempre 5 s**. Não é consumível. A % é sobre a **vida máxima**, qualquer que seja o dano já sofrido.
- **D-255:** a Pequena vem de graça; **7 compras** até o Barril, a `10 + 2^(n−1)` lascas vermelhas (11, 12, 14, 18, 26, 42, 74). A bomba **não pega crítico nem bônus**.
- **GDD §3.4:** a carga conta **offline**.
- **D-290:** o código do bônus de velocidade ficou "até a Bomba dizer se precisa". A Bomba não usa: ele continua sem uso, só para o v7.

## O que já existe (e o que muda)

| Peça | Hoje (v8 até o M9f) | No 9g |
|---|---|---|
| **Toque no cristal** | 33% do golpe, 5% de crítico, teto de 15 por segundo (D-86, D-119) | **sem dano**, sem combo. O toque no corredor fica só com a faísca no dedo (P5) |
| **Combo** | conta os toques; x10/x30/x50/x100 dão bônus ao parar (M9b) | **sai** do v8: o balanço do v8 fica sem toque e sem marcos de combo; o código fica para o v7, como o AD de velocidade (D-290) |
| **Centro da barra de baixo** | vazio desde o M9c (D-269), guardado para a Bomba | o **botão da Bomba**, com a carga enchendo |
| **Permanentes** | Memória, Sangue, Coração, Dedos Nervosos, Alma, Treinar, Promoção, Dedo Fogoso, Atalho, Raro Dourado | **+ Bomba** (nível 0/7), no descanso |
| **Dedos Nervosos** | "+5% de crítico no golpe e no toque" | só no golpe (o toque não bate mais) |
| **DBG** | Efeitos → Toques automáticos; Bônus → combos e o acúmulo | **Bomba automática** e um menu **Bomba**; os botões do combo somem no v8 |

## Perguntas para o Bruno

**P1 — Onde a bomba cai.**
**[REC]** No **alvo do anão** (o cristal que ele está batendo ou para onde corre). Sem alvo (descanso, transição de andar, fila vazia), a bomba **não sai**: continua cheia, esperando.

**P2 — O tempo da bomba no ar.** "O código é dono do tempo; a animação só ilustra."
**[REC]** O núcleo tem um **voo de 0,4 s** (dado do balanço). O dano entra na **chegada**, como evento do núcleo, e a animação só mostra a bomba voando nesse tempo. Se o cristal quebrar antes (pelo golpe do anão), a bomba cai no **próximo alvo**. A carga dos próximos 5 s começa no lançamento.

**P3 — A quebra pela bomba.**
**[REC]** Paga como **qualquer quebra**: as lascas do cristal com os bônus de lascas que estiverem valendo (a regra "não pega bônus" é sobre o **dano**, D-255), conta na barra do andar, e o raro abre a janela das cartas. Não tem crítico.

**P4 — Como lançar.**
**[REC]** **Arrastar a bomba para cima**: o dedo começa **no botão da bomba** e sobe pelo menos ~40 px (na tela de 270×480), ou faz um *flick* rápido para cima. Tocar na bomba cheia sem arrastar mostra uma setinha "arraste para cima" pulsando. Começar o gesto no botão evita lançar sem querer ao rolar ou tocar no corredor. **Mockup antes (E0)**, com o gesto desenhado.

**P5 — O que o toque no corredor faz agora.** O GDD mantém o pilar "cada toque, um efeito satisfatório", mas sem dano (D-237).
**[REC]** Fica **só a faísca no dedo**, que já existe, sem dano, sem som de golpe e sem número. O teto de 15 toques e o "um dedo por vez" (D-119, D-123) deixam de importar e ficam no código do v7.

**P6 — A carga: quando anda e o offline.**
**[REC]** A carga anda com a **run correndo**: para no descanso e na transição de andar, como o contador do raro (D-283). Ela não é guardada no save, porque são só 5 s: o jogo aberto de novo (ou voltando do offline) começa com a bomba **cheia**. É o "a carga conta offline" do GDD, sem guardar nada. Uma bomba por vez: não acumula.

**P7 — A permanente da Bomba no descanso.**
**[REC]** Uma linha **"Bomba"** na tela do descanso, como as outras: nível 0/7, a bomba de agora e a próxima ("Pequena 10% » Pequena Vermelha 20%"), o preço em vermelhas (11 … 74). No máximo, "Barril 100%". Aparece também na consulta das permanentes da LOJA.

**P8 — A arte das 8 bombas.**
**[REC]** **Provisória agora:** um desenho de bomba gerado por código, em 8 cores e 2 tamanhos (tier 1 pequena, tier 2 grande), com o pavio aceso. Ela entra no botão, no voo e na linha do descanso. **Depois:** a sua arte, se você desenhar. Uma folha de 8 quadros de 32×32, como a dos cristais, entra sem mudar código.

**P9 — O efeito da explosão.**
**[REC]** Na chegada: um clarão laranja no cristal, faíscas grandes, o tremor da quebra (com o "Tremor" dos AJUSTES respeitado) e um som de explosão sintetizado, como os outros sons do jogo. O cristal racha pelo estágio novo do M9f (o brilho e as lascas da D-303 também valem). O número do dano sobe em laranja, para diferenciar do golpe.

**P10 — As sessões longas.**
**[REC]** A sessão longa deste marco vai para os **testes longos do fim** (como a D-282, D-291 e D-305). A **E6 do M9b** (sessão longa com o combo) é **reescrita lá**, sem o combo e com a bomba, porque o combo sai.

## Etapas

### E0 — Mockups (P4, P7, P8)

- A barra de baixo com a bomba no centro: vazia carregando (o anel ou a barra enchendo) e cheia (pulsando, com a setinha).
- O gesto: o dedo na bomba subindo e a bomba voando até o cristal.
- A linha **Bomba** na tela do descanso.

### E1 — A Bomba no núcleo (P1, P2, P3, P6)

- `BombRules` no balanço do v8: as 8 bombas (nome e %), a carga de 5 s e o voo de 0,4 s. Preenchidas pelo setup, como os anúncios.
- Na run: a carga, `CanThrowBomb`, `ThrowBomb()`, a bomba no ar e o evento `BombLanded`. O dano é a % da vida máxima, sem crítico nem bônus, e quebra como qualquer quebra.
- A permanente `PermanentId.Bomb` (7 compras, `10 + 2^(n−1)`), pelo nome no save, sem versão nova.
- **DBG → Bomba** (menu novo): Encher, Lançar, Próxima bomba (troca a bomba sem comprar) e **Bomba automática** (lança sempre que enche, atalho para pular a repetição).
- **Testes EditMode:** a carga de 5 s, parada no descanso e na transição; não lança vazia nem sem alvo; o voo de 0,4 s e o dano na chegada; a % da vida máxima com o cristal já ferido; sem crítico nem bônus; o Barril quebra com qualquer vida; o cristal quebrado no voo passa a bomba ao próximo; a quebra paga, conta na barra e abre o raro; as 7 compras e os preços; o save da permanente.

### E2 — A saída do toque e do combo (P5)

- O balanço do v8 fica com o **toque sem dano** e **sem combo**: uma chave nas regras do golpe (`TapMines = false`) faz o `Tap()` não bater nem contar combo; os marcos do combo ficam vazios no v8. O v7 continua igual, para os testes dele.
- Na tela: o texto "xN" do combo, o som dos marcos e a seleção do acúmulo do combo (DBG → Cálculos) somem no v8. A faísca no dedo fica.
- **DBG → Efeitos → Toques automáticos** dá lugar à **Bomba automática** no v8.
- O texto do Dedos Nervosos perde o "e do toque".
- **Os testes que batiam pelo toque.** São 26 arquivos, com ~76 chamadas de `Tap()`/`TapAt()`. Os que medem o **toque e o combo** passam a rodar com o balanço do v7 (ou ficam ignorados no v8, como os do AD de velocidade, D-294). Os que só usavam o toque **para causar dano** passam a usar a bomba ou o golpe. Nenhum é apagado para passar.

### E3 — Na tela (P4, P7, P8, P9)

- O botão da Bomba no centro da barra de baixo, com a carga, o pulsar e a setinha (pelo mockup da E0).
- O gesto de arrastar para cima (Input System, o mesmo leitor dos toques) e o *flick*.
- O voo (0,4 s, da barra até o cristal, em arco), a explosão, o número laranja, o tremor e o som.
- A linha **Bomba** no descanso e na consulta da LOJA.
- A arte provisória das 8 bombas (P8).
- **Testes PlayMode:** arrastar a bomba cheia lança e o cristal perde a %; arrastar vazia não lança; tocar no corredor não tira vida; comprar a Pequena Vermelha no descanso e ver os 20%; a Bomba automática do DBG.

### E4 — Sessão longa e celular

- Vai para os **testes longos do fim** (P10), junto com a E6 do M9b reescrita.

## Como testar (Bruno) — ao fim de cada etapa

- **E0:** abrir os mockups em `tools/ci/out/m9g-e0-*.png`.
- **E1:** DBG → Bomba → **Lançar** e ver a vida do alvo cair a % da bomba (com a barra de vida do DBG → Andar ligada, para ver o número). **Próxima bomba** até o Barril e ver o cristal quebrar de uma vez.
- **E2:** tocar no cristal e ver que não tira vida nem mostra o "xN".
- **E3:** arrastar a bomba para cima a cada 5 s por alguns andares; comprar a Pequena Vermelha no descanso (DBG → Dia → +100 vermelhas); DBG → Bomba → **Bomba automática** para ver o jogo andar sozinho com a bomba.

## Critério de pronto

- No v8, o toque não bate e não há combo; a Bomba é a interação.
- As 8 bombas com as % da D-240, a carga de 5 s, o voo, e a compra no descanso pela D-255.
- A quebra pela bomba paga e conta como qualquer quebra.
- Testes EditMode e PlayMode das etapas passando (os do toque e do combo no v7 ou ignorados no v8, nunca apagados); a sessão longa nos testes longos do fim.

## Riscos

- **Muitos testes dependem do toque** (26 arquivos). A E2 é a etapa mais trabalhosa, e o risco é de teste, não de jogo.
- **Ritmo.** Sem o toque, o começo de cada run fica mais lento (o toque dava +33% do golpe a cada toque). A Pequena (10% a cada 5 s) compensa pouco no começo e muito em andares altos, porque é % da vida. Sem o relatório de economia (D-247), o número fino aparece no jogo, e vai para a lista de ajustes.
- **O gesto no celular.** O arrastar pode brigar com o "um dedo por vez" do leitor de toques. Por isso o gesto começa no botão, e o Moto G75 confere nos testes longos.

## Fora deste marco

- A câmera de 60° (Bloco G).
- A arte final das bombas (P8), se o Bruno desenhar.
- Uma bomba que acumula ou recarrega mais rápido (o GDD não tem).

## Esforço

| Etapa | Horas |
|---|---|
| E0 | 2–3 |
| E1 | 6–8 |
| E2 | 6–10 |
| E3 | 8–10 |

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| Plano | 📝 escrito em 2026-11-18, aguardando P1…P10 | — |
| E0 Mockups | ⏳ | — |
| E1 A Bomba no núcleo | ⏳ | — |
| E2 A saída do toque e do combo | ⏳ | — |
| E3 Na tela | ⏳ | — |
| E4 Sessão longa e celular | ⏭️ testes longos do fim (P10) | — |
