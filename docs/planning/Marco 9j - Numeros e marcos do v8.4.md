# Marco 9j — Números e marcos do v8.4

> Plano de 2026-11-25, **aprovado no mesmo dia** com as cinco recomendações (P1…P5 → D-367…D-371). **Marco aprovado em 2026-11-25 (D-372).** É o **bloco 1** da [Análise do GDD v8.4](Analise%20-%20GDD%20v8.4.md) §6, a base dos outros seis. Base: GDD v8.4 ([`docs/GDD_Idle_Miner v84.pdf`](../GDD_Idle_Miner%20v84.pdf), §3.3, §4.1, §4.2, §4.3; **D-347**, a referência) e as decisões **D-354** (estagiário sem andar), **D-365** (Dedo Fogoso +0,05) e **D-366** (sai o crítico acima de 100%).
>
> Começa depois do M9i fechado. **Os testes ficam para o fim de todo o v8.4** (pedido do Bruno em 2026-11-25): cada etapa só precisa compilar; os testes que hoje medem o v8.2 são corrigidos na bateria do fim para medir o v8.4, nunca apagados.

## Objetivo

**O jogo passa a ter os números e os marcos de andar do v8.4.** As melhorias da run ficam baratas no começo e crescem devagar (Picareta a `5 × 1,5^n`, Velocidade de 0,01 s em 0,01 s), o andar pede **metade** dos cristais (`andar × 5`), o avanço automático chega no **andar 3**, o estagiário se contrata desde o andar 1, a Promoção sai e vira o **marco do andar 15**, e entram os marcos **45** (dobra as lascas vermelhas) e **60** (−100 cristais por andar) e três permanentes novas: **Tempo Curto**, **Bombardeador** e **Olho Treinado**.

**Para você:** começar um save novo e ver a Picareta a 5 lascas, a barra do andar 1 encher em 5 cristais, o jogo descer sozinho a partir do andar 3; no descanso, ver as três permanentes novas e a Promoção fora; com o DBG, pular para o 15 e ver o dia seguinte começar com o estagiário.

## O que fica de fora (e vai para qual bloco)

| Assunto | Bloco |
|---|---|
| Lascas pelo dano (pagamento a cada golpe), a bomba pagando, o offline novo | 2 |
| Estagiário só offline, **a linha nova de Crítico dele** (P2), o anúncio que dobra o offline | 3 |
| Bomba tier 1 no andar 5, tier 2 no 50, força `base − 1% × andar`, a **permanente Bomba** com `10 + 3^n` (P3) | 4 |
| Contador do raro visível, cartas novas, anúncios em 3 temas | 5 |
| Cristais tier 2 e 3 | 6 |
| Bomba automática (25), compra automática (30, 40), pular andares de requisito 0 no INICIAR (D-362), lista dos marcos (D-363) | 7 |

Até o bloco 3, o estagiário continua aparecendo e batendo na tela como hoje; só o preço e o andar mudam aqui.

## Onde cada número entra

Os números ficam num **balanço novo, `Balance_GDD_v84`**, copiado do `Balance_GDD_v8` pelo setup em código (o mesmo caminho do v7 → v8 no M9c). Por quê: a cena e o DBG passam a usar o novo, e o v8.2 fica intacto em `Data/` para comparar e para os testes que ainda o medem até a bateria do fim. O método `EnsureGddV84Balance` só cria se o asset não existe: um ajuste feito à mão no Inspector fica.

`n` = compras feitas (D-202). Fórmulas do `UpgradeDefinition`: **Exponencial** `base × cresc^n`, **Linear** `base + cresc × n`, **OffsetExponencial** `base + cresc^n`.

### Melhorias da run (anão)

| Linha | Campo no asset | Hoje (v8.2) | v8.4 |
|---|---|---|---|
| Picareta | `pickaxe` | DamageScaled `7 × dano` · máx. nível 50 (49 compras) | **Exponencial `5 × 1,5^n` · máx. nível 200 (199 compras)** · o dano não muda (`pickaxeFormula` LevelScaled, passo 1, `1,2^nível`) |
| Velocidade | `speed` | `50 × 2^n` · −0,1 s · 19 | **Exponencial `10 × 3^n` · −0,01 s · 100** |
| Mineração | `mining` | `20 × 5^n` · +100% · 10 | **Exponencial `10 × 2,5^n` · +10% · 200** |
| Chance de Crítico | `critical` | `50 × 2^n` · +5% · 10 | **Exponencial `10 × 2,5^n` · +1% · 50** |
| Estagiário | `intern`, `internFloor` | 500 · andar 5 | 500 · **`internFloor` = 1** (D-354) |

### Melhorias do estagiário

| Linha | Campo | v8.4 |
|---|---|---|
| Picareta | `internPickaxe` | **Exponencial `50 × 1,5^n` · 100** |
| Velocidade | `internSpeed` | **Exponencial `50 × 2,5^n` · −0,01 s · 100** |
| Mineração | `internMining` | **Exponencial `50 × 2,5^n` · +10% · 100** (o teto de 1000% é o nível máximo) |
| Crítico | — (nova) | `20 × 2,5^n` · +1% · 50 → **bloco 3** (P2) |

### Permanentes (lascas vermelhas, no descanso)

| Linha | Campo | v8.4 |
|---|---|---|
| Memória | `memory` | **`10 + 1,1^n`** · −5 cristais · 100 |
| Sangue da Montanha | `mountainBlood` | **`10 + 1,5^n` · +100% de dano · 40** |
| Coração Ardente | `burningHeart` | **`10 + 1,5^n` · +100% de lascas · 40** |
| Dedos Nervosos | `nervousFingers` | **`10 + 1,5^n`** · +5% de chance · 10 |
| Dedo Fogoso | `fieryFinger` | **`10 + 1,5^n` · +0,05 no crítico · 40** (D-365: ×1,5 → ×3,5 no 40) |
| Alma dos Anões | `dwarfSoul` | **`5 + 1,1^n`** · +1 nível da Picareta · **100** |
| Treinar Estagiário | `internTraining` | `10 + 2^n` · **+100%** · 10 |
| Promoção do Estagiário | `internPromotion` | **sai** (vira o marco do andar 15) |
| Atalho | `shortcut` | **`10 + 5^n`** · −0,5 m (D-252) · 5 |
| Raro Dourado | `goldenRare` | 500 · única (igual) |
| Bomba | `bomb` | fica como está até o **bloco 4** (P3) |
| **Tempo Curto** | `shortTime` (novo) | **`10 + 2^n` · −0,1 s no intervalo do golpe · 10** |
| **Bombardeador** | `bomber` (novo) | **Linear `15 + 5n` · −0,5 s na carga da bomba · 4** (5 s → 3 s) |
| **Olho Treinado** | `trainedEye` (novo) | **Linear `10 + 5n` · +100% de lascas vermelhas · 10** |

As três novas entram **no fim** do `PermanentId` (depois da Bomba), para os índices salvos não mudarem; a Promoção continua no enum, só sai das regras e da tela.

### Andares e marcos

| Ponto | Campo | Hoje | v8.4 |
|---|---|---|---|
| Cristais por andar | `floorBreaks` | `andar × 10 − 5 × Memória` | **`andar × 5 − 5 × Memória`** (e −100 depois do 60) |
| Avanço automático | `autoAdvanceFloor` | 5 | **3** |
| Estagiário na LOJA | `internFloor` | 5 | **1** |
| Raro | `rareFloor` | 10 | 10 |
| Começa o dia com o estagiário | `internStartFloor` (novo) | Promoção (50 vermelhas, pede Treinar 5) | **andar 15** |
| Offline 6 h | `offlineDeepFloor` | 20 | 20 |
| Dobra as lascas vermelhas | `redShardsDoubleFloor` (novo) | — | **45** |
| −100 cristais por andar | `floorDiscountFloor`, `floorDiscount` (novos) | — | **60**, **100** |

Como os outros marcos, valem **para sempre** a partir da primeira vez que o andar é alcançado (D-253, pelo recorde `DeepestFloor`).

## As regras (o que já está decidido)

- **Requisito do andar:** `max(0, 5 × andar − 5 × Memória − (100 se o recorde ≥ 60))`. Com 0, a barra nasce cheia e, com o automático, o jogo passa direto; a contagem na placa e o pulo no INICIAR são do bloco 7 (D-362).
- **Crítico:** dano × `(1,5 + 0,05 × Dedo Fogoso)`. **Sai o excedente acima de 100%** (D-366): a chance para em 100%, que é exatamente o máximo do v8.4 (50 compras × 1% + Dedos Nervosos 10 × 5%).
- **Tetos:** não há teto escrito além do nível máximo de cada linha: Mineração da run até +2000% (200 × 10%), Coração Ardente até +4000% (40 × 100%), Mineração do estagiário até +1000%. Revê a **D-208** (os tetos de 1000%), que eram o nível máximo do v8.2.
- **Intervalo do golpe:** `max(0,1 s, 2 s − 0,01 × Velocidade − 0,1 × Tempo Curto)` (P5 sobre o piso).
- **Começar com o estagiário:** a partir do andar 15 alcançado uma vez, todo dia novo (INICIAR depois do descanso) já vem com o estagiário contratado, como a Promoção fazia; a regra "Promoção pede Treinar 5" sai junto.
- **Lascas vermelhas do descanso:** a tabela de hoje (`RedShardTable`) × `(1 + 1 × Olho Treinado)` × `(2 se o recorde ≥ 45)` (P4).
- **Bombardeador:** a carga de hoje (`bombChargeSeconds` = 5 s) menos 0,5 s por nível.

## Perguntas para o Bruno

| # | Pergunta | **[REC]** |
|---|---|---|
| **P1** (D-367) | **Quem já comprou a Promoção** (o seu save, o do Apellon): o que acontece com ela? | A versão do save sobe e aparece o **aviso de versão nova sugerindo recomeçar** (D-337), porque os preços mudam tudo. Quem segue sem recomeçar: o nível da Promoção some e as **50 lascas vermelhas voltam**; se o recorde já passou do 15, o estagiário vem igual. |
| **P2** (D-368) | **A linha nova de Crítico do estagiário** entra aqui ou no bloco 3? | **No bloco 3.** O efeito dela só vale no offline (D-354, crítico como média), que é o bloco 3; aqui ela seria uma compra que não faz nada. |
| **P3** (D-369) | **A permanente Bomba** (`10 + 3^n`, 7 compras, a 4ª pede o andar 50, a Pequena de graça no 5) entra aqui ou no bloco 4? | **No bloco 4**, com o resto da bomba (D-350…D-352): o preço novo só faz sentido com a Pequena de graça e o tier 2 no 50. O **Bombardeador** entra aqui, porque a carga de 5 s já existe. |
| **P4** (D-370) | **Olho Treinado e o marco 45** juntos: somam (+100% por nível e +100% do marco) ou multiplicam? | **Multiplicam:** "dobra as lascas vermelhas de todos os andares" é ×2 sobre o total, com o Olho já dentro. Com o Olho no 10 e o marco: ×11 × 2 = **×22**. |
| **P5** (D-371) | **O piso de 0,1 s no golpe** (`minHitInterval`) fica? Velocidade cheia + Tempo Curto cheio tiram os 2 s inteiros, então as últimas ~10 compras não mudariam nada. | **Fica 0,1 s.** A LOJA mostra a linha como **MÁX** quando o intervalo bater no piso, para ninguém comprar à toa (a mesma regra vale para o Tempo Curto no descanso). |

## Etapas

Cada etapa termina com o jogo rodando e os atalhos do DBG para não repetir a subida. Sem testes novos até a bateria do fim; cada etapa compila no batch do setup (`[Setup]` sem `error CS`).

### E1 — O balanço v8.4: preços, andar × 5 e o crítico

- `Balance_GDD_v84` criado pelo `EnsureGddV84Balance` com as tabelas acima (anão, estagiário, permanentes que já existem), `floorBreaks` 5, `autoAdvanceFloor` 3, `internFloor` 1. A cena, o `GameBootstrap` e o DBG passam a usar o v84.
- **Picareta** volta para a fórmula Exponencial (o `PickaxeLine` já trata a DamageScaled à parte; a Exponencial é o caminho comum).
- **Crítico:** sai o `Math.Max(0, CritChance() − 1)` do `CritMultiplier`; a chance para em 1. O Dedo Fogoso passa a +0,05.
- **Textos da LOJA:** a Velocidade com duas casas ("−0,01 s"; hoje o formatador mostra uma) e o **MÁX** da P5.
- **DBG → Andar:** o "Ir ao andar N" passa a percorrer os marcos do v8.4: 1 → 3 → 5 → 10 → 15 → 20 → 25 → 30 → 40 → 45 → 50 → 60 → 100 → 1.

**Como ficou (2026-11-25):**

- O `Balance_GDD_v84` nasce no setup (`EnsureGddV84Balance`, depois de todos os passos do v8.2) e a cena passou a usar ele; o `Balance_GDD_v8` fica em `Data/` para comparar.
- O que ainda não está no v84 fica como no v8.2 até a etapa dele: a Promoção (E2), as três permanentes novas (E3), a Bomba (bloco 4, D-369).
- A Velocidade **já mostrava duas casas** ("1,99s", o mesmo formatador do HUD): não precisou mexer. O Dedo Fogoso aparece como "+5%" por nível, a palavra do GDD.
- O **MÁX** do piso (D-371) está na regra de compra (`Purchases.NextCost` com as regras): vale na LOJA, na loja rápida e na compra, para o anão e o estagiário. Só a Velocidade cheia não chega ao piso (100 × 0,01 s = 1 s); dá para ver com o Tempo Curto, na E3.
- Compilado no batch do setup, sem `error CS`.

### E2 — Os marcos 15, 45 e 60 e a Promoção fora

- Campos novos no asset e no `MiningRules`/`PermanentRules`: `internStartFloor` (15), `redShardsDoubleFloor` (45), `floorDiscountFloor` (60) e `floorDiscount` (100).
- `Permanents.StartsWithIntern` passa a olhar o recorde (≥ 15) no lugar da Promoção; sai a trava "Promoção pede Treinar 5". A Promoção sai das regras e da loja do descanso.
- `FloorRequirement` com o desconto do 60; `Pending`/`Rest` com o ×2 do 45.
- **Save:** a versão sobe; o aviso da D-337 aparece; a migração da P1 (nível da Promoção some, 50 vermelhas voltam).
- **DBG:** o "Ir ao andar" já leva a 15, 45 e 60 e marca o recorde; o painel de cálculos mostra o requisito do andar com o desconto e as vermelhas pendentes com o ×2.

**Como ficou (2026-11-25):**

- Os três marcos moram numa peça nova do núcleo, `FloorMilestones` (dentro das `PermanentRules`), com os campos `internStartFloor`, `redShardsDoubleFloor`, `floorDiscountFloor` e `floorDiscount` no asset. O setup (`EnsureGddV84Milestones`) põe 15, 45, 60 e 100 no `Balance_GDD_v84` e desliga a Promoção (nível máximo 0), e a loja do descanso esconde a linha sozinha, como faz com toda linha desligada.
- O ×2 do 45 conta o recorde **ou o andar da run**: quem chega no 45 pela primeira vez já leva o dobro nesse descanso.
- Não precisou de botão novo no DBG: a barra do andar e o DBG → Dia (vermelhas pendentes) já leem o requisito com o desconto e as vermelhas com o ×2.
- O botão **Promoção** do DBG → Estagiário agora só leva o Treinar ao limite (a Promoção não existe mais no v8.4); o dia com o estagiário vem do "Ir ao andar 15".
- **Save versão 11:** um save até a 10 com a Promoção comprada abre com as **50 vermelhas de volta** e a Promoção zerada (o Console mostra `[Save] … lascas vermelhas de volta (D-367)`). O aviso de versão nova (D-337) compara a versão do app: aparece no celular com um APK novo; no Editor a versão é sempre a mesma, então ele não aparece lá.
- Compilado no batch do setup, sem `error CS`.

### E3 — As três permanentes novas

- `PermanentId.ShortTime`, `Bomber`, `TrainedEye` no fim do enum; as linhas no asset; os efeitos: Tempo Curto no intervalo do `MinerStats`, Bombardeador na carga da bomba, Olho Treinado nas vermelhas do descanso (P4).
- **Loja do descanso:** três linhas novas montadas pelo `PermanentShopSetup`, com nome e descrição nos dados de texto (pt-BR), e ícone provisório (um dos do Apellon que já temos, recolorido) até o desenho.
- **DBG → Dia:** o "+1 nv permanentes" que já existe passa a incluir as três; "+100 vermelhas" para comprar na loja do descanso.

**Como ficou (2026-11-25):**

- `PermanentId.ShortTime`, `Bomber` e `TrainedEye` no fim do enum; as linhas `shortTime`, `bomber` e `trainedEye` no asset, preenchidas no v84 pelo `EnsureGddV84NewPermanents` (Bombardeador e Olho Treinado com a fórmula Linear).
- **Tempo Curto** entra no intervalo do golpe do anão, junto da Velocidade, com o piso de 0,1 s. **Bombardeador** vira a carga da bomba de verdade (`BombChargeSeconds`): a faixa enche mais rápido e a bomba automática também. **Olho Treinado** multiplica as vermelhas do descanso, vezes o ×2 do marco do 45.
- **Loja do descanso:** as três no fim da lista, na ordem do GDD, com os textos no catálogo ("Golpe −0,1 s", "Carga da bomba −0,5 s", "Vermelhas +100%"). Ícones **provisórios** do pacote, até o desenho: poção azul, bola de fogo e o cristal vermelho em estrela.
- **DBG, atalho novo:** os botões "Comprar Picareta / Velocidade / Mineração / Crítico" do painel viraram **"Picareta +10"**, **"Velocidade +10"**… de graça (respeitam o limite e o piso). Com os preços a `3^n`, o "+100 lascas" nunca chegaria no MÁX da Velocidade; comprar com lascas continua na LOJA.
- Compilado no batch do setup, sem `error CS`.

### E4 — Documentos e conferência

- [Guia de Balanceamento](../Guia%20de%20Balanceamento.md) com o `Balance_GDD_v84`, os campos novos e qual mexe em quê.
- Uma subida à mão do andar 1 ao 20 num save novo, sem DBG, para o Bruno ver o ritmo dos preços novos (sem mudar número: ajuste de valor pede decisão, D-347).
- README, Roadmap e a Análise do v8.4 marcando o bloco 1.

**Como ficou (2026-11-25):**

- O [Guia de Balanceamento](../Guia%20de%20Balanceamento.md) fala do `Balance_GDD_v84`: as fórmulas e os valores novos (Picareta exponencial, Velocidade de 0,01 s, Mineração e Crítico, a árvore do estagiário, as permanentes com as três novas, a Promoção desligada), os marcos 3/15/45/60, o piso do golpe com o MÁX, o Bombardeador na recarga e o Olho Treinado com o ×2 do 45 nas vermelhas; e os atalhos do DBG para pular a subida. O que ainda é do v8.2 (lascas por cristal, bomba, raro, anúncios) está marcado como tal.
- README, Roadmap e a Análise do v8.4 com o bloco 1.
- **Falta a sua subida do andar 1 ao 20** (roteiro abaixo): é ela que fecha o marco.

## Como testar (Bruno) — ao fim de cada etapa

Cena `Assets/_Project/Scenes/Game.unity`, Play no Editor (ou `Tools → Miners Idle → Build → Rodar no celular (Android)`). Antes, `DBG → Save → Apagar save` para começar do zero.

**E1**

1. Abra a LOJA: Picareta **5**, Velocidade **10** (−0,01 s), Mineração **10** (+10%), Crítico **10** (+1%), Estagiário **500**, sem "Andar 5".
2. Quebre cristais no andar 1: a barra enche em **5**. Desça ao 2 (10 cristais) e ao 3: dali em diante o jogo desce sozinho.
3. `DBG → Andar → Ir ao andar` várias vezes: confira a ordem 1, 3, 5, 10, 15, 20, 25, 30, 40, 45, 50, 60, 100.
4. `DBG → +100 lascas` algumas vezes e compre Velocidade: o golpe anda de 0,01 em 0,01 s ("2,00s » 1,99s").
5. Na LOJA, Mineração mostra "+10%" por compra e Crítico "+1%". Descanse e veja na loja do descanso os preços novos: Memória 11, Sangue e Coração 11 (+100%), Dedo Fogoso 11 (+5%), Alma dos Anões 6, Atalho 11.

**E2**

1. `DBG → Andar → Ir ao andar 15`, descanse e INICIE: o dia começa **com o estagiário** contratado.
2. Na loja do descanso: **não há mais Promoção**.
3. `Ir ao andar 45`, descanse: as lascas vermelhas do descanso são o **dobro** do que o mesmo andar dava antes (o DBG → Cálculos mostra a conta).
4. `Ir ao andar 60`: o andar pede **100 cristais a menos** (o andar 60 pede 200 no lugar de 300); os andares até o 20 nascem com a barra cheia.
5. Se tiver um save antigo com a Promoção comprada (o do celular, por exemplo): no celular, com o APK novo, abre o aviso de versão nova; seguindo sem recomeçar, a Promoção sumiu e o banco de vermelhas tem 50 a mais (P1).

**E3**

1. Descanse (`DBG → Dia → Descansar agora`) e abra a loja do descanso: no fim da lista, **Tempo Curto** (11), **Bombardeador** (15) e **Olho Treinado** (10), com ícone, nível e efeito. `DBG → Dia → +100 vermelhas` para comprar.
2. Compre um Tempo Curto e INICIE: na LOJA, a Velocidade mostra "Golpe a cada 1,90s" (era 2,00s).
3. **MÁX:** toque em `DBG → Dia → +1 nv permanentes` até o Tempo Curto chegar a 10 (sobe as outras junto), INICIE e toque em `DBG → Velocidade +10` nove vezes. Com a Velocidade no 90, o golpe chega a 0,10s e a linha vira **MÁX** (D-371), na LOJA e na loja rápida.
4. Compre Bombardeador: depois de lançar, a faixa da bomba enche em 4,5 s (5 s → 3 s no nível 4).
5. Compre Olho Treinado: o `DBG → Dia` mostra as vermelhas que leva se descansar agora em dobro no nível 1, o triplo no 2…

**E4**

1. Save novo, sem DBG, do andar 1 ao 20: anote onde o ritmo trava ou corre demais. Vira pergunta para o Apellon ou decisão nova, não ajuste direto.

## Critério de pronto

- Os preços, efeitos e níveis máximos da LOJA e do descanso são os da §4 do v8.4 (tabelas acima), no `Balance_GDD_v84`.
- O andar pede `andar × 5 − Memória` cristais; o automático chega no 3; o estagiário se contrata desde o 1.
- Os marcos 15, 45 e 60 funcionam e valem para sempre; a Promoção não existe mais; o save antigo abre com o aviso.
- As três permanentes novas compram e fazem o que dizem.
- O crítico não passa de 100% nem dá dano extra por excedente.
- Os atalhos do DBG levam a todos os marcos do v8.4.
- Compila; a bateria de testes fica para o fim do v8.4 (os testes do v8.2 que quebrarem são corrigidos lá para medir o v8.4).
- Guia de Balanceamento, README, Roadmap e decisões atualizados.

## Riscos

- **O ritmo muda muito sem simulador** (D-347). A Picareta com 20 compras custa 16.626 (hoje 275) e a Mineração dá 10% por compra; enquanto as lascas forem as do v8.2 (o bloco 2 troca para lascas pelo dano), o jogo pode ficar **mais lento que o v8.4 de verdade** entre os blocos 1 e 2. A E4 mostra quanto; se travar, o DBG (+100 lascas) segura os testes até o bloco 2.
- **Andar × 5 com o estagiário online e o automático no 3**: os andares passam rápido no começo, e o offline (que conta na barra, D-246) desce mais andares. É o que o v8.4 pede.
- **Muitos testes do v8.2 quebram** (`GddV8RulesTests`, `FloorMilestoneTests`, os do crítico acima de 100%, da Promoção). Combinado: ficam para a bateria do fim e são corrigidos para medir o v8.4.
- **A Velocidade com duas casas** pode apertar o texto do card da loja rápida. Conferir no celular.

## Esforço

| Etapa | Horas |
|---|---|
| E1 | 5–7 |
| E2 | 4–6 |
| E3 | 5–7 |
| E4 | 1–2 |

Total: **~15–22 h**.

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| Plano | ✅ aprovado em 2026-11-25 ("Aprovo as 5"): P1…P5 → D-367…D-371 | — |
| E1 Balanço v8.4 | ✅ aprovada pelo Bruno em 2026-11-25 ("Testei, segue") | Compila; testes na bateria do fim. Ver "Como ficou" na E1 |
| E2 Marcos 15, 45, 60 | ✅ aprovada pelo Bruno em 2026-11-25 ("Testei, segue") | Compila; testes na bateria do fim. Ver "Como ficou" na E2 |
| E3 Permanentes novas | ✅ aprovada pelo Bruno em 2026-11-25 ("Testei, segue") | Compila; testes na bateria do fim. Ver "Como ficou" na E3 |
| E4 Documentos e conferência | ✅ documentos e a subida do Bruno do andar 1 ao 20 em 2026-11-25 | — |
| **Marco** | ✅ **aprovado pelo Bruno em 2026-11-25 ("Subi, aprovo o M9j"; D-372)** | Testes automáticos na bateria do fim do v8.4 |
