# Proposta — Descida em corrida (nova visão do jogo)

> **Proposta de 2026-10-22.** **Aprovada pelo Bruno em 2026-10-22**, com todas as recomendações: câmera **D-50**, E1 adiantada **D-51** (validada no Moto G75) e **D-52…D-58**, registradas em [Decisões e Riscos](Decisoes%20e%20Riscos.md) §1.2, com os riscos R-25…R-29. Execução: [plano do Marco 6](Marco%206%20-%20Descida%20em%20corrida.md).
> Antes da mudança: o Bruno fez uma cópia de segurança do projeto. Recomendação extra: uma **tag no git** no último commit do mapa aberto (ex.: `v0.5-campo-aberto`), para recuperar o código antigo de dentro do próprio repositório.
> **Revisão de 2026-10-22 (tarde):** a primeira versão propunha uma câmera alta, quase de cima, como o mockup. O Bruno pediu a câmera **de frente**, com pouca inclinação. Isso põe **perspectiva de verdade** na tela e muda a recomendação de projeção de 2D para **3D com cara de pixel art** (§3).

## 1. A ideia em uma frase

A câmera fica **à frente do anão, um pouco acima da cabeça, olhando para ele de frente** (pouca inclinação). O anão corre **na direção da câmera** por um corredor de caverna. As paredes passam sem parar, dando a sensação de descida. Quando um cristal chega até ele, o anão para, bate até quebrar e volta a correr para o próximo.

É o **Temple Run ao contrário**: lá a câmera filma as costas do corredor; aqui filma o rosto. É como filmar alguém numa esteira, de frente e de um pouco acima.

## 2. A câmera validada

Valores escolhidos pelo Bruno no simulador em 2026-10-22, ajustados a partir do preset "De frente":

| Parâmetro | Valor | Para comparar |
|---|---|---|
| Altura da câmera | **2,4 m** | o anão tem ~1,3 m: a câmera fica ~1 m acima da cabeça |
| Inclinação para baixo | **15°** | a proposta anterior, "de cima", tinha ~58° |
| Distância até o anão | **4,8 m** | |
| Campo de visão (vertical) | **40°** | |
| Largura do corredor | **3,0 m** | cabem o anão e o estagiário lado a lado |

O que isso dá na tela (retrato, 9:16):

| Elemento | Onde fica | Por quê |
|---|---|---|
| Fundo do corredor (ponto de fuga) | ~**13% do topo**, logo abaixo do HUD | O corredor some no escuro atrás do anão: é o "caminho que ficou para trás" |
| Anão | **fixo**, cabeça a ~44% e pés a ~78% do topo; ocupa ~**34% da altura** da tela | Grande e de corpo inteiro, o protagonista da tela |
| Paredes | saem das **bordas** e correm para o **fundo**, atrás do anão | É a sensação de descida vista de frente: como olhar pelo vidro de trás de um carro andando |
| Cristais | **entram por baixo** (estão entre a câmera e o anão), grandes, e **diminuem** até chegar nele | Cada cristal fica parado no chão; quem se mexe é o anão |
| Cristal sendo minerado | de ~69% a ~90% do topo, à frente das pernas do anão | Tapa só as botas e as pernas; o corpo e a picareta ficam à vista |
| Chão visível à frente do anão | ~**1,4 m** até os pés; a ponta do cristal aparece ~**2,4 m** antes | É o tempo que o jogador vê o cristal chegando (§5, D-58; R-27) |
| HUD, LOJA, AD, PASSAR DE CAVERNA | como hoje | Nada muda; o cristal minerado fica logo acima do botão PASSAR DE CAVERNA (R-29) |

**Comparado ao preset inicial (2,4 m · 16° · 6 m · 50°):** a câmera mais perto e o campo de visão mais fechado fazem o anão quase dobrar de tamanho na tela (de 22% para 34% da altura). O preço é ver menos chão à frente: o cristal aparece mais tarde e mais perto da borda de baixo.

**O anão corre parado; o mundo é que se move.** O Bruno aprovou essa parte como está ("deixa por enquanto como está").

## 3. Projeção: o que a câmera de frente exige

Com a câmera de frente e baixa, a tela tem **ponto de fuga**: tudo diminui com a distância. Na vista alta da proposta anterior (e do mockup), isso não acontecia, e o 2D comum resolvia.

| | **A. 3D simples com cara de pixel art** [REC] | B. 2D com perspectiva falsa | C. 3D completo |
|---|---|---|---|
| Como é | O corredor é 3D (blocos e planos com **texturas de pixel art**), a câmera é de perspectiva com os valores do §2, e tudo é renderizado em **baixa resolução** (ex.: 270×480) e ampliado sem suavizar, o que mantém o pixel nítido. Anão, estagiário e cristais continuam **sprites 2D de frente** em pé no mundo 3D, **exatamente como no simulador**. | Sprites ampliados e reduzidos a cada quadro, em camadas | Anão e cristais também como modelos 3D |
| Pixel art | Fica nítido: a tela inteira é pixelada por igual | Borra ou "dança" ao mudar de escala | Depende do modelo |
| Arte | Texturas que se repetem (parede, chão, teto) + props + sprites de frente (PixelLab continua servindo) | Muitas camadas e quadros para fingir profundidade | Modelagem e animação 3D (fora do alcance do projeto) |
| Custo | Médio: troca o renderer e a montagem da cena | Alto e frágil | Muito alto |

**Por que A:** é o único caminho em que a câmera validada e o pixel art convivem sem truques. Como a câmera vê o anão sempre de frente, um sprite 2D basta, e não é preciso modelo 3D de personagem.

**O que muda na técnica:**
- O URP continua, e o renderer também: o do projeto, apesar do nome `URP_2D_Renderer`, já é o **Universal** (3D) desde o M1 (achado na E1, 2026-10-22).
- Entra a **neblina** (fog) do próprio Unity para o fundo escuro.
- As lanternas podem ser luz pintada na textura mais um brilho em sprite. Não é preciso luz dinâmica.
- A D-14 (270×480, PPU 16) muda de sentido: **270×480 passa a ser a resolução interna da renderização**. O PPU vale para os sprites, e para as texturas vale a densidade de pixels por metro. Isso se valida no Art Spike.
- O estilo do mockup (D-05) continua: paleta, mina de madeira, fantasia e cristais coloridos. O que muda é o ângulo.

## 4. Regras: o que muda e o que não muda

**A câmera é só apresentação.** As regras são as mesmas da primeira versão desta proposta.

| Regra hoje | Na descida | Muda valor? |
|---|---|---|
| Mapa aberto 2D, cristais em lugares aleatórios (D-26, REQ-MAP-02) | **Trilha de uma dimensão**: cada cristal tem uma **distância** à frente; o anão tem a **distância descida** | Saem: área do mapa, distância mínima do anão. Fica: **espaço mínimo entre cristais na fila** (`MinCrystalSpacing` = 2) |
| Nasce 1 cristal a cada 5 s até 12 | **Igual** [REC D-54]: o cristal entra no fim da fila, fora da tela (atrás da câmera) | Não |
| Alvo "lote, depois mais próximo" (D-27) | **O próximo da fila.** Os lotes deixam de existir. | — (a regra some) |
| Caminhada em linha reta (D-31), 4 un/s | **Corrida na direção da câmera**, na mesma velocidade de regra | Não (ver D-58 para a escala na tela) |
| Botas = velocidade de caminhada | Botas = **velocidade de corrida** | Não |
| Sem cristal, o anão fica parado | **Continua correndo** [REC D-55] | Não (é só visual) |
| Golpe, crítico, toque, combo (M5) | **Igual.** O toque vale no alvo atual, mesmo com ele correndo (D-48a) | Não |
| Loja, melhorias, save | **Igual** | Não |

Depois de implementada, a economia é medida de novo no relatório (`Tools → Miners Idle → Debug → Relatório de economia`).

## 5. Decisões propostas

| # | Proposta | Rec. |
|---|---|---|
| **D-52** | **Nova visão: descida em corrida.** O corredor visto de frente substitui o mapa aberto da D-26. A D-27 (lote, depois mais próximo) e a D-31 (linha reta) ficam superadas; a paleta e o tema do mockup (D-05) continuam. | Aprovar |
| **D-50** | **Câmera de frente** com os valores do §2 (altura 2,4 m, inclinação 15°, distância 4,8 m, campo de visão 40°, corredor de 3,0 m), **escolhidos pelo Bruno em 2026-10-22**; ajuste fino permitido na E1, no celular | ✅ Decidida (registro) |
| **D-53** | **Projeção 3D simples com cara de pixel art** (§3, opção A): corredor 3D com texturas de pixel art, renderizado em baixa resolução; personagens e cristais como sprites 2D de frente. URP com o renderer Universal (o que o projeto já usa). | A |
| **D-54** | **Os cristais continuam nascendo por tempo** (mesmos 5 s e máximo de 12) e entram no fim da fila, atrás da câmera | Por tempo |
| **D-55** | **Sem cristal, o anão continua correndo** (a descida nunca para; só para no golpe) | Sim |
| **D-56** | **Ordem do roadmap:** um marco novo ("Descida em corrida") logo depois da aprovação do M5, **antes do Art Spike e do Estagiário**. Os marcos seguintes ganham +1 no número (como na D-28). | Sim |
| **D-57** | **Estagiário:** com a câmera de frente, o "2º cristal da fila" da primeira versão fica **entre a câmera e o anão** e o estagiário taparia o principal. Recomendação nova: ele **corre ao lado** do anão, um pouco atrás, como no simulador. Fica para o marco dele decidir se bate **no mesmo cristal** (muda o REQ-INT-03) ou **num cristal ao lado** (cristais em pares). | Ao lado; regra no marco dele |
| **D-58** | **Escala da trilha na tela** (só apresentação, não muda regra): quantos metros do corredor valem 1 unidade da regra. Com a câmera do §2, a ponta do cristal aparece ~2,4 m antes do anão. Com 1 un = 1 m e corrida de 4 un/s, o anão chega nele ~0,35 s depois de ele aparecer; com **1 un = 0,5 m** (2 m/s na tela), ~0,95 s; o simulador usou 2,2 m/s (~0,7 s). Proposta: **1 un = 0,5 m** como ponto de partida, ajustado na E1 pelo *feel* | Ajustar na E1 |

## 6. O que se reaproveita do código (M1–M5)

| Área | Arquivos | O que acontece |
|---|---|---|
| Economia, compras, preview da loja | `Core/Economy/*` | **Mantém** |
| Golpe, crítico, toque, combo | `StrikeRules`, `Tap()`, eventos | **Mantém** |
| Estado e save | `GameState`, `Persistence/*` | **Mantém** (o save guarda só o progresso; a fila nasce de novo) |
| HUD, loja, DBG, safe area, número pt-BR | `UI/*`, `NumberFormatter` | **Mantém**; o DBG perde "Mostrar ordem". A UI (Canvas em Screen Space) não depende do renderer. |
| Som | `Audio/*` | **Mantém** |
| Efeitos | `Presentation/Effects/*` | **Ajusta**: faíscas e lasca nascem na posição 3D do cristal; o voo da lasca até LASCAS converte posição 3D → tela; o tremor de tela continua na câmera |
| Leitura do toque | `TapInput` | **Mantém** (o toque vale na tela toda, não depende de onde o cristal está) |
| Simulação | `MiningSimulation` | **Ajusta**: andar, escolher alvo e nascer passam a ser na trilha; o resto (laço de eventos, golpe, compra, toque, combo) fica |
| Campo de cristais e alvo | `CrystalField`, `TargetSelector`, `Crystal.Batch` | **Reescreve** como fila (`CrystalTrack`); fica mais simples |
| Regras do mapa | `MiningRules`, `MiningRulesAsset` | **Ajusta**: saem a área do mapa e a distância mínima do anão |
| Desenho do mapa | `MapView`, `CrystalFieldView`, `MinerView`, `TargetLineView`, `CameraWidthFit` | **Reescreve**: `CorridorView` (segmentos 3D do corredor reciclados, sem alocar), sprites de frente para anão e cristais, câmera de perspectiva com render em baixa resolução |
| Montagem da cena e do projeto | `GrayboxSetup`, `ProjectSetup` | **Ajusta**: renderer Universal, câmera, neblina, render em baixa resolução |
| Testes | `TargetSelector`, nascimento no mapa, caminhada | **Reescritos para medir o comportamento novo** (regra do CLAUDE.md: nenhum é apagado para passar). Os de economia, toque, loja, save e som continuam; os de feedback ganham posições 3D. |

Ordem de grandeza: de ~5.600 linhas, umas **1.000–1.300** mudam (um pouco mais que na versão de câmera alta, por causa do 3D). **A câmera nova não toca no núcleo**: a regra continua sem saber que existe uma tela (Arquitetura D-T1).

**Detalhe técnico:** a câmera fica parada e o corredor é montado em relação ao anão (posição = distância do objeto − distância do anão), com os segmentos reciclados quando saem de vista. A "distância descida" pode crescer por horas sem perder precisão de `float`.

## 7. Onde entra no roadmap

```
M5  Toque e feedback ............ falta a E5 no Moto G75 + aprovação
M6  Descida em corrida (graybox)  ← NOVO (este documento)
M1b Art Spike ................... depois do M6, já desenhando para o corredor 3D
M7  Estagiário .................. (era M6)
M8  Letrado, penalidade, 10 cavernas (era M7)
M9  AD, save completo, offline .. (era M8)
M10 Arte das cavernas 2–10 ...... (era M9)
M11 Balanceamento e playtest .... (era M10)
M12 Regressão e RC .............. (era M11)
M13 Publicação .................. (era M12)
M14 Pós-lançamento .............. (era M13)
```

**Por que antes do Art Spike:** agora é obrigatório. A arte deixou de ser "sprites de cima" e passou a ser texturas de corredor e sprites de frente. Desenhar antes de a câmera e o render estarem rodando no celular seria desenhar às cegas.

**Por que antes do Estagiário:** é o sistema que mais depende do formato do mapa (D-57).

## 8. Plano do marco novo (M6 — Descida em corrida, graybox)

> Rascunho. Vira `Marco 6 - Descida em corrida.md` depois da aprovação, como os outros.

| # | Etapa | O que roda no fim |
|---|---|---|
| **E0** | Fechar o M5: teste de 10 min da E5, aprovação, commit e tag `v0.5-campo-aberto` | Ponto seguro no git |
| **E1** | **Câmera e render no Unity** (só apresentação): renderer Universal, corredor 3D em blocos cinza com segmentos reciclados, câmera com os valores do §2, render em baixa resolução, neblina, anão e cristais como sprites cinza. DBG com os mesmos controles do simulador (altura, inclinação, distância, campo de visão, largura) e a escala da D-58 | No Moto G75: a mesma cena do simulador, a 60 fps; você confirma câmera e escala no aparelho (fecha D-50/D-58) |
| **E2** | **Núcleo: a fila** — `CrystalTrack`, distância do anão, próximo da fila como alvo, corrida contínua, nascimento por tempo no fim da fila | Testes EditMode: ordem da fila, nada nasce à vista, quebra paga uma vez, determinismo, toque correndo, simulação de 10 min |
| **E3** | **Jogo no corredor** — a simulação de verdade dirigindo a cena da E1: corre, para no golpe, quebra, volta a correr; estagiário fica de fora (M7) | Play no Editor e no celular: o loop inteiro em graybox |
| **E4** | **Feedback e DBG** — faíscas, lasca voando até LASCAS (3D → tela) e tremores; DBG: "Gerar cristal" entra na fila, sai "Mostrar ordem", entra "Distância descida" | Cada reação do M5 aparece igual |
| **E5** | **Economia e celular** — relatório de economia antes × depois, 10 min no Moto G75, Profiler | Números para você comparar e a pergunta: **"a descida está gostosa?"** |

**Como testar (ao fim):** Rodar no celular → o anão corre na sua direção, as paredes correm para o fundo, um cristal entra por baixo e vai até ele, ele para e bate (toque, crítico e combo como hoje), o cristal quebra, a lasca voa até LASCAS e ele volta a correr. LOJA, DBG e save como hoje. Test Runner verde.

**Critério de pronto:** loop completo no corredor 3D em graybox · câmera e escala confirmadas no aparelho · nenhum teste apagado, os do mapa reescritos para a fila · sem alocação por quadro na rolagem · 60 fps no Moto G75 · relatório de economia comparado · Bruno aprova a sensação de descida.

**Esforço:** **40–60 h** (E1 10–16 · E2 8–12 · E3 10–16 · E4 6–8 · E5 4–6), ou seja, ~2–3 semanas a 20 h/semana. A E1 cresceu em relação à primeira versão por causa da troca de renderer e do 3D.

## 9. O que muda no Art Spike (M1b)

| Antes (mapa visto de cima) | Agora (corredor 3D, câmera de frente) |
|---|---|
| Anão em 1 pose + 1 golpe | Anão **de frente**: **ciclo de corrida** (4–6 quadros) + **golpe para a frente** (na direção da câmera) |
| Cristal da caverna 1 | Cristal **de frente**, baixo o bastante para não tapar o anão no golpe |
| Fundo do mapa | **Texturas que se repetem**: parede, chão, teto (pixel art, sem emenda nas duas direções) e props (vigas e esteios de madeira, lanternas, cristais incrustados na parede, como no mockup) |
| Teste de escala 270×480 × 360×640 | **Resolução interna** do render (270×480 × 360×640) com o corredor andando: ver no celular se as texturas "dançam" na perspectiva |
| — | **Densidade de pixel** das texturas (pixels por metro) combinando com a dos sprites, para não misturar tamanhos de pixel (R-04) |

**Arte das 10 cavernas (antigo M9):** cada caverna vira um **kit**: 3 texturas (parede, chão, teto), props e cristal. Costuma sair **mais barato** que um mapa inteiro por caverna; reavaliar depois do Art Spike.

## 10. Efeito nos marcos seguintes

- **Estagiário:** corre ao lado do anão (D-57). A regra de qual cristal ele bate se decide no marco dele.
- **Cavernas:** passar de caverna vira um momento natural. A D-07 ("quebrar a parede") encaixa bem: uma parede atravessa o corredor e o anão passa por ela. A troca de bioma é a troca do kit de texturas. Um contador de **profundidade** (metros descidos) pode entrar, mas só como ideia; precisa de decisão.
- **Offline e AD:** não mudam; a simulação por eventos continua.
- **Balanceamento:** o ritmo passa a depender do espaço entre cristais e da corrida; o relatório mede.

## 11. Riscos novos

| ID | Risco | Mitigação |
|---|---|---|
| R-25 | Texturas de pixel art "dançam" (shimmer) na perspectiva em baixa resolução | Render em baixa resolução com filtro *point*; testar na E1 com textura de teste e no Art Spike com a arte; ajustar a densidade de pixel e o tamanho dos blocos |
| R-26 | O cristal na frente tapa o anão no golpe | Cristal baixo; o anão para a uma distância que deixa o cristal abaixo do peito; validado com sprites cinza na E1 |
| R-27 | O cristal aparece por pouco tempo (menos de 1 s com a câmera do §2) e o jogador não o vê chegando | Escala da trilha (D-58); câmera ajustável no DBG; um **aviso na borda de baixo** (brilho na cor do cristal) antes de ele entrar na tela; som de aproximação |
| R-28 | O 3D com cara de pixel art se afasta do visual do mockup | Paleta e tema do mockup nas texturas; validar um quadro da caverna 1 no Art Spike antes de produzir as outras |
| R-29 | O cristal minerado (~69–90% da tela) fica colado no botão PASSAR DE CAVERNA (~91–97%) e o toque do jogador mira nele | O toque vale na tela toda, então não é preciso acertar o cristal; ver na E1 se o botão precisa subir ou ficar menor |
