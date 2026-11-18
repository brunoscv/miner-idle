# Marco 9e — Raro e anúncios do v8

> Plano de 2026-11-18, **aguardando as respostas do Bruno (P1…P9)**. É o **Bloco D** da [Análise do GDD v8](Analise%20-%20GDD%20v8.md) §5, depois do [Marco 9d](Marco%209d%20-%20Descansar%20e%20loja%20do%20v8.md) (aprovado em 2026-11-18, D-282). Base: GDD v8.2 §3.4–3.5 e §7.2, e as decisões D-243, D-244, D-246, D-250, D-251 e D-254.

## Objetivo

**O raro passa a ter hora marcada, e o Boost vira um menu de anúncios.** A partir do andar 10, um **contador de 60 s** faz nascer um cristal raro. Ao quebrá-lo, o jogador escolhe entre duas cartas do v8 (Dano ×2, Lascas ×2, Dano de Crítico ×2, +10% de crítico), que valem **30 s**. Com as **500 lascas vermelhas** do tier 2, as cartas viram ×4 e +20%. O botão do raio abre um **menu com três anúncios** (curto, médio e grande), cada um com bônus de minutos e horas extras de offline. No offline, o estagiário aproveita o que estiver valendo.

**Para você:** no andar 10, ver o contador zerar e o raro nascer; quebrar, escolher Lascas ×2 e ver o número subir; assistir o anúncio curto e depois o grande e ver o maior valer; fechar o jogo com o grande ativo e voltar com as horas extras e o estagiário tendo minerado com o bônus.

## O que já existe (e o que muda)

| Peça | Hoje (M9b, M9c) | No 9e (v8) |
|---|---|---|
| **Quando o raro nasce** | sorteio de **15%** em cada cristal que nasce (D-184), com o ritmo "lento" de 2% no DBG (D-185); só depois do andar 10 (M9c E3) | **contador de 60 s**; no zero, nasce um raro. Sai o sorteio e o ritmo lento |
| **As cartas** | Dano ×2, **golpe 2× mais rápido**, Lascas ×2, **crítico +25%**; **60 s** (D-190) | **Dano ×2, Lascas ×2, Dano de Crítico ×2, +10% de crítico; 30 s** (D-243). Sai o golpe rápido; entra o **dano do crítico** (um bônus novo) |
| **A janela das cartas** | 2 cartas sorteadas entre 4, 30 s para escolher (M9b E4) | igual |
| **Tier 2 do raro** | — | ×4, ×4, ×4, +20%. **500 lascas vermelhas** (permanente) ou um **4º anúncio**, "tier 2 por 1 h" (D-250) |
| **AD** | um botão, modal de 30 s "Visualizando AD", velocidade ×2,5 por 30 s (D-218, D-227…D-235) | **menu do Boost** com **curto, médio e grande** (+ o do tier 2). Bônus em Dano, Lascas, Dano de Crítico e Chance de Crítico, de **20 min a 1 h 20** (tabela abaixo) |
| **Repetir o anúncio** | só depois que o boost acaba (D-188) | o **mesmo tipo renova** a duração (D-244); tipos diferentes juntos: **vale o maior** em cada atributo (D-251) |
| **Offline** | teto de 4 h, 6 h depois do andar 20; raro e AD não valem para o estagiário (D-186, D-235) | **+1, +2 e +4 h** pelos anúncios ativos ao fechar, teto **8 h** (D-244, D-254). O bônus do anúncio vale para o estagiário pelo tempo que faltava; raro quebrado offline dá **carta aleatória** (D-246) |

### Os anúncios (GDD v8.2 §3.5)

| Anúncio | Dano, Lascas, Dano de Crítico | Chance de Crítico | Duração | Offline extra |
|---|---|---|---|---|
| Curto | ×2 | +10% | 20 min | +1 h |
| Médio | ×3 | +15% | 40 min | +2 h |
| Grande | ×4 | +20% | 1 h 20 | +4 h |
| Tier 2 do raro (D-250) | — | — | 1 h | — |

## As regras (o que já está decidido)

- **D-243:** as cartas do v8, 30 s; repetir a mesma carta **reinicia** o tempo, não soma.
- **D-244:** três anúncios; o mesmo tipo **renova**; as horas extras **somam**, com teto de 8 h, também depois do andar 20.
- **D-246:** raro quebrado offline dá um buff **aleatório** do tier, que vale para o estagiário; o anúncio ativo vale para o estagiário no offline pelo tempo que faltava.
- **D-250:** o 4º anúncio ("tier 2 por 1 h") aparece no andar 10 e **some** depois que o tier 2 é comprado.
- **D-251:** tipos diferentes juntos: **vale o maior** em cada atributo; o menor segue contando por baixo.
- **D-254:** as horas extras contam os anúncios **ativos na hora em que o app fecha**.
- **D-167:** o estagiário não tem crítico; as cartas e os anúncios de crítico não valem para ele.
- **D-218:** o anúncio continua **simulado** (o modal "Visualizando AD"), até o SDK de anúncios entrar.

## Perguntas para o Bruno

**P1 — O contador de 60 s, quando anda e o que faz no zero.** O GDD diz "no zero, nasce um raro".
**[REC]** O contador anda só com a run correndo: para no descanso (D-265), na transição de andar e com o jogo em segundo plano (o offline tem a regra da P2). No zero, o **próximo cristal que nasce** na fila é o raro, e o contador recomeça quando ele nasce. Nunca há dois raros na fila. Antes do andar 10 o contador não existe.

**P2 — Quantos raros no offline.** Com 8 h fora, seriam 480 raros, cada um com uma carta de 30 s.
**[REC]** Um raro a cada 60 s de offline, quebrado pelo estagiário; cada um dá uma carta aleatória do tier que vale **30 s** para ele. Na conta, o estagiário fica com meio tempo sob uma carta sorteada. As cartas de crítico (D-167) não fazem nada para ele, e é isso mesmo: o sorteio já é a regra (D-246).

**P3 — Mostrar o contador na tela?** O GDD não fala em mostrar.
**[REC]** **Não mostrar** na tela do jogo: o raro chega de surpresa. O DBG → Bônus mostra os segundos e tem "Raro agora" e "Contador 5 s".

**P4 — Onde se compra o tier 2 (500 vermelhas).**
**[REC]** Uma linha nova na tela do descanso, **"Raro Dourado"** (nível 0/1, 500), que aparece a partir do andar 10, como a Promoção. Comprado, vale para sempre, e o 4º anúncio some (D-250).

**P5 — Raro e anúncio juntos.** Com a carta Dano ×2 e o anúncio grande (Dano ×4), quanto vale o dano?
**[REC]** **Multiplicam** (×8): são fontes diferentes, como a carta e as permanentes hoje. A chance de crítico **soma** (+10% + 20% = +30%). A regra do "vale o maior" (D-251) é só entre anúncios.

**P6 — O menu do Boost.**
**[REC]** O botão AD da coluna da direita vira **BOOST** (o raio). Tocar abre um painel com as três linhas (curto, médio e grande): o bônus, a duração e as horas extras. Cada linha tem o botão **ASSISTIR** e, se estiver ativo, o tempo que falta. A 4ª linha, "Tier 2 por 1 h", aparece no andar 10. Assistir abre o modal de 30 s de hoje (D-218), com o "Pular espera" do DBG. **Mockup antes (E0).**

**P7 — Como mostrar bônus de 20 min a 1 h 20.** A coluna dos bônus hoje mostra segundos.
**[REC]** O mesmo ícone com o anel que esvazia, e o tempo em **"1h20"**, **"19m"** e, no último minuto, **"45s"**. Cada anúncio ativo é um ícone só, com o maior bônus dele; o toque abre o detalhe, como hoje.

**P8 — O AD de hoje (velocidade ×2,5 por 30 s).**
**[REC]** **Sai**, com a cor âmbar da VELOCIDADE na LOJA (nada mais acelera o golpe no v8). O código do bônus de velocidade fica, sem ninguém usando, até a Bomba (Bloco E) dizer se precisa.

**P9 — A sessão longa deste marco.**
**[REC]** Como na D-282: a sessão longa (contador, cartas, anúncios e offline) vai para os **testes longos do fim**, e o marco fecha com a E3.

## Etapas

### E0 — Mockups (P6, P7)

- O **menu do Boost** com os três anúncios, um ativo com o tempo e o 4º do tier 2.
- A **coluna dos bônus** com um anúncio de 1 h e uma carta de 30 s juntos.
- A linha **Raro Dourado** na tela do descanso.

### E1 — O raro do v8 (núcleo)

- O **contador de 60 s** (P1) no lugar do sorteio de 15% e do ritmo lento.
- As **cartas do v8**: Dano ×2, Lascas ×2, **Dano de Crítico ×2** (bônus novo) e +10% de crítico, **30 s**.
- O **tier 2**: a permanente Raro Dourado (500 vermelhas, P4) e o tier 2 por tempo (pelo anúncio da E2).
- **Save v10:** o contador e o tier 2.
- **DBG:** "Raro agora", "Contador 5 s", "Tier 2" (liga e desliga).
- **Testes EditMode:** o contador só a partir do andar 10 e parado no descanso; um raro por vez; as cartas e os valores; tier 1 e tier 2; repetir a carta reinicia.

### E2 — Os anúncios e o offline (núcleo)

- Os **três anúncios** e o do tier 2, com os bônus da tabela, a **renovação** do mesmo tipo e o **maior vale** entre tipos (D-251).
- Raro e anúncio juntos (P5).
- **Offline:**
  - as horas extras dos anúncios ativos ao fechar (+1, +2, +4 h, teto 8 h);
  - o anúncio vale para o estagiário pelo tempo que faltava;
  - um raro a cada 60 s com carta aleatória (P2).
- Sai o AD de velocidade (P8).
- **Save:** os anúncios ativos e o tempo de cada um.
- **DBG:** "Anúncios curtos" (as durações ÷ 60, para ver acabar) e o "Pular espera" de hoje para cada anúncio.
- **Testes EditMode:** renova e o maior vale; o menor volta quando o maior acaba; as horas extras e o teto; o estagiário com o bônus no offline; o raro do offline.

### E3 — Na tela

- O **menu do Boost** e o modal de cada anúncio (P6).
- A **coluna dos bônus** com o tempo longo (P7).
- A linha **Raro Dourado** na tela do descanso.
- O "Bem-vindo de volta" com as horas extras.
- **Testes PlayMode:** assistir, renovar e ver o maior; comprar o tier 2 e ver o 4º anúncio sumir; o raro nascer no andar 10 com o contador do DBG.

### E4 — Sessão longa e celular

- Vai para os **testes longos do fim**, se a P9 for aprovada.

## Como testar (Bruno) — ao fim de cada etapa

- **E1:** DBG → Andar → Recorde 10. DBG → Bônus → **Contador 5 s**: o raro nasce; quebrar e escolher; a carta vale 30 s. Comprar o Raro Dourado no descanso (DBG → Dia → +100 vermelhas, cinco vezes): as cartas viram ×4.
- **E2 e E3:** BOOST → **Curto** → ASSISTIR (DBG → Bônus → Pular espera): ×2 por 20 min na coluna. Assistir o **Grande**: vale o ×4. Fechar o jogo e abrir depois de um tempo: o "Bem-vindo de volta" com as horas extras e o estagiário tendo minerado com o bônus.

## Critério de pronto

- O raro nasce pelo contador, só a partir do andar 10, e as cartas são as do v8.
- O tier 2 se compra no descanso e se ganha por 1 h pelo anúncio.
- Os três anúncios valem com as regras de renovar e de valer o maior, e as horas extras chegam ao offline com o teto de 8 h.
- Testes EditMode e PlayMode das etapas passando; a sessão longa nos testes longos do fim (P9).

## Riscos

- **A conta do offline** (P2): com o bônus do anúncio e as cartas sorteadas, o offline fica mais generoso; sem o relatório de economia (D-247), o número fino aparece no jogo.
- **Anúncios de minutos e horas** são difíceis de testar à mão: os atalhos do DBG (durações ÷ 60, "Pular espera") são parte da etapa, não um extra.
- **A coluna dos bônus** pode encher com anúncios e cartas juntos; o mockup da E0 mostra se cabe.

## Fora deste marco

- O cristal rachando (Bloco B), a Bomba e a saída do toque (Blocos E e F), a câmera de 60° (Bloco G).
- O SDK de anúncios de verdade (o anúncio continua simulado, D-218).

## Esforço

| Etapa | Horas |
|---|---|
| E0 | 2–3 |
| E1 | 6–8 |
| E2 | 6–8 |
| E3 | 6–8 |

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| Plano | 📝 escrito em 2026-11-18, aguardando P1…P9 | — |
| E0 Mockups | ⏳ | — |
| E1 O raro do v8 | ⏳ | — |
| E2 Os anúncios e o offline | ⏳ | — |
| E3 Na tela | ⏳ | — |
| E4 Sessão longa e celular | ⏳ | testes longos do fim, se a P9 for aprovada |
