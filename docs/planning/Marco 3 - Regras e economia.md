# Marco 3 — Regras e economia (melhorias)

> **Plano aprovado pelo Bruno em 2026-10-21**, incluindo a interpretação do C-04 (nível = compras feitas).
> **✅ Marco aprovado pelo Bruno em 2026-10-21 (D-40).**

> Plano de 2026-10-21, escrito depois da aprovação do M2 (D-33). Roadmap: [M3](Roadmap.md). Decisões que definem este marco: **D-35** (fórmulas do GDD com base ajustável), **D-36** (escopo), **D-37** (compras pelo debug), **D-34** (caminhada aprovada) — ver [Decisões e Riscos](Decisoes%20e%20Riscos.md). Requisitos: REQ-ECO-02, 03, 04, 07, 08, 10; REQ-INT-* (só a spec).

## Objetivo

As **melhorias** passam a existir nas regras: gastar lascas deixa o anão mais forte, mais rápido no golpe, mais rápido na caminhada e mais rentável. Tudo no núcleo, testado, com um **relatório de economia** que mede o ritmo. E o desenho de como um **estagiário** (2º minerador) vai dividir os alvos no mapa, para o M6.

**Para você:** abrir o jogo, juntar lascas, apertar **Comprar Picareta** no painel DBG e ver **DANO** subir no HUD e os cristais quebrando mais rápido. A loja de verdade é o M4.

## Decisões usadas

| # | O que fixa neste marco |
|---|---|
| D-35 | Formato das fórmulas do GDD; números no asset, mirando a 1ª compra em < 3 min; `Balance_GDD_v1` guardado para comparar |
| D-36 | Linhas: Picareta, Velocidade, Mineração, Picareta Dupla, **Botas** (nova); spec do estagiário só no papel |
| D-37 | Botões de compra e níveis/custos no painel DBG; HUD mostra DANO e VELOCIDADE atuais |
| D-34 | Caminhada base de 4 unidades/s mantida |
| C-04 (interpretação **confirmada pelo Bruno**, 2026-10-21) | **Nível = número de compras** (começa em 0). A próxima compra custa `custo inicial × crescimento^nível`. Ou seja, a 1ª compra custa o custo inicial. É a leitura usada na análise da economia |

## As cinco linhas de melhoria

Formato do GDD (REQ-ECO), com os números em asset. "Nível" = compras feitas.

| Linha | Efeito no nível *n* | Custo da próxima compra | Limite |
|---|---|---|---|
| **Picareta** | dano = dano base × 2ⁿ | inicial × 4ⁿ | máx. de compras |
| **Velocidade** | intervalo = max(mínimo, intervalo base − passo × *n*) | inicial × 3ⁿ | até chegar ao mínimo |
| **Mineração** | lascas por cristal × (1 + 1% × *n*) | inicial × 1,2ⁿ | máx. de compras (+100%) |
| **Picareta Dupla** | dano × 2 | preço único | 1 compra |
| **Botas** (nova, D-36) | caminhada = base × (1 + 10% × *n*) | inicial × 2ⁿ | máx. de compras |

**Valores (todos ajustáveis no asset; protótipo ≠ balanceamento):**

| Linha | `Balance_Prototipo` (ativo) | `Balance_GDD_v1` (referência) |
|---|---|---|
| Base do anão | dano 1 · golpe 1,0 s · caminhada 4 | dano 0,5 · golpe 5,0 s · caminhada 4 |
| Cristal | vida 10 · 1 lasca | vida 10 · 1 lasca |
| Picareta | 10 lascas, ×4, 19 compras | 50, ×4, 19 compras |
| Velocidade | 15, ×3, −0,05 s até 0,5 s (10 compras) | 200, ×3, −0,1 s até 0,5 s (45 compras) |
| Mineração | 10, ×1,2, +1%, 100 compras | 50, ×1,2, +1%, 100 compras |
| Picareta Dupla | 500 | 5.000 |
| Botas | 10, ×2, +10%, 10 compras | — (não existe no GDD: desligada) |

Ritmo esperado no protótipo: ~5 lascas/min no começo → **1ª compra em ~2 min**. O relatório da E4 mede de verdade.

## Como funciona (regras do núcleo)

- **Atributos do anão** (`MinerStats`): dano, intervalo, velocidade de caminhada e multiplicador de lascas são **calculados** a partir dos valores base + níveis. Nada fica "somado" em variável solta: muda um nível, recalcula tudo. Assim, nenhum bônus acumula duas vezes.
- **Compra** (`Purchase`): só conclui se houver lascas **e** o limite não foi atingido (REQ-ECO-08). Desconta, sobe o nível, recalcula os atributos e avisa (`UpgradePurchased`, `StatsChanged`, `ShardsChanged`). A resposta diz o motivo quando falha (sem lascas, no máximo). Uma chamada = no máximo uma compra; o bloqueio de duplo toque na interface é do M4.
- **Compra no meio da ação:** a simulação usa os atributos novos a partir do instante da compra. Se o novo intervalo já passou do tempo acumulado do golpe, o golpe sai no próximo `Tick`. Caminhada mais rápida vale do ponto onde o anão está.
- **Níveis no `GameState`** (junto de lascas, golpes, quebras): é o que o save vai guardar no M4.
- **Números:** `double`; o custo é comparado com uma folga mínima (`lascas ≥ custo − 1e-9`) para não falhar por arredondamento (Arquitetura, D-T4).

## Etapas

Cada etapa termina com algo que roda e só fica ✅ depois da sua validação.

| # | Etapa | Entrega | O que roda no fim |
|---|---|---|---|
| **E0** | **Testes pendentes da E5** (D-32) | Rodar EditMode e PlayMode no Unity, com o Editor fechado | 57 EditMode + 3 PlayMode verdes (ou correção do que falhar) |
| **E1** | **Melhorias no núcleo** | `UpgradeId`, `UpgradeDefinition` (custo inicial, crescimento, efeito, limite), `EconomyRules`, níveis no `GameState`, `MinerStats`, `Purchase` | Testes: custo e efeito de **todos os níveis de todas as linhas**, inclusive no limite e além dele; sem lascas = sem compra; saldo nunca negativo; Velocidade nunca abaixo do mínimo; Dupla só uma vez |
| **E2** | **Simulação usa as melhorias** | `MiningSimulation` lê `MinerStats`; eventos `UpgradePurchased` e `StatsChanged`; recompensa com Mineração | Testes: compra no meio do golpe e da caminhada; compras na mesma ordem dão o mesmo jogo (determinismo); 1 h simulada com compras, sem erro |
| **E3** | **Assets, HUD e debug** | `Balance_Prototipo` ganha as melhorias; novo `Balance_GDD_v1`; HUD atualiza DANO e VELOCIDADE; painel DBG com **Comprar Picareta / Velocidade / Mineração / Dupla / Botas** e níveis + próximo custo nas estatísticas | Play: juntar lascas (ou +100 no debug), comprar e ver o HUD e o ritmo mudarem |
| **E4** | **Relatório de economia** | Menu `Tools → Miners Idle → Debug → Relatório de economia`: simula 30 min com uma política "compra a mais barata que der", para os dois assets | Console: tempo da 1ª compra, lascas/min aos 5/10/30 min e níveis finais. Meta do protótipo: 1ª compra < 3 min |
| **E5** | **Spec do estagiário no mapa** | `docs/design/Spec - Estagiario no mapa.md` | Documento com diagrama de estados, regra de alvo com reserva, os casos da matriz (REQ-INT-01…05 adaptados ao mapa) e o que muda do GDD (a "espera" do REQ-INT-04) — para você aprovar |
| **E6** | **No celular** | `Rodar no celular` | Você joga 5–10 min comprando pelo DBG |

## Como testar (Bruno) — ao fim do marco

1. Play na `Bootstrap` (ou **Rodar no celular**). Abra o **DBG**.
2. Espere ~2 min (ou **Tempo ×5**): com 10 lascas, **Comprar Picareta**. LASCAS cai 10, **DANO** vai de 1 para 2, e cada cristal passa a cair em 5 golpes em vez de 10.
3. **Comprar Velocidade** (15): VELOCIDADE vai de 1,00s para 0,95s. **Comprar Botas** (10): o anão anda visivelmente mais rápido.
4. Sem lascas, o botão não faz nada e as estatísticas mostram o custo que falta.
5. **+100 lascas** algumas vezes e **Comprar Dupla** (500): DANO dobra; uma 2ª compra da Dupla não acontece.
6. `Tools → Miners Idle → Debug → Relatório de economia`: duas linhas no Console (protótipo e GDD v1) com a 1ª compra e lascas/min.
7. Test Runner: tudo verde.
8. Leia a spec do estagiário e aprove (ou ajuste) a regra.

## Critério de pronto

- [x] E0: testes da E5 rodados no Unity.
- [x] Custo e efeito de todos os níveis de todas as linhas cobertos por teste, inclusive limites.
- [x] Compras pelo debug mudam o jogo e o HUD; nenhuma compra deixa saldo negativo ou passa do limite.
- [x] Relatório de economia rodando; 1ª compra do protótipo < 3 min (~112 s).
- [x] Spec do estagiário aprovada (D-39).
- [x] Rodou no Moto G75; README e registro de decisões atualizados; você validou o marco (D-40).

## Fora deste marco

Loja de verdade e save (M4) · toque, crítico, combo, feedback e arte (M5) · código do estagiário (M6) · Letrado, penalidade, cavernas e "Passar de caverna" (M7) · AD/boost (M8) · balanceamento fino (M10).

## Riscos

| Risco | Mitigação |
|---|---|
| Valores de protótipo viram "o balanceamento" sem querer | Estão marcados como protótipo (D-29/D-35); o `Balance_GDD_v1` fica ao lado para comparar; mudanças de verdade passam por decisão |
| Picareta ×2 por compra torna a caverna 1 trivial rápido | Esperado sem Letrado/cavernas (M7); o relatório mostra quando isso acontece |
| Compra no meio do golpe gera golpe duplo ou perdido | Teste específico na E2 |
| C-04 interpretado diferente do que você imaginava | Confirmar na aprovação deste plano |

## Esforço

**25–40 h** de desenvolvimento focado (o núcleo e o debug do M2 já existem; o grosso é a E1, os testes e a spec).

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| E0 Testes pendentes da E5 | ✅ 2026-10-21 | Em batch: EditMode **57/57**, PlayMode **3/3** (inclui `DebugPanelTests`, que clica cada botão) |
| E1 Melhorias no núcleo | ✅ validada pelo Bruno em 2026-10-21 | `Core/Economy`: `UpgradeId`, `UpgradeDefinition` (custo inicial, crescimento, limite, efeito; limite 0 = linha desligada), `EconomyRules`, `MinerStats.Compute` (fórmulas do plano, sempre recalculadas do zero), `Purchases` (`TryPurchase` → Comprado / Sem lascas / No limite; `NextCost`). `GameState` ganhou os níveis e `SpendShards` (nunca negativo). Folga do custo: 1e-9 **ou** 10⁻¹² do custo, o que for maior (custos do GDD chegam a ~10²³, onde o erro de arredondamento passa de milhões). Testes: `UpgradeEconomyTests` (12 testes, 14 casos). EditMode **71/71**. Ainda não muda nada no jogo: a simulação passa a usar as melhorias na E2 |
| E2 Simulação usa as melhorias | ✅ validada pelo Bruno em 2026-10-21 | `MiningSimulation` recebe a `EconomyRules` (opcional: sem ela, nada vende e os atributos são os valores base, o jogo do M2), guarda os `Stats` atuais e usa dano, intervalo, caminhada e multiplicador de lascas deles. `Purchase(id)` compra, recalcula e avisa na ordem `UpgradePurchased` → `StatsChanged` → `ShardsChanged`; compra que falha não muda nada nem avisa. Golpe já vencido pelo intervalo novo sai no próximo `Tick`, uma vez só; o resto do tempo conta para o golpe seguinte. Testes: `MiningUpgradesTests` (9). Em batch: EditMode **80/80**, PlayMode **3/3**. Ainda não aparece no jogo: o asset e os botões vêm na E3 |
| E3 Assets, HUD e debug | ✅ validada pelo Bruno em 2026-10-21 | `MiningRulesAsset` ganhou as cinco linhas (`UpgradeLineValues`: custo inicial, crescimento, limite, efeito) e o intervalo mínimo; os padrões são a tabela de protótipo deste plano. O setup cria `Data/Balance_GDD_v1` com os números do GDD (Botas desligadas). `MiningRunner` passa a economia do asset para a simulação. HUD: DANO e VELOCIDADE são os atributos atuais e mudam a cada compra. Painel DBG refeito (o setup troca o painel da E5): mais alto, linha de melhorias com nível e próximo custo ("máx." no limite), 5 botões **Comprar …**. Teste PlayMode `BuyButtons_ApplyTheUpgrade_AndTheHudFollows`. Em batch: setup ok, EditMode **80/80**, PlayMode **4/4** |
| E4 Relatório de economia | ✅ validada pelo Bruno em 2026-10-21 (menu e testes rodados no Editor) | Core: `EconomyReport.Run` (partida sem cena; a cada segundo junta para a melhoria mais barata e compra assim que der; mede 1ª compra, ritmo em lascas/min na média dos 5 min anteriores, compras, ganho, gasto e níveis). Editor: menu `Tools → Miners Idle → Debug → Relatório de economia` (protótipo e GDD v1, sementes 1–3, 30 min). Testes: `EconomyReportTests` (5, um deles com 3 sementes: **meta de D-35**, 1ª compra do `Balance_Prototipo` < 3 min). Rodado fora do Unity com os mesmos valores dos assets; compila sem erros |
| E5 Spec do estagiário | ✅ aprovada pelo Bruno em 2026-10-21 (D-39) | [Spec - Estagiario no mapa.md](../design/Spec%20-%20Estagiario%20no%20mapa.md): mesma regra de alvo com **reserva** (ninguém pega o alvo do outro; o principal escolhe primeiro quando empata no tempo), três estados iguais aos do principal, sem a "espera" do GDD, matriz de 10 testes para o M6 e 4 decisões pedidas (E-1…E-4) |
| E6 No celular | ✅ Bruno jogou no Moto G75 comprando pelo DBG em 2026-10-21: tudo ok | |

**Como testar a E1:** `Window → General → Test Runner → EditMode → Run All` (ou `bash tools/ci/run-tests.sh EditMode` com o Editor fechado): 71 verdes. Os que provam a regra estão em `UpgradeEconomyTests`:
- `Cost_OfEveryLevel_…` e `BuyingEveryLevel_…`: custo de cada nível de cada linha (protótipo e GDD v1), compra até o limite exato gastando a soma dos custos, e nada além.
- `Pickaxe_…`, `Speed_…`, `Mining_…`, `Boots_…`, `DoublePickaxe_…`: o efeito em cada nível (a Velocidade nunca passa do mínimo de 0,5 s; a Dupla só uma vez e a 2ª tentativa não cobra).
- `GddV1_MatchesTheEconomyAnalysis`: a 19ª compra da Picareta do GDD custa ≈ 3,4·10¹², igual à análise da economia.
- Nada visível no jogo ainda.

**Como testar a E2:** Test Runner → EditMode → Run All: 80 verdes. Os que provam a regra estão em `MiningUpgradesTests`:
- `Pickaxe_MakesCrystalsBreakInFewerHits`: com dano 2, um cristal de 10 de vida cai em 5 golpes.
- `FasterSpeed_BoughtMidSwing_…`: comprar Velocidade no meio do golpe faz o golpe sair no próximo `Tick`, uma vez só.
- `Boots_BoughtMidWalk_…`: Botas no meio da caminhada aceleram a partir de onde o anão está.
- `Mining_MultipliesTheShardsOfEachCrystal`: com +50%, um cristal de 1 lasca rende 1,5.
- `SamePurchasesAtTheSameTimes_…`: as mesmas compras nos mesmos instantes dão o mesmo jogo, com quadros pequenos ou um passo só.
- `OneHour_BuyingTheCheapest_…`: 1 h comprando a mais barata, sem saldo negativo, sem passar de limite e sem intervalo abaixo do mínimo.

**Como testar a E3:**
1. **Rodar no celular** (o build aplica o setup) ou, no Editor, `Tools → Miners Idle → Projeto → Aplicar configuração do projeto` e Play na `Bootstrap`.
2. Abra o **DBG**. Abaixo das estatísticas, a linha das melhorias: "Picareta nv 0 (próx. 10) · Velocidade nv 0 (próx. 15) · Mineração nv 0 (próx. 10) · Dupla nv 0 (próx. 500) · Botas nv 0 (próx. 10)".
3. Com 10 lascas (~2 min, ou **+100 lascas**), **Comprar Picareta**: LASCAS cai 10, **DANO** vai para 2, a linha mostra "Picareta nv 1 (próx. 40)" e os cristais caem com metade dos golpes.
4. **Comprar Velocidade**: VELOCIDADE vai para 0,95s. **Comprar Botas**: o anão anda mais rápido. **Comprar Mineração**: cada cristal passa a render 1,01 (LASCAS com casas decimais).
5. Sem lascas, o botão não faz nada. **Comprar P. Dupla** com 500: DANO dobra; depois disso a linha mostra "máx." e o botão não cobra de novo.
6. Para comparar com o GDD: no objeto `Mining`, troque *Rules* para `Balance_GDD_v1` e dê Play (DANO 0,5, VELOCIDADE 5,00s, 1ª Picareta custa 50). Volte para `Balance_Prototipo`.
7. Test Runner: EditMode 80, PlayMode 4, todos verdes.

O tamanho da letra e a altura do painel são um chute de graybox; se ficar apertado no celular, ajusto.

**Resultado do relatório (2026-10-21, rodado fora do Unity com os valores dos assets, 30 min, sementes 1–3):**

| Asset | 1ª compra | Ritmo aos 5 / 10 / 30 min | Compras em 30 min | Níveis finais (Pic, Vel, Min, Dupla, Botas) |
|---|---|---|---|---|
| `Balance_Prototipo` | **111–114 s** (meta < 3 min ✅) | 7,2 / 8,8–9,2 / 11,2–11,4 lascas/min | 13 (gastou 260 de ~283) | 2, 1, 8, 0, 2 |
| `Balance_GDD_v1` | **nenhuma** | 0,4 / 0,6 / 0,6 lascas/min | 0 (ganhou 17) | 0, 0, 0, 0, 0 |

**Observações (medem, não decidem; mudar valores pede decisão):**
1. A meta da D-35 foi atingida: a 1ª compra sai em ~1min52.
2. **O ritmo cresce devagar depois:** de 5 para ~11 lascas/min em 30 min. Comprando "a mais barata", o jogador simulado gasta muito em **Mineração** (8 níveis = só +8% de lascas), porque ela é a mais barata (×1,2) e rende pouco. Na prática, Mineração vira uma "armadilha" para quem compra o que é barato. A Picareta (×2 de dano por ×4 de custo) é a que realmente acelera. Isso vale olhar quando a loja de verdade existir (M4) e no balanceamento.
3. O GDD v1 literal não chega à 1ª compra em 30 min (50 lascas a ~0,6/min ≈ 80 min). Confirma o achado C-01 e justifica o protótipo.

**Como testar a E4:**
1. `Tools → Miners Idle → Debug → Relatório de economia`. O Console mostra um bloco `[Economia]` com uma linha por asset e semente (1ª compra, ritmo aos 5/10/30 min, compras, ganho, gasto e níveis). Leva alguns segundos.
2. No `Balance_Prototipo`, mude *Crystal Reward* de 1 para 0,5 e rode de novo: o ritmo cai à metade, a 1ª compra passa de ~4 min e a linha mostra "ACIMA da meta de 3 min". Volte para 1. (Mudar só o custo da Picareta não altera a 1ª compra: o jogador simulado compra antes a mais barata, Mineração ou Botas.)
3. Test Runner → EditMode: `EconomyReportTests` verdes (inclusive a meta de 3 min, com 3 sementes).

