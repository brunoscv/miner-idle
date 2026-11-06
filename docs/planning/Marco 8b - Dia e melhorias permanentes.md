# Marco 8b — Dia e melhorias permanentes

> Plano de 2026-11-05. **Decisões respondidas pelo Bruno no mesmo dia (D-151…D-159).** Base: GDD v7 ([`docs/gdd v7.pdf`](../gdd%20v7.pdf), D-132) e as decisões já tomadas: **D-137** (o botão de encerrar a run), **D-138** (lascas vermelhas), **D-143** (o padrão é o GDD; as alternativas de custo ficam no DBG → Cálculos). Revê a **D-112** (o jogo continuava na caverna 10, sem reset). Vem **depois do M8** (usa os 100 níveis) e **antes do M7** (as duas permanentes do estagiário e a aba dele na loja entram lá). O M8 foi **aprovado em 2026-11-05 (D-160)**; o teste no Moto G75 dele vai junto com o da E5 deste marco.
>
> Nome do arquivo: "8b" segue o precedente do M1b (um marco que nasceu entre dois números). Se preferir outro número, é só renomear.

## Objetivo

**O loop roguelite do v7: cada run é um dia.** O jogador desce o quanto aguenta. A partir do nível 10, quando quer (ou quando trava), toca em **Descansar**, vê o resumo do dia e quantas **lascas vermelhas** leva, e confirma. A run recomeça do nível 1, sem lascas e sem melhorias. Com as lascas vermelhas ele compra **melhorias permanentes**, que valem em todas as runs seguintes, e no dia seguinte desce mais fundo e mais rápido.

**Para você:** jogar até o nível 15–20, descansar, comprar uma permanente (Sangue da Montanha, por exemplo) e sentir na run nova que o anão quebra mais rápido do que antes.

## As regras (o que o GDD v7 e as decisões já dizem)

**Lascas vermelhas (D-138):** contadas a cada nível alcançado na run, **depois de quebrar a parede** (ao entrar no nível), em toda run.

| Nível alcançado | Lascas vermelhas |
|---|---|
| 1 a 9 | nenhuma |
| 10, 20, 30, 40 | +5 |
| 50, 60, 70, 80, 90, 100 | +10 |
| os outros acima do 10 | +1 |

Uma run até o nível 20 junta 19; até o 25, 24; até o 50, 66; do 1 ao 100, **161**.

**O que isso simplifica no código:** numa run o nível só sobe. Por isso, as lascas vermelhas que o jogador leva se parar agora saem direto do nível atual: são a soma da tabela até ele. O estado da run não precisa de contador novo. O banco (as que ele já levou para casa) fica no estado permanente.

**O fim da run (D-137, D-152):** dois botões na **barra de baixo**.
- **Descansar**, à esquerda: mostra as lascas vermelhas que o jogador leva se parar agora. Só aparece a partir do **nível 10**.
- **PASSAR DE CAVERNA**, à direita: só aparece com o Mapa comprado, como hoje.

Ao tocar em Descansar, abre o **modal** (D-153). Encerrar **zera tudo da run**: as lascas comuns, as melhorias base (Picareta, Velocidade, Mineração, Crítico, Mapa) e o nível, que volta ao 1. **Ficam as melhorias permanentes e as lascas vermelhas.**

**Melhorias permanentes do anão (GDD v7 §4.2, D-158; custo em lascas vermelhas; *n* = a compra da própria melhoria, D-38):**

| Melhoria | Custo da compra *n* | Efeito por nível | Limite | Total |
|---|---|---|---|---|
| **Memória** | `10 + 2^(n−1)` | −10 cristais na trava do Mapa (100 − 10 × nível); no 10, **sem trava** | 10 | 1.123 |
| **Sangue da Montanha** | `10 + 2^(n−1)` | dano final ×(1 + 0,1 × nível) | 20 | ~1,05 mi |
| **Coração Ardente** | `10 + 2^(n−1)` | lascas de cada quebra ×(1 + 0,5 × nível), teto +1000% | 20 | ~1,05 mi |
| **Dedos Nervosos** | `10 + 2^(n−1)` | +5% de chance de crítico, no golpe e no toque | 10 | 1.123 |
| **Alma dos Anões** | `10 + 2^(n−1)` | a run começa com a Picareta no nível *n*, como se comprada | 10 | 1.123 |

Primeiras compras: 11, 12, 14, 18, 26, 42, 74, 138, 266, 522. As duas do estagiário (**Treinar** e **Promoção do Estagiário**) ficam para o M7 (pela D-170, na aba das permanentes, depois das 5 do anão).

**Alternativas no DBG → Cálculos (D-143):**
- **Sangue e Coração:** `10 + 1,5^(n−1)` (total 6.849) ou a linear `10 + 2 × (n−1)` (total 580, a preferência do Bruno).
- **As 4 de 10 níveis:** a linear (total 190).

**O padrão continua o GDD.**

## Decisões (respondidas pelo Bruno em 2026-11-05: D-151…D-159)

1. **D-151: o nome é "Descansar"** (G28 fechada). O texto fica em dados (`FeedbackTexts`). No graybox, o ícone é uma fogueira.
2. **D-152: os botões da barra só aparecem quando valem.**
   - O PASSAR aparece depois do Mapa comprado.
   - O Descansar aparece a partir do nível mínimo de descansar, o 10.
   - Com os dois na tela, ficam lado a lado: Descansar à esquerda, PASSAR à direita.
   - Hoje o PASSAR fica centralizado embaixo (D-115). O mockup da E3 mostra como fica quando só um dos dois está na tela. A recomendação é cada um no seu lado, para o dedo não ter que procurar o botão.
3. **D-153: o modal de Descansar.**
   - Mostra os dados gerais do descanso: nível alcançado, tempo do dia e lascas ganhas na run.
   - Mostra as lascas vermelhas que o jogador leva e quanto o próximo nível somaria ("no próximo nível: ◆ +1"), para ele decidir se para ou continua.
   - Usa o **layout da loja, só que menor**: centralizado e baixo, porque tem pouca informação.
   - Botões **Descansar** e **Continuar**.
   - Escurece e bloqueia o mapa, mas não pausa a mineração (REQ-UI-04).
4. **D-154: resumo do dia** depois de descansar ("Dia 3 · nível 25 · ◆ +24 · banco ◆ 31").
   - **Melhorias permanentes** abre a aba delas.
   - **Começar** fecha o resumo. A run nova já começa por trás dele, com fila e semente novas, como o Reiniciar run do DBG.
   - O número do dia fica no estado permanente e não aparece no HUD.
5. **D-155: a LOJA ganha abas.**
   - 1ª aba: as melhorias da run.
   - **2ª aba: as permanentes.** As que **ainda dá para comprar vêm primeiro**, na ordem da tabela. As **completas** (no limite) ficam **desabilitadas no fim da lista**, quando houver.
   - **3ª aba: as do Estagiário**, que tem melhorias próprias. Ela nasce no M7; aqui a barra de abas já fica pronta para recebê-la.
   - Comprar no meio da run vale na hora.
6. **D-156: nada novo no HUD.** As lascas vermelhas que o jogador leva aparecem no botão Descansar; o banco aparece na aba das permanentes e no resumo.
7. **D-157: a tela do coração (nível 100) ganha Descansar** ao lado de CONTINUAR.
8. **D-158: como os efeitos agem**, conforme a tabela acima. Passam todos pelo `MinerStats` e pela trava do Mapa, então qualquer tela que mostra número (loja, DBG → Cálculos) já sai com eles.
9. **D-159: DBG → Dia.**
   - Atalhos: +10 ◆, +100 ◆, Descansar agora (sem modal), +1 nível em cada permanente, Zerar permanentes.
   - Linha no alto com o banco, as pendentes e o dia.
   - No DBG → Cálculos, as alternativas de custo das permanentes, com o aviso fixo.

## Etapas

### E1 — Núcleo: estado permanente, lascas vermelhas, descansar

- **`PermanentState`** (C# puro, ao lado do `GameState`, nunca dentro dele): banco de lascas vermelhas, níveis das 5 permanentes e dia.
- **Tabela das lascas vermelhas em dados** (`MiningRulesAsset`): os marcos e os valores entram como campos, não escritos no código. Inclui o **nível mínimo de descansar** (10).
- **As 5 linhas permanentes em dados**, com uma fórmula de custo nova na `CostFormula`: `base + taxa^(n−1)`. Hoje ela só tem a exponencial e a linear.
- **Comando Descansar** no núcleo: entrega as pendentes ao banco, soma um dia e devolve um `GameState` novo já com a Alma aplicada. O `MiningRunner` recomeça a simulação com esse estado, como faz o Reiniciar run.
- **Efeitos** no `MinerStats` e na trava do Mapa.
- **Compra de permanente**, com os resultados de sempre (`Purchased`, `TooExpensive`, `Maxed`).
- **Menu DBG → Dia** já nesta etapa, só com os atalhos e a linha de texto. É o que faz a etapa rodar antes da tela pronta.
- **Testes EditMode:**
  - Lascas vermelhas: 161 do 1 ao 100, 24 no 25 e 0 do 1 ao 9.
  - Custos: 11, 12, 14… e total de 1.123.
  - Cada efeito, testado sozinho.
  - Descansar zera a run e mantém as permanentes.
  - Alma no 3: a run começa com a Picareta no 3, e a próxima compra custa 2 × Dano Base.
  - Memória no 10: o Mapa fica liberado sem quebrar nada.

### E2 — Save v4: run e permanente

- O save passa a ter duas partes, `run` (o que já existe) e `permanent`, na versão 4.
- **Migração do v3:** a run fica como está e as permanentes começam zeradas. Ninguém perde nada.
- **Descansar grava na hora**, para que fechar o jogo logo depois não traga a run antiga de volta.
- **No DBG:** Apagar save apaga as duas partes; Zerar permanentes apaga só a segunda.
- **Testes EditMode:**
  - Salvar e carregar as duas partes.
  - Um save v3 abre com as permanentes zeradas.
  - Um save corrompido na parte permanente zera só essa parte.
- **Teste PlayMode:** descansar, reabrir pelo caminho do Bootstrap e conferir o banco.

### E3 — A barra de baixo, o modal e o resumo do dia

- **Primeiro, o mockup em 270×480** (D-152, D-153, D-154), aprovado pelo Bruno antes do código.
- A barra com Descansar e PASSAR. O setup do projeto migra a cena: o PASSAR sai do centro e vai para a direita.
- O modal no layout da loja, menor e centralizado, e o resumo do dia.
- A tela do coração ganha o botão Descansar (D-157).
- Textos em dados.
- **Testes PlayMode:**
  - O Descansar não aparece no nível 9. No 10 aparece com ◆ 5; no 11, com ◆ 6.
  - O PASSAR só aparece com o Mapa comprado.
  - O modal mostra o nível, o tempo, as lascas da run, as ◆ que o jogador leva e as do próximo nível.
  - Continuar não muda nada; confirmar zera a run e soma o banco.
  - Um toque fora da barra ainda bate no cristal.

### E4 — A aba das permanentes na LOJA

- **Primeiro, o mockup em 270×480** (D-155).
- A barra de abas na loja: Run e Permanentes. A barra já fica pronta para a do Estagiário, no M7.
- **Na aba Permanentes:** o banco no alto e as 5 linhas com nível, efeito, custo e Comprar. As completas ficam no fim, desabilitadas.
- O "!" da LOJA acende também quando alguma permanente pode ser comprada.
- A aba abre também pelo resumo do dia.
- **Testes PlayMode:**
  - Comprar Sangue tira ◆ 11 do banco e o dano sobe 10% na hora.
  - O botão fica apagado sem ◆ suficiente.
  - Uma linha que chega ao limite desce para o fim da lista, desabilitada.

### E5 — Alternativas no DBG → Cálculos e o celular

> **2026-11-05 (D-163, D-173):** esta etapa passa a ser feita junto com a **E4 do [M7](Marco%207%20-%20Estagiario.md)** (o mesmo menu Cálculos e o mesmo teste no Moto G75). O Marco 8b e o M7 são aprovados juntos no fim.

- **Seletores das permanentes no menu Cálculos** (D-143, D-159):
  - Sangue e Coração: GDD · `1,5^` · linear.
  - As 4 de 10 níveis: GDD · linear.
- Gravados no `calculations.json`, como os da Picareta, e mostrados no aviso fixo.
- **Teste no Moto G75** com o fluxo inteiro: jogar → descansar → comprar → run nova.

## Como testar (Bruno) — ao fim de cada etapa

Cena `Assets/_Project/Scenes/Bootstrap.unity` (o caminho com save) ou `Game.unity` (sem save), Play.

**E1**
1. Use DBG → Caverna → Próxima caverna até chegar ao nível 12.
2. Abra DBG → Dia: a linha mostra "pendentes ◆ 7".
3. Toque em Descansar agora: o nível volta ao 1, sem lascas e sem melhorias. O banco fica em ◆ 7 e o dia passa para 2.
4. Toque em +100 ◆ e depois em +1 nível em cada permanente: o dano sobe 10% e a Picareta começa no nível 1.

**E2**
1. No Bootstrap, descanse, pare o Play e dê Play de novo: o banco, as permanentes e o dia continuam lá.
2. Com um save antigo, o jogo abre normalmente.

**E3**
1. No nível 9, confira que o Descansar não aparece.
2. Chegue ao nível 11 e toque em Descansar: o modal, pequeno e no centro, mostra os dados do dia, ◆ 6 e "no próximo nível: ◆ +1".
3. Toque em Continuar: nada muda.
4. Toque em Descansar de novo e confirme: aparece o resumo do dia.
5. Confira que o PASSAR só aparece depois do Mapa.

**E4**
1. Abra LOJA → Permanentes e compre Sangue por ◆ 11.
2. Volte à run: o dano na loja subiu 10%.
3. Use o DBG para levar uma linha ao limite: ela desce para o fim, desabilitada.

**E5**
1. Em DBG → Cálculos, mude o custo de Sangue para "linear": a próxima compra muda e o aviso fica âmbar.
2. No G75, faça o fluxo inteiro.

## Critério de pronto

- Uma run termina por Descansar (a partir do nível 10), com o modal.
- As lascas vermelhas da tabela da D-138 entram no banco, e a run seguinte começa do nível 1 com as permanentes valendo.
- As 5 permanentes do anão podem ser compradas na 2ª aba da LOJA, com o custo do GDD, o efeito da D-158 e a ordem da D-155.
- As alternativas da D-143 estão no DBG.
- O save v4 guarda a run e a parte permanente, e o v3 migra sem perda.
- Os atalhos do DBG → Dia funcionam.
- Os testes EditMode e PlayMode passam, e o fluxo foi testado no Moto G75.
- README e decisões atualizados.

## Riscos

- **Custo das linhas de 20 níveis no GDD (~1 mi de ◆).** Já é sabido (D-143). O DBG deixa o time testar a linear, e o relatório de economia mede isso quando for a hora (a E4 do M8, adiada pela D-149).
- **Recomeçar a run sem deixar sobra.** Podem sobrar a fila de cristais, uma parede de pé, a tela do coração e o combo. O Reiniciar run do DBG (E3b-3) já resolve a maior parte, e os testes da E1 cobrem o resto.
- **Espaço na tela 270×480.** A barra de baixo e o modal disputam espaço com a área de toque e com o anão no Recuo (D-148). O mockup decide antes do código.
- **Save.** Esta é a primeira migração com duas partes. A E2 inclui o teste com um save v3 de verdade, gravado antes da mudança.

## Fora deste marco

- Treinar, Promoção do Estagiário e a 3ª aba da loja (M7).
- O offline (M9).
- Bônus temporários e cristal raro (D-89).
- Relógio de verdade: o "dia" é a run, e a montanha não se regenera pelo horário.
- Arte final dos botões e do ícone da lasca vermelha (M10). Por enquanto, graybox e, se servir, um recorte vermelho do cristal que já existe.

## Esforço

Por volta de **25–30 h** no total, umas 1,5 semana a 20 h/semana.

| Etapa | Horas |
|---|---|
| E1 | 7–8 |
| E2 | 3–4 |
| E3 (com o mockup) | 6–7 |
| E4 (com o mockup) | 6–7 |
| E5 | 2–3 |
| Teste no G75 | 1 |

## Estado

- **2026-11-05:** plano escrito; as decisões 1–9 foram respondidas pelo Bruno no mesmo dia (D-151…D-159). 
- **2026-11-05:** M8 aprovado (D-160).

| Etapa | Estado | Notas |
|---|---|---|
| E1 Núcleo e DBG → Dia | 🧪 implementada em 2026-11-05, aguardando validação do Bruno | Core: `PermanentId` (as 5 do anão), `RedShardTable` e `PermanentRules` (nível mínimo de descansar, tabela, linhas), `PermanentState` (banco, dia, níveis), `Permanents` (efeitos da D-158, compra, descanso, run nova com a Alma). Custo novo `CostFormula.OffsetExponential` (`inicial + crescimento^nível`). `MinerStats`, `Purchases` (trava `MapLock`) e `UpgradePreview` recebem as permanentes. `MiningSimulation`: `CanRest`, `PendingRedShards`, `NextLevelRedShards`, `Rest`, `PurchasePermanent`, `MapLock`, atalhos do debug e o evento `PermanentsChanged`. `MiningRunner`: o `PermanentState` fica de uma run para a outra; `Rest()` e o evento `Rested` (sai depois do `SimulationStarted` da run nova); Reiniciar run aplica a Alma; Apagar save zera as permanentes. Dados: `MiningRulesAsset` com o descanso e as 5 linhas (desligados por padrão; o setup grava os do GDD no `Balance_GDD_v7`). DBG → **Dia** (`DayDebugPanel`): +10/+100 vermelhas, Descansar agora, +1 nv permanentes, Zerar permanentes e o resumo no alto. (O save veio na E2.) Testes: EditMode `PermanentTests` (+15), PlayMode `DayDebugPanelTests` (+3), `DebugPanelTests` mede o botão Dia. Em batch: setup ok, EditMode **250/250**, PlayMode **94/94** |
| E2 Save v4: run e permanente | 🧪 implementada em 2026-11-05, aguardando validação do Bruno | `SaveData.CurrentVersion = 4`: os campos da run como até a 3 e a parte `permanent` (banco de lascas vermelhas, dia e níveis pelo nome do `PermanentId`). Até a 3 abre com a run como estava e as permanentes zeradas, no dia 1. Valores fora do lugar na parte permanente são corrigidos só nela (`PermanentState.Restore`); num balanço sem permanentes (o v5 do DBG → Cálculos) o nível guardado fica, sem efeito ali. `SaveService.Load(economy, permanentRules, out permanent, …)` e `Save(state, permanent)`. O `MiningRunner` carrega as duas partes no `UseSave` e grava no descanso e em toda compra ou atalho das permanentes (`PermanentsChanged`), além das gravações de sempre; Apagar save apaga as duas, Zerar permanentes grava a parte zerada. **Achado:** com as gravações seguidas, o `File.Replace` do Windows às vezes falhava por um instante (outro processo segurando o arquivo); o `FileSaveStore` tenta a troca até 4 vezes (15–45 ms), e o save anterior fica inteiro enquanto isso. Testes: EditMode `SaveServiceTests` (+4; os 3 que conferiam a versão 3 passaram a medir a 4), PlayMode `SaveFlowTests.RestAndPermanents_SurviveClosingAndReopening`. Em batch, duas rodadas: EditMode **254/254**, PlayMode **95/95** |
| E3 Barra de baixo, modal e resumo do dia | 🧪 implementada em 2026-11-05, aguardando validação do Bruno | Mockup em 270×480 aprovado pelo Bruno (D-161). **Barra de baixo:** `RestButton` no lado esquerdo (madeira, cristal vermelho, "DESCANSAR" e embaixo as lascas vermelhas que leva), só a partir do nível 10; o PASSAR (`CavePassButton`, sem mudança de regra) vai para o lado direito, com o nome em duas linhas; os dois com 126×36, sempre no seu lado. **Modal** (`RestScreen`): a janela da loja, 236×190, no meio; dia, nível alcançado, tempo do dia, lascas da run, "Você leva" e "No próximo nível: +n" (no último nível, um aviso no lugar); acompanha a run aberto (o tempo, uma vez por segundo); CONTINUAR fecha, DESCANSAR chama o `MiningRunner.Rest()` e abre o resumo. **Resumo do dia** (`DaySummaryScreen`): título "DIA n ENCERRADO", nível, tempo, lascas vermelhas ganhas e o banco; COMEÇAR fecha. O botão PERMANENTES do mockup chega na E4, com a aba. O atalho Descansar agora do DBG não abre o modal nem o resumo. **Tela do coração** (D-157): DESCANSAR ao lado de CONTINUAR, que abre o modal. **DBG:** o botão e o aviso do cálculo subiram para cima da barra (y = 54). Textos em `FeedbackTexts`. Cena montada pelo setup (`RestSetup`, `HudSkinSetup.PlacePassCaveOnTheRight`, `DebugPanelSetup.LiftAboveTheBar`), idempotente. Testes: PlayMode `RestFlowTests` (+7: Descansar no 9/10/11 com 5 e 6, PASSAR só com o Mapa e cada um no seu lado, modal e Continuar, Confirmar e o resumo, o atalho do DBG, toque fora da barra, coração). Em batch: EditMode **254/254**, PlayMode **102/102** |
| E4 Aba das permanentes na LOJA | 🧪 implementada em 2026-11-05, aguardando validação do Bruno | Mockup em 270×480 aprovado pelo Bruno (D-162). **Núcleo:** `PermanentPreview` (nível, limite, custo em lascas vermelhas, estado do botão, efeito "de → para") e `MiningSimulation.PreviewPermanent`. **Loja** (`ShopView`): abas MELHORIAS e PERMANENTES; a LOJA abre na das melhorias e `OpenPermanents()` na das permanentes; na das permanentes, o banco no alto e as linhas (`PermanentRowView`, prefab `PermanentRow` copiado da linha da loja, com o cristal vermelho no preço) na ordem da tabela, as que chegaram ao limite no fim, apagadas, com "máx."; compra vale na hora. O "!" da LOJA acende também com uma permanente para comprar; o de cada aba, quando ela não é a aberta. Sem permanentes no balanço (GDD v5), a aba some. **Resumo do dia:** PERMANENTES à esquerda do COMEÇAR, abre a loja na aba. Textos no `ShopCatalog` (abas e linhas, com o formato do efeito e o do limite) e no `FeedbackTexts`; o formatador do efeito passou para o `ShopCatalog.FormatValue`, o mesmo nas duas abas. Cena montada pelo setup (`PermanentShopSetup`), idempotente. **DBG:** os atalhos do DBG → Dia (+10/+100 ◆, +1 nível em cada permanente, Zerar) já servem para a aba. Testes: EditMode `PermanentTests` (+2, a prévia), PlayMode `PermanentShopTests` (+6: abas, comprar Sangue tira 11 e o dano sobe 10%, botão apagado sem ◆, linha no limite no fim, o "!" com as permanentes, o PERMANENTES do resumo). Em batch: EditMode **256/256**, PlayMode **108/108** |
