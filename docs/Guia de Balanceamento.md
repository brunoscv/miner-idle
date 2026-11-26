# Guia de balanceamento — onde ficam as fórmulas e como mexer

> Para quem vai testar e ajustar a jogabilidade (custos, danos, tempos) sem precisar ler o código todo.
> Escrito em 2026-11-19 com o jogo no GDD v8.2; **atualizado em 2026-11-25 para o GDD v8.4** (M9j, bloco 1: balanço ativo `Balance_GDD_v84`; M9k, bloco 2: lascas pelo dano, pagas a cada golpe). Os blocos 3 a 7 do v8.4 (estagiário só offline, bomba, raro e anúncios, tiers, automação) ainda não entraram: onde o guia fala deles, vale o v8.2.

## 1. Resumo em 30 segundos

- **Quase todo número do jogo está em um arquivo só:** `Assets/_Project/Data/Balance_GDD_v84.asset`. É um ScriptableObject e se edita no **Inspector** do Unity (clicar no asset na janela Project).
- **As fórmulas** (o "jeito" de fazer a conta) ficam em C#, no núcleo `Assets/_Project/Scripts/Core/`. Os **números** que entram nelas vêm do asset. Para balancear, quase sempre basta mexer nos números; só se mexe no C# para mudar o *formato* da conta.
- **Não edite o `Balance_GDD_v84` direto.** Duplique e teste na cópia (ver §2). Motivos: testes automáticos leem os balanços do GDD, o setup do projeto repõe alguns campos, e o Bruno registra toda mudança de valor como decisão (`docs/planning/Decisoes e Riscos.md`).

## 2. Como testar um ajuste (passo a passo seguro)

1. Abra o projeto no Unity 6000.6.0f1.
2. Na janela **Project**, vá em `Assets/_Project/Data/`, clique em `Balance_GDD_v84` e aperte **Ctrl+D**. Renomeie a cópia, por exemplo `Balance_Teste`.
3. Abra a cena `Assets/_Project/Scenes/Game.unity`.
4. Na **Hierarchy**, ache o objeto que tem o componente **Mining Runner** (use a busca da Hierarchy: `t:MiningRunner`). No campo **Rules**, arraste o `Balance_Teste`. Salve a cena.
   - O setup do projeto só troca esse campo se ele estiver num balanço antigo (protótipo, v5, v7 ou v8.2); um asset seu fica.
5. Clique no `Balance_Teste` e mude os valores no Inspector. Cada campo tem **tooltip** (passe o mouse no nome) dizendo o que faz e o valor do GDD.
6. Aperte **Play**. Os valores são lidos quando o jogo começa: mudou algo, pare e dê Play de novo.
7. Se o save antigo atrapalhar (níveis comprados com o preço velho, andar alto demais), use o painel **DBG** do jogo → apagar o save / **Reiniciar run**. Para pular a subida: **DBG → Picareta / Velocidade / Mineração / Crítico +10** (níveis de graça), **Ir ao andar N** (os marcos), **DBG → Dia → +1 nv permanentes** e **+100 vermelhas**.
8. Gostou? Leve os números para o Bruno. Ele registra a decisão e passa os valores para o `Balance_GDD_v84` (e corrige os testes que medem o valor antigo).

**Alternativa rápida para a Picareta e a distância dos cristais:** o menu **DBG → Cálculos**, dentro do jogo, troca a taxa e o incremento da Picareta (passos de 0,01 e 1) e a distância entre cristais (3 m ⇄ 1 m) sem mexer em asset nenhum (vale só no aparelho/Editor de quem trocou; "Padrão GDD" volta tudo).

### Como cada linha de melhoria é descrita no asset

Toda melhoria (Picareta, Velocidade, Memória…) é um bloco com 5 campos:

| Campo | O que é |
|---|---|
| **Cost Formula** | O formato do custo (tabela abaixo) |
| **Base Cost** | Custo da 1ª compra (ou o multiplicador, na DamageScaled) |
| **Cost Growth** | Quanto o custo cresce por compra |
| **Max Level** | Número máximo de compras. **0 desliga a linha** (some da loja) |
| **Effect** | O que cada nível dá; o significado depende da linha (ver §4) |

`n` = número de compras já feitas (começa em 0). O custo mostrado é o da **próxima** compra.

| Cost Formula | Conta do custo | Quem usa hoje |
|---|---|---|
| **Exponential** | `Base Cost × Cost Growth^n` | Picareta, Velocidade, Mineração, Crítico, Estagiário e a árvore dele |
| **Linear** | `Base Cost + Cost Growth × n` | Bombardeador e Olho Treinado (v8.4) |
| **OffsetExponential** | `Base Cost + Cost Growth^n` | as outras permanentes (lascas vermelhas): `10 + 1,5^n` = 11, 11,5, 12,25… |
| **DamageScaled** | `Base Cost × (dano atual da Picareta)` | ninguém no v8.4 (era a Picareta do v8.2) |

## 3. Onde fica cada coisa (arquivos)

| Arquivo | O que tem | Mexe para balancear? |
|---|---|---|
| `Assets/_Project/Data/Balance_GDD_v84.asset` | **Todos os números**: cristal, andares, anão, loja, estagiário, crítico, raro, anúncios, bomba, descanso, permanentes e marcos de andar | **Sim** (numa cópia) |
| `Assets/_Project/Scripts/Game/Data/MiningRulesAsset.cs` | A classe do asset acima: os campos, tooltips e valores padrão; converte o asset nas regras do núcleo | Só para criar campo novo |
| `Assets/_Project/Scripts/Core/Mining/MiningRules.cs` | Fórmulas de vida e lascas do cristal por andar, requisito do Próximo Andar, teto do offline, marcos de andar | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Economy/UpgradeDefinition.cs` | As 4 fórmulas de custo (tabela acima) | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Economy/MinerStats.cs` | Dano, intervalo, corrida, multiplicador de lascas e crítico **do anão** | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Economy/InternStats.cs` | Dano, intervalo e lascas offline **do estagiário** | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Economy/Permanents.cs` | Efeito de cada permanente (Sangue, Coração, Memória, Atalho…) | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Economy/PermanentRules.cs` | Tabela das lascas vermelhas por andar (`RedShardTable`) | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Economy/FloorMilestones.cs` | Os marcos 15, 45 e 60 do v8.4 (estagiário no começo do dia, vermelhas ×2, −100 cristais) | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Economy/Purchases.cs` | Regra de compra; a Velocidade vira MÁX quando o golpe chega ao piso (D-371) | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Mining/MiningSimulation.cs` | O loop: aplica bônus do raro/anúncio, rola o crítico, multiplicador do crítico (linhas ~850–870 e ~1290–1315) | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Mining/BombRules.cs` | Regras da bomba | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Economy/OfflineMining.cs` | Conta do que o estagiário minera com o app fechado | Só para mudar o formato |
| `Assets/_Project/Data/ShopCatalog.asset` | **Textos e ícones** da loja e das permanentes (título, formato do efeito) — não tem número de regra | Só se mudar o texto |
| `Assets/_Project/Data/BuffCatalog.asset`, `FeedbackTexts.asset` | Textos e ícones dos bônus e mensagens | Não |
| `Assets/_Project/Data/Biomes/Caverna_01…10.asset` | **Só visual** (texturas, cor, névoa) de cada bioma | Não |
| `Balance_GDD_v1/v5/v7/v8`, `Balance_Prototipo` | Balanços antigos (o `v8` é o GDD v8.2), só para comparação; **o jogo não usa** | Não |

## 4. Mapa: fórmula → funcionalidade

Nomes em **negrito** são os campos do asset como aparecem no Inspector (o Unity mostra `pickaxeCaveGrowth` como "Pickaxe Cave Growth"). Os valores são os do `Balance_GDD_v84` hoje.

**Unidades:** distância em "unidades da trilha", **1 m = 2 un**. Tempo em segundos. `andar` começa em 1.

### 4.1 Cristal e andares

| Funcionalidade | Fórmula | Campos (valor atual) | Código |
|---|---|---|---|
| **Vida do cristal** no andar | `Crystal Health × Cave Health Growth^(andar − 1)` | **Crystal Health** 1 · **Cave Health Growth** 1,5 | `MiningRules.CrystalHealthIn` |
| **Lascas (moeda) por cristal** | `vida do andar` × multiplicador de lascas (§4.3), **pagas a cada golpe pelo dano causado** (§4.3); a quebra conta, mas não paga (D-348) | **Reward Formula** Health (**Crystal Reward** e **Cave Reward Growth** não valem nela; no v8.2 era FloorScaled: `vida × 0,2 × max(1, andar − 1)`, pago na quebra) | `MiningRules.CrystalRewardIn`, `MiningRules.PaysPerHit` |
| **Número de andares** | — | **Cave Count** 100 | |
| **Bioma** (troca de visual) | a cada **Levels Per Biome** andares | **Levels Per Biome** 10 | `MiningRules.BiomeOf` |
| **Cristais para o Próximo Andar** | `andar × Floor Breaks − Memória − (Floor Discount com o recorde no Floor Discount Floor)` (mín. 0) | **Floor Breaks** 5 · **Floor Discount Floor** 60 · **Floor Discount** 100 | `Permanents.FloorRequirement` |
| **Fila de cristais** | começa com **Initial Crystals**; nasce 1 a cada **Spawn Interval** s até **Max Crystals**; abaixo de **Min Crystals** nasce na hora | 10 · 5 s · 12 · 10 | `CrystalTrack.cs` |
| **Distância entre cristais** | `Min Crystal Spacing − Atalho × 0,5 m` (mín. 10% da base) | **Min Crystal Spacing** 6 un (3 m) | `Permanents.CrystalSpacing` |
| Avanço automático / estagiário / raro liberam no andar | recorde de andar ≥ valor | **Auto Advance Floor** 3 · **Intern Floor** 1 · **Rare Floor** 10 | `MiningRules.Is…Unlocked` |
| Marcos do v8.4 (para sempre, pelo recorde) | dia começa com o estagiário · vermelhas ×2 · −100 cristais | **Intern Start Floor** 15 · **Red Shards Double Floor** 45 · **Floor Discount Floor** 60 | `FloorMilestones` |

Exemplo (vida / lascas do cristal inteiro sem bônus): andar 1 → 1 / 1 · andar 3 → 2,25 / 2,25 · andar 10 → 38,4 / 38,4 · andar 20 → 2.217 / 2.217. No v8.2 eram 0,2 · 0,9 · 69,2 · 8.424: o começo rende mais e, do andar 10 em diante, menos.

### 4.2 Anão: dano, velocidade, corrida

| Funcionalidade | Fórmula | Campos (valor atual) | Código |
|---|---|---|---|
| **Dano da Picareta** | `Damage + Pickaxe.Effect × n + Pickaxe Cave Growth^n` (n = compras) × Sangue da Montanha × bônus ativos | **Damage** 1 · **Pickaxe.Effect** 0 (o "incremento") · **Pickaxe Cave Growth** 1,2 (a "taxa") · **Pickaxe Formula** LevelScaled | `MinerStats.Compute` |
| **Custo da Picareta** | `5 × 1,5^n` (Exponential) | **Pickaxe**: Base Cost 5 · Cost Growth 1,5 · Max Level 199 (o nível 200) | `UpgradeDefinition.CostAt` |
| **Intervalo entre golpes** (Velocidade) | `max(Min Hit Interval, Hit Interval − Speed.Effect × n − Tempo Curto)` ÷ bônus; no piso, a linha vira MÁX (D-371) | **Hit Interval** 2 s · **Speed**: Effect 0,01 · Max 100 · **Min Hit Interval** 0,1 s | `MinerStats.Compute` |
| **Custo da Velocidade** | `10 × 3^n` | **Speed**: Base Cost 10 · Cost Growth 3 | |
| **Corrida** | `Walk Speed × (1 + Boots.Effect × n)` | **Walk Speed** 4 un/s · **Boots** desligada (Max 0) | |
| Primeiro golpe ao chegar no cristal | Immediate = bate na chegada | **Arrival Strike** Immediate | |
| Volta da picareta | segundos parados depois do golpe se o cristal quebrou | **Swing Recovery** 0,72 | |
| Picareta Dupla | ×Effect no dano | **Double Pickaxe** desligada (Max 0) | |

Exemplo da Picareta (dano / custo da próxima): 0 compras → 2 / 5 · 5 → 3,49 / 38 · 10 → 7,19 / 288 · 20 → 39,3 / 16.626 · 199 (máx.) → ~5,7 × 10¹⁵. Velocidade: 10, 2.430 na 6ª compra, 590.490 na 11ª.

### 4.3 Lascas (moeda) e crítico

| Funcionalidade | Fórmula | Campos (valor atual) | Código |
|---|---|---|---|
| **Lascas de um golpe** (v8.4) | `min(dano, vida que restava) × (lascas do cristal ÷ vida dele) × multiplicador de lascas`, na hora do golpe; vale para o golpe do anão, do estagiário na tela e da bomba (D-373). O dano além da vida não paga; o multiplicador é o **de agora** (uma carta ou compra vale já no golpe seguinte) | (vem da vida, §4.1, e do multiplicador abaixo) | `MiningSimulation.PayHit` |
| **Multiplicador de lascas** | `1 + Mining.Effect × n + Coração Ardente + Mineração do estagiário` × cartas/anúncios | **Mining**: Effect 0,1 (+10%/nível) · Max 200 (sem teto além do nível máximo) | `MinerStats.Compute` |
| **Custo da Mineração** | `10 × 2,5^n` | **Mining**: Base Cost 10 · Cost Growth 2,5 | |
| **Chance de crítico** | `Crit Chance + Critical.Effect × n + Dedos Nervosos + cartas/anúncios` (máx. do v8.4: 50% + 50% = 100%) | **Crit Chance** 0 · **Critical**: Effect 0,01 · Max 50 | `MiningSimulation.CritChance` |
| **Custo do Crítico** | `10 × 2,5^n` | **Critical**: Base Cost 10 · Cost Growth 2,5 | |
| **Dano do crítico** | `(Crit Multiplier + Dedo Fogoso) × cartas/anúncios de Dano de Crítico`; a chance acima de 100% não dá mais dano (D-366) | **Crit Multiplier** 1,5 | `MiningSimulation.CritMultiplier` |

### 4.4 Estagiário (2º minerador)

| Funcionalidade | Fórmula | Campos (valor atual) |
|---|---|---|
| **Contratar** | compra única, desde o andar 1 (**Intern Floor** 1) | **Intern**: Base Cost 500 · Max 1 (0 = sem estagiário) |
| **Dano dele** | `Intern Pickaxe.Effect × Intern Pickaxe Damage Growth^n × (1 + Treinar)` | **Intern Pickaxe**: Effect 5 · **Intern Pickaxe Damage Growth** 2 |
| Custo da Picareta dele | `50 × 1,5^n`, máx. 100 | **Intern Pickaxe**: Base Cost 50 · Cost Growth 1,5 |
| **Intervalo dele** | `max(Min Hit Interval, Hit Interval − Intern Speed.Effect × n)`; no piso, MÁX | **Intern Speed**: Effect 0,01 · custo `50 × 2,5^n` · máx. 100 |
| **Mineração dele** | +Effect nas lascas (online soma no multiplicador do anão; offline multiplica o que ele traz) | **Intern Mining**: Effect 0,1 · custo `50 × 2,5^n` · máx. 100 (+1000%) |

O Crítico dele (`20 × 2,5^n`, +1%, 50) entra no bloco 3, com o estagiário só offline (D-368).

Código: `InternStats.Compute`. O crítico e o Sangue da Montanha **não** valem para ele.

### 4.5 Offline (app fechado)

| Funcionalidade | Fórmula | Campos (valor atual) |
|---|---|---|
| Teto de tempo que conta | 4 h; 6 h depois de chegar ao andar 20; + horas dos anúncios ativos ao fechar, até o teto dos anúncios | **Offline Max Hours** 4 · **Offline Deep Hours** 6 · **Offline Deep Floor** 20 · **Ad Offline Cap Hours** 8 |
| O que rende | só o estagiário minera: cristais = tempo ÷ (corrida + ⌈vida ÷ dano⌉ golpes); cada cristal rende as lascas dele inteiro (no v8.4, a vida) × Mineração dele | (vem das linhas do estagiário) |

Código: `OfflineMining.cs`.

### 4.6 Bomba

| Funcionalidade | Fórmula | Campos (valor atual) |
|---|---|---|
| **Dano da bomba** | `fração × vida máxima` de cada cristal no alcance (sem crítico nem bônus); no v8.4 esse dano paga lascas como um golpe, com o multiplicador (D-373) | **Bomb Fractions** 0,1 · 0,2 · 0,3 · 0,4 · 0,5 · 0,6 · 0,7 · 1 (a 1ª é de graça; cada compra da permanente Bomba passa para a próxima) |
| Recarga / voo / alcance | recarga = `Bomb Charge Seconds − Bombardeador` (mín. 10%) | **Bomb Charge Seconds** 5 · **Bomb Flight Seconds** 0,4 · **Bomb Reach** 7 un (3,5 m) |

Código: `BombRules.cs` e `MiningSimulation`.

### 4.7 Cristal raro (as cartas)

| Funcionalidade | Fórmula | Campos (valor atual) |
|---|---|---|
| Quando nasce | a cada **Rare Interval** s de run, depois do andar **Rare Floor** (0 = usa **Rare Chance** por cristal) | **Rare Interval** 60 · **Rare Chance** 0,15 (só sem contador) |
| Tempo para escolher | depois disso o jogo sorteia | **Rare Choose Seconds** 30 |
| **Cartas (tier 1)** | Damage = dano ×valor · Shards = lascas ×valor · Critical = +valor na chance · CritDamage = dano do crítico ×valor · Speed = intervalo ÷valor | **Rare Cards**: Damage 2 · Shards 2 · Critical 0,1 · CritDamage 2, 30 s cada |
| **Cartas (tier 2)** | com o Raro Dourado ou o 4º anúncio | **Rare Tier 2 Cards**: 4 · 4 · 0,2 · 4, 30 s |

Código: `Bonuses.cs`, `RareWindow.cs`, `MiningSimulation`.

### 4.8 Anúncios (Boost)

| Anúncio | Efeito | Campos (valor atual) |
|---|---|---|
| Curto / Médio / Grande | dano, lascas e dano do crítico ×Multiplier; +Crit Chance; dura Minutes; soma Offline Hours ao teto do offline | **Ad Offers**: ×2 / +0,10 / 20 min / +1 h · ×3 / +0,15 / 40 min / +2 h · ×4 / +0,20 / 80 min / +4 h |
| 4º anúncio | cartas do raro no tier 2 | **Ad Rare Tier 2 Minutes** 60 |

**Ad Speed Divisor** e **Ad Boost Seconds** são do v7 e ficam em 0 (desligados).

### 4.9 Descansar, lascas vermelhas e permanentes

**Lascas vermelhas ganhas ao descansar** = soma, do andar **Rest Level** até o andar alcançado, de: **Red Shard Per Level** (1) por andar; nos múltiplos de **Red Shard Milestone Step** (10) vale **Red Shard Milestone** (5); a partir de **Red Shard Big Milestone From** (50), o múltiplo vale **Red Shard Big Milestone** (10). Ex.: chegar ao andar 20 dá 5 + 9 + 5 = 19. Depois, `× (1 + Olho Treinado) × (2 com o recorde no Red Shards Double Floor, 45)` (D-370). Código: `RedShardTable` em `PermanentRules.cs` e `Permanents.Pending`.

Permanentes (custo em lascas vermelhas; a maioria `Base Cost + Cost Growth^n`):

| Permanente | Campo | Efeito por nível (Effect) | Máx. | Onde age |
|---|---|---|---|---|
| Memória | **Memory** | −5 cristais no requisito do Próximo Andar (custo `10 + 1,1^n`) | 100 | `FloorRequirement` |
| Sangue da Montanha | **Mountain Blood** | +100% no dano final (`10 + 1,5^n`) | 40 | `DamageMultiplier` |
| Coração Ardente | **Burning Heart** | +100% de lascas (`10 + 1,5^n`) | 40 | `RewardBonus` |
| Dedos Nervosos | **Nervous Fingers** | +5% de chance de crítico (`10 + 1,5^n`) | 10 | `CritBonus` |
| Dedo Fogoso | **Fiery Finger** | +0,05 no dano do crítico, mostrado "+5%" (`10 + 1,5^n`) | 40 | `CritDamageBonus` |
| Alma dos Anões | **Dwarf Soul** | a run começa com +1 nível de Picareta (`5 + 1,1^n`) | 100 | `StartingPickaxe` |
| Treinar Estagiário | **Intern Training** | +100% no dano do estagiário (`10 + 2^n`) | 10 | `InternDamageMultiplier` |
| Promoção do Estagiário | **Intern Promotion** | **desligada no v8.4** (Max 0): o marco do andar 15 faz o mesmo (D-367) | 0 | `StartsWithIntern` |
| Atalho | **Shortcut** | −0,5 m entre cristais (`10 + 5^n`) | 5 | `CrystalSpacing` |
| Raro Dourado | **Golden Rare** | compra única (500): cartas no tier 2; abre no andar do raro | 1 | `HasGoldenRare` |
| Bomba | **Bomb** | passa para a próxima bomba da lista (ainda o v8.2: `10 + 2^n`; o v8.4 entra no bloco 4, D-369) | 7 | `BombLevel` |
| Tempo Curto | **Short Time** | −0,1 s no intervalo do golpe (`10 + 2^n`) | 10 | `ShortTimeSeconds` |
| Bombardeador | **Bomber** | −0,5 s na carga da bomba (Linear `15 + 5n`) | 4 | `BombChargeReduction` |
| Olho Treinado | **Trained Eye** | +100% de lascas vermelhas (Linear `10 + 5n`) | 10 | `Pending` |

Todas em `Assets/_Project/Scripts/Core/Economy/Permanents.cs`.

## 5. Cuidados

- **Max Level 0 desliga** a linha (some da loja). É o jeito certo de tirar uma melhoria do teste.
- **Valores inválidos desligam o sistema, com aviso no Console** (`[Valores] …`): fração da bomba fora de 0–1, carta repetida, anúncio sem minutos etc. Se uma coisa "sumiu", olhe o Console.
- **Setup repõe alguns campos** quando roda (menu `Tools → Miners Idle → Projeto` e todo build): o `Balance_GDD_v84` só nasce se não existe (apagar ele faz o setup criar de novo a partir do v8.2 com os números do v8.4); no v84, zerar o **Intern Start Floor** ou o Max Level do **Short Time** faz o setup preencher de novo os marcos ou as permanentes novas. Na cópia (`Balance_Teste`) isso não acontece.
- **Testes automáticos** (`Tests/EditMode/GddV8RulesTests.cs`, `CalculationTests.cs`, `AdOffersTests.cs`, `RareCounterTests.cs`, `PlayMode/CalculationsPanelTests.cs`) leem o `Balance_GDD_v8` (o v8.2) e conferem os números de lá. Combinado com o Bruno: na bateria do fim do v8.4 eles passam a medir o `Balance_GDD_v84`.
- **Build para o celular** usa o balanço que estiver no campo **Rules** do Mining Runner na cena `Game`. Para testar no aparelho com a cópia, basta ela estar ligada lá.
- **Textos da loja** ("+10% de dano" etc.) vêm de `ShopCatalog.asset` com o valor calculado pelo núcleo; mudar o Effect atualiza o número mostrado.
- **Arredondamento** só na exibição (`Core/Text/NumberFormatter.cs`); a conta usa `double` cheio.
