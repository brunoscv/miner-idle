# Marco 5 — Toque, crítico, combo e feedback (graybox)

> **Plano aprovado pelo Bruno em 2026-10-21 (D-48). Marco aprovado em 2026-10-22 com a E5 reduzida (D-59).**

> Plano de 2026-10-21. Roadmap: [M5](Roadmap.md). Decisões: **D-46** (M5 em graybox, arte depois do Art Spike) e **D-47** (regras do toque) — ver [Decisões e Riscos](Decisoes%20e%20Riscos.md). Requisitos: REQ-MIN-02 (toque), REQ-MIN-03 (crítico), REQ-MIN-04 (combo), REQ-CRY-04 (quebra e lasca voando), REQ-UI-05 (feedback por cor, forma e movimento), REQ-PRO-02 (teste estatístico do crítico).
> **Antes deste marco:** o M4 está com as 4 etapas validadas no celular; faltam rodar no Unity os testes da loja e do save e a sua aprovação. A E0 abaixo cobre os testes.

## Objetivo

**Fazer bater gostoso** (pilar "Toque com impacto"). O jogador toca na tela e sente: o cristal treme, sai faísca, às vezes "CRÍTICO!", o combo "x12" pulsa, a lasca voa até o HUD quando o cristal quebra, e cada coisa tem um som. Tudo ainda em formas simples; a arte da caverna 1 entra depois do Art Spike (D-46).

**Para você:** abrir o jogo no celular e ficar tocando na tela com o polegar: o anão quebra mais rápido, o combo sobe, aparece crítico, e dá vontade de continuar tocando.

## Decisões usadas

| # | O que fixa |
|---|---|
| D-47 | Toque em **qualquer ponto da tela fora de menus e botões**; **pode dar crítico**; **sem limite** de toques por segundo; dano = 10% de um golpe do principal (com a Picareta Dupla) |
| D-46 | Graybox: formas e cores atuais; arte depois do Art Spike |
| DEC-03 / REQ-MIN-03 | Crítico: **5%** de chance por golpe, dano **×2** (valores no asset) |
| REQ-MIN-04 | Combo: cada toque soma 1; **1 s** sem tocar zera; a cada múltiplo de 10, um efeito especial |
| D-48 (confirmadas pelo Bruno) | (a) o toque conta **mesmo com o anão andando** (vai para o alvo atual dele); (b) o combo é **só visual**, não muda dano (como no GDD); (c) sons **provisórios gerados no código** (bipes), sem licença de terceiros, trocados pelos reais junto com a arte |

## Regras (núcleo)

- **Toque** (`Tap()`): um comando da simulação, como a compra. Dano = 10% do dano do golpe atual (Picareta × Dupla), no **alvo atual** do anão. Sem alvo (mapa vazio), o toque não causa dano, mas ainda conta no combo. Toque que quebra o cristal paga a recompensa normalmente (uma vez só).
- **Crítico:** cada golpe automático e cada toque sorteia crítico (5%); no crítico, o dano dobra. O sorteio usa um **gerador próprio** (semente derivada da partida): assim os nascimentos de cristal continuam iguais com ou sem toques, e os testes de determinismo seguem valendo.
- **Combo:** contador no núcleo, com o tempo do jogo: cada toque soma 1; se passar 1 s sem toque, volta a 0. Eventos `ComboChanged` e `ComboMilestone` (10, 20, 30…).
- **Eventos:** `HitLanded` passa a dizer se foi **toque ou automático** e se foi **crítico**, para o feedback escolher a reação.
- **Valores no asset** (`Balance_Prototipo` e `Balance_GDD_v1`): chance de crítico (0,05), multiplicador (2), fração do toque (0,1), janela do combo (1 s). São valores do GDD; mudar exige decisão.

## Feedback (apresentação)

| Evento | Reação (REQ-UI-05: cor, forma, movimento) |
|---|---|
| Golpe automático | o cristal treme rápido; faíscas pequenas |
| Toque | faísca no ponto tocado e no cristal; tremor menor |
| Crítico | "CRÍTICO!" amarelo grande, estoura e sobe em posição aleatória perto do anão; tremor maior |
| Combo `xN` | número com contorno que pulsa perto do anão e some 1 s depois do último toque |
| Combo ×10, ×20… | "pop" maior em âmbar e um tremor curto do HUD |
| Quebra | tela treme de leve; uma lasca sai do cristal e voa **devagar** até o LASCAS do HUD, que pulsa ao chegar |

- Números e textos flutuantes vêm de **pool** (Arquitetura, D-T13): nada de criar e destruir objeto a cada toque.
- O tremor diminui quando os toques ficam muito rápidos, para a tela não virar uma bagunça.
- **Toques em UI não contam:** HUD, LOJA, painel da loja e DBG ficam fora. O toque é lido pelo **Input System** (D-T12) e passa pelo mesmo comando `Tap()` que os testes e o debug usam.

## Etapas

| # | Etapa | Entrega | O que roda no fim |
|---|---|---|---|
| **E0** | **Testes pendentes do M4** | Rodar EditMode e PlayMode no Unity (Editor fechado) | Tudo verde (ou corrigido); aí você aprova o M4 |
| **E1** | **Regras no núcleo** | `Tap()`, crítico com gerador próprio, combo, eventos novos, valores no asset | Testes: dano do toque (com e sem Dupla), toque andando e batendo, toque sem alvo, quebra por toque paga uma vez, **100.000 golpes ≈ 5% de crítico** (REQ-PRO-02), combo sobe/zera em 1 s/marcos de 10, determinismo com toques |
| **E2** | **Toque na tela** | Leitura do toque pelo Input System (dedo e mouse), ignorando UI; DBG: **Toques automáticos** (5/s, para testar combo sem cansar o dedo) e **Crítico sempre** | Play: tocar acelera a quebra; tocar no HUD/LOJA/DBG não conta. Teste PlayMode |
| **E3** | **Feedback visual** | Tremor do cristal e da tela, faíscas, "CRÍTICO!", "xN", marcos do combo, lasca voando ao HUD (com pool); DBG: **Tremor on/off** | Play: ver cada reação da tabela |
| **E4** | **Som provisório** | Bipes sintetizados (golpe, toque, crítico, quebra, compra, combo ×10) num pool de `AudioSource`; DBG: **Som on/off** | Play com som |
| **E5** | **Desempenho e celular** | Profiler (nenhuma alocação por quadro no loop, pendência do M2 pela D-32); Rodar no celular | 10 min tocando no Moto G75 a 60 fps; você (e, se quiser, 2–3 pessoas sem explicação) responde: **"bater está gostoso?"** |

## Como testar (Bruno) — ao fim do marco

1. **Rodar no celular**.
2. Toque em qualquer lugar do mapa: o cristal alvo treme, sai faísca, a barra cai mais rápido. Tocar no HUD, na LOJA ou no DBG não conta.
3. Toque várias vezes seguidas: aparece "x2, x3…" pulsando; pare 1 s e ele some. No x10, um "pop" maior.
4. De vez em quando aparece **"CRÍTICO!"** (5%). DBG → **Crítico sempre** para ver toda hora.
5. Quando o cristal quebra, a tela treme de leve e uma lasca voa até LASCAS, que pulsa.
6. Sons em cada golpe, toque, crítico, quebra e compra. DBG → **Som off** e **Tremor off** desligam.
7. DBG → **Toques automáticos**: o combo sobe sozinho (para testar sem cansar o dedo).
8. Test Runner: tudo verde.

## Critério de pronto

- [ ] E0: testes do M4 rodados; M4 aprovado.
- [ ] Toque, crítico e combo nas regras, com o teste estatístico do crítico (≈ 5% em 100.000 golpes) e determinismo.
- [ ] Toque em UI nunca vira toque no mapa; mineração continua igual sem tocar.
- [ ] Cada reação da tabela de feedback aparece; pool sem alocação por quadro (Profiler).
- [ ] Atalhos do DBG: Toques automáticos, Crítico sempre, Tremor on/off, Som on/off.
- [ ] Rodou no Moto G75 a 60 fps; você aprova o *game feel*.

## Fora deste marco

Arte final da caverna 1 (depois do Art Spike, D-46) · sons reais e música · Configurações de verdade (volume, tremor, vibração: M8) · vibração do celular (M8, junto das Configurações) · estagiário (M6) · Letrado, penalidade e cavernas (M7) · boost/AD (M8) · dicas de primeiro uso (S10, depois).

## Art Spike (M1b) em paralelo

Não bloqueia este marco. É com você, no seu ritmo: paleta-mestre, o anão (PixelLab) em 1 pose + 1 golpe, o cristal da caverna 1, um painel e um botão de UI, fonte com acentos, e o **teste de escala no celular** (270×480 × 360×640). Quando você quiser começar, eu escrevo o plano do M1b com o passo a passo e o que levar para o jogo.

## Riscos

| Risco | Mitigação |
|---|---|
| Toque domina e o idle perde sentido (R-10) | O toque é 10% do golpe; o relatório de economia ganha um perfil "jogador que toca 3/s" para medir |
| Feedback demais vira bagunça na tela | Tremor que diminui com toques rápidos; textos com tempo de vida curto; DBG com Tremor off |
| Alocação por toque (GC) no celular | Pool desde o primeiro texto flutuante; Profiler na E5 |
| Toque no menu vazando para o mapa | Checagem de UI antes de virar `Tap()`; teste PlayMode |

## Esforço

**35–50 h** de desenvolvimento focado (sem a arte, que saiu deste marco).

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| E0 Testes pendentes do M4 | ✅ 2026-10-21 | Em batch: EditMode **112/112**, PlayMode **15/15** (loja, save, toque e todos os anteriores). Falta só a sua aprovação do M4 |
| E1 Regras no núcleo | ✅ validada pelo Bruno em 2026-10-21 (testes verdes no Editor) | Core: `StrikeRules` (crítico 5% ×2, toque 10%, combo 1 s; valores no asset, seção "Golpe e toque"), `MiningHit` (o evento `HitLanded` diz se foi toque e se foi crítico), `MiningSimulation.Tap()`, crítico no golpe automático e no toque com **gerador próprio** (o `MiningRunner` deriva a semente da partida), combo com `ComboChanged`/`ComboMilestone` e reset por tempo de jogo, `ForceCritical` para o DBG. **Achado:** 0,1 × 10 em `double` dá 0,9999999999999999; um cristal de vida 1 nunca quebraria só com toques. O `Crystal` passou a tratar uma vida desprezível (≤ 10⁻⁹ da máxima) como zero. Testes: `StrikeTests` (11), incluindo **100.000 golpes ≈ 5% de crítico** (REQ-PRO-02). Fora do Unity: 93/93 do núcleo; tudo compila. **No Unity ainda não rodou** (Editor aberto). Nada visível no jogo ainda: o toque na tela é a E2 |
| E2 Toque na tela | ✅ validada em 2026-10-21. No Moto G75, 300 toques simulados pelo `adb` (~8 s de jogo) quebraram 6 cristais sem nenhum golpe do anão; o Bruno não percebia o toque porque ainda não há reação visual (é a E3). `TapInputTests` verdes no Unity | Presentation: `TapInput` (Input System: cada dedo que encosta é um toque; no Editor, clique do mouse; filtra a interface com um raycast de UI reaproveitando lista e evento, sem alocar; evento `Tapped(posição)` para o feedback da E3). `MiningRunner.ForceCritical` (vale depois de reiniciar). Setup: leitor de toque no objeto `Mining`; painéis do HUD passam a bloquear o toque (antes não recebiam raycast); DBG refeito com **Toques auto** (5/s no centro da tela) e **Crítico sempre** (18 botões). Teste PlayMode `TapInputTests` (mapa conta; LOJA, HUD e DBG não; toques automáticos; crítico sempre após reiniciar). Compila fora do Unity |
| E3 Feedback visual | ✅ validada pelo Bruno no Moto G75 em 2026-10-21. Ajuste pedido por ele: tremor de câmera mais forte no crítico (0,4 por 0,3 s), na quebra (0,3 por 0,25 s) e no marco do combo (0,25 por 0,2 s); amortecimento por toques rápidos só até 60%. Valores no Inspector (`Mining/Effects` → Mining Feedback View → "Tremor de tela") | Presentation/Effects: `ScreenShake` (câmera; desligável), `SparkBurst` (faíscas em pool), `FloatingTexts` ("CRÍTICO!" e marcos do combo, TMP de mundo em pool), `ShardFlight` (lasca voando devagar até LASCAS; avisa a chegada), `MiningFeedbackView` (liga cada evento à reação; tremores diminuem com toques muito rápidos). `CrystalView.Shake`. HUD: LASCAS pulsa na chegada da lasca; HUD treme no marco do combo. Textos em dados: `Data/FeedbackTexts` ("CRÍTICO!", "x{0}", "x{0}!"). DBG refeito com **Tremor on/off** (19 botões, 7 fileiras). Teste PlayMode `FeedbackTests` (5). Em batch: setup ok, EditMode **112/112**, PlayMode **20/20** |
| E4 Som provisório | ✅ validada pelo Bruno no Moto G75 em 2026-10-21 ("por enquanto tá muito bom"; os sons de verdade vêm depois) | Presentation/Audio: `SfxSynth` (6 bipes sintetizados por código, ondas quadrada/triangular e ruído com envelope; gerados uma vez ao abrir a cena, determinísticos) e `SoundBoard` (escuta golpe, toque, crítico, quebra, compra e marco do combo; pool fixo de 8 `AudioSource` em rodízio; o mesmo som não repete em menos de 0,03 s, para o tempo ×20 e os toques auto não virarem parede de som; altura varia ±6% no golpe e no toque). Ajustes no Inspector (`Mining/Effects/Sound`). Setup: objeto `Effects/Sound`; áudio com **DSP Buffer = Best latency** (bipe junto com o dedo no Android); DBG refeito com **Som on/off** (20 botões, 7 fileiras). Teste PlayMode `SoundTests` (4). Em batch: setup ok, EditMode **112/112**, PlayMode **24/24** |
| E5 Desempenho e celular | ✅ reduzida (D-59, 2026-10-22): parte automática pronta e commitada; o teste de 10 min no Moto G75 passa para a E5 do M6 | **Medição (Profiler, marcador GC.Alloc):** com a cena inteira escutando, golpe, toque, crítico e toque na tela (com a checagem da interface) alocam **0**; 20 s de jogo com ~17 golpes alocam só nas quebras (texto de LASCAS) e nos nascimentos (o objeto `Crystal`), poucos bytes a cada alguns segundos. Combo e "x10!" alocam só no Editor (o `SetText` do TextMeshPro copia o texto para o Inspector dentro de `#if UNITY_EDITOR`); no aparelho, zero. **Corrigido:** o "x10!" montava string com `string.Format` → `FloatingTexts.ShowNumber` (SetText com número); o rótulo de diagnóstico do DBG montava string a cada 0,5 s → cabeçalho fixo + SetText. **Achado no teste:** uma sonda que simula segundos de jogo sem deixar quadros passarem esgota o pool de faíscas (que voltam em 0,35 s reais) e mede o teste, não o jogo; `AllocationTests` deixa tempo real entre os blocos e aquece os pools (inclusive o de cristais na lotação máxima). **Teste instável corrigido:** `ComboMilestone_PopsABigText` olhava só o último texto e falhava quando o 10º toque saía crítico (5%); agora procura o "x10!" entre os textos na tela. **No aparelho:** `FrameStats` no DBG (fps, média, quadros lentos > 25 ms, pior quadro, quadros com GC), medindo só com o DBG fechado; botão **Zerar medição** (21 botões, 7 fileiras). Teste PlayMode `AllocationTests` (3). Em batch: EditMode **112/112**, PlayMode **27/27** (duas vezes) |

**Como testar a E1:** Test Runner → EditMode → Run All: os 11 de `StrikeTests` verdes, junto com os anteriores. Os principais: `Tap_TakesTenPercentOfAHit_…` (o toque tira 0,1 de um golpe de 1), `Tap_WorksWhileTheMinerIsStillWalking`, `TenTaps_BreakACrystalOfOneHealth_AndPayOnce`, `HundredThousandHits_AboutFivePercentCritical`, `Combo_GrowsWithTaps_AndResetsAfterOneSecondWithout` e `TapsAndCriticals_DoNotChangeWhereCrystalsAreBorn`.

**Como testar a E2:**
1. **Rodar no celular** (o build aplica o setup) ou, no Editor, setup pelo menu e Play na `Bootstrap`. Linhas `[Setup]` esperadas: leitor de toque, "HUD passa a bloquear o toque", painel de debug refeito.
2. Toque várias vezes no mapa: a barra do alvo cai mais rápido (cada toque tira 10% de um golpe). Ainda **sem reação visual própria do toque** (é a E3); dá para ver pela barra e pelo DBG.
3. Toque no HUD, no botão LOJA e no botão DBG: nada acontece no mapa (só o botão faz o que faz).
4. DBG → **Toques auto**: o anão quebra mais rápido sozinho; toque de novo para desligar. DBG → **Crítico sempre**: o anão quebra em metade dos golpes; vale mesmo depois de **Reiniciar**.
5. Test Runner → PlayMode: `TapInputTests` verdes.

**Como testar a E3:**
1. **Rodar no celular** (o build aplica o setup).
2. Toque no mapa: o cristal alvo treme, saem faíscas brancas no cristal e no ponto do dedo.
3. Toque várias vezes seguidas: aparece **"x2", "x3"…** sobre o anão, pulsando a cada toque; pare 1 s e ele some. No **x10**, um **"x10!"** grande em âmbar sobe e o HUD treme.
4. De vez em quando (5%) aparece **"CRÍTICO!"** amarelo perto do anão, com faíscas amarelas e tremor de tela. DBG → **Crítico sempre** para ver toda hora.
5. Golpe automático do anão: o cristal treme e solta faíscas laranja (mesmo sem tocar).
6. Quando o cristal quebra: a tela treme de leve e uma **lasca roxa sobe em arco até LASCAS**, que dá um pulso ao chegar.
7. DBG → **Tremor on/off** desliga os tremores de tela e do HUD.
8. Diga se está "bater gostoso" ou o que falta (intensidade, tamanho, duração, cores — tudo é graybox e fácil de ajustar).

**Como testar a E4** (com som ligado no celular):
1. **Rodar no celular** (o build aplica o setup). Linhas `[Setup]` esperadas: "Áudio: DSP Buffer Size = Best latency", "Som provisório criado", painel de debug refeito.
2. Sem tocar: cada golpe do anão faz um **"tum"** grave; quando o cristal quebra, um **estouro** que desce.
3. Toque no mapa: um **"tic"** agudo a cada toque, saindo junto com o dedo.
4. DBG → **Crítico sempre**: cada golpe ganha **duas notas subindo**.
5. Toque 10 vezes seguidas: no **x10**, um **arpejo** subindo.
6. LOJA → compre algo: som de **moeda**.
7. DBG → **Toques auto** e **Tempo ×20**: os sons não podem embolar num chiado contínuo.
8. DBG → **Som on/off** silencia tudo; de novo, volta.
9. Diga o que incomoda (volume, algum som chato de ouvir 100 vezes). São provisórios; o ajuste fino é na arte sonora de verdade.

**Como testar a E5** (Moto G75, ~12 min):
1. **Rodar no celular** (o build aplica o setup; linha `[Setup]` esperada: painel de debug refeito).
2. Espere a cena carregar uns 5 s, abra o DBG, toque **Zerar medição** e feche o DBG (a medição só conta com ele fechado).
3. Jogue **10 minutos** como jogador: toque bastante (rajadas e pausas), compre na LOJA, deixe o anão minerar sozinho um pouco. Pode ligar **Toques auto** e **Tempo ×20** no meio para estressar, mas feche o DBG de novo.
4. Abra o DBG e leia o canto direito: `fps (média)`, `min · lentos`, `pior quadro` e `GC … quadros · … B`. Me mande os números (ou um print).
5. O esperado: média perto de 60, poucos quadros lentos (os primeiros segundos e a abertura da LOJA podem dar algum), **GC só em poucos quadros** (quebras e nascimentos de cristal) e nenhum travamento perceptível.
6. A pergunta do marco: **"bater está gostoso?"** Se quiser, peça a 2–3 pessoas para jogar sem explicação e conte o que fizeram e disseram.

