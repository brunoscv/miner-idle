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

### E3b — O M8 no GDD v7 (aprovada em 2026-11-04: D-128…D-130, D-133…D-136, D-143)

> O GDD v7 é a referência (D-132), e o Bruno aprovou a E3b em 2026-11-04. Ela muda o que a E1–E3 fizeram: o Letrado vira **Mapa**, a descida vai a **100 níveis** e os números do anão passam aos do v7. **O padrão é sempre o que o GDD diz**; as variações ficam no DBG (D-143). Diferenças: [Análise - GDD v7](Analise%20-%20GDD%20v7.md). A proposta anterior, pelo v6, fica superada.

| Hoje (E1–E3, v5) | Com o v7 | Decisão |
|---|---|---|
| Vida `10 × 8^(c−1)`, lascas `1 × 3^(c−1)` | vida `1 × 1,25^(nível−1)`, lascas = **vida × 1,2** | D-130, D-134 |
| Picareta: dano `5 × 2^(n−1)`, custo `50 × 4^(n−1)` | **`Dano Base + taxa^(nível da caverna − 1)`**; Dano Base 1, taxa 1,2; cada compra +1 no Dano Base; custo 2 × Dano Base; até 50 | D-133 |
| Velocidade `50 × 5^(n−1)`, 2,0 → 0,1 s | `50 × 2^(n−1)`, 2,0 → 0,1 s (19 compras) | D-135 |
| Mineração `50 × 1,2^(n−1)` | `20 × 5^(n−1)`, +100% por nível, 10 níveis | D-130 |
| Crítico fixo de 5% em todo golpe | linha **Chance de Crítico** `50 × 2^(n−1)`, +5% por compra, 10 compras; base **0% no automático, 5% no toque**; a linha soma nos dois | D-136 |
| Picareta Dupla 5.000 | **sai** (limite 0, como as Botas na D-103) | D-136 |
| Letrado `100 × 10^(n−1)`, ×2 de dano da caverna 2 em diante | **Mapa**, custo zero (só a trava dos 100 cristais), **sem ×2** | D-128 |
| 10 cavernas; Letrado 10 → coração | **100 níveis**; Mapa 100 → coração (a última parede e a tela da D-112 vão para o nível 100) | D-128, D-129 |
| Bioma muda a cada caverna | bioma a cada **10 níveis** (os 10 da D-102 servem); escurece só na troca de bioma; a faixa em todo nível | D-129 |
| Save v2, caverna 1–10 | **save v3**, nível 1–100, migração do v2 | D-129 |

A E3b é dividida em três partes, mais o formato dos números (feito antes). Cada uma termina com o jogo rodando e é testada por você antes da seguinte.

#### E3b-1 — Fórmulas de custo e o `Balance_GDD_v7`

- **Núcleo:** hoje o custo só sabe uma conta (`base × crescimento^nível`, `UpgradeDefinition.CostAt`). Ele passa a ter um **tipo de fórmula**: exponencial (`base × taxa^(n−1)`), "soma" (`base + taxa^(n−1)`, a das permanentes), linear (`base + passo × (n−1)`) e "× Dano Base" (a Picareta do anão). O efeito também ganha o tipo da Picareta pela caverna (`MinerStats`): `Dano Base + taxa^(nível − 1)`. Nova linha `UpgradeId.Critical`; o `StrikeRules` separa a chance base do golpe automático (0%) e a do toque (5%), e a linha soma nas duas.
- **Dados:** `Balance_GDD_v7` (o setup cria; o v5 fica para comparação) com os números da tabela acima; a taxa (1,2) e o incremento (+1) da Picareta ficam no asset (D-133). Dupla com limite 0; Chance de Crítico na loja, com o texto em dados.
- **Ainda sem o Mapa:** nesta parte o Letrado e as 10 cavernas continuam como estão, só com os números novos. É o jogo do v7 nas 10 cavernas de hoje.
- **Testes EditMode:** custo e efeito de todos os níveis de cada tipo de fórmula (inclusive o limite); Picareta pela caverna (caverna 1 com Dano Base 1 dá 2); crítico 0% no automático e 5% no toque (100.000 golpes, como no M3), e a linha somando nos dois; os testes que medem os números do v5 continuam com o `Balance_GDD_v5` (nenhum é apagado).
- **Esforço:** 6–9 h.

#### E3b-2 — Mapa e 100 níveis

- **Núcleo:** `UpgradeId.Letrado` vira **Mapa** no texto (o id interno continua, para o save); custo zero; o `×2` sai do `MinerStats`; `caveCount = 100`; `MiningRules` ganha o bioma do nível (`(nível − 1) / 10`). A vida da última parede segue a D-98 no nível 100 (a D-114 é revista pela conta nova).
- **Tela:** "Mapa" na loja ("Quebre 100 cristais: 37/100" continua); CAVERNA do HUD mostra o nível; o `DescentStage` troca o bioma só nos múltiplos de 10 e escurece só nessa troca; a faixa vira "Nível 12 · Galeria rochosa" (texto em `FeedbackTexts`). O botão PASSAR e o livro ficam até haver um ícone de mapa (D-142).
- **Save v3:** nível 1–100; o v2 abre no mesmo nível (1–10), com o Mapa correspondente.
- **DBG:** **Próxima caverna** continua; **Caverna 10 (fim)** vira **Nível 100 (fim)**; atalho novo **+10 níveis**, para ver os biomas.
- **Testes:** os da E1–E3 que medem o custo do Letrado, o ×2 e as 10 cavernas **mudam para medir o v7**; novos: bioma troca no 11, 21…; escurecimento só na troca; save v2 → v3; nível 100 leva ao coração.
- **Esforço:** 6–9 h.

#### E3b-3 — DBG → Cálculos e o aviso na tela (D-143)

- **DBG → Cálculos** (página nova): um seletor geral **GDD v5 (o jogo de antes) / GDD v7** e, embaixo, uma linha por melhoria que tem alternativas: cada toque passa para a próxima fórmula, mostrando o nome dela e o total da linha (como o DBG → Câmera → Golpe). Nesta etapa entram a **taxa** e o **incremento** da Picareta do anão (passos de 0,01 e de 1), porque são os números que a D-133 deixou configuráveis. As alternativas do estagiário (M7) e das permanentes (marco "Dia e melhorias permanentes") entram na mesma página quando essas linhas existirem.
- **Reiniciar run** no DBG: volta ao nível 1, lascas zeradas e melhorias base no zero, para comparar fórmulas do começo. O "Apagar save" continua.
- **Aviso fixo na tela**, só no build de teste (some no release, como o DBG): "Cálculo: GDD v7" quando tudo está no padrão; em âmbar, quando algo saiu do GDD, com a lista ("Cálculo: GDD v7 · Picareta taxa 1,22"). A escolha fica salva no aparelho.
- **Núcleo:** nenhuma regra nova; a troca reconstrói a economia com as mesmas regras e outra fórmula na linha. O núcleo continua sem saber do DBG.
- **Testes:** PlayMode: trocar a fórmula muda o custo na loja e o aviso; Reiniciar run volta ao nível 1; a escolha sobrevive a reabrir o jogo; o build de release não tem o aviso.
- **Esforço:** 5–8 h.

#### Formato dos números (D-144) — feito antes, à parte

O `NumberFormatter` passa a abreviar a partir de mil, com 2 casas (K, M, B, T, Qa, Qi). Independe das outras partes e é feito primeiro.

- **Fora desta etapa:** lascas vermelhas, permanentes e o botão de descansar (marco "Dia e melhorias permanentes", D-131); a Memória (a trava fica fixa em 100); o estagiário (M7).

### E4 — Economia 1→10 e celular

> **Com o v7 (E3b aprovada em 2026-11-04):** o relatório vai do nível 1 ao 100, com o `Balance_GDD_v7`, e diz **até que nível uma run chega sem melhorias permanentes** em 1, 2, 4 e 8 h. É o número que o marco "Dia e melhorias permanentes" vai precisar para calibrar as lascas vermelhas.

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
| Formato dos números (D-144) | 🔨 implementado em 2026-11-04; aguardando validação do Bruno | `NumberFormatter`: a partir de mil, centésimos inteiros do grupo (valor ÷ 10^(3·grupo − 2)), arredondados (`Format`) ou truncados (`FormatShards`), sufixos K/M/B/T/Qa/Qi, sobe de grupo em 1.000,00; acima de Qi, notação científica. `NumberFormatterTests` mudou para medir o formato novo (+15 casos). Em batch: EditMode **201/201**, PlayMode **76/76** |
| E3b-1 Fórmulas de custo e `Balance_GDD_v7` | ✅ validada pelo Bruno em 2026-11-05 (D-149) | Custo linear/exponencial por linha, Picareta `Dano Base + 1,2^(caverna−1)` (D-133), linha Crítico no lugar das Botas e da Dupla (D-136), crítico base 0% no golpe e 5% no toque; jogo em `Balance_GDD_v7`. Testes de tela do golpe/pose fixados no v5 (`MiningRunner.UseRules`). EditMode 216/216, PlayMode 76/76 |
| E3b-2 Mapa e 100 níveis | ✅ validada pelo Bruno em 2026-11-05 (D-149) | `Balance_GDD_v7` com 100 níveis, `levelsPerBiome` 10 (`MiningRules.BiomeOf` = (nível − 1) ÷ 10; o v5 fica com 1) e o Mapa (id `Letrado`) de custo zero, efeito ×1, até o nível 100. `DescentStage` troca o bioma e escurece só nos níveis 11, 21…; a faixa diz "Nível N · bioma" em todo nível (no mesmo bioma, entra na hora). Loja: "Mapa", "Nível 1 » 2". Última parede no nível 100: `2 × 1,25^100` ≈ 9,8 bi (fórmula da D-98, revendo o número da D-114). **Save v3** (mesmos campos; o v2 abre no mesmo nível, com o Mapa que tinha; o coração da caverna 10 cai). DBG: **Nível 100 (fim)** e **+10 níveis**. Testes: EditMode `GddV7RulesTests` (+11: bioma, Mapa grátis sem ×2, nível 100 → coração, asset), `SaveServiceTests` (+1 e o v3); PlayMode `CaveFlowTests` (mede o v7: nível 2 sem escurecer; novo `TenLevels_ChangeTheBiome_InTheDark`). EditMode 228/228, PlayMode 77/77 |
| E3b-3 DBG → Cálculos e aviso | ✅ validada pelo Bruno em 2026-11-05 (D-160) | Menu **Cálculos** no DBG (`CalculationsPanel`): seletor GDD v7/v5 (`MiningRunner.Balances`; trocar recomeça a run), taxa ±0,01 e incremento ±1 da Picareta (mantêm o progresso; custo "2 × Dano Base"), Reiniciar run, Padrão GDD; linha da Picareta (dano agora, próxima compra, a linha inteira); aviso fixo "Cálculo: …" ao lado do DBG, em âmbar fora do GDD. Escolha em `calculations.json` (`CalculationStore`), só pelo caminho do Bootstrap e nunca no release (D-150). O núcleo não mudou: o ajuste entra em `MiningRulesAsset.ToRules/ToEconomy`. Testes: EditMode `CalculationTests` (+7), PlayMode `CalculationsPanelTests` (+5) e `DebugPanelTests` (mede o botão Cálculos). EditMode 235/235, PlayMode 91/91 |
| E4 Economia 1→100 e celular | ⏸️ relatório adiado (D-149): só depois de muitos testes | O teste no Moto G75 vai junto com o do Marco 8b (D-160) |

**✅ M8 aprovado pelo Bruno em 2026-11-05 (D-160).** Segue o [Marco 8b — Dia e melhorias permanentes](Marco%208b%20-%20Dia%20e%20melhorias%20permanentes.md).

## Ajustes pedidos pelo Bruno em 2026-10-31 (depois da E3) — implementados, aguardando validação

Em batch: setup ok (`[Setup]` do cristal por caverna, do botão embaixo e das opções do esteio), EditMode **156/156**, PlayMode **62/62** (o `CrystalArtTests` ganhou o teste da parede; o `CorridorArtTests`, o do esteio baixo). Captura conferida: o botão cabe com folga (145 × 30 px da referência), acima do DBG.

| Ajuste | O que mudou |
|---|---|
| **Botão embaixo (D-115)** | PASSAR DE CAVERNA sai da coluna da direita e fica embaixo, no meio, com o livro à esquerda do nome; a largura acompanha o nome. O setup converte o quadrado "DESCER" das cenas de antes (`HudSkinSetup.PlacePassCaveAtBottom`). |
| **Chão de cabeça para baixo** | Achado do dev: o desenho do chão aparecia invertido. Era assim desde o M6 E1: na malha do corredor, o v do chão crescia para perto da câmera, então o alto do PNG ficava embaixo na tela (a luz e a sombra das pedras do tile ficavam trocadas). Agora o v cresce para longe (`DescentMeshes.Corridor`) e a rolagem do chão tem o sinal trocado, para andar junto da trilha. Parede e teto não mudam. |
| **Esteio baixo (D-117)** | O `mine_wood` de 4,5 m tem a viga (4,1–4,5 m) acima da câmera (3,6 m, olhando para baixo): ela nunca aparece; e o lampião desenhado nele fica com ~1 m. O setup (`SupportArtSetup`) gera o mesmo esteio com **3,0 / 2,8 / 2,4 m** tirando linhas do meio dos postes (sem esticar pixel; `Art/Corridor/mine_wood_h30…h24`), e nessas opções o lampião é o `lamp_16x16` solto, com 0,45 m, no poste da esquerda. **DBG → Câmera → Esteio** passa 4,5 → 3,0 → 2,8 → 2,4 (não fica salvo). **Escolha do Bruno em 2026-11-01: 3,0 m é o padrão, com um lampião em cada esteio, em todas as cavernas** (`lampEvery = 1`), sem mudar a câmera. 3,2 m foi descartado: a viga ficaria toda atrás do HUD. No pior caso (o esteio mais perto, 0,6 m atrás do anão), a viga fica de 15 a 70 px (3,0 m), 34 a 88 px (2,8 m) ou 88 a 139 px (2,4 m) dos 600 da tela; a cabeça do anão fica a ~237 px. Teste: `CorridorArtTests.ShortSupports_StayBelowTheCamera_WithASmallLooseLamp`. |
| **Um cristal por caverna (D-116)** | A fila usa só o cristal do bioma: caverna 1 branca, depois azul, amarela, em ciclo (dado em `Data/Biomes/Caverna_NN`). A parede continua com os três misturados. Teste: `CrystalArtTests` passa a medir um cristal por caverna e o revezamento só na parede. |

**Como testar:** no Unity, `Tools → Miners Idle → Projeto → Aplicar configuração do projeto`, abrir a cena `Game` e dar Play. (1) **DBG → Encher 100 cristais**, comprar o Letrado na LOJA: PASSAR DE CAVERNA aparece embaixo, no meio, com o livro. (2) Olhar o chão: o desenho do tile como no PNG (`Art/Tile/tileset_ground_01`), não de cabeça para baixo, e andando junto com as pedras da beira. (3) Na caverna 1 só cristais brancos; **DBG → Câmera → Bioma** passa pelas cavernas: 2 azul, 3 amarelo, 4 branco… (4) **DBG → Câmera → Esteio**: passar por 4,5 / 3,0 / 2,8 / 2,4 (o jogo abre no 3,0 m, com um lampião pequeno no poste da esquerda de cada esteio). No Moto G75: `Tools → Miners Idle → Build → Rodar no celular (Android)`.

## Correções do toque e da fila (pedido do Bruno em 2026-11-01) — ✅ testadas pelo Bruno no Moto G75 em 2026-11-01

> Vem do teste de um amigo do Bruno, no celular dele (modelo ainda não anotado), batendo com 3 dedos depois de comprar muita melhoria. Decisões, todas aprovadas pelo Bruno em 2026-11-01: **D-118** (um toque por dedo), **D-119** (teto de 15/s), **D-120** (a tela nunca sem cristais), **D-121** (toque só no alvo na tela), **D-122** (fila mínima de 10) e **D-123** (um toque de cada vez: dedos juntos valem 1). O `tapReach` medido (3,9 un) foi aprovado no teste do celular. Entra antes da E4. Pendente: o modelo do celular do amigo e o print do DBG nele.

### O que aconteceu

1. **Dedo parado contando toque (D-118).** Com 3 dedos parados na tela, o combo foi a **x640 em menos de 3 s**, e cada um desses toques deu dano de toque (33% de um golpe): uns 210 golpes de dano de uma vez. A conta bate com um toque **por dedo por quadro** (3 dedos × 90–120 quadros/s). O `TapInput` lê os dedos direto do `Touchscreen` (`press.wasPressedThisFrame`), um jeito que o manual do Input System desaconselha para ler toques quadro a quadro; em alguns aparelhos o dedo parado volta a parecer "encostou agora". O combo e o dano nunca se multiplicam (o combo é só visual, D-48b); o que multiplicou foi o número de toques.
2. **Tela sem cristais (D-120).** O toque acerta o alvo mesmo com ele ainda longe (D-48a), e o cristal novo nasce a 12 un (6 m) à frente, fora da tela (D-60, D-80). Com o anão forte e muitos toques, o alvo quebra **antes de entrar na tela**, o próximo nasce mais longe ainda e tem o mesmo destino: a fila tem 4 cristais o tempo todo, todos fora da tela, e o anão corre num corredor vazio.

### Decididas para a C2 (Bruno aprovou as duas recomendações em 2026-11-01)

| # | Decisão | Por quê |
|---|---|---|
| **D-121** | **O toque só acerta um alvo que já está na tela.** Muda a D-48a: com o anão andando, o toque vale se o alvo já apareceu; com ele ainda fora da tela, o toque só conta no combo (como hoje sem alvo). O alcance (`tapReach`, em un) é medido na C2, na câmera F1 (D-69): a distância em que o cristal entra pela base da tela. O número medido é mostrado ao Bruno e registrado antes de valer. | É a peça principal: ninguém quebra um cristal que não viu. |
| **D-122** | **Fila mínima de 4 para 10** (`minCrystals` no `Balance_GDD_v5`, = os 10 iniciais): cada cristal comum quebrado faz nascer outro no fim da fila, no mesmo lugar de sempre (D-60). O relógio continua enchendo até 12. | Com 4, depois que os cristais da tela acabam, o próximo está a 12 un; com 10, a fila vai do anão até lá, um a cada 2 un, e o próximo alvo está sempre perto. |

A parede continua igual (D-107): com ela de pé nada nasce, e os cristais antes dela o anão quebra. Efeito na economia: o ritmo de lascas com toque passa a depender do anão chegar no cristal; entra no relatório da E4.

### C1 — Um toque por dedo e o teto de 15/s (D-118, D-119)

- **Presentation (`TapInput`):** liga o `EnhancedTouch` no `OnEnable` e desliga no `OnDisable`; percorre `Touch.activeTouches` e conta só a fase **Began** de um dedo (`touchId`) que ainda não contou. O id sai da lista na fase **Ended/Canceled**. A lista é um vetor fixo (10 dedos), sem alocação no `Update`. O mouse do Editor continua igual; os testes e o DBG continuam passando por `TapAt`.
- **Core (`StrikeRules`, `MiningSimulation.Tap`):** `MaxTapsPerSecond` (15, campo `maxTapsPerSecond` ao lado de `tapFraction`). `Tap()` guarda os instantes dos últimos 15 toques contados (anel fixo, relógio da simulação); se o 15º foi há menos de 1 s, o toque é ignorado e `Tap()` devolve `false`: nada de dano, crítico ou combo. A faísca no dedo sai mesmo assim (o jogador vê que tocou). No ×20 do DBG o teto acompanha o relógio do jogo.
- **DBG:** linha **"Toques/s: contados · ignorados"** e o botão **"Registro de toques"**: guarda os eventos de dedo dos últimos 10 s (id, fase, quadro) e mostra na tela, para o celular do amigo dizer o que o aparelho manda.
- **Testes EditMode:** 100 `Tap()` no mesmo instante dão 15 danos e combo 15; 1 s depois volta a contar; crítico e quebra só nos contados; toque ignorado não mexe no combo nem na janela dele. Os testes que dão mais de 15 toques seguidos sem o tempo andar (ex.: `StrikeTests` com 25) passam a andar o relógio entre os toques, para medir o comportamento novo.
- **Testes PlayMode (dedos simulados do Input System):** 3 dedos encostam e ficam **parados 120 quadros** → **3 toques**; 3 dedos arrastando → 3 toques; 3 dedos batendo (encosta, solta) 5 vezes com intervalo → 15 toques; dedo sobre a interface não conta; nenhuma alocação por quadro com 3 dedos na tela.

### C2 — A tela sempre com cristais (D-120, D-121, D-122)

- **Core:** `MiningRules.TapReach`; `Tap()` só tira vida do alvo com `alvo − anão ≤ TapReach`. `minCrystals = 10` no `Balance_GDD_v5` (o protótipo fica como está).
- **Medir:** o `tapReach` numa captura da câmera F1, no ponto em que o cristal aparece inteiro na base da tela.
- **Testes EditMode:** toque num alvo além do alcance não tira vida (mas conta no combo); sem parede, a fila nunca fica abaixo de 10; com a parede de pé, nada nasce; **tela nunca vazia:** 120 s de jogo com toque no teto e o anão forte, e o tempo sem nenhum cristal dentro do alcance fica no máximo no tempo de correr 2 un.

### O que foi feito (2026-11-01)

Em batch: setup ok (`[Setup] Estatísticas do painel de debug atualizadas`), EditMode **166/166**, PlayMode **73/73** (depois da D-123).

| Parte | O que mudou |
|---|---|
| **Teto de 15/s (D-119)** | `StrikeRules.MaxTapsPerSecond` (campo `maxTapsPerSecond` no `MiningRulesAsset`, 15). `MiningSimulation.Tap()` passa a devolver `bool`: guarda os instantes dos últimos 15 toques que valeram num anel fixo (relógio do jogo); o 16º dentro de 1 s volta `false`, sem dano, crítico, combo nem evento. O toque sem alvo no alcance também gasta o teto (senão o combo, que vai dar bônus pela D-96, subiria sem limite). No núcleo, 0 = sem teto (o padrão dos testes antigos). |
| **Um toque por dedo (D-118)** | `TapInput` lê `EnhancedTouch` (`Touch.activeTouches`) e guarda os `touchId` dos dedos na tela (vetor fixo de 10, sem alocação). Conta o dedo que aparece pela 1ª vez; um "Began" repetido de um dedo já guardado não conta; o dedo é esquecido no Ended/Canceled ou se sumir da lista. Primeiro trata os dedos que saíram, para um id solto e encostado de novo no mesmo quadro contar. Um toque que começou e acabou entre dois quadros conta uma vez. |
| **Um toque de cada vez (D-123)** | Pedido do Bruno depois da primeira versão. O dedo novo só vale se a tela estava livre (nenhum outro dedo guardado) e se nenhum outro dedo já contou no mesmo quadro: 2 ou 3 dedos juntos = 1 toque; dedo novo com outro segurado = não conta; um dedo de cada vez (alternando) = cada um conta. |
| **Rendering Debugger do URP (achado)** | Em build de desenvolvimento, **dois toques de 3 dedos abrem o Rendering Debugger do URP**, um painel invisível na tela de jogo que engole os toques seguintes (achado pelo teste de 3 dedos batendo). O `TapInput` desliga esse gesto (`DebugManager.instance.enableRuntimeUI = false`). Os APKs de teste são de desenvolvimento: quem bate com 3 dedos (como o amigo do Bruno) caía nele. |
| **Toque só na tela (D-121)** | `MiningRules.TapReach` (campo `tapReach`). Com o alvo além disso, o toque só conta no combo. **Valor medido: 3,9 un (1,95 m)**: na câmera F1, a tela 9:16 mostra 1,99 m de chão à frente do anão (`DescentFraming.GroundAhead`); o Moto G75 (20:9) mostra 2,43 m. 4 un passava 1 cm da borda. O teste `GameValues_TapReachIsOnScreen` trava isso se a câmera mudar. **Aprovado pelo Bruno no Moto G75 em 2026-11-01.** |
| **Fila de 10 (D-122)** | `minCrystals: 10` no `Balance_GDD_v5` (e no setup que cria o asset). |
| **DBG** | Duas linhas novas nas estatísticas: **"Toques/s N valeram · N acima do teto"** e **"Dedos: N começos · N repetidos · N juntos · N na tela"** (*juntos* = dedos que não valeram pela D-123) (o último segundo completo). No lugar do botão "Registro de toques" do plano: a grade de botões já está cheia, e essas contagens dizem o que importa do aparelho (muitos *repetidos* = o aparelho repete o começo do toque, o caso da D-118; muitos *começos* com os dedos parados = o contato pisca, e quem segura é o teto). |
| **Testes** | EditMode `TapGuardTests` (10): teto (100 de uma vez → 15; volta depois de 1 s; 20/s por 3 s → 15 em cada segundo; acima do teto sem crítico nem evento; 0 = sem teto), alcance (fora só combo; dentro com o anão correndo; o limite conta), fila de 10 e **a tela nunca vazia** (2 min de anão forte e toque a cada quadro: nenhum cristal quebra fora do alcance, e o maior tempo sem cristal no alcance é o de correr 2 un). PlayMode `TouchFingerTests` (11), com uma tela de toque virtual: 3 dedos parados → 1; aparelho repetindo "Began" a cada quadro → 1; 3 dedos juntos × 4 encostadas com os mesmos ids → 4 (e o Rendering Debugger não abre); um dedo de cada vez, 3 dedos × 2 → 6; dedo novo com outro segurado → não conta; arrastar → 1; toque dentro de um quadro → 1; dois toques no mesmo quadro → 1; 30 de uma vez → 15 valem e 30 faíscas; os valores do jogo; ler os dedos não aloca. Nenhum teste antigo mudou. |

**O que os números mostram:** com o toque só no alcance, o tempo sem cristal no alcance fica em no máximo ~0,5 s (o anão correndo 2 un até o próximo); antes, com o toque a qualquer distância, podia não acabar nunca. Com o D-121 valendo, a fila de 10 muda pouco esse tempo: o que segura o cristal na tela é o alcance; a fila maior garante a reposição 1 por 1 que você pediu. Batendo 60 vezes por segundo, os 15 do teto acabam no primeiro quarto de segundo, e o cristal que entra no alcance depois espera a janela abrir (ou a picareta).

### Como testar (Bruno)

1. Editor fechado: `bash tools/ci/run-tests.sh` (ou o Claude roda em batch e mostra o resultado).
2. `Tools → Miners Idle → Build → Rodar no celular (Android)`; no jogo, compre melhorias pelo DBG (lascas).
3. **3 dedos juntos e parados** na tela por 5 s: conta **1 toque** só (o combo não aparece: "x1" não é mostrado) e a barra de vida desce uma vez.
4. **Bater com os 3 dedos juntos** várias vezes: cada batida vale 1 (o combo sobe de 1 em 1). O Rendering Debugger do URP não abre mais.
5. **Segurar um dedo e bater com outro:** o segundo não conta. **Alternar os dedos** (um sai, o outro encosta): cada um conta.
6. **Bater o mais rápido possível** por 10 s e abrir o DBG logo depois: "Toques/s" mostra no máximo **15 valeram**; "juntos" mostra os dedos que não valeram.
7. Um dedo só, toque normal: igual a hoje.
8. (C2) Bater rápido por 1 min com o anão forte: **sempre há cristal na tela**; nenhum quebra antes de aparecer, e o toque só acerta o cristal quando ele já está inteiro na tela (é o 3,9 un a aprovar).
9. (C2) Comprar o Letrado e PASSAR DE CAVERNA: os cristais antes da parede quebram e só então ela aparece (igual a hoje).
10. **No celular do amigo:** anotar o modelo, segurar 3 dedos parados por uns segundos, abrir o DBG e mandar print das linhas "Toques/s" e "Dedos".

### Critério de pronto

- [ ] Dedo parado vale 1 toque em qualquer aparelho testado (Moto G75 e o do amigo).
- [ ] No máximo 15 toques com dano por segundo, com teste no núcleo.
- [ ] (C2) A tela nunca fica sem cristal fora da passagem; a parede como antes.
- [ ] Nenhum teste apagado; os que mudaram medem o comportamento novo.

**Esforço:** C1 4–6 h · C2 3–5 h.

## Golpe fluido (pedido do Bruno em 2026-11-01) — ✅ aprovado pelo Bruno no Editor em 2026-11-02 ("agora ficou bom"); falta o Moto G75 e o batch

> O Bruno achou pouco fluida a sequência correr → chegar → parar → bater até quebrar → correr. Causa principal: o relógio do golpe zerava na chegada, então o anão chegava e ficava **um intervalo inteiro parado** antes do primeiro golpe (2 s no começo do v5), quase todo numa pose congelada. Decisões: **D-124** (regra: quando sai o primeiro golpe), **D-125** (só apresentação) e **D-126** (regra: o toque no meio da picaretada espera o golpe terminar). Entra antes da E4, que mede o efeito na economia.

### O que foi feito

| Parte | O que mudou |
|---|---|
| **Regra (D-124)** | `ArrivalStrike` no núcleo (`Wait`, `Charge`, `Immediate`), em `MiningRules` e no campo `arrivalStrike` do `MiningRulesAsset` (padrão **Immediate**, a escolha do Bruno em 2026-11-02, em todos os balanços; um asset sem o campo abre nele). `MiningSimulation.ArrivalStrike` (troca no meio da partida) e `SecondsToNextHit` (quanto falta para o próximo golpe, para a apresentação levantar a picareta antes). Charge: o `HitTimer` enche também correndo, até um intervalo, e a chegada não o zera. Immediate: cheio na chegada. `MiningRunner.ArrivalStrike` guarda a escolha do DBG entre reinícios (não salva). |
| **Apresentação (D-125)** | `DescentStage`: hit-stop (o impacto parado e o resto do golpe depois), preparo correndo (quadros de levantar da folha de mineração com a regra ainda correndo), freada (achata e volta), respiração na pose parada, e a arrancada: na parada da quebra a descida desenhada (`ShownDistance`) fica atrás da regra (`DrawnLag`) e alcança com constante de 0,08 s, com a corrida mais rápida enquanto isso. O golpe e o dano continuam na hora da regra; um quadro não aloca. Números no Inspector (cabeçalho "Golpe fluido"). |
| **DBG** | DBG → Câmera → **Golpe** passa por: *Golpe hoje* (Wait, sem efeitos) → *Só efeitos* (Wait + D-125) → *Golpe A1* (Charge + D-125) → *Golpe A2* (Immediate + D-125, o padrão). Não fica salvo. Criado pelo `StrikeFeelSetup`. |
| **Testes** | EditMode `ArrivalStrikeTests` (13): primeiro golpe em cada modo (corrida curta e longa), nunca mais rápido que o intervalo, troca no meio da partida, `SecondsToNextHit` acerta o golpe nos 3 modos, um `Tick` grande = muitos pequenos, e 59 s da caverna 1 (13 / 14 / 23 quebras). PlayMode: `DwarfArtTests` ganhou o teste do preparo e da arrancada (o anão levanta a picareta correndo; a descida alcança a regra antes da chegada); o "correndo nunca mostra a pose parada" passa a aceitar os quadros do golpe com o anão ainda correndo; `MiningSceneTests` passa a medir a posição dos cristais com o atraso desenhado (no máximo a corrida de uma parada). `FeedbackTests` (o do crítico e o da intensidade) param o jogo (`TimeScale = 0`) enquanto medem: com o golpe na chegada, o primeiro golpe automático sai antes de 1 s e as faíscas dele entravam na conta. |

**Correção de 2026-11-02 (achado do Bruno no A2, tocando):** com toques, o anão repetia a picaretada umas 3 vezes e deslizava parado no último quadro do golpe antes de voltar a dar dano. Causa: a picaretada de despedida (D-125 manteve a do pedido antigo) tocava sempre que um toque quebrava o cristal a até 1 m, mesmo com o anão ainda correndo ou já tendo batido nele; ela dura 0,6 s e a corrida até o próximo cristal 0,5 s, então ele chegava "picaretando" e batia de novo. E o levantar da picareta nos últimos passos começava e era interrompido quando o toque quebrava o cristal antes da chegada. Agora: a despedida só toca com o anão **parado** no cristal **antes do primeiro golpe dele** (o caso do pedido antigo, que no A2 não acontece mais); e com o combo ativo o anão não levanta a picareta correndo. Teste novo: `DwarfArtTests.ArrivalStrike_Tapping_RunsStraightWithoutRepeatingTheSwing` (A2, um toque por quadro: correndo, nunca a despedida nem o levantar); o `TapBreak_StillShowsAFullSwing` passa a rodar em Wait, o único modo em que a despedida existe.

**Picaretada inteira com toques (D-126, 2026-11-02):** depois da correção acima, o Bruno achou a picaretada rápida demais, sem o movimento suave da picaretada no cristal vivo: tocando, o cristal quebrava ~0,25 s depois do golpe da chegada e o anão corria na hora, cortando a volta da picareta (~0,7 s). Escolha do Bruno: o anão termina o golpe parado (regra nova). Núcleo: `MinerState.Recovering`, `MiningRules.SwingRecovery` (campo `swingRecovery` = 0,72 s em todos os balanços) e `MiningSimulation.SwingRecovery` (no máximo o intervalo); `Break` sabe se foi o toque (`byTap`) e se a picareta já desceu no alvo. Apresentação: a volta da picareta (`FollowThroughFrame`) é a mesma no cristal vivo e no `Recovering`, com o quadro ajustado para acabar junto com a volta da regra; o levantar com o combo ativo volta a aparecer quando o alvo aguenta os toques que cabem nele (teto de 15/s, sem crítico), e uma vez começado vai até a chegada. Testes: EditMode `SwingRecoveryTests` (8: termina parado e corre; volta já acabada corre; toque antes da chegada corre; quebra pela picareta corre; toques na volta acertam o próximo sem mexer nela; a volta não passa do intervalo; 0 desliga; um `Tick` grande = muitos pequenos com toques); PlayMode `DwarfArtTests.TapBreak_MidSwing_FinishesTheSwingStanding`. **Em batch (2026-11-02, tudo junto: D-124…D-126): setup ok, EditMode 187/187, PlayMode 76/76.**

**O que os números mostram (v5, caverna 1, sem compras):** com 0,5 s de corrida entre cristais, o A1 tira só 0,5 s da espera na chegada (2,0 → 1,5 s); o A2 tira tudo, mas rende ~80% mais lascas/s no começo. Com o intervalo em 0,5 s ou menos (Velocidade 15+), A1 e A2 são iguais: o anão chega batendo.

### Como testar (Bruno)

1. Editor fechado: `bash tools/ci/run-tests.sh` (ou no Editor, `Window → General → Test Runner`).
2. No Unity: `Tools → Miners Idle → Projeto → Aplicar configuração do projeto`, cena `Game`, Play. No celular: `Tools → Miners Idle → Build → Rodar no celular (Android)`.
3. Abrir **DBG → Câmera**: o botão **Golpe** mostra *Golpe A2* (o padrão). Fechar o painel da câmera e olhar alguns cristais.
4. Apertar **Golpe** até *Golpe hoje* e comparar: a espera parada na chegada, sem o impacto segurado.
5. *Só efeitos*: mesma espera, mas com o impacto segurado, a freada, a respiração e a arrancada depois da quebra.
6. *Golpe A2*: o anão chega já batendo, com a picareta subindo nos últimos passos.
7. Comprar Velocidade pelo DBG (5–10 níveis) e repetir 4–6: com golpes rápidos as paradas encurtam junto.
8. Dizer qual modo fica (A1 ou A2) e se algum efeito está forte ou fraco demais (os números ficam no Inspector do `DescentStage`).

### Critério de pronto

- [x] O Bruno escolhe o modo da D-124: **A2** (teste no Editor, 2026-11-02), com a correção do toque abaixo.
- [x] Os efeitos da D-125 e a picaretada inteira da D-126 aprovados no Editor (2026-11-02).
- [ ] Conferido no Moto G75.
- [x] Testes em batch: EditMode 187/187, PlayMode 76/76 (2026-11-02).
- [ ] Nenhum teste apagado; os que mudaram medem o comportamento novo.

**Esforço:** ~3–4 h.
