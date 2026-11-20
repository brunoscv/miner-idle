# Guia de balanceamento — onde ficam as fórmulas e como mexer

> Para quem vai testar e ajustar a jogabilidade (custos, danos, tempos) sem precisar ler o código todo.
> Escrito em 2026-11-19, com o jogo no **GDD v8.2** (balanço ativo: `Balance_GDD_v8`).

## 1. Resumo em 30 segundos

- **Quase todo número do jogo está em um arquivo só:** `Assets/_Project/Data/Balance_GDD_v8.asset`. É um ScriptableObject e se edita no **Inspector** do Unity (clicar no asset na janela Project).
- **As fórmulas** (o "jeito" de fazer a conta) ficam em C#, no núcleo `Assets/_Project/Scripts/Core/`. Os **números** que entram nelas vêm do asset. Para balancear, quase sempre basta mexer nos números; só se mexe no C# para mudar o *formato* da conta.
- **Não edite o `Balance_GDD_v8` direto.** Duplique e teste na cópia (ver §2). Motivos: testes automáticos leem o v8, o setup do projeto repõe alguns campos do v8, e o Bruno registra toda mudança de valor como decisão (`docs/planning/Decisoes e Riscos.md`).

## 2. Como testar um ajuste (passo a passo seguro)

1. Abra o projeto no Unity 6000.6.0f1.
2. Na janela **Project**, vá em `Assets/_Project/Data/`, clique em `Balance_GDD_v8` e aperte **Ctrl+D**. Renomeie a cópia, por exemplo `Balance_Teste`.
3. Abra a cena `Assets/_Project/Scenes/Game.unity`.
4. Na **Hierarchy**, ache o objeto que tem o componente **Mining Runner** (use a busca da Hierarchy: `t:MiningRunner`). No campo **Rules**, arraste o `Balance_Teste`. Salve a cena.
   - O setup do projeto só troca esse campo se ele estiver num balanço antigo (protótipo, v5 ou v7); um asset seu fica.
5. Clique no `Balance_Teste` e mude os valores no Inspector. Cada campo tem **tooltip** (passe o mouse no nome) dizendo o que faz e o valor do GDD.
6. Aperte **Play**. Os valores são lidos quando o jogo começa: mudou algo, pare e dê Play de novo.
7. Se o save antigo atrapalhar (níveis comprados com o preço velho, andar alto demais), use o painel **DBG** do jogo → apagar o save / **Reiniciar run**.
8. Gostou? Leve os números para o Bruno. Ele registra a decisão e passa os valores para o `Balance_GDD_v8` (e corrige os testes que medem o valor antigo).

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
| **Exponential** | `Base Cost × Cost Growth^n` | Velocidade, Mineração, Crítico, Estagiário e a árvore dele |
| **Linear** | `Base Cost + Cost Growth × n` | ninguém no v8 |
| **OffsetExponential** | `Base Cost + Cost Growth^n` | as permanentes (lascas vermelhas): 11, 12, 14, 18, 26… |
| **DamageScaled** | `Base Cost × (dano atual da Picareta)` | só a Picareta do anão |

## 3. Onde fica cada coisa (arquivos)

| Arquivo | O que tem | Mexe para balancear? |
|---|---|---|
| `Assets/_Project/Data/Balance_GDD_v8.asset` | **Todos os números**: cristal, andares, anão, loja, estagiário, crítico, raro, anúncios, bomba, descanso e permanentes | **Sim** (numa cópia) |
| `Assets/_Project/Scripts/Game/Data/MiningRulesAsset.cs` | A classe do asset acima: os campos, tooltips e valores padrão; converte o asset nas regras do núcleo | Só para criar campo novo |
| `Assets/_Project/Scripts/Core/Mining/MiningRules.cs` | Fórmulas de vida e lascas do cristal por andar, requisito do Próximo Andar, teto do offline, marcos de andar | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Economy/UpgradeDefinition.cs` | As 4 fórmulas de custo (tabela acima) | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Economy/MinerStats.cs` | Dano, intervalo, corrida, multiplicador de lascas e crítico **do anão** | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Economy/InternStats.cs` | Dano, intervalo e lascas offline **do estagiário** | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Economy/Permanents.cs` | Efeito de cada permanente (Sangue, Coração, Memória, Atalho…) | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Economy/PermanentRules.cs` | Tabela das lascas vermelhas por andar (`RedShardTable`) | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Mining/MiningSimulation.cs` | O loop: aplica bônus do raro/anúncio, rola o crítico, multiplicador do crítico (linhas ~850–870 e ~1290–1315) | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Mining/BombRules.cs` | Regras da bomba | Só para mudar o formato |
| `Assets/_Project/Scripts/Core/Economy/OfflineMining.cs` | Conta do que o estagiário minera com o app fechado | Só para mudar o formato |
| `Assets/_Project/Data/ShopCatalog.asset` | **Textos e ícones** da loja e das permanentes (título, formato do efeito) — não tem número de regra | Só se mudar o texto |
| `Assets/_Project/Data/BuffCatalog.asset`, `FeedbackTexts.asset` | Textos e ícones dos bônus e mensagens | Não |
| `Assets/_Project/Data/Biomes/Caverna_01…10.asset` | **Só visual** (texturas, cor, névoa) de cada bioma | Não |
| `Balance_GDD_v1/v5/v7`, `Balance_Prototipo` | Balanços antigos, só para comparação; **o jogo não usa** | Não |

## 4. Mapa: fórmula → funcionalidade

Nomes em **negrito** são os campos do asset como aparecem no Inspector (o Unity mostra `pickaxeCaveGrowth` como "Pickaxe Cave Growth"). Os valores são os do `Balance_GDD_v8` hoje.

**Unidades:** distância em "unidades da trilha", **1 m = 2 un**. Tempo em segundos. `andar` começa em 1.

### 4.1 Cristal e andares

| Funcionalidade | Fórmula | Campos (valor atual) | Código |
|---|---|---|---|
| **Vida do cristal** no andar | `Crystal Health × Cave Health Growth^(andar − 1)` | **Crystal Health** 1 · **Cave Health Growth** 1,5 | `MiningRules.CrystalHealthIn` |
| **Lascas (moeda) por cristal** | `vida do andar × Crystal Reward × max(1, andar − 1)` × multiplicador de lascas (§4.3) | **Crystal Reward** 0,2 · **Reward Formula** FloorScaled (**Cave Reward Growth** não vale nela) | `MiningRules.CrystalRewardIn` |
| **Número de andares** | — | **Cave Count** 100 | |
| **Bioma** (troca de visual) | a cada **Levels Per Biome** andares | **Levels Per Biome** 10 | `MiningRules.BiomeOf` |
| **Cristais para o Próximo Andar** | `andar × Floor Breaks − Memória` (mín. 0) | **Floor Breaks** 10 | `Permanents.FloorRequirement` |
| **Fila de cristais** | começa com **Initial Crystals**; nasce 1 a cada **Spawn Interval** s até **Max Crystals**; abaixo de **Min Crystals** nasce na hora | 10 · 5 s · 12 · 10 | `CrystalTrack.cs` |
| **Distância entre cristais** | `Min Crystal Spacing − Atalho × 0,5 m` (mín. 10% da base) | **Min Crystal Spacing** 6 un (3 m) | `Permanents.CrystalSpacing` |
| Avanço automático / estagiário / raro liberam no andar | recorde de andar ≥ valor | **Auto Advance Floor** 5 · **Intern Floor** 5 · **Rare Floor** 10 | `MiningRules.Is…Unlocked` |

Exemplo (vida / lascas sem bônus): andar 1 → 1 / 0,2 · andar 3 → 2,25 / 0,9 · andar 10 → 38,4 / 69,2 · andar 20 → 2.217 / 8.424.

### 4.2 Anão: dano, velocidade, corrida

| Funcionalidade | Fórmula | Campos (valor atual) | Código |
|---|---|---|---|
| **Dano da Picareta** | `Damage + Pickaxe.Effect × n + Pickaxe Cave Growth^n` (n = compras) × Sangue da Montanha × bônus ativos | **Damage** 1 · **Pickaxe.Effect** 0 (o "incremento") · **Pickaxe Cave Growth** 1,2 (a "taxa") · **Pickaxe Formula** LevelScaled | `MinerStats.Compute` |
| **Custo da Picareta** | `Pickaxe.Base Cost × dano atual da Picareta` (DamageScaled) | **Pickaxe**: Base Cost 7 · Max Level 49 (o Cost Growth salvo não vale; usa a taxa) | `UpgradeDefinition.CostAt` + `MiningRulesAsset.PickaxeLine` |
| **Intervalo entre golpes** (Velocidade) | `max(Min Hit Interval, Hit Interval − Speed.Effect × n)` ÷ bônus | **Hit Interval** 2 s · **Speed**: Effect 0,1 · Max 19 · **Min Hit Interval** 0,1 s | `MinerStats.Compute` |
| **Custo da Velocidade** | `50 × 2^n` | **Speed**: Base Cost 50 · Cost Growth 2 | |
| **Corrida** | `Walk Speed × (1 + Boots.Effect × n)` | **Walk Speed** 4 un/s · **Boots** desligada (Max 0) | |
| Primeiro golpe ao chegar no cristal | Immediate = bate na chegada | **Arrival Strike** Immediate | |
| Volta da picareta | segundos parados depois do golpe se o cristal quebrou | **Swing Recovery** 0,72 | |
| Picareta Dupla | ×Effect no dano | **Double Pickaxe** desligada (Max 0) | |

Exemplo da Picareta (dano / custo da próxima): 0 compras → 2 / 14 · 5 → 3,49 / 24,4 · 10 → 7,19 / 50,3 · 20 → 39,3 / 275 · 49 (máx.) → ~7.585.

### 4.3 Lascas (moeda) e crítico

| Funcionalidade | Fórmula | Campos (valor atual) | Código |
|---|---|---|---|
| **Multiplicador de lascas** | `1 + Mining.Effect × n + Coração Ardente + Mineração do estagiário` × cartas/anúncios | **Mining**: Effect 1 (+100%/nível) · Max 10 | `MinerStats.Compute` |
| **Custo da Mineração** | `20 × 5^n` | **Mining**: Base Cost 20 · Cost Growth 5 | |
| **Chance de crítico** | `Crit Chance + Critical.Effect × n + Dedos Nervosos + cartas/anúncios` | **Crit Chance** 0 · **Critical**: Effect 0,05 · Max 10 | `MiningSimulation.CritChance` |
| **Custo do Crítico** | `50 × 2^n` | **Critical**: Base Cost 50 · Cost Growth 2 | |
| **Dano do crítico** | `(Crit Multiplier + Dedo Fogoso + max(0, chance − 1)) × cartas/anúncios de Dano de Crítico` | **Crit Multiplier** 1,5 | `MiningSimulation.CritMultiplier` |

### 4.4 Estagiário (2º minerador)

| Funcionalidade | Fórmula | Campos (valor atual) |
|---|---|---|
| **Contratar** | compra única | **Intern**: Base Cost 500 · Max 1 (0 = sem estagiário) |
| **Dano dele** | `Intern Pickaxe.Effect × Intern Pickaxe Damage Growth^n × (1 + Treinar)` | **Intern Pickaxe**: Effect 5 · **Intern Pickaxe Damage Growth** 2 |
| Custo da Picareta dele | `50 × 4^n`, máx. 19 | **Intern Pickaxe**: Base Cost 50 · Cost Growth 4 |
| **Intervalo dele** | `max(Min Hit Interval, Hit Interval − Intern Speed.Effect × n)` | **Intern Speed**: Effect 0,1 · custo `50 × 5^n` · máx. 19 |
| **Mineração dele** | +Effect nas lascas (online soma no multiplicador do anão; offline multiplica o que ele traz) | **Intern Mining**: Effect 1 · custo `50 × 1,2^n` · máx. 10 |

Código: `InternStats.Compute`. O crítico e o Sangue da Montanha **não** valem para ele.

### 4.5 Offline (app fechado)

| Funcionalidade | Fórmula | Campos (valor atual) |
|---|---|---|
| Teto de tempo que conta | 4 h; 6 h depois de chegar ao andar 20; + horas dos anúncios ativos ao fechar, até o teto dos anúncios | **Offline Max Hours** 4 · **Offline Deep Hours** 6 · **Offline Deep Floor** 20 · **Ad Offline Cap Hours** 8 |
| O que rende | só o estagiário minera: cristais = tempo ÷ (corrida + ⌈vida ÷ dano⌉ golpes); lascas × Mineração dele | (vem das linhas do estagiário) |

Código: `OfflineMining.cs`.

### 4.6 Bomba

| Funcionalidade | Fórmula | Campos (valor atual) |
|---|---|---|
| **Dano da bomba** | `fração × vida máxima` de cada cristal no alcance (sem crítico nem bônus) | **Bomb Fractions** 0,1 · 0,2 · 0,3 · 0,4 · 0,5 · 0,6 · 0,7 · 1 (a 1ª é de graça; cada compra da permanente Bomba passa para a próxima) |
| Recarga / voo / alcance | — | **Bomb Charge Seconds** 5 · **Bomb Flight Seconds** 0,4 · **Bomb Reach** 7 un (3,5 m) |

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

**Lascas vermelhas ganhas ao descansar** = soma, do andar **Rest Level** até o andar alcançado, de: **Red Shard Per Level** (1) por andar; nos múltiplos de **Red Shard Milestone Step** (10) vale **Red Shard Milestone** (5); a partir de **Red Shard Big Milestone From** (50), o múltiplo vale **Red Shard Big Milestone** (10). Ex.: chegar ao andar 20 dá 5 + 9 + 5 = 19. Código: `RedShardTable` em `PermanentRules.cs`.

Permanentes (custo em lascas vermelhas, `Base Cost + Cost Growth^n` → 11, 12, 14, 18, 26…):

| Permanente | Campo | Efeito por nível (Effect) | Máx. | Onde age |
|---|---|---|---|---|
| Memória | **Memory** | −5 cristais no requisito do Próximo Andar | 100 | `FloorRequirement` |
| Sangue da Montanha | **Mountain Blood** | +10% no dano final | 20 | `DamageMultiplier` |
| Coração Ardente | **Burning Heart** | +50% de lascas | 20 | `RewardBonus` |
| Dedos Nervosos | **Nervous Fingers** | +5% de chance de crítico | 10 | `CritBonus` |
| Dedo Fogoso | **Fiery Finger** | +0,5 no dano do crítico | 10 | `CritDamageBonus` |
| Alma dos Anões | **Dwarf Soul** | a run começa com +1 nível de Picareta | 10 | `StartingPickaxe` |
| Treinar Estagiário | **Intern Training** | +10% no dano do estagiário | 10 | `InternDamageMultiplier` |
| Promoção do Estagiário | **Intern Promotion** | compra única (50): run já começa com ele; pede o Treinar no **Intern Promotion Training Level** (5) | 1 | `StartsWithIntern` |
| Atalho | **Shortcut** | −0,5 m entre cristais (custo `50 + 2^n`) | 5 | `CrystalSpacing` |
| Raro Dourado | **Golden Rare** | compra única (500): cartas no tier 2; abre no andar do raro | 1 | `HasGoldenRare` |
| Bomba | **Bomb** | passa para a próxima bomba da lista | 7 | `BombLevel` |

Todas em `Assets/_Project/Scripts/Core/Economy/Permanents.cs`.

## 5. Cuidados

- **Max Level 0 desliga** a linha (some da loja). É o jeito certo de tirar uma melhoria do teste.
- **Valores inválidos desligam o sistema, com aviso no Console** (`[Valores] …`): fração da bomba fora de 0–1, carta repetida, anúncio sem minutos etc. Se uma coisa "sumiu", olhe o Console.
- **Setup repõe alguns campos do `Balance_GDD_v8`** quando roda (menu `Tools → Miners Idle → Projeto` e todo build): se no v8 você zerar o Rare Interval, esvaziar os anúncios ou as bombas, zerar os três marcos de andar ou o Max Level do Dedo Fogoso, ele preenche de novo com o GDD. Na cópia (`Balance_Teste`) isso não acontece.
- **Testes automáticos** (`Tests/EditMode/GddV8RulesTests.cs`, `CalculationTests.cs`, `AdOffersTests.cs`, `RareCounterTests.cs`, `PlayMode/CalculationsPanelTests.cs`) leem o `Balance_GDD_v8` e conferem os números do GDD. Mudou o v8, esses testes falham até serem corrigidos para os valores novos.
- **Build para o celular** usa o balanço que estiver no campo **Rules** do Mining Runner na cena `Game`. Para testar no aparelho com a cópia, basta ela estar ligada lá.
- **Textos da loja** ("+10% de dano" etc.) vêm de `ShopCatalog.asset` com o valor calculado pelo núcleo; mudar o Effect atualiza o número mostrado.
- **Arredondamento** só na exibição (`Core/Text/NumberFormatter.cs`); a conta usa `double` cheio.
