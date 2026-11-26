# Marco 9k — Lascas pelo dano

> Plano de 2026-11-25, **aprovado no mesmo dia** com as três recomendações (P1…P3 → D-373…D-375). **Marco aprovado em 2026-11-25 (D-376).** É o **bloco 2** da [Análise do GDD v8.4](Analise%20-%20GDD%20v8.4.md) §6, depois do [M9j](Marco%209j%20-%20Numeros%20e%20marcos%20do%20v8.4.md) (aprovado, D-372). Base: GDD v8.4 ([`docs/GDD_Idle_Miner v84.pdf`](../GDD_Idle_Miner%20v84.pdf), §2.1, §3.1) e a **D-348** (lascas pelo dano, a lasca voando a cada golpe; revê a D-238), com os efeitos limpos da **D-346**.
>
> **Os testes ficam para o fim de todo o v8.4** (pedido do Bruno em 2026-11-25): cada etapa só precisa compilar; os testes que medem o pagamento na quebra são corrigidos na bateria do fim para medir o pagamento por golpe.

## Objetivo

**Cada golpe paga, na hora, as lascas do dano que causou.** Some a fórmula `vida × 0,2 × (andar − 1)`, paga na quebra: um cristal de 38,4 de vida passa a render 38,4 lascas (vezes a Mineração, o Coração Ardente e as cartas), em pedaços, a cada picaretada. O dano que passa da vida que restava não paga. A bomba paga do mesmo jeito. O contador de lascas sobe a cada golpe e uma lasca voa até ele.

**Para você:** num save novo, ver o contador de LASCAS subir a cada golpe no andar 1 (o primeiro cristal dá 1 lasca, não 0,2), a lasca voando do cristal até o HUD a cada picaretada, e a mesma subida do andar 1 ao 20 do M9j ficar mais rápida no começo.

## O que muda (e o que fica)

| Peça | Hoje (v8.2, também no M9j) | No 9k (v8.4) |
|---|---|---|
| **Lascas do cristal** | `vida × 0,2 × max(1, andar − 1)` | **a vida do cristal** (andar 1 → 1; 10 → 38,4; 20 → 2.217; 50 → 4,3 × 10⁸) |
| **Quando paga** | na quebra, tudo de uma vez | **a cada golpe**: `min(dano, vida que restava)`, vezes o multiplicador **daquela hora** |
| **Bomba** | paga na quebra (D-309) | paga o dano que causou, como um golpe |
| **Estagiário na tela** | o golpe dele não paga; a quebra paga | o golpe dele paga como o do anão (até o bloco 3 tirar ele da tela, D-354) |
| **Offline** | cada cristal paga as lascas do andar × Mineração dele | cada cristal paga **a vida dele** × Mineração dele (a conta é a mesma; muda o valor) |
| **Lasca voando** | uma por quebra | **uma por golpe**, com o ritmo dos efeitos limpos (P2) |
| **Contador de LASCAS** | sobe na quebra | sobe a cada golpe |
| **Barra do andar, quebras, raro** | contam na quebra | iguais: a quebra continua contando na barra e abrindo a janela do raro |

Ficam **como estão**: os preços do M9j, as lascas vermelhas (pelo andar, no descanso), o crítico (o dano maior paga mais, até a vida que restava), os balanços antigos (`Balance_GDD_v8` e antes continuam pagando na quebra, para comparar).

## As regras (o que já está decidido)

- **Lascas do golpe** = `min(dano, vida antes do golpe) × (lascas do cristal ÷ vida máxima) × multiplicador`. No v8.4 as lascas do cristal **são** a vida máxima, então fica `min(dano, vida que restava) × multiplicador` (D-348). Escrito com a razão para o tier 2 e 3 do bloco 6 caberem sem mudar a conta.
- **Multiplicador** = o de agora: `1 + Mineração + Coração Ardente (+ Mineração do estagiário)` × cartas e anúncios de lascas ativos **no golpe** (D-348: "o que valer naquela hora").
- **Soma de um cristal inteiro** = a vida dele × multiplicador, igual ao que a quebra pagaria com a fórmula nova; só reparte no tempo.
- **Quebra:** conta na barra, nas quebras e no raro como hoje; não paga nada a mais (no tier 2 e 3, o bloco 6 soma a vida de novo na quebra).
- **Lascas do dia** (o resumo do descanso) contam o que os golpes pagaram.

## Perguntas para o Bruno

| # | Pergunta | **[REC]** |
|---|---|---|
| **P1** (D-373) | **A bomba paga com o multiplicador** (Mineração, Coração, cartas)? O dano dela não tem crítico nem bônus (D-255), mas as lascas são outra coisa. | **Sim, com o multiplicador**, igual ao golpe: "lascas = dano causado" vale para todo dano, e a bomba que não multiplicasse ficaria fraca justo onde ela é mais útil (andares fundos). O dano dela continua sem crítico nem bônus. |
| **P2** (D-374) | **A lasca voando a cada golpe** com a Velocidade no máximo são 10 por segundo (e o estagiário e a bomba juntos). Quantas na tela? | **Uma por golpe até ~4 por segundo**; mais rápido que isso, uma a cada 0,25 s (o contador sobe a cada golpe do mesmo jeito). A bomba solta **uma por cristal atingido**, até 3. O estagiário **não** solta (ele sai da tela no bloco 3). Na quebra, os estilhaços na cor do cristal continuam, sem a lasca extra. É a regra dos efeitos limpos (D-346): o efeito acompanha o ritmo, sem encher a tela. |
| **P3** (D-375) | **Um número "+N" de lascas** subindo do cristal a cada golpe? | **Não.** O contador de LASCAS e a lasca voando já mostram; um número por golpe a 10 por segundo é o que a D-346 tirou. Fica o número do dano do estagiário e da bomba, como hoje. |

## Etapas

Cada etapa termina com o jogo rodando. Sem testes novos até a bateria do fim; cada etapa compila no batch do setup (`[Setup]` sem `error CS`).

### E1 — O núcleo: o golpe paga

- `RewardFormula.Health` (GDD v8.4): as lascas do cristal são a vida dele. `MiningRules.PaysPerHit` liga o pagamento por golpe só nessa fórmula, e os balanços de antes continuam pagando na quebra.
- `MiningSimulation`: o golpe do anão, o do estagiário e a bomba pagam `min(dano, vida que restava)` × a razão × o multiplicador da hora (P1), com o `ShardsChanged`; a quebra conta e não paga. `GameState` ganha o pagamento sem quebra (soma no saldo e nas lascas do dia).
- O setup põe `rewardFormula = Health` no `Balance_GDD_v84` (um passo novo, que só troca se ainda estiver na FloorScaled).
- **Offline:** nada a escrever: a conta já usa as lascas do cristal do andar, que viram a vida.
- **DBG → Cálculos:** "lascas por cristal" passa a mostrar a vida (é o que o cristal inteiro rende sem bônus).

**Como ficou (2026-11-25):**

- `RewardFormula.Health` e `MiningRules.PaysPerHit`; o setup (`EnsureGddV84ShardsByDamage`) passou o `Balance_GDD_v84` para ela. Os balanços de antes seguem pagando na quebra.
- Um método só, `PayHit`, paga o golpe do anão, o do estagiário (enquanto ele está na tela) e a bomba (com o multiplicador, D-373): `min(dano, vida que restava) × lascas ÷ vida × multiplicador de agora`. A quebra conta na barra, nas quebras e no raro, e paga 0. O "Quebrar alvo" do DBG paga o que faltava do cristal, como um golpe.
- O offline não precisou de código: a conta dele já usa as lascas do cristal do andar, que agora são a vida.
- "Lascas por cristal" fica no **DBG → Offline** (não no Cálculos): a vida do andar × a Mineração do estagiário.
- Compilado no batch do setup, sem `error CS`.

### E2 — Na tela: a lasca a cada golpe

- `MiningFeedbackView`: a lasca voa no golpe do anão, com o ritmo da P2 (o mesmo relógio dos efeitos limpos), e na bomba, uma por cristal atingido até 3; na quebra fica só o estilhaço.
- O HUD de LASCAS já atualiza a cada `ShardsChanged`; conferir que não aloca a cada golpe (a regra do loop sem alocação). Se alocar, ele passa a atualizar no máximo a cada 0,1 s.
- **DBG → Efeitos → "Efeitos: limpos/antigos"** (D-346) também vale para a lasca: no "antigos", uma por golpe sem limite, para comparar.

**Como ficou (2026-11-25):**

- `MiningFeedbackView`: no balanço que paga por golpe (`Rules.PaysPerHit`), a lasca sai do golpe do anão, no limpo no máximo uma a cada 0,25 s (`calmShardGap`, ~4 por segundo; o relógio é o mesmo dos efeitos limpos). A bomba solta uma por cristal atingido, até 3 por explosão (`calmBombShards`; zera no fim da explosão). O estagiário não solta (D-374). Na quebra fica só o estilhaço; nos balanços antigos (que pagam na quebra) a lasca continua saindo da quebra, como antes. No "Efeitos: antigos", uma por golpe e uma por cristal da bomba, sem limite.
- **O HUD e as lojas por golpe:** o texto do LASCAS montava uma string a cada `ShardsChanged`, e a LOJA e a loja rápida refaziam todos os botões (preços, prévias, `MinerStats`) a cada um, o que alocava várias vezes por golpe. Agora o HUD guarda o saldo e escreve uma vez por quadro; as duas lojas marcam "sujo" e se refazem no máximo a cada 0,1 s pelo saldo (compra, andar e atributos continuam refazendo na hora). O botão que fica comprável aparece com até 0,1 s de atraso, o que não se nota.
- Os testes (`AllocationTests` e os de efeitos) ficam para a bateria do fim.

### E3 — Documentos e conferência

- [Guia de Balanceamento](../Guia%20de%20Balanceamento.md) §4.1 e §4.3 com o pagamento por golpe; a Análise do v8.4 marcando o bloco 2.
- A mesma subida do andar 1 ao 20 do M9j, sem DBG, para ver o ritmo com as lascas do v8.4 de verdade (sem mudar número: ajuste de valor pede decisão, D-347).

**Como ficou (2026-11-25):**

- [Guia de Balanceamento](../Guia%20de%20Balanceamento.md): §4.1 com a fórmula Health (lascas = vida, pagas a cada golpe; o v8.2 ao lado para comparar) e os exemplos novos (andar 10: 38,4; andar 20: 2.217); §4.3 com a linha "Lascas de um golpe" (`MiningSimulation.PayHit`); §4.5 e §4.6 dizendo que o offline rende a vida inteira e que a bomba paga com o multiplicador. O cabeçalho diz que os blocos 1 e 2 entraram.
- Análise do v8.4, Roadmap e README marcando o bloco 2.
- A subida do Bruno do andar 1 ao 20, sem DBG, em 2026-11-25 fechou o marco (D-376).

## Como testar (Bruno) — ao fim de cada etapa

Cena `Assets/_Project/Scenes/Game.unity`, Play no Editor (ou `Tools → Miners Idle → Build → Rodar no celular (Android)`). Antes, `DBG → Save → Apagar save`.

**E1**

1. No andar 1, olhe o contador de LASCAS: o primeiro cristal rende **1** (era 0,2), em pedaços a cada golpe. Com a Picareta no 1 (dano 2) e o cristal de 1 de vida, um golpe só: paga 1, não 2.
2. `DBG → Andar → Ir ao andar 10`: cada cristal (38,4 de vida) rende 38,4 sem bônus, em pedaços do tamanho do dano. Com o estagiário contratado, o `DBG → Offline` mostra "lascas por cristal" = 38,4 × a Mineração dele.
3. Compre Mineração na LOJA e veja os golpes seguintes pagarem 10% a mais; pegue a carta "Lascas ×2" do raro (`DBG → Bônus`) e veja o pagamento dobrar já no golpe seguinte.
4. Jogue a bomba num grupo de cristais: o contador sobe na hora da explosão, mesmo nos que não quebraram.
5. `DBG → Offline → Fora 1 h` com o estagiário: o "Bem-vindo de volta" traz cristais × vida × Mineração dele.

**E2**

1. A cada golpe, uma lasca voa do cristal até o contador. Com `DBG → Velocidade +10` várias vezes, as lascas continuam visíveis sem encher a tela (no máximo ~4 por segundo).
2. A bomba solta lascas dos cristais que atingiu (até 3).
3. `DBG → Efeitos → Efeitos: antigos`: uma lasca por golpe, sem limite, para comparar.

**E3**

1. `DBG → Save → Apagar save` e jogue sem DBG do andar 1 ao 20 (pode ser em mais de uma sessão; o offline também conta).
2. O que olhar, comparando com a subida do M9j:
   - **Andares 1 a 9:** o cristal rende mais que no M9j (andar 3: 2,25 contra 0,9). As primeiras compras devem chegar mais cedo.
   - **Andares 10 a 20:** rende menos (andar 10: 38,4 contra 69,2; andar 20: 2.217 contra 8.424). É aqui que o ritmo pode travar.
   - **Lascas na tela:** o contador sobe aos poucos a cada golpe, e as lascas voando não cansam a vista numa sessão longa.
3. Anote em que andar cada compra importante saiu e onde ficou mais de ~5 min sem comprar nada. Isso vira pergunta para o Apellon ou decisão nova (sem mudar número antes, D-347).

## Critério de pronto

- No `Balance_GDD_v84`, cada golpe (anão, estagiário na tela, bomba) paga o dano causado até a vida que restava, vezes o multiplicador da hora; a quebra conta e não paga.
- Um cristal inteiro rende a vida dele × multiplicador, online e offline.
- A lasca voa a cada golpe no ritmo da P2; o "Efeitos: antigos" mostra sem limite.
- Os balanços antigos continuam pagando na quebra.
- Compila; a bateria de testes fica para o fim do v8.4.
- Guia de Balanceamento, README, Roadmap e decisões atualizados.

## Riscos

- **O meio do jogo fica mais pobre.** Do andar 10 em diante a fórmula nova rende menos que a do v8.2 (andar 20: 2.217 contra 8.424), com os preços do M9j. Sem simulador (D-347), quem diz é a subida da E3; o DBG (+100 lascas, "+10" nas linhas) segura os testes.
- **Muitos eventos por golpe.** O `ShardsChanged` passa de um por quebra para um por golpe (até ~10 por segundo, mais o estagiário). O HUD, a LOJA e a loja rápida escutam: conferir a alocação (E2) e o `AllocationTests` na bateria do fim.
- **Testes do pagamento na quebra quebram** (`GddV8RulesTests`, `OfflineMiningTests`, `OfflineFloorsTests`, `CalculationTests`, os PlayMode que contam lascas). Combinado: bateria do fim, corrigidos para medir o pagamento por golpe.

## Fora deste marco

- Tier 2 e 3, que pagam a vida de novo na quebra (bloco 6).
- O estagiário só offline, o Crítico dele e o anúncio que dobra o offline (bloco 3).
- A bomba do v8.4 (110%…190%, bloco 4) e a Ganância com tier 2 (bloco 5).

## Esforço

| Etapa | Horas |
|---|---|
| E1 | 4–6 |
| E2 | 3–4 |
| E3 | 1–2 |

Total: **~8–12 h**.

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| Plano | ✅ aprovado em 2026-11-25 ("Aprovo as 3"): P1…P3 → D-373…D-375 | — |
| E1 O golpe paga | ✅ aprovada pelo Bruno em 2026-11-25 ("Testei, segue") | Compila; testes na bateria do fim. Ver "Como ficou" na E1 |
| E2 A lasca a cada golpe | ✅ aprovada pelo Bruno em 2026-11-25 ("Testei, segue") | Compila; ver "Como ficou" na E2 |
| E3 Documentos e conferência | ✅ documentos e a subida do Bruno do andar 1 ao 20 em 2026-11-25 | — |
| **Marco** | ✅ **aprovado pelo Bruno em 2026-11-25 ("Subi, aprovo o M9k"; D-376)** | Testes automáticos na bateria do fim do v8.4 |
| E3 Documentos e conferência | ⏳ | — |
