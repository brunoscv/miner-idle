# Marco 9g — Bomba e o fim do toque

> Plano de 2026-11-18, **aprovado no mesmo dia (D-307…D-317)**. O Bruno mudou três pontos: a bomba atinge **todos os cristais da tela** (P1), o lançamento é um **deslize da esquerda para a direita** (P4) e **o toque sai do jogo de vez**, com os testes dele. O resto seguiu as recomendações. São os **Blocos E e F** da [Análise do GDD v8](Analise%20-%20GDD%20v8.md) §5, juntos de propósito: o toque sai quando a Bomba entra, para a tela nunca ficar sem interação. Vem depois do [Marco 9f](Marco%209f%20-%20Cristal%20rachando.md) (aprovado em 2026-11-18, D-306). Segue o processo do v8 (D-298): as melhorias vistas nos testes vão para a [lista de ajustes](Ajustes%20do%20v8%20-%20lista.md). Base: GDD v8.2 §2.1, §3.4, §7.4 e as decisões D-237, D-240 e D-255.

## Objetivo

**Tocar não bate mais; quem bate é a Bomba.** No centro da barra de baixo, entre o Descansar e o Próximo Andar, fica a bomba, carregando em **5 s**. Cheia, o jogador **desliza o dedo da esquerda para a direita** e ela explode em **todos os cristais da tela**. Cada um perde uma **% fixa da vida máxima**: a Pequena tira 10%, o Barril tira 100%. A bomba não acaba (D-240); é só esperar carregar de novo. As bombas maiores se compram no descanso, com lascas vermelhas. Saem o toque, o combo e os bônus do combo (D-237, D-311).

**Para você:** jogar alguns andares só com a bomba, deslizando a cada 5 s, e ver os cristais da tela rachando juntos; comprar a Pequena Vermelha no descanso e ver os 20%; tocar no corredor e ver que não bate mais.

## As regras

- **D-237, D-311:** o toque não existe mais. Saem o dano do toque, o combo, os bônus do combo, o teto de toques e o "um dedo por vez", **também no v7**, e os testes deles. Fica só a faísca no dedo, como efeito.
- **D-240:** 8 bombas. Tier 1: Pequena 10%, Pequena Vermelha 20%, Normal 30%, Normal Vermelha 40%. Tier 2: Dinamite 50%, Pacote 60%, Caixa 70%, Barril 100%. Carga **sempre 5 s**. Não é consumível. A % é sobre a **vida máxima**.
- **D-255:** a Pequena vem de graça; **7 compras** até o Barril, a `10 + 2^(n−1)` lascas vermelhas (11, 12, 14, 18, 26, 42, 74). A bomba **não pega crítico nem bônus**.
- **D-307:** atinge **todos os cristais da tela**: no núcleo, a fila até um alcance à frente do anão (`bombReach`). Sem cristal no alcance, não sai.
- **D-308:** voo de **0,4 s**; o dano entra na chegada, nos cristais que estiverem no alcance naquele instante.
- **D-309:** a quebra pela bomba paga e conta como qualquer quebra, sem crítico.
- **D-310:** lançar = **deslizar da esquerda para a direita**.
- **D-312:** nada muda para compensar o ritmo sem o toque.
- **D-313:** a carga anda com a run, para no descanso e na transição, não vai para o save; o jogo abre com a bomba cheia.
- **D-314:** linha "Bomba" no descanso, nível 0/7.
- **D-315:** a arte é o `icon_bomb.png` do Bruno; as vermelhas recoloridas, o tier 2 provisório.
- **D-316:** a explosão: clarão, faíscas, tremor, som e número laranja.
- **D-317:** sessões longas nos testes do fim, com a E6 do M9b reescrita lá.
- **D-290:** o bônus de velocidade continua sem uso, só para o v7. A Bomba não usa.

## O que já existe (e o que muda)

| Peça | Hoje (v8 até o M9f) | No 9g |
|---|---|---|
| **Toque no cristal** | 33% do golpe, 5% de crítico, teto de 15 por segundo (D-86, D-119) | **sai do jogo e do código**. Fica a faísca no dedo |
| **Combo** | conta os toques; x10/x30/x50/x100 dão bônus ao parar (M9b) | **sai do jogo e do código**, com a coluna dos bônus do combo, o "xN", o som dos marcos e o acúmulo no DBG → Cálculos |
| **Centro da barra de baixo** | vazio desde o M9c (D-269), guardado para a Bomba | o **botão da Bomba**, com a carga enchendo |
| **Permanentes** | Memória, Sangue, Coração, Dedos Nervosos, Alma, Treinar, Promoção, Dedo Fogoso, Atalho, Raro Dourado | **+ Bomba** (nível 0/7), no descanso |
| **Dedos Nervosos** | "+5% de crítico no golpe e no toque" | só no golpe |
| **DBG** | Efeitos → Toques automáticos; Bônus → combos | um menu **Bomba** com a **Bomba automática**; os botões do toque e do combo saem |

## As perguntas e as respostas

| | Pergunta | Resposta |
|---|---|---|
| P1 | Onde a bomba cai | **Todos os cristais da tela** (Bruno, D-307) |
| P2 | O tempo no ar | 0,4 s no núcleo, dano na chegada (D-308) |
| P3 | A quebra pela bomba | paga e conta como qualquer quebra (D-309) |
| P4 | Como lançar | **deslizar da esquerda para a direita** (Bruno, D-310) |
| P5 | O toque no corredor | só a faísca no dedo; o toque sai do jogo (Bruno, D-311) |
| P6 | A carga | anda com a run, sem save, abre cheia (D-313) |
| P7 | A compra | linha "Bomba" no descanso (D-314) |
| P8 | A arte | o `icon_bomb.png` do Bruno (D-315) |
| P9 | A explosão | clarão, faíscas, tremor, som, número laranja (D-316) |
| P10 | Sessões longas | nos testes longos do fim (D-317) |

## Etapas

### E0 — Mockups e a arte (D-310, D-314, D-315)

- A arte: `icon_bomb.png` em `Assets/_Project/Art/UI/` (96×96, como os outros ícones do Bruno, 48×48 ampliado 2×), o `.svg` em `ArtSource/`. As 8 variações geradas pelo setup: as vermelhas recoloridas, o tier 2 com outra cor e maior.
- **Mockups** (270×480, sobre capturas reais):
  - a barra de baixo com a bomba no centro, vazia carregando e cheia (pulsando, com uma setinha "→" de dica);
  - o **deslize**: o dedo da esquerda para a direita, com o rastro, e a bomba explodindo nos cristais da tela;
  - a linha **Bomba** na tela do descanso.
- O mockup propõe o tamanho mínimo do deslize (~60 px na tela de 270) e onde ele pode começar: em qualquer lugar do corredor, fora dos botões da interface.

**Como ficou (2026-11-18):** o Editor estava aberto, então os mockups foram montados fora do Unity, sobre as capturas reais do M9e (interface) e do M9f (corredor e cristal), na escala da tela (1080×2400 = 270×480 ×4). As bombas dos mockups seguem a mesma regra do gerador do jogo.

- **A arte:** `icon_bomb.png` em `Assets/_Project/Art/UI/` e o `.svg` em `ArtSource/ui/`. O `BombArtSetup` (no setup do projeto) gera `bombs.png`, com os 8 quadros de 48 px. Só o corpo muda de cor (as duas cores do corpo, nas linhas 15–31 do quadro); o contorno, o brilho, o pavio e a sombra ficam. As vermelhas são vermelhas; Dinamite (laranja), Pacote (couro), Caixa (oliva) e Barril (ouro) têm cor provisória (lista de ajustes, item 1). O `bombs.png` sai na primeira rodada do setup com o Editor fechado, e a arte entra no commit da E1 com os `.meta`.
- `m9g-e0-bombas.png`: as 8 bombas, ampliadas, com os nomes.
- `m9g-e0-barra.png` e `m9g-e0-tela.png`: o botão da bomba no vão do meio, entre o Descansar e o Próximo Andar, com a moldura do Próximo Andar.
  - **Carregando:** a bomba apagada (cinza, meio transparente), o fundo do botão enchendo de baixo para cima em 5 s e os segundos que faltam embaixo.
  - **Cheia:** a bomba acesa, com um contorno amarelo que pulsa e a setinha "→" embaixo.
- `m9g-e0-deslize.png`, em três quadros:
  1. **O deslize.** O rastro segue o dedo, com a faísca no fim. Vale em qualquer lugar do corredor (verde no mockup), fora da barra do andar, da coluna da direita e da barra de baixo. O mínimo é **60 px na tela de 270** (um pouco menos de um quarto da largura), só da esquerda para a direita.
  2. **O voo.** A bomba sai do botão e faz um arco de 0,4 s até cada cristal da tela.
  3. **A explosão** em cada um, com o número laranja (no mockup, "-10%" só para indicar; no jogo é o dano).
  - O espaçamento é de 3 m, então quase sempre há um cristal na tela, às vezes dois (o próximo entrando por baixo). O mockup mostra dois.
- `m9g-e0-descanso.png`: a linha **Bomba**, no molde das outras: o ícone da bomba atual, "nv 0/7", "Pequena 10% » 20%" e o preço (11 vermelhas na primeira compra). Fica entre o Atalho e a Promoção.

### E1 — A Bomba no núcleo (D-307, D-308, D-309, D-313, D-314)

- `BombRules` no balanço do v8: as 8 bombas (nome e %), a carga de 5 s, o voo de 0,4 s e o **alcance** (`bombReach`). O alcance é medido nesta etapa: até onde a câmera de hoje mostra o corredor à frente do anão. Preenchidas pelo setup, como os anúncios.
- Na run: a carga, `CanThrowBomb`, `ThrowBomb()`, a bomba no ar e os eventos `BombThrown` e `BombLanded` (com os cristais atingidos). O dano é a % da vida máxima de **cada cristal no alcance** na chegada, sem crítico nem bônus, e quem quebra paga como qualquer quebra.
- A permanente `PermanentId.Bomb` (7 compras, `10 + 2^(n−1)`), pelo nome no save, sem versão nova.
- **DBG → Bomba** (menu novo): Encher, Lançar, Próxima bomba (troca a bomba sem comprar) e **Bomba automática** (lança sempre que enche, atalho para pular a repetição).
- **Testes EditMode:**
  - a carga de 5 s, parada no descanso e na transição; não lança vazia nem sem cristal no alcance;
  - o voo de 0,4 s e o dano na chegada; a % da vida máxima com o cristal já ferido; sem crítico nem bônus;
  - **todos os cristais no alcance** perdem a %, e os de fora não; o Barril quebra todos;
  - a quebra paga, conta na barra e abre o raro;
  - as 7 compras e os preços; o save da permanente.

### E2 — O toque sai do jogo (D-311)

- **Núcleo:** saem `Tap()`, o combo, os bônus do combo (`ComboBonusRule`, `ComboStacking`, a fonte Combo dos bônus), o teto de toques e a fração do toque das regras do golpe (`StrikeRules`) e dos balanços (v7 e v8). Saem também o crítico do toque, o "toque quebra" do golpe na chegada e o arremate do toque (D-124). Um bônus de combo num save antigo é descartado ao abrir.
- **Jogo e tela:** o `TapInput` vira o leitor do **gesto da bomba** (E3), com a faísca no dedo. Saem o "xN" do combo, o som dos marcos do combo, os bônus do combo na coluna e no catálogo, o acúmulo do combo no DBG → Cálculos e no `calculations.json`, e o "Toques automáticos" do DBG. O Dedos Nervosos perde o "e do toque".
- **Testes:**
  - saem os do toque e do combo (`ComboBonusTests`, `ComboBonusFlowTests`, `TapGuardTests`, `TapInputTests`, `TouchFingerTests` e os casos do toque em `StrikeTests` e afins);
  - os que usavam o toque **para causar dano** (feedback, som, coluna, raro, estagiário, alocação) passam a usar o golpe ou a bomba.

### E3 — Na tela (D-310, D-314, D-315, D-316)

- O botão da Bomba no centro da barra de baixo, com a carga, o pulsar e a setinha (pelo mockup da E0).
- O **deslize da esquerda para a direita** (Input System), com o rastro do dedo.
- O voo (0,4 s, da barra até os cristais), a explosão em cada cristal, o número laranja, o tremor e o som.
- A linha **Bomba** no descanso e na consulta da LOJA.
- **Testes PlayMode:**
  - deslizar com a bomba cheia lança, e os cristais da tela perdem a %; deslizar vazia não lança;
  - deslizar para a esquerda ou para cima não lança; tocar no corredor não tira vida;
  - comprar a Pequena Vermelha no descanso e ver os 20%; a Bomba automática do DBG.

### E4 — Sessão longa e celular

- Vai para os **testes longos do fim** (D-317), junto com a E6 do M9b reescrita.

## Como testar (Bruno) — ao fim de cada etapa

- **E0:** abrir os mockups em `tools/ci/out/m9g-e0-*.png`.
- **E1:** DBG → Bomba → **Lançar** e ver a vida dos cristais da tela cair a % da bomba (com a barra de vida do DBG → Andar ligada, para ver). **Próxima bomba** até o Barril e ver tudo quebrar de uma vez.
- **E2:** tocar no cristal e ver que não tira vida nem mostra o "xN".
- **E3:** deslizar da esquerda para a direita a cada 5 s por alguns andares; comprar a Pequena Vermelha no descanso (DBG → Dia → +100 vermelhas); DBG → Bomba → **Bomba automática** para ver o jogo andar sozinho com a bomba.

## Critério de pronto

- O toque não bate e não há combo, em nenhum balanço; a Bomba é a interação.
- As 8 bombas com as % da D-240, a carga de 5 s, o voo, todos os cristais da tela atingidos, e a compra no descanso pela D-255.
- A quebra pela bomba paga e conta como qualquer quebra.
- Testes EditMode e PlayMode das etapas passando; a sessão longa nos testes longos do fim.

## Riscos

- **A E2 mexe em muita coisa** (o toque e o combo estão no núcleo, no save, no DBG, nos cálculos, na coluna dos bônus e em 26 arquivos de teste). Por isso é uma etapa só para isso, entre o núcleo da bomba e a tela.
- **O alcance da bomba depende da câmera.** O valor da E1 é o da câmera de hoje; com a de 60° (Bloco G), ele se mede de novo.
- **O deslize no celular** pode brigar com a rolagem de algum painel aberto. Ele só vale com o corredor livre (sem LOJA, descanso ou janela aberta), e o Moto G75 confere nos testes longos.

## Fora deste marco

- A câmera de 60° (Bloco G).
- A arte final do tier 2 (Dinamite, Pacote, Caixa, Barril), quando o Bruno desenhar (D-315).
- Uma bomba que acumula ou recarrega mais rápido (o GDD não tem).

## Esforço

| Etapa | Horas |
|---|---|
| E0 | 2–3 |
| E1 | 6–8 |
| E2 | 6–8 |
| E3 | 8–10 |

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| Plano | ✅ aprovado em 2026-11-18 (D-307…D-317) | P1, P4 e o toque mudados pelo Bruno |
| E0 Mockups e a arte | 🔍 aguardando o Bruno | mockups em `tools/ci/out/m9g-e0-*.png`; `bombs.png` sai no próximo setup com o Editor fechado |
| E1 A Bomba no núcleo | ⏳ | — |
| E2 O toque sai do jogo | ⏳ | — |
| E3 Na tela | ⏳ | — |
| E4 Sessão longa e celular | ⏭️ testes longos do fim (D-317) | — |
