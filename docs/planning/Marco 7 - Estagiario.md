# Marco 7 — Estagiário

> Plano de 2026-11-05. **Perguntas respondidas pelo Bruno no mesmo dia (D-164…D-173):** todas as recomendações aceitas. Base: GDD v7 ([`docs/gdd v7.pdf`](../gdd%20v7.pdf), §4.1, §4.2, §5; D-132) e as decisões já tomadas: **D-57** (corre ao lado do anão, um pouco atrás), **D-76** (o anão grisalho de capacete de aço), **D-95** (uma recompensa por cristal, conta na trava do Mapa), **D-137** (descansar zera o estagiário, a não ser com a Promoção), **D-139** (o offline é dele, conta no M9), **D-143** (o padrão é o GDD; as alternativas de custo ficam no DBG → Cálculos) e **D-155** (a 3ª aba da LOJA é a dele).
>
> Começa **antes de fechar o Marco 8b** (D-163): a E5 do 8b e a validação das E1–E4 dele ficam para depois. O M7 vem antes do marco "Bônus temporários" (a ordem entre os dois estava em aberto desde a D-89).

## Objetivo

**Um segundo minerador que ajuda o anão no mesmo cristal.** O jogador contrata o estagiário por 500 lascas, vê o anão grisalho bater junto no cristal da frente e avançar com ele, e passa a comprar as melhorias dele numa aba própria da LOJA. Com as lascas vermelhas, **Treinar** deixa o estagiário mais forte em todas as runs, e a **Promoção** faz a run já começar com ele.

**Para você:** contratar no nível 3–5, comprar a Picareta dele e ver os cristais caírem mais rápido; descansar, comprar Treinar e sentir a diferença na run seguinte.

**O que o v7 simplificou:** no GDD v1 o estagiário escolhia o próprio alvo, e o risco maior do projeto era o alvo duplicado e a espera infinita (R-03, R-24, a [spec do mapa](../design/Spec%20-%20Estagiario%20no%20mapa.md)). No v7 ele **bate no mesmo cristal do anão e avança junto** (§5.2): não há reserva de alvo nem matriz de ordem de quebra. O risco que sobra é o de duas picaretadas no mesmo instante e o da quebra no meio do golpe, que os testes cobrem.

## As regras (o que o GDD v7 e as decisões já dizem)

**Contratar (§4.1, §5.1):** a linha **Estagiário** da 1ª aba, **compra única de 500** lascas. A compra cria o estagiário no cristal atual do anão; as compras seguintes são as da árvore dele, que só aparece depois de contratar.

**Alvo e avanço (§5.2):** ele bate no **mesmo cristal** do anão (também na parede) e, quando o cristal quebra, **avança junto**. Quebrou, paga uma vez, não importa quem deu o último golpe, e a quebra conta na trava do Mapa (D-95).

**A árvore dele (§5.4; custo em lascas comuns; *n* = a compra da própria melhoria, D-38):**

| Melhoria | Custo da compra *n* | Efeito | Limite |
|---|---|---|---|
| **Picareta** | `50 × 4^(n−1)` (50, 200, 800, 3.200…) | dano `5 × 2^(n−1)` (5, 10, 20, 40…) | 20 |
| **Velocidade** | `50 × 5^(n−1)` (50, 250, 1.250…) | −0,1 s por compra, a partir de **2,0 s** | 20 (P2) |
| **Mineração** | `50 × 1,2^(n−1)` (50, 60, 72…) | +100% de lascas por compra | +1000% (10) |

**Permanentes dele (§4.2; custo em lascas vermelhas):**

| Melhoria | Custo da compra *n* | Efeito | Limite |
|---|---|---|---|
| **Treinar Estagiário** | `10 + 2^(n−1)` (11, 12, 14, 18…; total 1.123) | dano do estagiário ×(1 + 0,1 × nível) | 10 |
| **Promoção do Estagiário** | 50 | a run já começa com o estagiário contratado; pede Treinar 10 | compra única |

**Descansar (D-137):** zera o estagiário com o resto da run. Com a Promoção, a run nova já começa com ele (P6).

**Alternativas no DBG → Cálculos (D-143):** Picareta do estagiário `50 × 3^(n−1)` ou `50 × 2,5^(n−1)`; Velocidade `50 × 3^(n−1)` ou `50 × 2^(n−1)`. **O padrão continua o GDD** (18 tri e 238 tri para completar as duas linhas).

**O que fica para o M9:** o offline (só ele minera fora do jogo, até 6 h, D-139).

## Decisões (respondidas pelo Bruno em 2026-11-05: D-164…D-173)

O Bruno aceitou todas as recomendações ("todas suas respostas para as perguntas estão ok"). A P1 virou a D-164, a P2 a D-165, e assim até a P10, a D-173 (no fim das etapas). As alternativas ficam aqui só como registro do que foi descartado.

1. **P1 (D-164) (G27): quanto ele bate antes de comprar a Picareta dele?** O GDD diz "atributos base do minerador" na contratação e "nível 1 é o valor-base" na Picareta.
   - **Decidido:** contratado e sem compras, ele bate **1** (o Dano Base do anão, sem o crescimento pela caverna, porque o §5.3 diz que o dano dele vem só das melhorias dele) a cada **2,0 s**. A 1ª compra da Picareta dele (50) leva o dano a 5, a 2ª (200) a 10, e assim por diante, como a convenção da D-38.
   - Alternativa: a contratação já vem com a Picareta dele no nível 1 (dano 5), e a 1ª compra leva ao nível 2.
2. **P2 (D-165) (G27): a Velocidade dele para em 0,1 s?** O GDD diz 0 s, como dizia a do anão.
   - **Decidido:** o mesmo piso da D-135: 0,1 s, e a linha acaba em **19 compras** (2,0 − 0,1 × 19 = 0,1).
3. **P3 (D-166): o que a Mineração dele faz no jogo aberto?** A D-95 diz que cada cristal paga uma vez, seja quem for que quebrou.
   - **Decidido:** a Mineração dele vale **no offline** (no M9, quando só ele minera). No jogo aberto, o cristal paga sempre com a Mineração do anão. Na loja, a linha diz "Lascas offline +100% » +200%". Assim nenhum cristal paga menos por ter sido quebrado por ele.
   - Alternativa A: o cristal que **ele** quebra paga com a Mineração dele (enquanto a dele for menor que a do anão, contratar faria a mina render menos).
   - Alternativa B: a dele **soma** com a do anão no jogo aberto (como custa 1.298 para chegar a +1000%, a Mineração do anão, que custa 20 × 5^(n−1), deixaria de valer a pena).
4. **P4 (D-167): o que da parte do anão vale para ele?**
   - **Decidido:** só as melhorias dele e o Treinar. Ele **não tem crítico** (a árvore dele não tem a linha), e o **Sangue da Montanha** e os **Dedos Nervosos** ficam só no anão (são "as 5 do anão", Marco 8b). O toque continua só do jogador.
5. **P5 (D-168): quando sai o 1º golpe dele num cristal?** Igual ao do anão (D-124: o modo do valor `arrivalStrike`, hoje A1, o relógio enche correndo).
   - **Decidido:** o mesmo modo do anão, com o relógio próprio dele, que conta enquanto o anão está batendo. Quando o cristal quebra, os dois correm juntos.
6. **P6 (D-169): com a Promoção, o que a run nova traz?**
   - **Decidido:** o estagiário contratado, com a árvore dele **no zero** (as melhorias dele são da run, como as do anão). Treinar continua valendo, como toda permanente.
7. **P7 (D-170): onde ficam Treinar e Promoção?** O plano do 8b diz "na aba dele". Mas elas se pagam com lascas vermelhas, e a Promoção serve justamente para quando ele **ainda não foi contratado** na run.
   - **Decidido:** na aba **PERMANENTES**, depois das 5 do anão, onde fica o banco de lascas vermelhas. A aba **ESTAGIÁRIO** fica com as 3 da run, em lascas comuns, e só aparece depois da contratação.
   - Na 1ª aba, a linha **Estagiário** (500) vai para o fim como "contratado" depois da compra, como uma permanente no limite.
8. **P8 (D-171): o que muda no HUD e no feedback?**
   - **Decidido:** nada novo no HUD (como na D-156): o DANO e a VELOCIDADE continuam os do anão, e os do estagiário aparecem na aba dele. Os números que saltam do cristal saem também nos golpes dele, **menores e cinza**, para o jogador ver os dois trabalhando sem confundir com o golpe do anão. O mockup da E4 mostra isso junto com a aba.
9. **P9 (D-172): e com o balanço GDD v5 (o seletor do DBG → Cálculos)?**
   - **Decidido:** o estagiário do v7 só existe no v7. No v5, a linha volta a ser "Em breve", como as permanentes, que somem no v5.

## Etapas

### E1 — Núcleo, save e DBG → Estagiário

- **Núcleo (C# puro):**
  - Linhas novas em `UpgradeId`: `Intern` (compra única de 500), `InternPickaxe`, `InternSpeed` e `InternMining`, com os números no `Balance_GDD_v7`. As do estagiário só se compram depois do `Intern` (`Purchases`).
  - `InternStats`: dano (P1, com o Treinar), intervalo (P2) e Mineração (P3), sempre recalculados do zero, como os `MinerStats`.
  - Na `MiningSimulation`: o estagiário (relógio próprio), que bate no `Target` do anão e corre com ele; os golpes dele saem num evento à parte, `InternHitLanded` (o `HitLanded` continua só do anão e do toque, para quem desenha o anão não confundir os dois). Uma quebra paga uma vez (D-95).
  - `PermanentId` ganha `InternTraining` e `InternPromotion` (pede Treinar 10). Descansar zera o estagiário; com a Promoção, a run nova já começa com ele.
- **Save:** os níveis já são gravados pelo nome da linha (run e permanente), e um nome desconhecido é ignorado. Então o estagiário entra sem versão nova, e um save v4 abre com ele não contratado. Um teste confirma.
- **DBG → Estagiário** (um menu novo, como o Dia): Contratar/Demitir, +1 nível em cada melhoria dele, Treinar +1, Promoção, e a linha do alto com dano, intervalo e golpes dele. O botão **Estagiário** do DBG → Câmera (que só ligava o desenho, D-76) sai: quem decide se ele aparece passa a ser a regra.
- **Testes EditMode:** custo e efeito de todos os níveis das 3 linhas e das 2 permanentes; sem contratar, nada da árvore se compra; os dois batem no mesmo cristal e nenhum golpe vai para um cristal que já quebrou; golpes dos dois no mesmo instante pagam uma vez; quebra pelo toque no meio do golpe dele; ele também bate na parede; descansar zera, com a Promoção não; save v4 abre sem estagiário e o de agora volta igual.

### E2 — Na tela

- O boneco do estagiário (o que já existe, D-76) aparece **quando ele é contratado**, ao lado e um pouco atrás do anão (D-57), e some ao descansar sem a Promoção. (Já veio na E1, junto com a saída do botão da câmera, para o boneco não sumir entre as duas etapas.)
- A animação de golpe dele segue os golpes do núcleo (o código é dono do tempo, a animação só ilustra), e ele corre quando o anão corre.
- Os números dos golpes dele saem no cristal (P8).
- **Testes PlayMode:** contratar pelo DBG mostra o boneco; descansar sem a Promoção esconde; com a Promoção ele já está lá na run nova; um golpe dele no núcleo dispara a picaretada dele na tela.

### E3 — A aba do Estagiário na LOJA

- **Primeiro, o mockup em 270×480**, como nas E3 e E4 do Marco 8b.
- Na 1ª aba, a linha **Estagiário** (500) deixa de ser "Em breve"; comprada, vai para o fim como "contratado".
- A **3ª aba, ESTAGIÁRIO**, aparece depois da contratação, com Picareta, Velocidade e Mineração dele, na mesma linha da loja (`UpgradeRow`) e com a prévia "de → para".
- **Treinar** e **Promoção** na aba das permanentes (P7). A Promoção fica apagada até o Treinar 10, com o motivo na linha.
- O "!" da LOJA e o das abas acendem também com as compras do estagiário, como na E4 do 8b.
- **Testes PlayMode:** a aba só aparece depois de contratar; comprar a Picareta dele por 50 sobe o dano dele para 5 na hora; a Promoção só acende com o Treinar 10; o "!" acende com uma compra dele.

### E4 — Alternativas no DBG → Cálculos, sessão longa e o celular

- **Seletores do estagiário no menu Cálculos** (D-143): Picareta · GDD · `3^` · `2,5^`; Velocidade · GDD · `3^` · `2^`. Gravados no `calculations.json` e mostrados no aviso fixo, como os da Picareta do anão.
- As alternativas das permanentes que faltavam na E5 do 8b entram aqui, juntas (D-173): Sangue e Coração · GDD · `1,5^` · linear; as 4 de 10 níveis · GDD · linear.
- **Sessão de 30 min** em ×20 pelo DBG, com o estagiário contratado e descansos no meio, sem travar (a Definição de Pronto do Roadmap).
- **Teste no Moto G75** com o fluxo inteiro: contratar → comprar as dele → descansar → Treinar → run nova.

**P10 (D-173): juntar a E5 do 8b com esta E4?** As duas são seletores no mesmo menu Cálculos e um teste no Moto G75. **Decidido:** sim, fazer as duas aqui, e o Marco 8b e o M7 são aprovados juntos no fim, com um único teste no celular.

## Como testar (Bruno) — ao fim de cada etapa

- **E1:** cena `Game.unity`, Play. DBG → **Estagiário** → Contratar: a linha do alto mostra "contratado · dano 1 · intervalo 2,00s", e o anão grisalho aparece ao lado do anão. +1 nv melhorias: dano 5 e intervalo 1,90s. Para ver os golpes dele contando: DBG → Caverna → Próxima caverna até o nível 20 ou mais (no nível 1 o anão quebra cada cristal com um golpe, e no mesmo instante ele bate primeiro). DBG → Dia → Descansar agora: o estagiário vai embora. Promoção e descansar de novo: a run nova já começa com ele.
- **E2:** a mesma cena. DBG → Estagiário → Contratar: o anão grisalho aparece ao lado do anão. DBG → Caverna → Próxima caverna até o nível 20 ou mais (no 1 ele não chega a bater). Ele levanta a picareta e bate no mesmo cristal no ritmo dele (a cada 2 s, sem as melhorias), fora do compasso do anão; cada golpe dele solta poucas faíscas cinza e um número cinza pequeno no cristal, sem tremer a tela. Quando o cristal quebra, ele corre junto. +1 nv melhorias algumas vezes: o ritmo dele acelera e o número cresce. DBG → Dia → Descansar agora: ele some; Promoção e descansar de novo: ele já está lá.
- **E3:** LOJA com lascas (DBG → Loja → +100 lascas até passar de 500): na aba MELHORIAS, o Estagiário custa 500; compre: a linha desce para o fim, apagada, com "contratado", e a aba ESTAGIÁRIO aparece à direita com o "!". Nela, Picareta "Dano 1 » 5", Velocidade "Golpe a cada 2,00s » 1,90s" e Mineração "Offline +0% » +100%", a 50 cada; compre a Picareta. Na aba PERMANENTES (DBG → Dia → +100 ◆), as 7 linhas cabem acima do VOLTAR; Treinar "Dano dele +0% » +10%" e a Promoção apagada com "Pede o Treinar no 10". DBG → Estagiário → Treinar +1 até o 10: o Treinar desce para o fim com "máx." e a Promoção acende. DBG → Estagiário → Demitir com a aba dele aberta: a aba some e a loja volta para MELHORIAS.
- **E4:** DBG → Cálculos, troque a Picareta do estagiário para `3^`: o custo na aba dele muda e o aviso fixo mostra a troca. No Moto G75, o fluxo inteiro.

## Critério de pronto

- O estagiário é contratado por 500 na 1ª aba, bate no mesmo cristal do anão e avança junto, sem alvo duplicado nem espera.
- A árvore dele (Picareta, Velocidade, Mineração) é comprada na aba ESTAGIÁRIO, com o custo e o efeito do GDD e as respostas das P1–P3.
- Treinar e Promoção funcionam entre as runs; descansar zera o estagiário, a não ser com a Promoção.
- As alternativas da D-143 do estagiário estão no DBG → Cálculos.
- O save guarda tudo, e um save v4 abre sem perda.
- Os atalhos do DBG → Estagiário funcionam.
- Os testes EditMode e PlayMode passam, a sessão de 30 min não trava e o fluxo foi testado no Moto G75.
- README e decisões atualizados.

## Riscos

- **Dois golpes no mesmo instante.** O anão e o estagiário podem vencer o relógio no mesmo passo da simulação; o segundo golpe não pode ir para um cristal que já quebrou, nem pagar duas vezes. A ordem fica fixa (o anão primeiro) e os testes da E1 cobrem.
- **Quebra no meio do golpe.** O anão já tem a regra de terminar a picaretada (D-126). O estagiário segue a mesma, para os dois não saírem correndo em tempos diferentes.
- **Custos de trilhões** (18 tri e 238 tri). Já é sabido (D-143); o DBG deixa o time testar as alternativas.
- **Espaço na tela.** O estagiário já foi desenhado ao lado do anão (D-57, D-76); com o Recuo da câmera (D-148), conferir que ele não sai do quadro nem cobre o cristal.

## Fora deste marco

- O offline (M9): a Mineração dele, se a P3 ficar como recomendado, só passa a render lá.
- Mais de um estagiário (o GDD diz que as compras seguintes melhoram o mesmo).
- A arte final do estagiário e da aba (M10).
- Bônus temporários e o cristal raro (marco próprio, D-89).

## Esforço

| Etapa | Horas |
|---|---|
| E1 | 7–9 |
| E2 | 3–4 |
| E3 (com o mockup) | 6–7 |
| E4 | 3–4 (+2 com a E5 do 8b) |

Total: **~19–24 h** (o Roadmap estimava 35–50 h para o estagiário do v1, que escolhia o próprio alvo).

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| Plano | ✅ escrito e respondido em 2026-11-05 (D-163…D-173) | — |
| E1 Núcleo, save e DBG → Estagiário | 🧪 implementada em 2026-11-05, aguardando validação do Bruno | **Núcleo:** linhas novas em `UpgradeId` (`Intern`, `InternPickaxe`, `InternSpeed`, `InternMining`; a árvore só se compra depois do `Intern`, `Purchases.Blocked`) e em `PermanentId` (`InternTraining`, `InternPromotion`, que pede o Treinar no limite). `InternStats` (D-164…D-167): sem compras, o Dano Base do anão a cada 2,0 s; Picareta `5 × 2^(n−1)`; Velocidade até 0,1 s em 19 compras; Mineração só nas lascas offline; × Treinar. Na `MiningSimulation`: o relógio do estagiário conta com o anão batendo e segue o mesmo modo de chegada (D-168); ele bate no alvo do anão depois do anão, no mesmo instante, e nunca num cristal que já caiu; a quebra dele paga uma vez com a Mineração do anão e conta na trava do Mapa; evento `InternHitLanded` e `InternChanged`. `Permanents.NewRun`: com a Promoção, a run começa com ele e a árvore no zero. **Dados:** o setup grava os números no `Balance_GDD_v7` (`EnsureGddV7Intern`); os outros balanços ficam sem estagiário (D-172). **Save:** sem versão nova (os níveis vão pelo nome); um save v4 abre com ele não contratado. **DBG → Estagiário** (`InternDebugPanel`, `InternDebugSetup`): Contratar, Demitir, +1 nv melhorias, Treinar +1, Promoção e a linha do alto; o DBG ganhou a 4ª fileira de botões. O botão Estagiário do DBG → Câmera saiu: o boneco aparece quando ele é contratado. O "!" da LOJA passa a olhar só as linhas que aparecem na loja (sem a aba dele, a compra de 500 não acende um "!" que o jogador não acha). Testes: EditMode `InternTests` (+22, com uma sessão longa com toques que confere que ele só bate no alvo do anão e nunca num cristal que caiu), `PermanentTests` mede as 5 do anão no atalho; PlayMode `InternDebugPanelTests` (+3), `InternArtTests` mede a contratação no lugar do botão, `DebugPanelTests` mede o 10º botão. Em batch: setup ok e idempotente, EditMode **278/278**, PlayMode **111/111** |
| E2 Na tela | 🧪 implementada em 2026-11-05, aguardando validação do Bruno | **Golpe próprio:** `DescentStage` escuta o `InternHitLanded` e guarda o instante do último golpe dele; parado no cristal, o quadro dele é o impacto e a volta da picareta a partir desse instante, e o levantar quando falta pouco no relógio dele (`InternHitTimer`), no mesmo passo de quadro do anão para o intervalo dele. Sem o hit-stop, que é do anão. Se o golpe dele quebra o cristal, a volta vai até o fim. Corre quando o anão corre; na parada da quebra do anão, fica no cristal. **Feedback** (`MiningFeedbackView.OnInternHit`, D-171): 4 faíscas cinza, o cristal treme pouco, sem tremor de tela, e o dano sobe do cristal num número cinza (`#B8BCC2`) com pouco mais de metade do tamanho do "CRÍTICO!". O número é só o valor, pelo formatador único: não há texto novo. Os golpes do anão continuam sem número (como antes). Testes: PlayMode `InternOnScreenTests` (+5: contratar pelo DBG mostra; descansar sem Promoção esconde; com Promoção já está lá; um golpe dele no núcleo põe o boneco no quadro de impacto no quadro seguinte e o número dele no cristal; a cor é cinza). Em batch: setup ok, EditMode **278/278**, PlayMode **116/116** |
| E3 Aba do Estagiário na LOJA | 🧪 implementada em 2026-11-05, aguardando validação do Bruno | Mockup em 270×480 aprovado pelo Bruno (D-174). **Catálogo** (`ShopCatalog`): a linha Estagiário deixa de ser "Em breve" e compra o `UpgradeId.Intern`, com o texto próprio da compra feita (`boughtLabel`, "contratado"); `internTabLabel` e `internEntries` (Picareta, Velocidade e Mineração dele, com os ícones das do anão); nas permanentes, Treinar e Promoção, com o `lockedFormat` ("Pede o Treinar no {0}"). **Núcleo:** `Permanents.RequiredLevel` e `PermanentPreview.RequiredLevel` (o nível que a Promoção espera do Treinar, das regras). **Loja** (`ShopView`): a 3ª aba (`Tab.Intern`) só com ele contratado, com o saldo de lascas no alto e o "!" dela; demitido (atalho) ou num descanso sem a Promoção, a aba some e a loja volta para a da run; na aba da run, a compra única feita desce para o fim. **Linha** (`UpgradeRowView`): compra única sem o "nv 0/1"; feita, apagada com a moldura cinza (`doneFrame`). **Permanentes** (`PermanentRowView`): a linha que espera outra mostra o motivo e o preço apagado; o prefab `PermanentRow` passa a 40 px (7 linhas cabem). Cena e dados montados pelo setup (`InternShopSetup`), idempotente. Num balanço sem estagiário (GDD v5), a linha Estagiário some (antes, "Em breve"). Testes: PlayMode `InternShopTests` (+4: comprar o Estagiário, a 3ª aba e o fim da lista; a aba com as 3 linhas e a compra; demitir esconde a aba; Treinar e Promoção com o motivo e as linhas de 40 px); `ShopTests` mede nenhuma linha bloqueada e o Estagiário apagado sem lascas; `PermanentShopTests` mede as 7 linhas. Em batch: setup ok e idempotente, EditMode **278/278**, PlayMode **120/120** |
| E4 Cálculos, sessão longa e celular | ⏳ | — |
