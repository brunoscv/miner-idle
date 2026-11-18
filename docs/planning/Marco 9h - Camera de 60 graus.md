# Marco 9h — Câmera de 60°

> Plano de 2026-11-18, escrito junto com a E0 e a E1, a pedido do Bruno ("pode fazer a implementação do bloco g"). Os números da câmera já estavam decididos (D-245), então a E1 só troca o padrão e deixa a F1 no DBG para comparar. **As perguntas P1…P4 esperam o Bruno**; até lá, nenhum valor de regra mudou. É o **Bloco G**, o último da [Análise do GDD v8](Analise%20-%20GDD%20v8.md) §5, e vem depois do [Marco 9g](Marco%209g%20-%20Bomba%20e%20o%20fim%20do%20toque.md) (aprovado em 2026-11-18, D-323). Segue o processo do v8 (D-298): as melhorias vistas nos testes vão para a [lista de ajustes](Ajustes%20do%20v8%20-%20lista.md). Base: GDD v8.2 §7.5 e as decisões D-245, D-252, D-277 e D-319.

## Objetivo

**O corredor visto de cima, como no GDD.** A câmera sobe de 3,6 m para **5,5 m**, inclina de 30° para **60°**, chega a **3 m** do anão e abre o campo de visão de 40° para **60°**. O anão fica no meio da tela, menor, e embaixo dele aparece o chão por onde ele vai correr.

**Para você:** dar Play e jogar uns andares com a câmera nova; no DBG → Câmera, apertar **Câmera v8** para ver a F1 de antes e voltar.

## As regras

- **D-245:** inclinação 60°, altura 5,5 m, 3 m até o anão, campo de visão 60°. Substitui o enquadramento de frente da D-52 (e a F1 da D-69).
- **D-252, D-277:** 3 m entre cristais no começo, e o Atalho tira 0,5 m por nível. "O ajuste fino vem com a câmera" (P2).
- **D-319:** a bomba atinge os cristais até **4 m** à frente do anão, medidos na F1. "Reajustado com a câmera do Bloco G" (D-307) — P1.

## Como a câmera foi lida

O GDD dá só os quatro números. Duas leituras eram possíveis para "3 m até o anão": em linha reta (impossível, com a câmera a 5,5 m de altura) ou **na horizontal**. Na horizontal, a câmera a 5,5 m de altura e 3 m à frente olha para o anão num ângulo de 61°, o que bate com os 60° de inclinação: **a câmera olha direto para ele.** Foi essa a leitura usada.

A câmera continua **de frente** (o anão corre na direção dela), como desde o M6. Uma câmera atrás do anão pediria a arte dele de costas, que não existe, e esconderia os cristais chegando.

O campo de visão de 60° vale para a tela 9:16, como os 40° da F1: numa tela mais alta (o Moto G75 é 9:20) a largura vista é a mesma e a sobra vai para cima e para baixo.

## O que muda na tela (medido)

| | F1 (até o M9g) | v8 (desde o M9h) |
|---|---|---|
| Câmera | 3,6 m, 30°, 5 m, 40° | **5,5 m, 60°, 3 m, 60°** |
| Anão na tela (9:16) | da cabeça a 37% aos pés a 64% do topo (27% da altura da tela) | de **41% a 52%** (11%, menos da metade) |
| Anão na tela (9:20) | — | de 43% a 52% |
| Chão à frente dos pés | 1,98 m (9:16) · 2,43 m (9:20) | **3,0 m (9:16) · 3,56 m (9:20)** |
| Cristais à frente, com 3 m entre eles | 0 ou 1 | 1, às vezes 2 (o próximo entrando por baixo) |
| Acima do anão | a boca do corredor, com os esteios | ~9 m de corredor atrás dele, com os esteios e o lampião |
| Teto e vigas | o teto some (câmera acima dele) | igual: a câmera está acima da parede (3,2 m) |

Prévias em `tools/ci/out/`:
- `m9h-e0-comparacao.png`: F1 e v8 lado a lado, 9:20 e 9:16.
- `m9h-e1-tudo.png`: o jogo na câmera nova (a cena aberta, com o estagiário, logo depois da bomba, a transição de andar subindo e chegando, e o andar 2).

## As perguntas (esperam o Bruno)

| | Pergunta | Recomendação |
|---|---|---|
| **P1** | O **alcance da bomba** (D-319, hoje 4 m): a tela agora mostra 3,0 m (9:16) a 3,56 m (9:20) à frente. Com 4 m, um cristal fora da tela, logo abaixo da borda, ainda leva a bomba. | **3,5 m**: cobre o que o Moto G75 mostra e não pega cristal escondido numa tela 9:16 além de meio metro. Alternativa: deixar 4 m (a bomba pega um pouco além da borda, e ninguém vê). |
| **P2** | A **distância entre cristais** (D-277, 3 m): com a câmera nova, um cristal fica à vista de cada vez. | **Manter 3 m**: com o Atalho (0,5 m no nível 5) chegam a 7 cristais na tela, que é o "quase colado" da D-241. |
| **P3** | O **tamanho do anão**: ele cai para menos da metade da altura na tela. A arte continua nítida (o render é de 270 px de largura, D-14), mas os detalhes do rosto quase somem. | **Ficar assim** e decidir no Moto G75. Se pedir mais anão, o ajuste mais barato é o campo de visão (55° ainda mostra 2,8 m à frente), sem mudar a D-245 sem registro. |
| **P4** | A **transição de andar B** (D-268) sobe a câmera mais 4 m: a 9,5 m ela vê por cima das paredes, e os cantos de cima ficam escuros. | **Deixar**: é só durante o cartão escuro e o desfoque; se incomodar, entra na lista de ajustes (paredes mais altas só na transição). |

## Etapas

### E0 — Prévias da câmera ✅ feita (2026-11-18)

- Capturas do próprio jogo (cena Game, câmera da descida desenhada numa textura de 270×600 e 270×480), F1 e v8, nas duas proporções. Resultado: a câmera do GDD funciona com o corredor de hoje sem buracos; o teto e as vigas já somem com a câmera acima da parede; os esteios ficam atrás do anão, como antes.

### E1 — A câmera do v8 como padrão ✅ feita, espera o teste do Bruno

- `DescentFraming.GddV8` (D-245) e `DescentFraming.F1` (o `Validated` de antes). `DescentFraming.Default` é a do v8: é a do `DescentStage` novo e a do botão **Padrão v8** do DBG.
- Os limites dos ± no DBG abrem espaço em volta da câmera do v8: inclinação até 75° (era 65°) e distância a partir de 1,5 m (era 3 m, o valor da v8 batia no limite).
- **DBG → Câmera → "Câmera v8"**: troca v8 ⇄ F1 (só altura, inclinação, distância e campo de visão; a largura do corredor fica). Depois de um ±, o botão mostra "Câmera ?" e o próximo clique volta para a do v8. Não fica salvo.
- Continuam ligados à F1, de propósito: o **recuo** (D-148, ferramenta de comparação da F1: na v8 o botão mostra "Recuo ?" e o primeiro clique leva o anão aos 5 m), a **névoa** (conta a partir dos 5 m da F1: na v8 ela começa 2 m mais perto, e o anão, a 3 m, fica com a mesma névoa que tinha na F1) e o **limite dos esteios** (0,6 m atrás do anão: 3,6 m da câmera na v8).
- O setup (`CameraV8Setup`, no fim do `GrayboxSetup`) passa a cena que ainda está na F1 para a v8 (uma câmera mexida à mão no Inspector fica), cria o botão e troca o rótulo "Padrão F1" para "Padrão v8".
- Testes: `DescentTests` mede a F1 com o nome dela, ganha `GddV8Framing_PutsTheDwarfInTheMiddle_AndIsTheDefault` (posições na tela e chão à frente nas duas proporções) e `CameraPanel_Preset_SwitchesBetweenV8AndF1`; os testes do recuo (`DescentTests`, `CorridorArtTests`) passam para a F1 antes de começar, porque o recuo é dela.

### E2 — Os ajustes das respostas

- P1: o `bombReach` no `Balance_GDD_v8` (via `EnsureGddV8Bomb`) e no `MiningRulesAsset`, com o teste do alcance.
- P2–P4: só se as respostas pedirem mudança.

### E3 — Celular

- Vai para os **testes longos do fim** (D-298), como as dos outros blocos.

## Como testar (Bruno)

- **E0:** abrir `tools/ci/out/m9h-e0-comparacao.png` e `tools/ci/out/m9h-e1-tudo.png`.
- **E1:** aplicar o setup (Tools → Miners Idle → Projeto → Aplicar configuração do projeto; no Console, a linha `[Setup] Câmera da descida passa para a do GDD v8`) e dar Play na cena Game.
  1. COMEÇAR: o corredor de cima, o anão no meio da tela e o cristal da frente embaixo dele.
  2. Jogar uns andares com a bomba: o cristal entrando por baixo, a explosão nos cristais da tela, o número laranja.
  3. DBG → Andar → Encher a barra e Próximo Andar: a transição B (a câmera sobe e desce) termina na câmera nova.
  4. DBG → Estagiário → Contratar: ele aparece ao lado do anão.
  5. DBG → Câmera → **Câmera v8**: passa para a F1 ("Câmera F1"); de novo, volta. **Padrão v8** também volta.
  6. A linha de medidas do DBG → Câmera mostra onde o anão fica e quanto chão se vê, para conferir no Moto G75.
- Nenhuma cena nova; um botão novo no DBG → Câmera (Câmera v8 / F1).

## Critério de pronto

- O jogo abre na câmera da D-245, a F1 continua no DBG para comparar.
- P1…P4 respondidas; o alcance da bomba registrado com a câmera nova.
- Testes da câmera passando; o celular nos testes longos do fim.

## Riscos

- **O anão pequeno** (P3): é o maior risco visual, e só o celular responde.
- **O alcance da bomba** (P1) é valor de regra: só muda com decisão registrada.
- A câmera mais alta mostra mais corredor atrás do anão (já minerado). Se parecer vazio, a lista de ajustes pode trazer algo para lá (o estagiário atrás, poeira, a marca das quebras).

## Fora deste marco

- Uma câmera atrás do anão (pede arte de costas).
- Paredes mais altas ou tampas de pedra para a transição (P4), se pedidas, vão para a lista de ajustes.

## Esforço

| Etapa | Horas |
|---|---|
| E0 | 1 |
| E1 | 2–3 |
| E2 | 1 |

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| Plano | 📝 escrito em 2026-11-18, P1…P4 esperam o Bruno | números da câmera já decididos (D-245) |
| E0 Prévias | ✅ feita | `tools/ci/out/m9h-e0-comparacao.png`, `m9h-e1-tudo.png` |
| E1 A câmera do v8 como padrão | 🔶 feita, espera o teste do Bruno | setup aplicado na cena; testes PlayMode da câmera, do corredor, dos biomas, da cena, do andar, da rachadura e do estagiário passando (64 de 65; a falha é a de baixo) |
| E2 Os ajustes das respostas | ⏳ espera P1…P4 | |
| E3 Celular | ➡️ testes longos do fim | |

**Achado fora do marco:** `BombFlowTests.SwipingRight_WithTheBombFull_Throws_AndEveryCrystalInReachLosesTheFraction` falha também no commit `4d5ecaa` (sem a câmera nova): "a bomba caiu em algum cristal", com a lista vazia. Fica para a rodada de testes do fim (o Bruno roda e corrige os testes no fim).
