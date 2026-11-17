# Análise — o que o GDD v8.2 muda no plano

> Análise de 2026-11-16. Compara o [GDD v8.2](../GDD_Idle_Miner%20v82.pdf) (versão 8.2, 12 de novembro de 2026) com o [GDD v7](../gdd%20v7.pdf) e com o que o jogo faz hoje (M9 e M9b E1…E5 prontos).
>
> **Estado:** **o v8.2 é a referência desde 2026-11-16 (D-248).** As P1…P22 foram respondidas (com as respostas do Apellon) e viraram as **D-236…D-247**; as Q1…Q8 da §4.1 foram aprovadas como recomendadas (**D-248…D-255**). O código ainda é o do v7: muda pelos blocos da §5, cada um com plano de marco antes.
>
> O PDF foi lido pelo texto (sem as páginas renderizadas); tabelas remontadas pelo texto e conferidas pelas fórmulas. Termos: o v8 troca "caverna/nível" por **"andar da caverna"** em quase todo lugar, mas mantém "Caverna (nível)" na tabela das lascas vermelhas e nos biomas (ver P1).

## Resumo

O v8 não é um ajuste de números como o v7: **muda o jeito de jogar**.

- **Sai:** o Mapa (a compra de custo zero e as 100 cavernas "trancadas"), a barra de vida do cristal, o **combo manual** (x10…x100) e a regra do **toque de 33%**, a parede com vida.
- **Entra:** o botão **Próximo Andar** (barra que enche com cristais quebrados), a **Bomba** (swipe up), **cristal raro por contador de 60 s** (liberado no andar 10) com **tier 2**, **3 anúncios** (curto/médio/grande) com horas extras de offline, loja de permanentes **só ao Descansar** (tela preta), permanentes novas (**Dedo Fogoso**, **Atalho**), **distância entre cristais** e **câmera inclinada de 60°**.
- **Muda de valor:** vida do cristal 1,25 → **1,5**ⁿ; lascas por cristal; crítico ×2 → **×1,5**; Picareta custa **7 × Dano Base**; Memória −10 → **−5 por andar** com 100 níveis; offline 6 h → **4 h** (6 h no andar 20, 8 h com anúncio); estagiário só a partir do **andar 5**.
- **Pontas soltas:** o v8 tem várias contradições e lacunas (P1…P22), principalmente o **toque** (sem regra de dano), o **combo** (some, mas a tabela de feedback ainda o cita) e a **Bomba** (sem a melhoria permanente que a libera).

## 1. Passagem entre andares (§2.1, §4.1, §4.3, §7.4) — *a maior mudança de estrutura*

| Ponto | v7 (o jogo hoje) | v8.2 |
|---|---|---|
| Como avança | Comprar o **Mapa** do nível seguinte (custo zero, exige 100 cristais) + quebrar a parede; botão central "PASSAR DE CAVERNA" | **Botão "Próximo Andar"** à direita: **barra de carga** que enche conforme cristais quebrados; liberado a 100% |
| Quantos cristais | 100 fixos (a Memória reduz em 10 por nível) | **`Andar × 10 − Memória`** (quanto mais fundo, mais cristais); a Memória reduz **5 por andar** por nível; se der negativo, vale 0 |
| Ao mudar de andar | — | O botão **volta a 0%** |
| Offline | contagem de cristais não era tema | **a contagem de cristais continua offline** |
| Mapa | linha da loja, 100 níveis | **some** (nem na §4.1 nem na §4.3 há Mapa) |
| Parede com vida (D-98) | existe | **não aparece** no v8 |
| Marcos de andar | — | **Andar 5:** avanço automático (liga/desliga em Ajustes) · **Andar 10:** libera cristais raros para sempre · **Andar 20:** offline base sobe para 6 h |

Impacto: mexe no que o M8 construiu (Letrado/Mapa, parede, botão central, `BalanceConfig`, `CavePassButton`) e no M8b (Memória). O `MiningRunner` e a simulação precisam de **contagem de cristais quebrados por andar** e da barra; a parede sai.

## 2. Números (§3.1, §3.3, §4.1, §4.2)

| Item | v7 | v8.2 |
|---|---|---|
| Vida do cristal | `1 × 1,25^(nível−1)` | `1 × 1,5^(andar−1)` |
| Lascas por cristal | `vida × 1,2` | andar 1: `vida × 0,2` · demais: `vida × 0,2 × (andar−1)` |
| Vida exibida | barra | **sem barra:** o cristal **racha** conforme perde vida |
| Crítico | ×2 | **×1,5**; chance acima de 100% **converte o excedente em dano crítico** (cada 1% extra aumenta o %) |
| Picareta, custo | `2 × Dano Base` | `Taxa Base (7) × Dano Base` |
| Velocidade | igual | igual, **mais:** com 0,5 s o anão usa **broca** em vez de picareta (animação) |
| Mineração, Chance de Crítico, Estagiário (árvore) | — | iguais ao v7 |
| Estagiário | compra a qualquer hora | **andar mínimo 5** |
| Memória | −10 cristais, nível 10, tira o Mapa travado | **−5 por andar**, **nível 100** |
| Sangue da Montanha | +10% por nível | +10% **"em cima do dano atual da Picareta"** (ver P9) |
| Dedo Fogoso | — | **nova:** +0,5 no dano do crítico, nível 10 |
| Atalho | — | **nova:** −1 m na distância entre cristais, nível 5, custo `50 + 2^(n−1)` |
| Promoção do Estagiário | exige Treinar nível 10 | exige Treinar **nível 5** |
| Tabela de escala (§6.1: vida/lascas por caverna) | existe | **removida** (a calibração passa a depender só das fórmulas) |

A vida cresce **1,5**ⁿ contra o dano da Picareta **1,2**ⁿ: o jogo fica bem mais duro por andar do que no v7. Quem fecha a conta são as permanentes e os bônus. **[REC]** rodar o relatório de economia (simulador) antes de aprovar os números; mas a **D-149** adiou esse relatório a pedido do Bruno, então isso é decisão dele (P22).

## 3. Sistemas novos ou removidos

### 3.1 Bomba (§3.4, §7.4) — *sistema novo*
- Bomba no **centro** da tela; **barra enche com o tempo**; cheia, lança com **swipe up**. A carga conta **offline**.
- **10 bombas**, liberadas por **melhoria permanente** (lascas vermelhas). Mas a lista só tem **8** (4 do tier 1 + 4 do tier 2).
- **Tier 1** (5 s de carga): Pequena 10% · Pequena Vermelha 20% · Normal 30% · Normal Vermelha 40% da vida do cristal. **Tier 2:** Dinamite 50% · Pacote 60% · Caixa 70% · Barril 100%.
- Lacunas: não há melhoria permanente da Bomba na §4.2; o tempo de carga do tier 2 não está escrito; "vida do cristal" é a atual ou a máxima? (P5…P7).

### 3.2 Combo e toque — *some, sem substituto escrito*
O v8 tira o **combo manual** (§3.4 do v7), os bônus x10/x30/x50/x100 e a regra "toque = 33% de uma picaretada com 5% de crítico". Só sobram "o jogador pode interagir na tela para causar dano" (§2.1) e o pilar "cada toque ... efeito satisfatório". **Não há regra de dano do toque.** E a tabela de feedback (§8) ainda tem a linha "Toque: xN pulsante" (sobra do combo?).
**Impacto no código:** o M5 (toque/combo) e o M9b E1…E3 (núcleo dos bônus com o combo, coluna de buffs, seletor de acúmulo do combo no DBG) dependem disso. Ver P2.

### 3.3 Cristal raro (§3.4 do v8) — *reescrito*
| Ponto | v7 + decisões D-184/D-190 | v8.2 |
|---|---|---|
| Quando libera | desde o início | **andar 10**, depois **permanente** |
| Aparece | chance de 15% de cada cristal que nasce (D-184; ritmo alternativo no DBG) | **contador de 60 s**; no zero, nasce um raro |
| Ao quebrar | janela com **2 cartas sorteadas** entre 4, 30 s para escolher (D-186, D-190) | **escolhe entre dois boosts** (do tier liberado): igual |
| Tier 1 | dano ×2 · **golpe 2× mais rápido** · lascas ×2 · **crítico +25%**; **60 s** cada (D-190) | 2× Dano · 2× Lascas · **2× Dano de Crítico** · **+10%** Chance de Crítico; **30 s** |
| Tier 2 (30 s) | — | 4× · 4× · 4× · +20%. **Custa 500 lascas vermelhas** (permanente) ou **anúncio: 1 h** (qual anúncio? P20) |
| Offline | raro quebrado pelo estagiário **não dá bônus** (D-186, D-189); carta ativa vale para o estagiário até acabar (D-181, D-190) | os bônus valem offline enquanto o contador não zerar; raro quebrado offline dá um buff **aleatório** do tier (P16) |
| Repetir o mesmo buff | só renova o tempo (D-190) | **reinicia** o tempo (não soma): igual |

O M9b E4 já tem a janela de 2 cartas; o que muda são **as cartas** (sai o golpe rápido, entra o dano de crítico), **os valores** (+25% → +10%), **a duração** (60 → 30 s), **como nasce** e o **offline** (P10, P16).

### 3.4 Anúncios (§3.5, §7.2) — *de 1 para 3*
| Anúncio | Dano, Lascas, Dano de Crítico | Chance de Crítico | Duração | Offline extra |
|---|---|---|---|---|
| Curto | 2× | +10% | 20 min | +1 h |
| Médio | 3× | +15% | 40 min | +2 h |
| Grande | 4× | +20% | 1 h 20 | +4 h |

O botão **Boost** (raio) abre um **menu** com as três opções (v7: um modal único de 5 s com bônus ×2,5 de velocidade por 30 s, que é o que o M9b E5 implementou, D-187/D-188, D-227…D-235). Valem **offline**; assistir outro do mesmo tipo **renova/sobrepõe** (mas o botão do tipo ativo fica travado, o que contradiz "renovar", P11). Offline: base **4 h**, **6 h no andar 20**, **até 8 h** com os anúncios (os extras somam +7 h; qual é o teto? P18). Como offline só o estagiário minera, "valem offline" esbarra na **D-177** (o AD não vale para ele, P17).

### 3.5 Loja das permanentes (§4.2) e Descansar (§7.4)
- As permanentes **só se compram ao Descansar**, numa **tela preta** com a loja; na run dá só para **ver** o que já tem.
- Botão **Descansar** à esquerda, mostrando as lascas vermelhas da run; animação **ZZZ** na transição.
- O M8b (Dia e melhorias permanentes) tem a loja hoje; só muda **onde** abre.

### 3.6 Distância entre cristais e câmera (§2.1, §7.5)
- Cristais começam a **~3 m** um do outro; o Atalho reduz **1 m** por nível.
- Câmera: **inclinação 60°, altura 5,5 m, distância até o anão 3 m, campo de visão 60°.** Hoje o corredor usa `DescentFraming` (FOV 40°, câmera de frente, D-52). Isso **muda o enquadramento** e o corredor (P12, P13).

### 3.7 Interface (§7)
| Região | v7 | v8.2 |
|---|---|---|
| HUD | lascas, dano, nível, velocidade | **lascas e lascas vermelhas** (a fatia mostra só dois indicadores) |
| Direita | Loja, AD | **Ajustes** (música, efeitos, tremor OFF, tela ligada ON, Sobre), **Loja**, **Boost** |
| Esquerda | coluna de buffs | **barra vertical com o andar** + ícones de buffs (círculo que esvazia) |
| Inferior | PASSAR DE CAVERNA (centro), Descansar | **Descansar** (esq.), **Bomba** (centro), **Próximo Andar** (dir.) |

## 4. Perguntas para o Bruno (ou para o autor do GDD)

| # | Pergunta | Por que importa |
|---|---|---|
| **P1** | "Andar" × "caverna": são a **mesma coisa** (andar 1…100) e os biomas/lascas vermelhas/offline "andar 20" seguem por andar? | O texto mistura os dois |
| **P2** | O **combo (x10…x100) e o toque de 33%** saem de vez? Qual é o **dano do toque** no v8? A linha "xN pulsante" da §8 e o "combo" da §8.1 são sobra? | Muda o M5 e o M9b E1…E3 já feitos |
| **P3** | A fórmula das lascas `vida × 0,2 × (andar−1)` é isso mesmo? No andar 2 dá 0,3 contra 0,2 do andar 1; no andar 100, ≈ 1,5⁹⁹×19,8. | Crescimento muito diferente do v7 |
| **P4** | `Andar × 10 − Memória`: o andar 100 pede **1.000 cristais**; quanto tempo isso é para o jogador? | Ritmo do jogo |
| **P5** | Qual **melhoria permanente libera as bombas**? Uma linha de 10 níveis? Custo? E são **10 ou 8** bombas (a lista tem 8)? | Não existe na §4.2 |
| **P6** | **Tier 2** das bombas: quanto de carga? Dinamite/Barril usam o mesmo contador? | Só o tier 1 tem tempo (5 s) |
| **P7** | A % da bomba é sobre a **vida atual** ou a **máxima** do cristal? Com 100% (Barril), mata sempre? | Define a força |
| **P8** | **Atalho** (−1 m, nível 5) sobre 3 m: chega a −2 m. Há distância mínima? | Contradição de números |
| **P9** | **Sangue da Montanha** "em cima do dano atual da Picareta": continua multiplicando o **dano final** (D-158, como está) ou só o da Picareta, antes dos bônus? | Muda a conta |
| **P10** | As cartas do raro passam a ser as do v8 (sai o **golpe 2× mais rápido**, entra **2× dano de crítico**; crítico **+10%** em vez de +25%; **30 s** em vez de 60 s)? | Revê a D-190 e o M9b E4 |
| **P11** | Anúncio: "pode assistir outro para renovar" e "não pode interagir com o mesmo botão" — qual vale? Os 3 anúncios acumulam entre si? | Contradição |
| **P12** | A **câmera de 60°** (3D em perspectiva) **substitui** o enquadramento atual (D-52, de frente)? | Arte e corredor |
| **P13** | A distância "3 m" e a "distância até o anão 3 m" da câmera são coisas diferentes? | Escala do mundo |
| **P14** | Memória **nível 100** com custo `10 + 2^(n−1)`: o nível 100 custa ~6×10²⁹. É para valer? | Impossível de comprar |
| **P15** | O **avanço automático** (andar 5) liga a barra sozinha, e a barra enche offline também? O offline passa de andar? (D-189 diz que não) | Choca com D-189 |
| **P16** | Raro quebrado **offline**: o v8 dá um buff aleatório; a D-186 diz que não dá nada ("bônus é para quem está jogando"). Qual vale? E o buff vale para o estagiário (D-190 diz que sim)? | Revê D-186/D-189 |
| **P17** | Os **anúncios valem offline**, mas offline só o estagiário minera e a D-177 diz que o AD não vale para ele. Passa a valer? | Sem isso, "vale offline" não faz nada |
| **P18** | **Teto do offline:** 4 h (6 h no andar 20) + 1/2/4 h dos anúncios. Os três extras somam (+7 h)? O teto é 8 h mesmo no andar 20? | O texto diz "até 8 h" |
| **P19** | **Crítico acima de 100%:** quanto cada 1% extra soma no dano do crítico (×1,5 → ×1,515? ou +1% absoluto)? Dá para passar de 100%: 50% + 50% das melhorias + 20% do anúncio + 20% do raro. | Sem número, não dá para codar |
| **P20** | O **tier 2 do raro por anúncio (1 h)**: é um 4º botão no menu Boost ou vem junto de um dos três? | Não está na §3.5 |
| **P21** | Memória "reduz **5 para cada andar**": é −5 por nível em todo andar (`andar×10 − 5×nível`) ou −5×andar por nível (`andar×(10 − 5×nível)`, que zera tudo no nível 2)? | Junto com a P14 |
| **P22** | Rodar o **relatório de economia** (simulador 1→100) antes de aprovar os números do v8, apesar da D-149 tê-lo adiado? | Vida 1,5ⁿ é bem mais dura |

**Respostas (2026-11-16):** P1 → D-236 · P2 → D-237 · P3 → D-238 · P4, P14, P21 → D-239 · P5…P7 → D-240 (8 bombas) · P8, P13 → D-241 (+ Q4) · P9 → D-242 · P10 → D-243 · P11, P18 → D-244 (+ Q3) · P12 → D-245 · P15…P17 → D-246 · P19 → em aberto (Q1) · P20 → em aberto (Q2) · P22 → D-247.

### 4.1 O que sobrou (aprovado como recomendado em 2026-11-16: Q1 → D-249 · Q2 → D-250 · Q3 → D-251 · Q4 → D-252 · Q5 → D-253 · Q6 → D-254 · Q7 → D-255 · Q8 → D-248)

| # | Pergunta | **[REC]** |
|---|---|---|
| **Q1** | **Crítico acima de 100%** (o Apellon ainda vai pensar): quanto cada 1% extra soma no dano do crítico? | Cada 1% acima de 100% soma **+1 ponto** no dano do crítico (150% → 151%), junto com o Dedo Fogoso. Até decidir, o excedente se perde. |
| **Q2** | **Tier 2 do raro por anúncio (1 h):** é um 4º anúncio no menu do Boost? E depois de comprar o tier 2 com as 500 lascas vermelhas? | 4º item do menu, que aparece com os raros liberados (andar 10) e **some** depois da compra, até o Apellon definir outro benefício (tier 3?). |
| **Q3** | **Anúncios de tipos diferentes ao mesmo tempo** (curto 2× + grande 4×): multiplicam (8×), somam (6×) ou vale o maior (4×)? | **Vale o maior** em cada atributo; o menor segue contando o tempo por baixo. Multiplicando, só os três anúncios dariam 24× (2 × 3 × 4). |
| **Q4** | **Distância entre cristais:** hoje o espaço mínimo é 2 unidades = **1 m** (`minCrystalSpacing`, D-58). O Atalho são 5 níveis de −1 m sobre 3 m. | **3 m − 0,5 m por nível** (3 → 2,5 → 2 → 1,5 → 1 → **0,5 m**): mantém os 3 m do GDD e chega quase colado. Alternativa: **5,5 m − 1 m** (mantém o passo do GDD, mas o começo fica 5,5× mais lento que hoje). Ajustar de vez com a câmera nova. |
| **Q5** | **Avanço automático do andar 5:** fica liberado para sempre ao chegar no andar 5 pela 1ª vez (como o 10 e o 20) ou só naquela run? | Para sempre, ligado por padrão (desliga em Ajustes). |
| **Q6** | **Horas extras de offline:** valem só se o bônus do anúncio estiver ativo quando o app fecha? | Sim: conta o extra de cada anúncio ativo na saída (teto 8 h, D-244). |
| **Q7** | **Bomba:** a Pequena vem de graça desde o início e são **7 compras** até o Barril (11, 12, 14, 18, 26, 42, 74 lascas vermelhas)? A bomba pega crítico e bônus? | Sim às 7 compras; **sem** crítico e bônus (é uma % fixa da vida máxima). |
| **Q8** | **O v8.2 passa a ser a referência** (como a D-132 fez com o v7)? Com isso entram também os valores que não viraram pergunta: vida 1,5ⁿ, crítico ×1,5, Picareta `7 × Dano Base`, Dedo Fogoso, Atalho, estagiário no andar 5, Promoção com Treinar 5, offline 4 h/6 h. | Sim, e atualizar README e Roadmap. |

## 5. Plano de trabalho proposto (**[REC]**, nada começa sem decisão)

O jeito mais seguro: **não** aplicar o v8 de uma vez. Pela regra "jogável antes de sistema" e para o jogo não ficar quebrado, em blocos pequenos:

| Bloco | O que entra | Depende de | Tamanho |
|---|---|---|---|
| **A. Números e passagem** | vida 1,5ⁿ, lascas, crítico ×1,5, custo da Picareta, **Próximo Andar com barra** (cristais por andar), sai o Mapa/parede, Memória nova, andar 5/10/20, estagiário no andar 5 | D-236…D-239, D-248, D-249, D-253 | médio |
| **B. Cristal rachando** | estados de rachadura no lugar da barra de vida | arte (PixelLab/Aseprite) | médio |
| **C. Descansar e loja** | permanentes só no Descansar, tela preta + ZZZ, novas (Dedo Fogoso, Atalho) | D-252 | pequeno |
| **D. Raro e anúncios** | contador de 60 s no andar 10, tier 2, 3 anúncios, horas extras de offline | D-243, D-244, D-246, D-250, D-251, D-254 | médio |
| **E. Bomba** | sistema novo: carga de 5 s, swipe up, 8 bombas, permanente (D-240) | D-255 | médio |
| **F. Toque e combo** | **tirar** o dano do toque, o combo e os bônus dele (D-237) | — | pequeno/médio |
| **G. Câmera 60°** | novo enquadramento (D-245) e distância entre cristais | D-252 | médio, risco visual |

**Ordem sugerida:** responder as perguntas → **A** (é a base de todas as outras) → **C** → **D** → **B** → **E** → **G**; **F** junto com a **E**: o toque sai quando a Bomba entra, para a tela nunca ficar sem interação. O **M9b E6** (que estava parado) deve ser revisto: parte dele (combo) pode cair pela P2.

**Documentos a atualizar quando o v8 virar referência:** README (referência passa a ser o v8), [Roadmap](Roadmap.md) (nota "GDD v8" e novos marcos), [Decisões e Riscos](Decisoes%20e%20Riscos.md) (D-236 em diante), planos dos marcos afetados.
