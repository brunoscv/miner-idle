# Marco 2 — Protótipo do loop no mapa (graybox)

> Plano de 2026-10-21, escrito depois da aprovação do M1 (D-25). Roadmap: [M2](Roadmap.md). Decisões que definem este marco: **D-26** (mineração no mapa), **D-27** (regra de alvo), **D-28** (roadmap reordenado), **D-29** (valores de protótipo) — ver [Decisões e Riscos](Decisoes%20e%20Riscos.md).
> Referência de tela: [Referencia de tela - mapa.jpg](../design/Referencia%20de%20tela%20-%20mapa.jpg).

## Objetivo

O primeiro jogável. Um anão (cápsula) **anda pelo mapa**, **minera grupos de cristais** (losangos) na ordem da regra D-27, cada quebra **soma lascas**, e o HUD mostra o jogo andando sozinho. Tudo em formas simples: o objetivo é ver e sentir o loop, **não** a arte.

**Para o jogador (você):** abrir o jogo, não tocar em nada, e ver o anão ir de cristal em cristal, as barras de vida caindo, cristais novos aparecendo e as lascas subindo.

## Decisões usadas

| # | O que fixa neste marco |
|---|---|
| D-26 | Mapa visto de cima; cristais nascem continuamente até um máximo; cada grupo de cristais = 1 alvo (1 vida, 1 recompensa) |
| D-27 | Alvo = **menor lote, depois mais próximo**; empate exato → o que nasceu primeiro; o alvo não muda até quebrar; sem cristal, o anão fica parado |
| D-28 | Só as regras que este loop pede; economia, loja e estagiário ficam no M3 em diante |
| D-29 | Valores de protótipo (tabela abaixo), ajustáveis durante o marco sem nova decisão |
| D-14 (provisório) | 270×480, PPU 16: a tela tem ~16,9 × 30 unidades de mundo |
| D-11 (aberto) | Formato numérico **provisório** em pt-BR (`1.234`; com abreviação K/M/B/T acima de 1 milhão); a decisão final continua para o M4 |
| D-30 (aberto, recomendação adotada) | "Contar picaretas" = **contador de golpes**, mostrado no painel de debug. Se você decidir que é outra coisa, muda só a exibição |
| D-31 (aberto, recomendação adotada) | Anão anda em **linha reta** até o alvo; cristais não bloqueiam o caminho |

## Valores de protótipo (D-29) — não são balanceamento

Ficam em `Assets/_Project/Data/Balance_Prototipo.asset` (ScriptableObject). Unidade de distância = unidade de mundo (16 px).

| Valor | Protótipo | Observação |
|---|---|---|
| Área do mapa | x −7,5…7,5 · y −11…10 | Entre o HUD de cima e a faixa do botão de baixo |
| Cristais no início (lote 1) | 10 | Como no seu exemplo |
| Máximo de cristais na tela | 12 | Ao chegar no máximo, para de nascer até alguém quebrar |
| Intervalo de nascimento | 5 s | Nasce mais rápido do que se quebra: a fila fica sempre cheia, o que deixa a regra D-27 visível |
| Distância mínima entre cristais | 2,0 | E 1,5 do anão. Se não achar lugar em algumas tentativas, pula aquele nascimento |
| Vida do grupo de cristais | 10 | GDD v1 caverna 1 |
| Recompensa | 1 lasca | GDD v1 caverna 1 |
| Dano por golpe | 1 | GDD v1 é 0,5 (100 s por cristal: lento demais para sentir o loop) |
| Intervalo entre golpes | 1,0 s | GDD v1 é 5,0 s, idem |
| Velocidade de caminhada | 4 /s | Atravessar o mapa leva ~5 s |
| Alcance de mineração | 1,0 | O anão para a esta distância do centro do cristal e começa a bater |

Ritmo esperado: ~10 s batendo + ~1–3 s andando por cristal ≈ **5 lascas/min**. Serve para avaliar o *feel*, não a economia.

## Como funciona (regras do núcleo)

```
            ┌──────────── sem cristal na tela ────────────┐
            ▼                                              │
 [Parado] ──(cristal disponível)──► escolhe alvo (D-27) ──►[Andando]──(distância ≤ alcance)──►[Batendo]
     ▲                                                                                          │
     └──────────────────── cristal quebrou: soma lascas, escolhe o próximo ◄─────────────────────┘
```

- **O tempo é do código.** `MiningSimulation.Tick(dt)` avança tudo: posição do anão, acumulador de golpes, relógio de nascimento. Um `Tick` grande atravessa várias transições sem perder tempo (chega ao cristal no meio do `dt` → o que sobra já conta para os golpes). Resultado: **o mesmo estado** com 60 ticks por segundo ou com um tick de 10 minutos. É isso que permite o ×20 do debug, o simulador do M3 e o progresso offline no futuro.
- **Andar é regra, não animação.** A posição do anão vive no núcleo porque o tempo andando custa lascas/min (R-23). A cena só desenha o anão onde o núcleo diz.
- **Lote:** os cristais do início recebem lote 1; cada nascimento seguinte recebe 2, 3, 4… (D-27).
- **Aleatoriedade com semente:** posições de nascimento vêm de um gerador próprio com semente (`IRandom`). A mesma semente reproduz a partida inteira, o que serve para reproduzir um bug.

## Estrutura do código

Segue a [Arquitetura](Arquitetura%20Tecnica.md) (D-T1, D-T2, D-T5, D-T13). Dependências só descem.

| Camada | Peças |
|---|---|
| **Core** (C# puro) | `Vec2` (vetor em `double`) · `IRandom` + gerador com semente · `Crystal` (id, posição, vida, recompensa, lote) · `CrystalField` (cristais vivos e regras de nascimento) · `TargetSelector` (D-27, função pura) · `Miner` (Parado/Andando/Batendo) · `MiningSimulation` (`Tick`, eventos) · `MiningRules` (valores, somente leitura) · `GameState` (lascas, golpes, quebras) · `NumberFormatter` (único formatador) |
| **Game** | `MiningRulesAsset` (o ScriptableObject → `MiningRules`) · `MiningRunner` (chama `Tick` no `Update` com o tempo do quadro × escala do debug; limita o `dt` real a 0,25 s para pausas do app) · composição explícita da sessão, sem singleton |
| **Presentation** | `MapView` (retângulo do chão) · `MinerView` (lê a posição do núcleo a cada quadro, sem alocar) · `CrystalView` com *pool* (`ObjectPool`: cristais nascem e somem o tempo todo) · barra de vida só no alvo atual (REQ-CRY-03) |
| **UI** | `HudView` (LASCAS, DANO, CAVERNA, VELOCIDADE; atualiza por evento) · `DebugPanel` |

**Eventos do núcleo:** `CrystalSpawned`, `TargetChanged`, `HitLanded`, `CrystalBroken`, `ShardsChanged`. Views assinam em `OnEnable` e cancelam em `OnDisable`.
**Textos:** rótulos do HUD e do debug ficam nos prefabs/cena (dados), não no código.
**Graybox:** sprites brancos 16×16 (quadrado, losango, círculo) gerados pelo `ProjectSetup` em `Assets/_Project/Art/Graybox/`, coloridos por material/cor. Nenhum asset de terceiros.

## Etapas

Cada etapa termina com algo que roda e só fica ✅ depois da sua validação.

| # | Etapa | Entrega | O que roda no fim |
|---|---|---|---|
| **E1** | **Seletor de alvo** | `Vec2`, `Crystal`, `IRandom`, `TargetSelector` + testes EditMode | Test Runner verde. Casos: lote menor vence mesmo muito longe · mesmo lote → mais próximo · empate exato → nasceu primeiro · cristal quebrado é ignorado · sem cristais → nenhum alvo |
| **E2** | **Simulação do loop** | `Miner`, `CrystalField`, `MiningSimulation`, `GameState`, `MiningRules` + testes | Testes verdes **e** menu `Tools → Miners Idle → Debug → Simular 10 minutos` que escreve no Console: lascas, golpes, quebras, lascas/min, semente |
| **E3** | **Cena graybox** | Mapa, anão, cristais com *pool*, barra de vida do alvo, `MiningRunner`, valores no `Balance_Prototipo` | Play na cena `Bootstrap`: o anão anda e quebra cristais sozinho |
| **E4** | **HUD** | `HudView` com os 4 indicadores + `NumberFormatter` (testado) | Lascas sobem no HUD a cada quebra. O rótulo de diagnóstico do M1 sai da tela e vai para o debug |
| **E5** | **Painel de debug** | Botão discreto no canto (Editor e *Development Build*; fora do release) | Atalhos funcionando (lista abaixo) |
| **E6** | **Fluxo e aparelho** | Teste PlayMode do loop + APK no Moto G75 | APK roda 10 min sem erro a 60 fps |

**Testes da E2 (os que protegem a regra):** o anão anda na velocidade certa e para no alcance · só bate depois de chegar, um golpe a cada intervalo · a quebra soma a recompensa **uma vez** · o alvo não muda quando nasce um cristal mais perto durante a caminhada · o nascimento respeita máximo, área e distância mínima · mesma semente = mesma partida · **o mesmo estado com `dt` de 1/60 s ou de 10 min** · 1 h simulada sem exceção e com `lascas = quebras × recompensa`.

**Atalhos do painel de debug (E5):** tempo ×1 / ×5 / ×20 · gerar cristal agora · quebrar o alvo atual · +100 lascas · reiniciar com a mesma semente / com semente nova · **mostrar ordem** (número do lote em cada cristal e uma linha do anão até o alvo, para validar a D-27 de olho) · estatísticas: semente, golpes (D-30), quebras, lascas/min, versão/resolução/FPS (o rótulo do M1).

## Como testar (Bruno) — ao fim do marco

**No Editor:**
1. Abra `Assets/_Project/Scenes/Bootstrap.unity` e aperte **Play**. Não toque em nada.
2. Veja o anão (cápsula) sair do lugar, andar até um losango e parar ao lado dele. A barra de vida do alvo cai a cada ~1 s; ao zerar, o losango some e **LASCAS** sobe 1.
3. Abra o painel de debug (botão no canto) e ligue **Mostrar ordem**. Confira a regra D-27: enquanto houver cristal com o número **1**, ele só vai para esses, sempre o mais próximo; depois segue 2, 3, 4… mesmo que haja um cristal novo colado nele.
4. No debug, ponha **×20** por ~1 minuto: cristais novos aparecem até o máximo (12), nada sobrepõe, nada trava.
5. **Reiniciar com a mesma semente** duas vezes: a partida se repete igual.
6. `Tools → Miners Idle → Debug → Simular 10 minutos`: o Console mostra lascas/golpes/quebras.
7. `Window → General → Test Runner`: EditMode e PlayMode verdes (ou `bash tools/ci/run-tests.sh` com o Editor fechado).

**No Moto G75:** gerar o APK (`Tools → Miners Idle → Build`), instalar, deixar rodando **10 minutos**; abrir o debug e conferir FPS (60) e que lascas = quebras.

**Pergunta para você responder ao fim:** *ver o anão ir e voltar pela ordem do lote é gostoso, ou irrita?* (R-23). A resposta orienta a velocidade de caminhada e a regra antes do M3.

*Vão ser criados:* menu `Tools → Miners Idle → Debug → Simular 10 minutos`; asset `Data/Balance_Prototipo`; prefabs de graybox. Nenhuma cena nova (o protótipo vive na cena `Game`).

## Critério de pronto

- [ ] Testes EditMode da E1/E2 verdes, incluindo determinismo e independência do `dt`.
- [ ] Na cena, o anão segue a regra D-27, conferida com "Mostrar ordem".
- [ ] HUD com LASCAS, DANO, CAVERNA e VELOCIDADE atualizando por evento; lascas = quebras × recompensa.
- [ ] Painel de debug com todos os atalhos da E5; fora do build de release.
- [ ] Nenhuma alocação por quadro no loop (Profiler no Editor) e nada de `Find`/`GetComponent` no `Update`.
- [ ] APK no Moto G75: 10 min sem erro, 60 fps.
- [ ] README atualizado; você validou o marco e respondeu à pergunta do *feel*.

## Fora deste marco

Tiles, animações, arte e biomas · loja e melhorias · toque, crítico e combo · estagiário (a reserva de alvo entra junto com ele, R-24) · Letrado, penalidade e "Passar de caverna" · save e offline · AD · som · elementos do print sem regra (barras laterais, timers, "Modifiers", botões subir/descer/Auto).

## Riscos

| Risco | Mitigação |
|---|---|
| R-23: o vaivém da ordem do lote parece "burro" | Pergunta do *feel* no fim; velocidade de caminhada ajustável no asset; "Mostrar ordem" deixa a regra visível |
| Determinismo quebrado por ponto flutuante ou por `System.Random` diferente entre plataformas | Gerador próprio com semente; teste de `dt` grande × `dt` pequeno com tolerância |
| Nascimento sem lugar livre com o mapa cheio | Número fixo de tentativas e pula o nascimento; coberto por teste |
| Começar a desenhar arte cedo | Graybox obrigatório; arte segue no M1b (Art Spike) em paralelo |

## Esforço

**30–45 h** de desenvolvimento focado (≈ 2 semanas a 20 h/semana). Reavaliar ao fim da E2.

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| E1 Seletor de alvo | ✅ validada pelo Bruno em 2026-10-21 | `Core/Math/Vec2`, `Core/Random/IRandom` + `SeededRandom` (SplitMix64: mesma sequência no Editor e no celular, ao contrário do `System.Random`), `Core/Mining/Crystal`, `Core/Mining/TargetSelector`. Testes: `TargetSelectorTests` (8) e `CoreBasicsTests` (7). EditMode **19/19 verdes** (com os 4 do M1) |
| E2 Simulação do loop | ✅ validada pelo Bruno em 2026-10-21 | Núcleo: `MiningRules`, `GameState`, `Miner`, `CrystalField`, `MiningSimulation` (avança de evento em evento: chegada, golpe, nascimento). Game: `MiningRulesAsset` (o `Balance_Prototipo`). Editor: menu `Tools → Miners Idle → Debug → Simular 10 minutos`; o `ProjectSetup` cria o asset. Testes: `MiningSimulationTests` (13). Os 28 testes do núcleo passaram compilados fora do Unity (C# 9); no Unity, 32/32 verdes (Bruno) |
| E3 Cena graybox | ✅ validada pelo Bruno em 2026-10-21 | Game: `MiningRunner` (dono da partida na cena; `Tick` com o tempo do quadro limitado a 0,25 s × escala do debug; semente fixa opcional no Inspector). Presentation: `MapView`, `MinerView` (posição lida do Núcleo; "soco" de escala no golpe), `CrystalView` + `CrystalFieldView` (pool; barra de vida só no alvo), `CameraWidthFit`. Editor: `GrayboxSetup` (sprites 16×16, prefab `Prefabs/Graybox/CrystalView`, objeto `Mining` na cena Game). Teste PlayMode `MiningSceneTests`. Em batch: setup ok (reaplicar não muda nada), EditMode **32/32**, PlayMode **2/2**. Bug achado e corrigido no caminho: o setup carregava o prefab do cristal antes de abrir a cena Game, e abrir a cena descarrega assets "sem uso" (a referência ficava vazia); agora carrega depois |
| E4 HUD | ⏳ | |
| E5 Painel de debug | ⏳ | |
| E6 Fluxo e aparelho | ⏳ | |

**Como testar a E1:** com o projeto aberto, `Window → General → Test Runner → EditMode → Run All` (ou `bash tools/ci/run-tests.sh EditMode` com o Editor fechado). O teste que reproduz o seu exemplo é `TargetSelectorTests.FullSequence_ClearsInitialBatchByProximity_ThenFollowsBirthOrder`: o lote inicial é limpo de vizinho em vizinho (2 → 1 → 3) e depois a fila segue 4 → 5 → 6, mesmo com o 4 colado no anão e o 6 mais perto que o 5. Ainda não há nada na cena (a E3 é a primeira etapa visível).

**Detalhes de regra fixados na E2** (dentro do que D-27/D-29 permitem, ajustáveis no protótipo):
- O **1º golpe vem um intervalo depois de chegar** ao cristal (a "puxada" da picareta ocupa o intervalo; a animação vai se encaixar nisso).
- Com o mapa **cheio**, o relógio de nascimento espera no limite: **um cristal novo nasce no instante em que outro quebra**. Com os valores atuais o mapa enche em ~10 s e fica sempre com 12.
- Sem lugar livre em 30 tentativas, o nascimento é pulado.
- Ritmo medido (3 sementes, 10 min): **5,00 lascas/min**, 50 quebras, ~500 golpes; ~12 s por cristal (10 s batendo + ~2 s andando).

**Como testar a E2:**
1. No Editor, espere a recompilação (barra de progresso no canto) e confira que o Console não tem erros vermelhos.
2. `Tools → Miners Idle → Debug → Simular 10 minutos`. Na primeira vez, ele cria `Assets/_Project/Data/Balance_Prototipo.asset`. O Console mostra uma linha `[Simulação]` com lascas (~50), golpes (~500), quebras (~50), lascas/min (~5) e a semente.
3. Clique no asset `Balance_Prototipo`: os valores da tabela deste plano aparecem no Inspector. Mude, por exemplo, *Hit Interval* para 0,5 e rode o menu de novo: as lascas/min devem subir (~8–9). Volte para 1.
4. `Window → General → Test Runner → EditMode → Run All`: **32 testes verdes** (4 do M1 + 15 da E1 + 13 da E2).

**Achado da E3 — câmera:** a câmera do M1 tinha altura fixa (tamanho 15 = 30 unidades). Num celular mais alto que 9:16 (o Moto G75 é ~9:20) a largura visível cai para ~13,6 unidades e os cristais nas bordas do mapa (±7,5) sairiam da tela. O `CameraWidthFit` fixa a **largura** em 16,875 unidades (270 px ÷ 16, como o Canvas, que também casa pela largura) e deixa a sobra na altura. Provisório até o Art Spike decidir resolução e Pixel Perfect (D-14).

**Como testar a E3:**
1. Com o Editor aberto: `Tools → Miners Idle → Projeto → Aplicar configuração do projeto`. Se a cena atual tiver mudanças, ele pergunta se quer salvar. O Console mostra linhas `[Setup]`: sprites de graybox, prefab `CrystalView`, "Graybox do mapa criado na cena Game" e o asset `Balance_Prototipo`.
2. Abra `Assets/_Project/Scenes/Bootstrap.unity` e aperte **Play**, sem tocar em nada.
3. Na aba Game (de preferência numa resolução retrato, ex.: 1080×2400): chão ardósia, losangos magenta, o anão (oval âmbar) indo até um losango e parando ao lado. Uma barra verde aparece **só** no alvo e diminui a cada ~1 s; o anão dá um "soco" a cada golpe; ao zerar, o losango some e o anão segue para o próximo.
4. Deixe ~1 min: cristais novos aparecem até 12, sem sobrepor; o anão continua sozinho.
5. Para repetir uma partida: no objeto `Mining`, ligue *Use Fixed Seed*. A mesma semente repete o mesmo jogo.
6. `Window → General → Test Runner → PlayMode → Run All`: 2 testes verdes (o do M1 e `MiningSceneTests`).

*Criados:* pasta `Art/Graybox` (3 sprites), `Prefabs/Graybox/CrystalView.prefab`, objeto `Mining` (com `Map`, `Crystals`, `Miner`) e `CameraWidthFit` na câmera da cena Game. O painel âmbar do Safe Area ficou transparente (o componente continua).
