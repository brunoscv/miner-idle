# Análise — o que o GDD v8.4 muda em relação ao v8.2

> Análise de 2026-11-25. Compara o [GDD v8.4](../GDD_Idle_Miner%20v84.pdf) (versão 8.4, 23 de novembro de 2026) com o [GDD v8.2](../GDD_Idle_Miner%20v82.pdf) (a referência desde a D-248) e com o que o jogo faz hoje (blocos A…G do v8 no jogo, M9i E1…E8 e #23).
>
> **Estado:** **o v8.4 é a referência desde 2026-11-25 (D-347).** O Bruno respondeu as P1…P24 no mesmo dia → **D-347…D-366** ([Decisões e Riscos](Decisoes%20e%20Riscos.md) §1.18); a P21 já era a D-345. Mudaram da recomendação: **P1** (a lasca voando sai a cada golpe), **P7** (o estagiário não aparece na tela) e **P13** (a compra automática em sequência, regra do Bruno). Com o Apellon ficam P4 (bomba em área), P12 (o ×2 do Médio) e P18 (a frase cortada). O código muda pelos blocos da §6, cada um com plano de marco antes.

## Resumo

O v8.4 **não mexe na estrutura** que construímos (andares com o Próximo Andar, descanso, bomba, raro por contador, câmera de 60°), mas traz **quatro mudanças grandes** e uma **tabela de números quase toda nova**:

1. **Lascas = dano.** Cada golpe paga na hora as lascas do dano causado (até a vida que restava). Sai a fórmula `vida × 0,2 × (andar − 1)`.
2. **Cristais tier 2 e tier 3 ("paredes de cristal").** Mais vida, pagam o dobro e contam 4 e 10 cristais na barra do andar; a chance cresce com o andar.
3. **O estagiário só trabalha offline.** Online ele corre atrás do anão, sem bater. Ganha a linha de Crítico. A Promoção sai: o andar 15 dá o estagiário em todo dia.
4. **Automação.** Comprar melhorias sozinho (andar 30, e o anúncio "Descanso"), comprar as do estagiário sozinho (andar 40), jogar a bomba sozinho (andar 25).

E várias médias: a bomba passa a quebrar tudo no começo (110%…190% da vida) e enfraquece 1% por andar; os anúncios viram **3 temas × 3 durações**; o contador do raro **aparece**; o modal do raro **fecha sozinho**; o Descansar e a volta do offline ganham **anúncio que dobra**; a **loja rápida** entra no GDD.

**O que o v8.4 já adotou do que fizemos:** bomba por deslize para a direita ou toque, com a escolha nos Ajustes (D-310, D-333); 8 bombas (D-240); a fileira de compras rápidas embaixo (§7.4, a loja rápida da D-343).

## 1. Economia: lascas pelo dano (§3.1)

| Ponto | v8.2 (o jogo hoje) | v8.4 |
|---|---|---|
| Lascas por cristal | `vida × 0,2 × max(1, andar − 1)`, pagas **na quebra** | **a própria vida**: cada golpe paga o dano causado, **na hora**; o dano que passa da vida restante não paga |
| Bomba | paga na quebra (D-309) | o dano da bomba **paga lascas** do mesmo jeito |
| Tabela da planilha (vida/lascas por andar) | no GDD | **removida** |

Comparação sem bônus: andar 1 → **1** (era 0,2) · andar 10 → **38,4** (era 69,2) · andar 20 → **2.217** (era 8.424) · andar 50 → **4,3×10⁸** (era 4,2×10⁹). O começo fica 5× mais rico; do andar 10 em diante, mais pobre.

Impacto no código: o pagamento sai da quebra e vai para cada golpe (anão, bomba; o estagiário só no offline). O offline (`OfflineMining`) fica mais simples: cada cristal paga a vida dele. O feedback da lasca voando ("Coletar", §2.1) continua escrito como animação na quebra.

## 2. Cristais tier 2 e tier 3 (§3.1) — *sistema novo*

| | Tier 2 | Tier 3 ("parede de cristal") |
|---|---|---|
| Vida | 2× a do cristal base do andar | 4× |
| Lascas | a vida durante a quebra **+ a vida de novo ao quebrar** (10 de vida → 20) | igual (100 → 200) |
| Conta na barra do andar | **4** cristais (o que passar do requisito se perde) | **10** |
| Chance | andar 1–20: 20% · 20–49: 40% · 50–60: 80% · **60+: substitui o cristal base** | 1–30: 10% · 31–50: 20% · 51–80: 40% · 81–100: 50% |

Lacunas: as faixas se sobrepõem (20, 60) e as chances somadas passam de 100% (andar 55: 80% + 40%); o texto diz "dobro de cristais" onde parece ser "dobro de lascas"; não diz como fica o raro. Precisa de **arte** (dois desenhos novos por cor, ou um provisório gerado, como o raro dourado da D-302).

## 3. Mudanças por sistema

### 3.1 Bomba (§3.4, §4.2, §4.3)

| Ponto | Hoje | v8.4 |
|---|---|---|
| Lançar | deslize para a direita, ou toque nos Ajustes (D-310, D-333) | **igual** ✅ |
| Quando aparece | desde o começo, a Pequena de graça (D-255) | **andar 5** libera o tier 1; **andar 50** libera o tier 2 |
| Força | 10% · 20% · 30% · 40% · 50% · 60% · 70% · 100% da vida máxima | **110% · 120% · 130% · 140% · 150% · 160% · 170% · 190%**, menos **1% por andar** (Pequena no andar 11 = 99%, no 100 = 10%) |
| Alvo | todos os cristais até 3,5 m (D-307, D-324) | **começa em 1 cristal**; "uma melhoria" leva a todos da tela, "detalhes na seção 6" — **que não fala disso** |
| Permanente Bomba | `10 + 2^(n−1)`, 7 compras | `10 + 3^(n−1)`, "nível 8", **a partir do nível 4 pede o andar 50** |
| Carga | 5 s | 5 s; **Bombardeador** (nova permanente) tira 0,5 s por nível (4 níveis → 3 s); cartas do raro tiram 1 s (tier 1) ou 2 s (tier 2) |
| Automática | — | **andar 25**: joga sozinha (liga/desliga nos Ajustes) |

### 3.2 Estagiário (§1, §4.1, §5)

| Ponto | Hoje | v8.4 |
|---|---|---|
| Online | bate no mesmo cristal do anão | **corre atrás do anão e não bate**; só minera **offline** (o texto riscado tira o "online" e o "causa o dano no mesmo cristal"; sai a §5.2 inteira) |
| Contratar | 500 lascas, a partir do andar 5 (D-262) | 500 lascas; **sem andar mínimo** escrito |
| Começar o dia com ele | permanente Promoção (Treinar 5) | **sai a Promoção**; o **andar 15** dá o estagiário em todo dia |
| Árvore | Picareta `50×4^n` (20) · Velocidade `50×5^n`, −0,1 s (20) · Mineração `50×1,2^n`, +100% (1000%) | Picareta `50×1,5^n` (**100**) · Velocidade `50×2,5^n`, **−0,01 s** (**100**) · Mineração `50×2,5^n`, **+10%** (100, 1000%) · **Crítico** `20×2,5^n`, +1% (**50**, nova; revê a D-167) |
| Treinar Estagiário | +10% por nível | **+100%** por nível |
| Volta do offline | COLETAR | **anúncio longo que dobra** o que ele trouxe |

Revê as D-209 e D-292 (o estagiário online), D-169 e D-262 (Promoção, andar 5).

### 3.3 Cristal raro (§3.4, §3.4.1)

| Ponto | Hoje | v8.4 |
|---|---|---|
| Contador | escondido (D-285) | **aparece na tela** |
| Depois da escolha | a janela fica aberta mostrando a carta, com o botão de fechar (`RareWindowView`) | **fecha sozinha** quando a carta ativa |
| Cartas tier 1 | Dano ×2 · Lascas ×2 · Dano de Crítico ×2 · +10% Chance de Crítico | ×2 · ×2 · ×2 · **bomba carrega 1 s mais rápido** (sai a chance de crítico) |
| Cartas tier 2 | ×4 · ×4 · ×4 · +20% | ×4 · ×4 · ×4 · **bomba 2 s mais rápido** · **2× cristais do andar** (cada quebra conta dobrado, em todos os tiers) |
| Tier 2 por anúncio | 4º anúncio, 1 h (D-250) | vem dentro do anúncio **Ganância** (20 min, 40 min, 1 h 20) |

### 3.4 Anúncios (§3.5) — *de 3 pacotes para 3 temas × 3 durações*

| Tema | Curto (20 min) | Médio (40 min) | Longo (1 h 20) |
|---|---|---|---|
| **Ganância** | Lascas ×2 + tier 2 do raro | Lascas ×2 + tier 2 | Lascas ×4 + tier 2 |
| **Fúria** | Dano ×2 + Dano de Crítico ×2 | ×2 + ×2 | ×4 + ×4 |
| **Descanso** | +1 h de offline + **Auto Upgrade** 20 min | +2 h + Auto Upgrade 40 min | +4 h + Auto Upgrade 1 h 20 |

**O que o Unity Ads permite (pesquisa de 2026-11-25):** os formatos são **Rewarded** (o jogador escolhe assistir e ganha algo; não pula), **Interstitial** (tela cheia nas pausas, pode pular depois de um tempo), **Banner** (faixa fixa) e, pelo LevelPlay, MREC (retângulo médio) e nativo. **Não há como escolher a duração do vídeo**: quem decide é o anunciante (o guia da Unity para anunciantes fala em 10 a 60 s). O SDK só avisa que o anúncio terminou; **a recompensa é decidida pelo jogo**. Dá para ter várias *placements* (nomes) no mesmo Rewarded, cada uma com limite e intervalo próprios no painel e com o nome nos relatórios. Então "Curto/Médio/Longo" **não pode ser o tamanho do anúncio**; os três temas continuam possíveis, todos no Rewarded (ver P12). Desde 1/4/2026 a Unity pede a integração pelo LevelPlay (o pacote antigo, *Advertisement Legacy*, perde desempenho).

Hoje cada anúncio dá tudo junto (dano, lascas, dano de crítico, chance de crítico e horas). Saem a **chance de crítico** dos anúncios, o **×3** do médio e o **4º anúncio**. O jogador escolhe um por vez; sugestão de **abas** Curtos/Médios/Longos. O texto repete a contradição "pode renovar" × "não pode tocar no mesmo botão" que a D-244 já resolveu (pode renovar).

### 3.5 Andares e marcos (§4.3)

| Ponto | Hoje | v8.4 |
|---|---|---|
| Cristais por andar | `andar × 10 − 5 × Memória` | **`andar × 5 − 5 × Memória`** (andar 100 = 500) |
| Vários andares de uma vez | — | se o começo do dia pede 0 cristais por vários andares, mostra a placa e **conta os andares** de 1 até onde o dia começa; as lascas vermelhas desses andares vêm igual |
| Texto dos marcos | — | "mostrar na UI os bônus da progressão com texto detalhado" |
| Regra de passagem | — | a frase termina cortada ("Após pressionar o") |

| Andar | Hoje | v8.4 |
|---|---|---|
| 3 | — | **avanço automático** (era no 5) |
| 5 | avanço automático, estagiário | **bomba tier 1** |
| 10 | raro | raro (igual) |
| 15 | — | **começa os dias com o estagiário** |
| 20 | offline 6 h | igual |
| 25 | — | **bomba automática** |
| 30 | — | **compra de melhorias automática** (Sempre / Nunca / Só offline) |
| 40 | — | **compra automática das melhorias do estagiário** (Sempre / Nunca / Só offline) |
| 45 | — | **dobra as lascas vermelhas** de todos os andares |
| 50 | — | **bomba tier 2** |
| 60 | — | **−100 cristais** no requisito de todo andar (soma com a Memória) |

### 3.6 Descansar e interface (§7.4)

- **Descansar** ganha a opção de **anúncio longo que dobra** as lascas vermelhas.
- **"Botões das melhorias que podem ser compradas"** na seção de baixo: é a loja rápida, que hoje só existe ligando no DBG (D-343).
- Ajustes (§7.2) iguais; mais as opções novas espalhadas no texto (bomba automática, compra automática, compra do estagiário).

### 3.7 Crítico (§3.3)

- Sai a regra do crítico acima de 100% (a D-249, que era provisória). Com os números novos, a chance máxima é **50% + 50% = 100%** (a linha da loja e os Dedos Nervosos; anúncio e raro não dão mais chance), então a regra deixaria de acontecer.

## 4. Tabela de números (§4.1, §4.2)

`n` = compras feitas (a leitura da D-202: "nível − 1" = compras). Para a Picareta, "nível 1" é o valor-base (D-207).

### 4.1 Melhorias da run (anão)

| Linha | Hoje (v8.2) | v8.4 |
|---|---|---|
| Picareta | custo `7 × dano atual` · máx. nível 50 | custo **`5 × 1,5^n`** · máx. nível **200** · dano igual |
| Velocidade | `50 × 2^n` · −0,1 s · 19 compras | **`10 × 3^n` · −0,01 s · 100** |
| Mineração | `20 × 5^n` · +100% · 10 (teto 1000%) | **`10 × 2,5^n` · +10% · 200** (sem teto escrito) |
| Chance de Crítico | `50 × 2^n` · +5% · 10 | **`10 × 2,5^n` · +1% · 50** |
| Estagiário | 500 · andar 5 | 500 · sem andar |

Atenção ao ritmo: a Picareta com 20 compras custa **16.626** (hoje **275**), e a Velocidade passa a dar 0,01 s por compra a preços de `3^n`. Pela D-247 os números entram sem simulador; vale o Bruno confirmar se continua assim (P15).

### 4.2 Permanentes (lascas vermelhas)

| Linha | Hoje (v8.2) | v8.4 |
|---|---|---|
| Memória | `10 + 2^n` · −5 · 100 | **`10 + 1,1^n`** · −5 · 100 |
| Sangue da Montanha | `10 + 2^n` · +10% · 20 | **`10 + 1,5^n` · +100% · 40** |
| Coração Ardente | `10 + 2^n` · +50% · 20 (teto 1000%) | **`10 + 1,5^n` · +100% · 40** |
| Dedos Nervosos | `10 + 2^n` · +5% · 10 | **`10 + 1,5^n`** · +5% · 10 |
| Dedo Fogoso | `10 + 2^n` · +0,5 no crítico · 10 | **`10 + 1,5^n` · "+5%" · 40** |
| Alma dos Anões | `10 + 2^n` · +1 Picareta · 10 | **`5 + 1,1^n`** · +1 · **100** |
| Treinar Estagiário | `10 + 2^n` · +10% · 10 | `10 + 2^n` · **+100%** · 10 |
| Promoção do Estagiário | 50 · única | **sai** (vira o andar 15) |
| Atalho | `50 + 2^n` · −0,5 m (D-252) · 5 | **`10 + 5^n`** · "−1 m" (texto igual ao v8.2; a D-252 resolveu) · 5 |
| Bomba | `10 + 2^n` · 7 compras | **`10 + 3^n` · "nível 8" · nível 4+ pede o andar 50** |
| Raro Dourado | 500 · única | 500 (igual, na §3.4.1) |
| **Tempo Curto** | — | **nova:** `10 + 2^n` · −0,1 s no intervalo · 10 |
| **Bombardeador** | — | **nova:** `15 + 5n` (linear) · −0,5 s na carga da bomba · 4 |
| **Olho Treinado** | — | **nova:** `10 + 5n` (linear) · +100% de lascas vermelhas · 10 |

Velocidade cheia (100 × 0,01 s) + Tempo Curto cheio (10 × 0,1 s) = 2 s, o intervalo inteiro: precisa de piso (hoje 0,1 s).

## 5. Perguntas (com recomendação)

**Respostas (2026-11-25):** P23, P15 → D-347 · P1 → D-348 (a lasca voa a cada golpe) · P2, P3 → D-349 · P4 → D-350 (provisória) · P5 → D-351 · P6 → D-352 · P14 → D-353 · P7, P8 → D-354 (o estagiário não aparece) · P9 → D-355 · P10 → D-356 · P11 → D-357 · P12 → D-358 · P13 → D-359 (regra do Bruno) · P22 → D-360 · P24 → D-361 · P16 → D-362 · P17 → D-363 · P18 → D-364 · P19 → D-365 · P20 → D-366 · P21 → D-345.

| # | Pergunta | **[REC]** |
|---|---|---|
| **P1** | **Lascas = dano:** os multiplicadores (Mineração, Coração, cartas, Ganância) valem em cada pagamento? A lasca voando fica na quebra ou em cada golpe? | Sim, multiplicam cada pagamento. O número sobe a cada golpe; a lasca voando continua só na quebra (é só desenho). |
| **P2** | **Tier 2 e 3, as chances:** como juntar (andar 55: 80% + 40%) e as faixas que se repetem (20, 60)? | Sorteia o **tier 3 primeiro**, o tier 2 no que sobrou, o resto é base. Faixas 1–19, 20–49, 50–59, 60+. No 60+, o "base" vira tier 2 (o tier 3 continua sorteando). |
| **P3** | **Tier 2 e 3, o resto:** o "dobro de cristais" é dobro de **lascas**? O raro pode ser tier 2/3? A bomba tira a % da vida **de cada um**? Arte? | Dobro de lascas; o raro é sempre base; a bomba vale sobre a vida de cada cristal; arte provisória gerada (maior e mais escura) até o Bruno desenhar. |
| **P4** | **Bomba em área:** o GDD manda para a "seção 6", que não fala disso. Qual melhoria leva a bomba para todos os cristais? | Perguntar ao Apellon. Provisório: **1 cristal no tier 1, área no tier 2** (andar 50). |
| **P5** | **Bomba, permanente:** "nível 8" e "nível 4+ pede o andar 50". | A Pequena vem **de graça no andar 5**; **7 compras** a `10 + 3^(n−1)` (11, 13, 19, 37, 91, 253, 739); a **4ª compra** (Dinamite) em diante pede o andar 50. É a única leitura em que o "nível 4" cai no tier 2. |
| **P6** | **Bomba, força:** `base − 1% × andar` (Pequena no andar 1 = 109%, no 11 = 99%)? | Sim, é o que o exemplo do GDD dá. |
| **P7** | **Estagiário só offline:** online ele aparece correndo atrás, sem bater? As melhorias dele valem só offline? | Sim aos dois. |
| **P8** | **Estagiário, andar e crítico:** contrata desde o andar 1? O crítico dele no offline (que é uma conta fechada) entra como média? | Contrata desde o andar 1, como o GDD (os 500 já seguram). Crítico como **média** no offline: `dano × (1 + chance × 0,5)`, com o ×1,5 de base e sem o Dedo Fogoso. |
| **P9** | **Contador do raro visível** (revê a D-285): onde? | Um ícone de ampulheta com anel que esvazia, no topo da coluna dos bônus. Mockup. |
| **P10** | **Modal do raro:** fecha logo depois da escolha (e do sorteio dos 30 s)? | Sim, com a carta escolhida acesa por ~0,6 s antes de fechar. |
| **P11** | **Cartas tier 2 sem tempo escrito** ("bomba 2 s mais rápido", "2× cristais do andar"): 30 s? | 30 s, como as outras. |
| **P12** | **Anúncios:** o Unity Ads não deixa escolher a duração do vídeo (§3.4). O que vira o Curto/Médio/Longo? O Médio da Ganância e da Fúria é ×2 mesmo? Dois do mesmo tema: vale o maior (D-251)? | **Os 3 temas ficam; o tamanho vira "quantos anúncios"**: cada tema tem uma barra de 3 passos; 1 anúncio dá o Curto, o 2º sobe para o Médio, o 3º para o Longo (cada passo renova o tempo). Tudo no **Rewarded**, uma *placement* por tema. Alternativa mais simples: um anúncio por tema, sempre o Longo, com limite por dia no painel. Confirmar o ×2 do Médio com o Apellon; vale o maior; menu com os 3 temas. |
| **P13** | **Auto Upgrade** (anúncio Descanso e andar 30): o que ele compra? E o "Só offline"? | Compra **a mais barata** que der, uma por vez, só da run do anão (nunca permanentes; o estagiário é o andar 40). "Só offline" = na volta do offline, gasta as lascas trazidas na hora do COLETAR. |
| **P14** | **Bomba automática (andar 25):** o texto diz "3 opções: Sempre, nunca" (só duas). | Liga/desliga nos Ajustes, ligada de fábrica. |
| **P15** | **Números novos sem simulador** (D-247) mesmo com a Picareta ~60× mais cara no meio da run? | Decisão do Bruno. Sugestão: entram como estão, e a sessão longa dos testes do fim mostra o ritmo. |
| **P16** | **Pular andares no começo do dia:** só quando o requisito é 0? Com o avanço automático desligado também? | Sim, os andares que pedem 0 cristais se pulam no INICIAR, sempre, com a contagem na placa. Recorde e lascas vermelhas contam. |
| **P17** | **"Texto detalhado dos marcos"**: onde? | Tocar na barra do andar abre a lista dos marcos (feitos e por vir); ao chegar num marco, a placa da transição mostra o que liberou. Mockup. |
| **P18** | **Regra de passagem cortada** ("Após pressionar o…"). | Perguntar ao Apellon; até lá, como hoje. |
| **P19** | **Dedo Fogoso "+5%"**: +0,05 no multiplicador (1,5 → 1,55; 40 níveis → 3,5)? | Sim. |
| **P20** | **Crítico acima de 100%** (D-249) sai do código? | Sai: o GDD tirou e, com os números novos, não acontece. |
| **P21** | **Loja rápida** sai do DBG e fica ligada de fábrica? | Sim; o botão do DBG continua para comparar. |
| **P22** | **Anúncios que dobram** (Descansar e volta do offline): o "anúncio longo" pedido pelo GDD não existe no Unity Ads. | Um **Rewarded** comum, cada um com a sua *placement*; no jogo, o modal simulado de 30 s (D-218) até o SDK entrar. |
| **P24** | **Banner** em modais que não atrapalham (ideia do Bruno, 2026-11-25): quais? | Só em telas paradas e sem botão perto da borda de baixo: a tela do descanso, o "Bem-vindo de volta" e os AJUSTES. Nunca no jogo correndo nem na LOJA (o dedo esbarra). Mockup antes. |
| **P23** | **O v8.4 passa a ser a referência** (como a D-248 fez com o v8.2), com os números da §4 que não viraram pergunta? | Sim, e atualizar README, docs/README e o Roadmap. |

## 6. Plano proposto (**[REC]**, nada começa sem decisão)

Mesmo processo da D-298: blocos em sequência, cada um com plano de marco aprovado antes; as melhorias do Bruno vão para a lista de ajustes; sessões longas no fim.

| Bloco | O que entra | Tamanho |
|---|---|---|
| **1. Números e marcos** ([M9j](Marco%209j%20-%20Numeros%20e%20marcos%20do%20v8.4.md), ✅ aprovado em 2026-11-25, D-372) | tabela da §4 (anão, estagiário, permanentes), `andar × 5`, Tempo Curto, Bombardeador, Olho Treinado, sai a Promoção, marcos 3/15/45/60 (sem os de automação), crítico >100% sai, loja rápida ligada | médio (quase tudo no `Balance_GDD_v8` + 3 permanentes) |
| **2. Lascas pelo dano** | pagamento por golpe, bomba paga, offline novo | médio (mexe no núcleo da economia e em muitos testes) |
| **3. Estagiário offline** | online só corre atrás, Crítico dele, anúncio que dobra o offline | médio |
| **4. Bomba do v8.4** | andar 5/50, 110%…190% − 1%/andar, 1 cristal → área, permanente nova, Bombardeador na carga | médio |
| **5. Raro e anúncios do v8.4** | contador visível, cartas novas, modal fecha sozinho, 9 anúncios em abas, Ganância com tier 2, anúncio que dobra o descanso | médio |
| **6. Cristais tier 2 e 3** | sorteio por andar, vida, lascas, peso na barra; arte provisória | médio, depende de arte |
| **7. Automação e progressão** | compra automática (30, 40), bomba automática (25), Auto Upgrade do Descanso, pular andares no começo do dia, lista dos marcos | médio/grande |

**Ordem sugerida:** responder as perguntas → **1** (é a base) → **2** → **3** → **4** → **5** → **6** → **7**. O **M9i** fecha antes como está (E1…E8 e #23); a E9 (a bateria de testes) vai junto dos testes longos do fim, porque os números do v8.4 mudam vários testes que mediriam o v8.2.

**Revisões de decisões já registradas:** D-238 (lascas), D-239 (×10), D-249 (crítico >100%), D-250 (4º anúncio), D-253 (avanço no andar 5), D-255 e D-307 (bomba de graça e em área), D-260 (custo da Picareta), D-167, D-169, D-209, D-262, D-292 (estagiário), D-285 (contador escondido).
