# Marco 6 — Descida em corrida

> Plano de 2026-10-22. Proposta e motivos: [Proposta - Descida em corrida](Proposta%20-%20Descida%20em%20corrida.md). Decisões em [Decisões e Riscos](Decisoes%20e%20Riscos.md) §1.2: **D-50** (câmera de frente), **D-51** (E1 adiantada, numa cena separada), **D-52…D-58** (proposta aprovada) e **D-59** (M5 aprovado; o teste de 10 min no celular vem para a E5 deste marco).
> **E1 validada. Plano das etapas E2–E5 aprovado pelo Bruno em 2026-10-22**, com as decisões **D-60** e **D-61**.

## Objetivo

Trocar o mapa aberto pela **descida em corrida**: o jogo de verdade (lascas, golpe, toque, crítico, combo, loja, save) rodando na tela nova, com o anão correndo na direção da câmera por um corredor de caverna e parando para bater nos cristais da fila.

**Para você:** abrir o jogo no celular e ver o mesmo jogo de hoje, só que descendo a caverna: tocar faz o cristal quebrar mais rápido, a lasca voa até LASCAS, a LOJA funciona e o progresso continua salvo.

A E1 (abaixo) foi o primeiro passo: a câmera e o render numa cena separada, para validar com o dev principal no aparelho antes de mexer nas regras.

## E1 — Câmera e render da descida (cena separada)

### O que entra

| Parte | Como fica |
|---|---|
| **Cena `Descida`** | Nova, gerada pelo setup do projeto. O jogo atual (cena `Game`) não muda; troca-se de cena pelo DBG. |
| **Renderer** | Nada muda: o renderer do projeto, apesar do nome `URP_2D_Renderer`, já é o **Universal** (3D) desde o M1 (achado de 2026-10-22). A câmera da descida usa o mesmo; o setup só acrescentaria um Universal se faltasse. |
| **Câmera** | De perspectiva, com os valores da D-50: altura 2,4 m, inclinação 15°, distância 4,8 m, campo de visão 40°, corredor de 3,0 m. O campo de visão vale para a tela **9:16** do simulador; num celular mais alto (o Moto G75 é ~9:20), a largura vista é a mesma e a sobra vai para cima e para baixo, como no jogo atual (`CameraWidthFit`). |
| **Render pixelado** | A câmera desenha numa textura de **270 px de largura** (D-14), ampliada sem suavizar; a altura acompanha a proporção da tela. No DBG dá para trocar para 360 px ou para a resolução do aparelho e comparar. |
| **Corredor** | Chão, paredes e teto em 3D, com texturas de pedra em pixel art geradas por código, que **rolam** conforme o anão corre (a malha não se move). Esteios e vigas de madeira a cada 4 m, lanternas e cristais incrustados nas paredes, reciclados quando saem de vista. Neblina escura no fundo. |
| **Anão e estagiário** | Sprites de frente gerados por código (o mesmo desenho do simulador): 4 quadros de corrida e 4 de golpe. O estagiário corre ao lado, com outro capacete e outra barba. |
| **Cristais** | Sprites de frente nas 5 cores do mockup; entram por baixo, o anão para, bate 3 vezes (faíscas, cristal treme) e quebra (estilhaços). |
| **HUD** | Uma maquete do HUD (LASCAS, DANO, CAVERNA, VELOCIDADE, LOJA, AD, PASSAR DE CAVERNA) só para julgar o enquadramento; não funciona. |
| **DBG da descida** | Botões − e + para altura, inclinação, distância, campo de visão, largura do corredor e velocidade; Estagiário on/off, HUD on/off, Pausar, Resolução (270/360/nativa), Padrão D-50 e Voltar ao jogo. Uma linha mostra onde o anão fica na tela (% do topo), quanto chão se vê à frente dele e o fps. |
| **DBG do jogo** | Botão novo **Descida (teste)**: salva o progresso e abre a cena `Descida`. "Voltar ao jogo" reabre o jogo pelo caminho normal (com o save). |

### O que fica de fora (e por quê)

- **Regras.** O loop da cena é **só visual**, como no simulador (D-51): não há lascas, compras nem save. A fila de verdade é a E2.
- **Toque.** Tocar no mapa ainda não faz nada nesta cena; entra com as regras (E3).
- **Arte de verdade.** Tudo é gerado por código, no espírito do graybox; a arte é do Art Spike, depois da validação.

### Valores da cena

Ficam no componente `DescentStage` da cena (Inspector) e no DBG. São de **apresentação**: não são valores de regra (CLAUDE.md), e por isso podem ser ajustados sem decisão nova. A câmera validada é a D-50.

| Valor | Padrão | Origem |
|---|---|---|
| Altura · inclinação · distância · campo de visão · largura | 2,4 m · 15° · 4,8 m · 40° · 3,0 m | D-50 |
| Velocidade de corrida na tela | 2,0 m/s | 4 un/s da regra × 0,5 m/un (D-58, proposta) |
| Espaço entre cristais | 3,6 a 5,2 m | o mesmo do simulador |
| Golpes para quebrar · ritmo | 3 golpes · 0,55 s | o mesmo do simulador |
| Resolução interna | 270 px de largura | D-14 |

### Testes

- PlayMode `DescentTests`:
  - a cena abre e a câmera usa o renderer Universal e a textura de baixa resolução;
  - com o tempo acelerado, o anão corre, para, quebra cristais e volta a correr;
  - o enquadramento da D-50 numa tela 9:16 põe a cabeça do anão a ~44% e os pés a ~78% do topo;
  - um quadro da descida não aloca memória (corrida, golpe, quebra e nascimento).
- Os testes atuais continuam verdes (EditMode e PlayMode).
- No Moto G75: 60 fps com a cena rodando.

### Como testar (Bruno)

1. **Rodar no celular** (`Tools → Miners Idle → Build → Rodar no celular (Android)`; o build aplica o setup). Linhas `[Setup]` esperadas: "Cena criada: Assets/_Project/Scenes/Descida.unity", "Painel de debug de versão anterior removido para ser refeito".
2. No jogo, abra o **DBG** e toque **Descida (teste)**.
3. Olhe a cena por uns segundos: o anão corre na sua direção, as paredes correm para o fundo, um cristal entra por baixo, ele para, bate 3 vezes e o cristal quebra.
4. Abra o **DBG** da descida e mexa nos − e + (altura, inclinação, distância, campo de visão, largura, velocidade). Tudo muda na hora. **Padrão D-50** volta aos valores validados.
5. Teste **Resolução** (270 → 360 → nativa): é a diferença entre o "pixelado" e o "liso".
6. **Estagiário**, **HUD** e **Pausar** ligam e desligam cada coisa.
7. Mostre ao dev principal. Se mudarem a câmera, me mandem os números da linha de baixo do DBG (ou um print).
8. **Voltar ao jogo** volta ao jogo normal, com o progresso.

No Editor: setup pelo menu, abrir `Assets/_Project/Scenes/Descida.unity` e dar Play. (Pela `Bootstrap`, vai pelo DBG como no celular.)

### Critério de pronto da E1

- [ ] Cena `Descida` rodando no Moto G75 a 60 fps, igual ao simulador.
- [ ] Controles do DBG mudam a câmera na hora; Padrão D-50 volta aos valores validados.
- [ ] Ida e volta entre o jogo e a descida sem perder o progresso.
- [ ] Testes verdes (os novos e os de antes).
- [ ] Você (e o dev principal) confirmam a câmera no aparelho, ou mandam os números novos.

### Esforço

10–16 h (proposta, §8).

## Decisões deste plano (aprovadas em 2026-10-22)

| # | Proposta | Por quê | Rec. |
|---|---|---|---|
| **D-60** | **Onde o cristal nasce na fila:** no fim da fila, a pelo menos **12 un (6 m) à frente do anão**, fora da tela (atrás da câmera). O valor substitui a "distância mínima do anão" do mapa (1,5 un). O espaço entre cristais continua **2 un** (`minCrystalSpacing`). Os **10 cristais iniciais** já começam na tela: o primeiro a **3 un** do anão, os outros a cada 2 un. | Com 1,5 un, o cristal "brotaria" na frente do anão, à vista. Começar com a fila cheia mostra o jogo logo no primeiro segundo, como no mockup. | 12 un · 2 un · fila inicial na tela |
| **D-61** | **A cena de teste `Descida` sai na E3**, quando a cena `Game` passar a ser o corredor. Os controles da câmera (− e +) vão para o DBG do jogo. O código do mapa aberto (mapa, cristais espalhados, linha até o alvo, "Mostrar ordem") também sai; ele fica na tag `v0.5-campo-aberto`. | Duas cenas e dois loops (o visual e o de verdade) dobrariam o trabalho a cada ajuste. | Sim |

**Efeito esperado na economia (só medir, sem mudar valor):** no mapa, o anão andava em média alguns metros até o próximo cristal; com a fila cheia, ele anda só o espaço entre cristais (2 un). Por isso as **lascas por minuto devem subir** enquanto a fila estiver cheia. O relatório de economia mede antes e depois na E5; qualquer ajuste de valor vira decisão própria.

## E2 — A fila de cristais no núcleo

O núcleo (C# puro) troca o mapa 2D por uma **trilha de uma dimensão**. Tudo o que não é mapa continua igual.

| Parte | Como fica |
|---|---|
| `CrystalTrack` (substitui `CrystalField`) | A fila: cada cristal tem uma **distância** na trilha. Nascimento por tempo (D-54) no fim da fila, na regra da D-60. Sem posição sorteada: a mesma semente repete a mesma partida. |
| `Miner` | `Distance` (unidades corridas) no lugar da posição 2D. Estados: **correndo** (com ou sem alvo, D-55) e **batendo**. |
| Alvo | Sempre o **primeiro da fila** (o `TargetSelector` e o número de lote saem). O alvo não muda até quebrar, como antes. |
| `MiningSimulation` | O laço de eventos fica (chegada, golpe, nascimento, combo). Muda só o "andar": a distância até o alvo vira uma subtração. Sem alvo, o anão continua correndo, e o relógio de eventos só espera o próximo nascimento. |
| `MiningRules` e o asset | Saem: área do mapa e posição inicial do anão. Entram: distância de nascimento (12 un) e posição do 1º cristal inicial (3 un). Fica todo o resto: vida, recompensa, dano, intervalo, corrida de 4 un/s, alcance, melhorias, crítico, toque e combo. `Balance_GDD_v1` recebe os mesmos campos novos. |
| Save | Não muda (guarda só o progresso, D-43). |
| DBG (texto) | "Lote do alvo" vira "distância descida". "Gerar cristal" põe um cristal no fim da fila. |

**Testes (EditMode):**
- os testes do mapa são **reescritos para a fila**, medindo o comportamento novo (nenhum é apagado para passar): ordem da fila, nascimento por tempo no fim da fila e a 12 un, espaço de 2 un, máximo de 12, fila inicial;
- o anão corre até o primeiro cristal, para no alcance, bate, quebra, recebe uma vez e segue para o próximo;
- **sem cristal, o anão continua correndo** e para no cristal que nascer;
- as Botas aceleram a corrida;
- determinismo: mesma semente, mesma partida; `Tick(600)` igual a 36.000 `Tick(1/60)`;
- toque, crítico, combo e compras: os testes atuais continuam passando como estão.

**O que roda no fim:** testes verdes. **Nada muda na tela ainda**: a cena `Game` continua com a vista de cima, com os cristais desenhados ao longo de uma linha, até a E3.

## E3 — O jogo no corredor

A cena `Game` passa a ser o corredor da E1, dirigido pela simulação de verdade.

- A câmera, o render de 270 px, o corredor, os esteios, as lanternas e a arte gerada por código da E1 vão para a cena `Game`. O anão e os cristais são desenhados **pela simulação** (distância do cristal − distância do anão, na escala da D-58).
- O anão corre, para no cristal, bate no ritmo da Velocidade, quebra e volta a correr; com a fila vazia, continua correndo.
- HUD, LOJA (com a mineração continuando por trás), save e toque na tela ficam como hoje.
- O estagiário **não aparece** (entra no M7).
- DBG do jogo: entram os − e + da câmera e a linha de medidas da E1; saem "Mostrar ordem" e "Descida (teste)".
- Saem a cena `Descida`, o loop visual (`DescentPreview`) e as views do mapa (D-61).

**Testes:** os PlayMode da cena (o anão corre e quebra; o toque acelera a quebra; toque na interface não conta; a loja não pausa a mineração; save e reabertura) reescritos para o corredor; os da E1 passam a olhar a cena `Game`.

**O que roda no fim:** Rodar no celular → o jogo de sempre, já na descida.

## E4 — Feedback e DBG

O feedback do M5, adaptado ao corredor 3D:

- faíscas no cristal e no ponto do dedo; o cristal treme;
- "CRÍTICO!" e o combo "xN" perto do anão, no espaço da tela;
- a lasca sai do cristal (posição 3D → tela) e voa até LASCAS;
- o tremor de tela usa a câmera da descida;
- os sons ficam como estão.

O DBG de antes continua, com os atalhos de sempre (tempo ×20, gerar cristal, quebrar alvo, compras, toques auto, crítico sempre, tremor, som, medição), e ganha "distância descida" nas estatísticas.

**Testes:** os PlayMode do feedback (`FeedbackTests`, `SoundTests`, `AllocationTests`) olhando a cena nova; nenhuma alocação por quadro na corrida, no golpe, no toque e no crítico.

## E5 — Economia e celular

- **Relatório de economia** (`Tools → Miners Idle → Debug → Relatório de economia`) antes (tag `v0.5-campo-aberto`) e depois, nos 3 perfis: lascas por minuto e tempo até cada compra. Os números vão para você comparar; nenhum valor muda sem decisão.
- **10 min no Moto G75** (o teste herdado do M5 pela D-59): tocando, comprando e deixando minerar sozinho, com a medição do DBG (fps, quadros lentos, GC).
- A pergunta do marco: **"a descida está gostosa?"**

## Como testar (Bruno) — ao fim do marco

1. **Rodar no celular**.
2. O anão corre na sua direção, as paredes passam, os cristais vêm por baixo; ele para em cada um, bate e quebra; a lasca voa até LASCAS.
3. Toque na tela: o cristal treme, sai faísca, a barra cai mais rápido; combo e crítico aparecem como no M5.
4. LOJA: compre Picareta, Velocidade e Botas; o anão bate mais forte, mais rápido e corre mais rápido. A mineração continua com a loja aberta.
5. Feche e reabra o app: as lascas e as melhorias continuam.
6. DBG: os − e + da câmera ajustam o enquadramento; Tempo ×20 e Toques auto funcionam.
7. Jogue 10 minutos e mande os números do DBG.

## Critério de pronto

- [ ] Fila de cristais no núcleo, com os testes do mapa reescritos para ela (nenhum apagado) e o determinismo mantido.
- [ ] A cena `Game` é o corredor, com o jogo inteiro funcionando (golpe, toque, crítico, combo, loja, save).
- [ ] Feedback do M5 no corredor; nenhuma alocação por quadro.
- [ ] Relatório de economia comparado (antes × depois).
- [ ] 10 min no Moto G75 a 60 fps; você aprova a sensação de descida.

## Fora deste marco

Arte de verdade (Art Spike, logo depois, já desenhando para o corredor) · estagiário (M7) · Letrado, penalidade, cavernas e a passagem com parede (M8) · boost/AD e offline (M9) · sons reais.

## Riscos

| Risco | Mitigação |
|---|---|
| A economia muda com a fila (R-01, R-23) | Só medir na E5; ajuste de valor só com decisão |
| O cristal aparece por pouco tempo (R-27) | Escala da trilha (D-58) e câmera ajustável; avaliar na E5 |
| Muitos testes de cena para reescrever | Reescrever na E3, junto com a cena; os testes de regra (economia, toque, loja, save) não mudam |
| Pixel art "dançando" no corredor (R-25) | Filtro *point* e mipmaps; avaliar no celular |

## Esforço

E1 ✅ · E2 8–12 h · E3 12–18 h · E4 6–10 h · E5 4–6 h → **30–46 h** para o que falta (~2 semanas a 20 h/semana).

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| E1 Câmera e render da descida | ✅ validada pelo Bruno no Moto G75 em 2026-10-22 | Presentation/Descent: `DescentStage` (câmera da D-50, render de 270 px com filtro *point*, corredor parado com a textura rolando no shader, esteios, lanternas e cristais de parede reciclados, partículas em pool), `DescentPreview` (loop só visual, fila circular sem alocação), `DescentArt` (anão, estagiário, cristais e texturas desenhados por código, o mesmo desenho do simulador), `DescentMeshes`, `DescentFraming` (enquadramento e as medidas da tela). Shader `MinersIdle/DescentUnlit` (corte por alfa, rolagem e neblina). UI: `DescentScreen` (imagem na tela inteira) e `DescentDebugPanel` (− e + da câmera e da velocidade, Estagiário, HUD, Pausar, Resolução, Padrão D-50, Voltar ao jogo, linha de medidas). Setup: `DescentSetup` cria a cena `Descida` e a põe no build; o DBG do jogo ganhou **Descida (teste)** (22 botões, 8 fileiras). **Achados:** (1) o renderer do projeto (`URP_2D_Renderer`) já é o Universal desde o M1, então nada mudou no URP; (2) a neblina saía fraca e cinza: `Shader.SetGlobalColor` não converte a cor para o espaço linear, e a mistura em espaço linear parece lenta ao olho; corrigido (cor convertida, mistura em sRGB, fim em 25 m), conferido em capturas renderizadas de verdade. Teste PlayMode `DescentTests` (6). Em batch: setup ok, EditMode **112/112**, PlayMode **33/33** |
| E2 A fila no núcleo | ✅ validada pelo Bruno em 2026-10-22 | Core: `CrystalTrack` (fila em ordem de distância; nascimento por tempo no fim da fila, a ≥ 12 un do anão e a 2 un do último; fila inicial de 10 a partir de 3 un), `Crystal.Distance`, `Miner.Distance` com os estados Correndo e Batendo (sem alvo, continua correndo), alvo = primeiro da fila. Saem `CrystalField`, `TargetSelector`, o lote e a posição 2D. A simulação passa a ter **um só sorteio** (o do crítico): os cristais nascem em lugares fixos. Asset: `firstCrystalDistance` 3 e `spawnDistance` 12 nos dois balanços (sem área do mapa). Vista de cima provisória: o anão parado no alto e a fila subindo por uma faixa vertical. DBG: "Descida N un" no lugar do lote. **Achado:** `CrystalSpawned?.Invoke(TrySpawn())` pulava o nascimento quando ninguém escutava o evento (só nos testes e no relatório de economia; no jogo sempre há quem escute); corrigido e anotado no código. Testes reescritos para a fila, nenhum apagado: `MiningSimulationTests` (14), `CrystalTrackTests` (8, era `TargetSelectorTests`), `DebugCommandsTests`, `StrikeTests` (o teste de nascimento passa a comparar o instante, não o lugar), `MiningSceneTests`. Primeira medida da fila: 1 h sem compras = 342 quebras (~5,7 lascas/min; o mapa dava ~5). Em batch: setup ok, EditMode **113/113**, PlayMode **33/33** |
| E3 O jogo no corredor | 🔨 em andamento (2026-10-22) | |
| E4 Feedback e DBG | ⏳ | |
| E5 Economia e celular | ⏳ | |
