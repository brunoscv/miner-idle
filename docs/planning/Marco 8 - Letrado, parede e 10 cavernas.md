# Marco 8 — Letrado, parede e 10 cavernas

> Plano de 2026-10-31, **aprovado pelo Bruno no mesmo dia**, com as recomendações abaixo (D-107…D-113). O M8 vem antes do M7 (D-88). Base: GDD v5 e as respostas do Apellon. Decisões já tomadas que este marco implementa: **D-92** (vida e lascas por caverna), **D-93** (sem penalidade; os 100 cristais são só a trava do Letrado), **D-95** (toda quebra conta), **D-98** (parede com vida) e **D-102** (os 10 biomas, já prontos como dados). O M6 foi aprovado em 2026-10-31 (D-106).

## Objetivo

**Descer de verdade.** O jogador quebra 100 cristais na caverna, compra o Letrado, aperta **Passar de caverna**, o anão corre até uma parede de cristais, quebra a parede e entra na caverna seguinte, com o bioma novo, cristais mais duros e mais lascas. Da caverna 1 à 10 e, no fim, o **coração da montanha**.

**Para você:** jogar no celular e sentir que desceu: o chão muda, a luz muda, o nome da caverna aparece, e o número de CAVERNA no HUD sobe.

## As regras (o que o GDD v5 e as decisões já dizem)

| Regra | Valor | De onde |
|---|---|---|
| Vida do cristal na caverna *c* | `10 × 8^(c−1)` (caverna 2: 80; caverna 10: ~1,34 bi) | D-92 |
| Lascas por cristal na caverna *c* | `1 × 3^(c−1)` (caverna 2: 3; caverna 10: 19.683) | D-92 |
| Letrado: custo do nível *n* | `100 × 10^(n−1)` (1º: 100; 9º: 10 bi) | GDD v5, D-87 |
| Letrado: trava | só compra depois de **100 cristais quebrados na caverna atual**; toda quebra conta (anão, toque) | D-93, D-95 |
| Letrado: o que faz | o nível *n* leva à caverna *n+1*; o 10 leva ao coração da montanha (fim do jogo). **Dobra o dano** na caverna correspondente | GDD v5 §5 |
| Parede | depois do Letrado, o botão **Passar de caverna**; a parede aparece à frente, o anão chega nela correndo e bate até quebrar | D-98 |
| Vida da parede | `2 × vida do cristal da caverna nova` (entrando na caverna 2: 160) | D-98 |

**Uma consequência do "dobra o dano":** como só se entra na caverna *k* com o Letrado *k−1*, o anão tem sempre ×2 de dano da caverna 2 em diante. Na prática, o Letrado é "×2 de dano e uma caverna nova".

Valores novos entram no `Balance_GDD_v5` (crescimento 8 e 3, os 100 cristais, o fator 2 da parede, a linha Letrado). No `Balance_Prototipo` o Letrado fica desligado (limite 0): o protótipo continua com uma caverna só.

## Decisões (aprovadas em 2026-10-31: D-107…D-113, na ordem)

1. **Onde a parede aparece.** Rec.: a **12 un à frente do anão** (onde os cristais nascem, fora da tela). Os cristais que já estão entre o anão e a parede (os ~4 visíveis) ele quebra normalmente; os que estavam depois da parede (fora da tela) somem; não nasce cristal novo até a parede cair. Assim ela nunca surge do nada e a passagem leva poucos segundos.
2. **G14 (pendente com o Apellon).** Rec., até ele responder: o **toque bate na parede**; a parede **não dá lascas** e **não conta** nos 100 cristais (é passagem, não cristal). Crítico e combo valem nela como num cristal.
3. **O progresso dos 100 cristais.** Rec.: na linha do Letrado da loja: "Quebre 100 cristais: 37/100" com o botão apagado; aos 100, o botão acende com o preço. Sem contador novo no HUD.
4. **O botão Passar de caverna.** Rec.: botão âmbar com o livro (como o GDD descreve), na coluna da direita, abaixo de AJUSTES. **Só aparece** quando o Letrado da caverna atual foi comprado; some ao apertar.
5. **A troca de bioma.** Rec.: quando a parede quebra, a tela escurece por meio segundo, o bioma troca e aparece uma faixa com **"Caverna 2 · Galeria rochosa"** (o nome do bioma, D-102). A faixa de chão de transição na emenda fica para o M10 (arte).
6. **O fim.** Rec.: na caverna 10, o Letrado 10 leva a uma última parede; ao quebrá-la, uma tela **"Coração da montanha"** com o tempo de jogo e o total de lascas. Depois o jogo continua na caverna 10 (sem reset; recomeço/prestígio não está no GDD).
7. **O que fica de fora:** a linha **Crítico** (D-100) espera custo e efeito (G16); o nome na loja fica **"Letrado"** até a G16. Estagiário (M7), bônus temporários (D-89), offline (M9).

## Etapas

### E1 — Cavernas e Letrado no núcleo, passagem direta

O jogo jogável com cavernas, ainda sem a parede: comprar o Letrado e apertar Passar troca de caverna na hora.

- **Núcleo:** `GameState` ganha `Cave` (1–10) e `CaveBreaks` (quebras na caverna atual). Os cristais nascem com a vida e as lascas da caverna atual. Nova linha `UpgradeId.Letrado`, com a trava dos 100 cristais (`PurchaseResult` novo: `Locked`) e o limite "um Letrado por caverna" (só compra o nível *n* na caverna *n*). Dano ×2 a partir da caverna 2. Comando `PassCave()` e evento `CaveChanged`.
- **Tela:** CAVERNA no HUD lê o estado; a linha Letrado deixa de ser "Em breve" e mostra o progresso (decisão 3); o botão Passar de caverna (decisão 4); o bioma da caverna atual (o `DescentStage` já sabe trocar).
- **DBG:** **Encher 100 cristais** e **Próxima caverna** (passa sem comprar).
- **Testes EditMode:** trava dos 100 (inclusive quebra por toque); custo do Letrado; um Letrado por caverna; vida e lascas por caverna; ×2 de dano da caverna 2 em diante; `CaveBreaks` zera ao passar; determinismo com a mesma semente. **PlayMode:** HUD e loja acompanham; o botão aparece e some; o bioma troca.

### E2 — A parede

- **Núcleo:** `PassCave()` passa a pôr a parede (decisão 1): um alvo especial na fila, com a vida da D-98, sem lascas, fora da contagem (decisão 2). A caverna só muda quando ela quebra. Nada nasce enquanto ela está de pé.
- **Tela:** a parede no corredor em graybox (blocos de cristal de lado a lado do corredor, desenhados por código, como os cristais do M6 antes da arte), com barra de vida, faíscas e tremor; ao quebrar, estilhaços maiores, o escurecimento e a faixa com o nome da caverna (decisão 5).
- **Testes EditMode:** a parede nasce a 12 un; cristais depois dela somem; nenhum nasce até ela cair; toque e crítico batem nela; não paga lascas nem conta nos 100; quebrou, muda a caverna. **PlayMode:** a parede aparece, quebra e o bioma troca; nenhuma alocação por quadro com a parede na tela.

### E3 — Save e o fim da descida

- **Save versão 2:** `cave` e `caveBreaks` (o nível do Letrado vai junto dos outros, pelo nome). O save da versão 1 abre na caverna 1 com 0 quebras. Uma parede de pé não é salva: ao reabrir, o anão está antes dela e o botão Passar volta a aparecer.
- **Fim (decisão 6):** Letrado 10, a última parede e a tela "Coração da montanha".
- **DBG:** **Ir para a caverna 10** (para testar o fim sem jogar horas).
- **Testes:** ida e volta do save v2; migração do v1; corte de valores fora do lugar (caverna 0 ou 15 vira 1 ou 10); o fim aparece uma vez só.

### E4 — Economia 1→10 e celular

- **Relatório de economia** com cavernas: a estratégia "compra o mais barato, passa assim que puder" e o tempo para chegar a cada caverna (até 8 h de jogo simulado, sem toque). Mede se a descida 1→10 é **alcançável** (o risco do Roadmap). Se não for, os números vão para você e para o Apellon; **nada muda sem decisão**.
- **Moto G75:** descer pelo menos até a caverna 3 jogando, e o resto pelos atalhos do DBG, olhando os 10 biomas na passagem.

## Como testar (Bruno) — ao fim de cada etapa

1. `Tools → Miners Idle → Build → Rodar no celular (Android)`; no jogo, **DBG → Apagar save**.
2. Jogue até 100 cristais (ou **DBG → Encher 100 cristais**). Na LOJA, o Letrado vai de "37/100" até acender com o preço de 100.
3. Compre o Letrado: aparece **Passar de caverna** na coluna da direita.
4. Aperte: (E1) a caverna troca na hora; (E2 em diante) a parede aparece lá na frente, o anão chega, bate (toque ajuda) e quebra; a tela escurece e aparece "Caverna 2 · Galeria rochosa".
5. CAVERNA no HUD mostra 2; o DANO dobrou; os cristais aguentam mais e pagam 3 lascas.
6. Feche e reabra: continua na caverna 2 (E3).
7. **DBG → Ir para a caverna 10**, compre o Letrado 10 e quebre a última parede: "Coração da montanha" (E3).

## Critério de pronto

- [ ] Caverna 1 → 10 jogável, com Letrado, trava dos 100, parede e troca de bioma; a regra no núcleo, com testes, e o determinismo mantido.
- [ ] Save v2 com migração do v1; nenhum teste apagado.
- [ ] Relatório de economia 1→10 feito e mostrado a você.
- [ ] No Moto G75, você aprova a passagem (parede, escurecimento, nome) e a sensação de descer.

## Riscos

| Risco | Mitigação |
|---|---|
| A descida 1→10 inalcançável com os números do v5 (cristal da caverna 10 com ~1,34 bi de vida) | Medir na E4 antes de qualquer ajuste; o ajuste é do Apellon (decisão registrada) |
| A Mineração do v5 barateia tudo cedo demais (G17) | O relatório da E4 mostra o efeito em várias cavernas; leva números para a resposta da G17 |
| Parede em graybox destoando da arte do Apellon | É o mesmo caminho dos cristais no M6: graybox primeiro, a arte entra no lugar (pedido ao Apellon) |
| Save v1 de jogadores de teste | Migração testada na E3 |

## Fora deste marco

Linha Crítico (G16) · estagiário (M7) · bônus do combo e cristal raro (D-89) · offline e "Bem-vindo de volta" (M9) · arte da parede e da emenda do chão, cristais próprios de cada caverna (M10) · recomeço/prestígio.

## Esforço

E1 10–14 h · E2 10–14 h · E3 6–8 h · E4 4–6 h → **30–42 h** (~2 semanas a 20 h/semana).

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| E1 Cavernas e Letrado, passagem direta | ✅ testada pelo Bruno no Moto G75 em 2026-10-31 (junto com a E2) | Core: `GameState.Cave` e `CaveBreaks`; `MiningRules` com `CaveCount`, crescimento da vida e das lascas e `LetradoBreaks`; os cristais nascem com os valores da caverna (`CrystalTrack.SetCrystalValues`); `UpgradeId.Letrado` com a trava em `Purchases.Blocked` (`Locked`: faltam cristais; `PassFirst`: o desta caverna já foi comprado); dano × 2 da caverna 2 em diante (`MinerStats`); `PassCave`, `CanPassCave` e o evento `CaveChanged`. Os cristais já na fila continuam da caverna anterior. Tela: CAVERNA do HUD; Letrado na loja ("Quebre 100 cristais: 37/100", D-109; "DESCER" no botão depois de comprado); botão **DESCER** (`CavePassButton`, âmbar com o livro, abaixo de AJUSTES, D-110); o corredor troca para o bioma da caverna. DBG: **Encher 100 cristais**, **Próxima caverna** (dá o Letrado de graça) e o Letrado na linha das melhorias. Dados: o setup põe as 10 cavernas e o Letrado no `Balance_GDD_v5` (o protótipo fica com uma caverna) e tira o Letrado do "Em breve" no catálogo. O relatório de economia ainda não compra o Letrado (E4). Nome do botão: "DESCER", porque "Passar de caverna" não cabe no quadrado. Testes: EditMode `CaveTests` (10), PlayMode `CaveFlowTests`; `ShopTests` passa a esperar só o Estagiário bloqueado. Em batch: setup ok, EditMode **136/136**, PlayMode **58/58** |
| E2 A parede | ✅ testada pelo Bruno no Moto G75 em 2026-10-31: Letrado com os 100 cristais, parede, escuro e faixa da caverna 2. Achado: o save v1 guarda o nível do Letrado mas não a caverna (ao reabrir, volta à caverna 1 com o Letrado comprado); corrigido pelo save v2 da E3 | Core: a parede é um `Crystal` com `IsWall` (recompensa 0), posta por `CrystalTrack.PlaceWall` no fim da fila; `PassCave()` a põe a 12 un do anão (D-107), ou logo depois do alvo se ele estiver além disso, para o anão nunca perder o alvo; os cristais depois dela somem, e também um que ficaria a menos do espaço mínimo dela. Vida: `MiningRules.WallHealthFactor` (2, D-98) × a vida do cristal da caverna nova; campo `wallHealthFactor` no `MiningRulesAsset`. Enquanto ela está de pé, nada nasce (nem pelo DBG) e `CanPassCave` é falso; golpe, toque, crítico e combo batem nela; quebrada, não paga lascas nem conta nos 100 (D-108), e `EnterCave` leva à caverna seguinte. **DBG → Próxima caverna** tira a parede, se houver. Tela: `DescentStage` desenha a parede em graybox (7 blocos de cristal de lado a lado, baixos no meio para não esconder o anão), com barra de vida maior; a troca de caverna escurece só o corredor por 0,5 s e troca o bioma no escuro; `MiningFeedbackView` faz estilhaços em três pontos e tremor forte na quebra, sem lasca voando; `CaveBanner` (novo, `Effects/CaveBanner` na cena) mostra "Caverna 2 · Galeria rochosa" por ~2 s, texto em `FeedbackTexts.caveBannerFormat` (D-111). Testes: EditMode `CaveTests` (+7; o da passagem mudou para medir a caverna trocando só na queda da parede), PlayMode `CaveFlowTests` (o fluxo inteiro) e `AllocationTests.HittingTheWall_DoesNotAllocate` (0 alocações num intervalo de golpe com a parede na tela). Em batch: setup ok, EditMode **143/143**, PlayMode **59/59** |
| E3 Save e o fim da descida | 🔨 implementada em 2026-10-31; aguardando validação no Moto G75 | **Save v2** (`SaveData.CurrentVersion = 2`): caverna, quebras nela, tempo de jogo, lascas ganhas e coração da montanha. O v1 abre migrado na caverna 1, com 0 quebras e as lascas atuais como "ganhas" (o save do Moto G75 com o Letrado 1 vira caverna 1 com DESCER na tela). `GameState.Restore` corrige o que estiver fora do lugar: caverna entre 1 e a última; o Letrado acompanha a caverna (na caverna *c*, nível *c−1* ou *c*); o coração só vale na última caverna com o último Letrado. Uma parede de pé não é salva: ao reabrir, DESCER volta. O `MiningRunner` grava também a cada troca de caverna e no coração, não só a cada 30 s. **Fim (D-112):** na caverna 10, o Letrado 10 libera DESCER para a última parede; a vida dela segue a fórmula da D-98 uma caverna além (`2 × 10 × 8^10` ≈ 21,5 bi, D-114). Quebrada, o evento `HeartReached` e, 1,2 s depois, a tela **"Coração da montanha"** (`HeartScreen`, graybox nas cores da loja) com o tempo de jogo (`NumberFormatter.FormatDuration`) e as lascas coletadas (`GameState.ShardsEarned`: só as das quebras); CONTINUAR fecha e o jogo segue na caverna 10, sem passagem. Textos em `FeedbackTexts`. **DBG → Caverna 10 (fim)** (`SkipToCave`): a última caverna, os 100 cristais e as lascas do Letrado 10. Testes: EditMode `CaveTests` (o da última caverna mudou para medir o fim; +2), `SaveServiceTests` (+4: v2, migração do v1, valores fora do lugar, parede não salva e coração mantido), `NumberFormatterTests` (+7 casos do tempo); PlayMode `CaveFlowTests.LastCave_TheLastWall_ShowsTheHeartOnce`. Em batch: setup ok, EditMode **156/156**, PlayMode **60/60** |
| E4 Economia 1→10 e celular | ⏳ | |
