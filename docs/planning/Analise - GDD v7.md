# Análise — o que o GDD v7 muda no plano

> Análise de 2026-11-04. Compara o [GDD v7](../gdd%20v7.pdf) (versão 7.0, "Idle Miner Roguelite") com o [GDD v6](../gdd%20v6.pdf), com a [Análise - GDD v6](Analise%20-%20GDD%20v6.md) e com o que o jogo faz hoje (`Balance_GDD_v5`, Letrado, 10 cavernas).
>
> **Estado:** o v7 passa a ser a referência (**D-132**). No mesmo dia, o Bruno respondeu às contradições e lacunas que sobraram: as respostas viraram as decisões **D-133…D-142** ([Decisões e Riscos §1.10](Decisoes%20e%20Riscos.md)). As perguntas G15, G16 e G18…G25 ficam respondidas (inteira ou em parte), e entram as **G26…G28**. As propostas de plano do v6 (**D-128…D-131**) continuam pendentes, agora pelos números do v7.
>
> O PDF foi lido pelo texto (sem as páginas renderizadas); as tabelas foram remontadas pelo texto e conferidas pelas fórmulas.

## Resumo

- **O v7 responde quase tudo o que o v6 deixou aberto:** a Picareta Dupla sai, entra a linha **Chance de Crítico**, o estagiário é **compra única de 500** com árvore própria, as lascas vermelhas são **por nível alcançado**, as permanentes ganham custo e limites coerentes e a seção "fora do MVP" (que proibia o roguelite) foi removida.
- **O que o v7 não dizia, o Bruno decidiu em 2026-11-04:** a run termina por um **botão de descansar** ao lado do PASSAR DE CAVERNA (D-137); o dano da Picareta do anão **cresce com o nível da caverna** e a compra soma no Dano Base (D-133); a fórmula das lascas vale sobre a tabela (D-134); a Velocidade para em 0,1 s (D-135); o boost de 60 s é do **cristal raro** (D-140); os buffs ativos aparecem numa **coluna de ícones à esquerda** (D-141).
- **Proporção:** 80% idle-clicker e 20% roguelite (era 90/10). O roguelite agora inclui a moeda das melhorias permanentes.
- **Para o jogo ficar igual ao v7**, a maior distância está no M8 já implementado: números, Picareta, Crítico, Dupla, Mapa e 100 níveis (a etapa E3b proposta na D-128…D-130).

## Mudanças, uma a uma

### 1. Melhorias base do anão (§4.1) — M8 E3b

| Linha | Jogo hoje (`Balance_GDD_v5`) | GDD v7 + decisões de 2026-11-04 |
|---|---|---|
| **Picareta** | dano `5 × 2^(n−1)`, custo `50 × 4^(n−1)`, 19 compras | **`Dano = Dano Base + taxa^(nívelDaCaverna − 1)`**; Dano Base 1, **taxa 1,2 configurável**; cada compra **+1 no Dano Base** (configurável), até o nível 50; custo da próxima compra **2 × Dano Base atual** (D-133) |
| **Velocidade** | `50 × 5^(n−1)`; 2,0 → 0,1 s | **`50 × 2^(n−1)`**; −0,1 s por compra a partir de 2,0 s; **mínimo 0,1 s** (D-135), então 19 compras |
| **Mineração** | `50 × 1,2^(n−1)`, +100%/nível, 10 níveis | **`20 × 5^(n−1)`**, +100%/nível, 10 níveis, máximo +1000% |
| **Chance de Crítico** | não existe (crítico fixo de 5% em todo golpe) | **nova:** `50 × 2^(n−1)`, **+5% por compra**, 10 compras. Base: **0% no golpe automático, 5% no toque**; a linha soma nos dois (D-136) |
| **Picareta Dupla** | 5.000, compra única, ×2 no dano | **sai** (D-136; a menção no §2.1 é sobra, D-142) |
| **Estagiário** | (M7, não implementado) | **500, compra única**: libera a árvore dele (§5) |
| **Mapa** (era Letrado) | `100 × 10^(n−1)`, ×2 de dano, 10 níveis | **custo zero**; pede 100 cristais quebrados no nível atual (a Memória reduz); **sem ×2 de dano**; 100 níveis; o 100 leva ao coração da montanha |

**O que a Picareta pela caverna faz no jogo:** o dano acompanha a vida do cristal quase no mesmo passo (1,2 contra 1,25 por nível). A compra pesa no começo e perde peso no fundo, onde quem decide são as permanentes e os bônus:

| Nível | Vida do cristal | Lascas (vida × 1,2) | Golpes com Dano Base 1 | Golpes com Dano Base 50 |
|---|---|---|---|---|
| 1 | 1 | 1,2 | 1 | 1 |
| 10 | 7,5 | 8,9 | 2 | 1 |
| 30 | 646 | 775 | 4 | 3 |
| 50 | 56.052 | 67.262 | 8 | 8 |
| 100 | 3,93 bi | 4,71 bi | 57 | 57 |

Os 50 níveis da Picareta custam ao todo 2.450 lascas. A taxa e o incremento por compra ficam no asset para o relatório da E4 calibrar.

> **Atualização 2026-11-10 (D-202):** a tabela que o Apellon mandou (Dano Base 1, taxa 1,2, custo 2,1) fecha em `Dano = 1 + 1,2^(nível − 1)` com o **Dano Base fixo** e **custo = 2,1 × o dano** da Picareta no nível atual (4,20 no nível 1; 12,94 no 10; 417,51 no 30). O "+1 por compra" e o "2 × Dano Base" desta seção (D-133) ficaram para trás: as 49 compras passam a somar ≈ 79.721 lascas.

### 2. Cristais (§3.1, §6.1) — M8 E3b

- Vida `1 × 1,25^(nível da caverna − 1)` (igual ao v6).
- Lascas = **vida × 1,2** (o v6 dizia × 0,5). A tabela do §6.1 foi feita com × 0,5 e pula o nível 70: **vale a fórmula** (D-134).
- Os 10 cristais e biomas continuam em faixas de 10 níveis (1–9 Vidro … 90–100 Magma), sem mudança em relação ao v6.

### 3. Toque, crítico e combo (§3.3, §3.4)

- Dano automático = Picareta + melhorias + bônus (sem a Dupla).
- Toque = 33% de uma picaretada, com **5% de crítico base** (D-136). Dano crítico = 2 × o golpe.
- Combo igual ao v6: ×10 (+10% de dano, 5 s), ×30 (+15%, 10 s), ×50 (crítico garantido, 20 s), ×100 "Frenesi" (+50%, 30 s), acumulativos, visíveis enquanto ativos; zera sem toque por 1 s.

### 4. Run, lascas vermelhas e o botão de descansar (§1.3, §3.1) — marco "Dia e melhorias permanentes"

**Lascas vermelhas (tabela do §3.1 + D-138):**

| Nível alcançado na run | Lascas vermelhas |
|---|---|
| 1 a 9 | nenhuma |
| 10, 20, 30, 40 | +5 |
| 50, 60, 70, 80, 90, 100 | +10 |
| os outros níveis acima do 10 | +1 |

- Somam **a cada nível alcançado**, contado **depois de quebrar a parede** (ao entrar no nível). Valem em toda run.
- Uma run que chega ao nível 25 junta 24; uma que vai do 1 ao 100 junta **161** (20 + 60 + 81).

**Fim da run (D-137):**

- Um botão de encerrar a run (nome a definir: Descansar, Dormir, Ir para casa; G28) fica **na barra de baixo, lado a lado com o PASSAR DE CAVERNA**: à esquerda o de descansar, com o ícone e a quantidade de lascas vermelhas que o jogador leva se parar agora; à direita o PASSAR, apagado até o Mapa liberar.
- Do nível 1 ao 9 o botão aparece, sem lascas.
- Ao tocar, um **modal de confirmação** ("Encerrar o dia? Você leva ◆ N e perde as melhorias desta run").
- Ao encerrar, **zera tudo da run**: lascas comuns, melhorias base, nível (volta ao 1) e estagiário (a não ser com a Promoção). **Ficam só as melhorias permanentes**, e as lascas vermelhas, que são o que se "leva para casa" (§1.3).
- Isso **revê a D-112** (o fim era continuar na caverna 10, sem reset).

**Melhorias permanentes (§4.2, custo em lascas vermelhas; nível pela convenção da D-38):**

| Melhoria | Custo | Efeito | Limite | Custo total |
|---|---|---|---|---|
| Memória | `10 + 2^(n−1)` | −10 cristais na trava do Mapa; no máximo **remove a trava** | 10 | 1.123 |
| Sangue da Montanha | `10 + 2^(n−1)` | +10% de dano | 20 | ~1,05 mi (G26) |
| Coração Ardente | `10 + 2^(n−1)` | +50% de lascas, máximo +1000% | 20 | ~1,05 mi (G26) |
| Dedos Nervosos | `10 + 2^(n−1)` | +5% de crítico (automático e toque) | 10 | 1.123 |
| Alma dos Anões | `10 + 2^(n−1)` | a run começa com a Picareta +1 nível por nível | 10 | 1.123 |
| Treinar Estagiário | `10 + 2^(n−1)` | +10% de dano do estagiário | 10 | 1.123 |
| Promoção do Estagiário | 50 | a run já começa com o estagiário; pede Treinar 10 | compra única | 50 |

As linhas de 10 níveis custam 1.123 cada (cerca de 7 runs completas). Nas de 20, do nível 11 em diante cada nível dobra: o 20 sozinho custa 524.298 (G26).

### 5. Estagiário (§4.1, §5) — M7

- **Compra única de 500**; nasce no cristal atual do anão, bate no mesmo cristal e avança junto.
- **Árvore própria**, comprada depois de contratar: Picareta `50 × 4^(n−1)`, dano `5 × 2^(n−1)`, 20 níveis; Velocidade `50 × 5^(n−1)`, −0,1 s a partir de **2,0 s** (era 5,0); Mineração `50 × 1,2^(n−1)`, +100%/nível, máximo +1000%.
- **A Picareta Dupla do estagiário sumiu**: a D-101 cai. A D-94 (10% × nível de uma picareta do anão) fica superada pela árvore dele.
- Online e offline. Permanentes dele: Treinar e Promoção (§4).
- Em aberto: G27.

### 6. Offline (§1.1, §2.2) — M9

- Até **6 h**, só do estagiário. A "eficiência reduzida" é isto: **no offline só valem o dano, os bônus e as melhorias do estagiário** (D-139). A conta exata fica no plano do M9 (G15).

### 7. Bônus temporários (§2.2, §7.2, §8) — marco "Bônus temporários"

- **Boost do AD (§7.2):** modal com 5 s, botão "Aprimorar", bônus ×2,5 por 30 s, recarga de 30 s; raio pulsante acima do minerador por 30 s (§8, linha Boost). **[INT]** o ×2,5 é na velocidade (intervalo ÷ 2,5), como no v6 e na D-21; confirmar quando o marco começar.
- **"Boost ativo: dano dobrado por 60 s" (§2.2) é o cristal raro** (D-140): a melhoria temporária escolhida dura 60 s (D-97), e "dano ×2" é uma das opções. A lista de opções e a chance seguem na G12 e G13 (o próprio GDD marca a mecânica como "a definir").
- **Indicador de buffs (D-141):** uma coluna de ícones pequenos na lateral esquerda, abaixo do HUD (a direita já tem LOJA/AD/AJUSTES). Cada ícone mostra o símbolo do buff, um anel ou barra que esvazia e os segundos restantes; pisca nos últimos 5 s; o mais recente entra embaixo; um toque mostra por 2 s nome, efeito e tempo, sem pausar. Serve para o raro, os quatro marcos do combo e o AD. **Mockup na resolução de referência antes do código.**

### 8. Sobras de versões antigas (D-142)

- O §7.3 ainda fala em "Letrado" e no ícone de livro: vale o **Mapa** (o livro fica até haver um ícone de mapa).
- O §2.1 ainda cita a "picareta dupla": **não existe mais**.
- Saíram do v7 a direção de arte (§8 do v6) e o "fora do escopo" (§10 do v6, G25). A direção de arte do projeto continua a das nossas decisões (D-05, D-62…D-81).

## Custos altos e as alternativas do DBG (D-143)

Medida: até que nível uma descida (100 cristais por nível, lascas = vida × 1,2) precisa chegar para pagar a linha inteira, sem Mineração (×1) e com a Mineração no máximo desde o começo (×11). Pela estimativa, uma run sem permanentes trava entre os níveis 60 e 70 (a E4 mede). Lascas vermelhas por run: 19 até o nível 20, 33 até o 30, 66 até o 50, 161 até o 100.

| Linha (v7) | Total | Paga até o nível (×1 / ×11) | Veredito |
|---|---|---|---|
| Anão · Picareta (2 × Dano Base) | 2.450 | 9 / 2 | ok (barata) |
| Anão · Velocidade `50 × 2^(n−1)` | 26 mi | 49 / 39 | ok |
| Anão · Mineração `20 × 5^(n−1)` | 49 mi | 52 / 41 | ok |
| Anão · Chance de Crítico `50 × 2^(n−1)` | 51 mil | 21 / 11 | ok |
| **Estagiário · Picareta `50 × 4^(n−1)`** | 18 tri | 110 / 99 | **muito alta** |
| **Estagiário · Velocidade `50 × 5^(n−1)`** | 238 tri | 121 / 110 | **muito alta** |
| Estagiário · Mineração `50 × 1,2^(n−1)` | 1.298 | 6 / 1 | ok (barata) |
| **Sangue da Montanha, Coração Ardente** (20 níveis) | 1.048.775 lascas vermelhas | ~15.900 runs até o 50 | **muito alta** |
| Memória, Dedos Nervosos, Alma, Treinar (10 níveis) | 1.123 cada | ~17 runs até o 50, cada | longa; a discutir |

O padrão é sempre o GDD. As alternativas, no DBG → Cálculos:

| Linha | Alternativas | Total | Paga até o nível (×1 / ×11) |
|---|---|---|---|
| Estagiário · Picareta | `50 × 3^(n−1)` · `50 × 2,5^(n−1)` | 87 bi · 3 bi | 86 / 75 · 71 / 60 |
| Estagiário · Velocidade | `50 × 3^(n−1)` · `50 × 2^(n−1)` | 29 bi · 26 mi | 81 / 70 · 49 / 39 |
| Sangue, Coração | `10 + 1,5^(n−1)` · `10 + 2 × (n−1)` | 6.849 · 580 | — |
| As 4 de 10 níveis | `10 + 2 × (n−1)` | 190 | — |

No nível da fórmula das permanentes vale o **nível da melhoria** (a compra *n*), não o da caverna: com a caverna, o custo deixaria de subir a cada compra e um "descansar no nível 1" tornaria tudo barato.

## Perguntas que sobraram

| # | Pergunta | Onde |
|---|---|---|
| ~~G26~~ | ~~Custo das permanentes de 20 níveis~~ → **D-143**: o GDD fica como padrão; as alternativas no DBG | §4.2 |
| G27 | Estagiário: o "nível" do dano `5 × 2^(n−1)` é o da melhoria (o do anão é o da caverna, D-133)? Antes de comprar a Picareta dele, ele bate com quanto ("atributos base do minerador")? A Velocidade dele também para em 0,1 s (o GDD diz 0 s, como dizia a do anão)? | §4.1, §5.4 |
| G28 | Nome e ícone do botão de encerrar a run: Descansar, Dormir, Ir para casa? | §1.3 (D-137) |

Continuam abertas, sem mudança: G11 (detalhe do acúmulo do combo), G12 e G13 (cristal raro), G14 (parede, provisória pela D-108) e a conta do offline (G15, no M9).

## Proposta para os marcos (para decidir: D-128…D-131, revistas pelo v7)

1. **M8 E3b, "o M8 no GDD v7"** (D-128…D-130): `Balance_GDD_v7` no lugar do v6 proposto, com os números das seções 1 e 2; Picareta pela caverna (D-133); linha Crítico e crítico automático base 0% (D-136); Dupla fora; Letrado → Mapa (custo zero, sem ×2, 100 níveis); níveis 1–100 com o bioma trocando a cada 10 (D-129); save v3. Os 5% de crítico hoje valem para o automático e para o toque (`MiningSimulation.RollCritical`).
2. **M8 E4:** relatório de economia do 1 ao 100, medindo até onde uma run chega **sem** as permanentes, e calibrando a taxa e o incremento da Picareta; teste no G75.
3. **Dia e melhorias permanentes (D-131):** botão de descansar, lascas vermelhas, modal, reset da run, tela das permanentes do anão, save separado em run × permanente. Com a G21, a G22 e a G25 respondidas, **o marco já pode ser planejado**; falta só a G26 (que não bloqueia: os níveis 1–10 das linhas de 20 já têm custo).
4. **M7 Estagiário:** compra de 500, árvore própria, Treinar e Promoção. Espera a G27.
5. **Bônus temporários:** cristal raro (60 s), combo, AD (×2,5 por 30 s) e a coluna de buffs (D-141). Espera a G12 e a G13.
6. **M9:** offline de 6 h só com o estagiário (D-139).
