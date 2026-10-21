# Arquitetura Técnica

> Proposta de 2026-10-20 (**[REC]**; o que vem do Roadmap original é marcado **[DOC]**). **Não existe projeto Unity ainda**: nada aqui foi criado, instalado ou configurado.
> Não há código nesta fase; pseudocódigo e diagramas só onde esclarecem. Princípio-mestre herdado do we-had-a-plan: *"não implementar hoje o que queremos amanhã; garantir que o de hoje
> não impeça o amanhã."* Requisitos: REQ-TEC-xx em [Requisitos](../design/Requisitos%20e%20Restricoes.md).

## 1. Estado atual do ambiente (verificado)

| Item | Estado |
|---|---|
| Projeto Unity em `miners-idle` | **Inexistente** (só `docs/` e `.git`). **Não criar sem aprovação (D-12).** |
| Unity instalada | `6000.6.0f1` (a mesma dos 2 outros jogos, URP 17.6, Input System 1.20, Test Framework 1.8) — módulos **WebGL e Windows**, **sem Android** |
| Android | SDK platform-tools no PATH, JDK 17; **módulo Android Build Support do Unity precisa ser instalado** via Hub (C-15) |
| Git | repositório com 1 commit; sem `.gitignore`/`.gitattributes`; Git LFS 3.7.1 instalado |
| Versão "LTS" | **Não confirmei** se 6000.6 é marcada LTS no Hub. Se não for, escolher: (a) manter 6000.6 por paridade com os outros projetos e scripts; (b) usar a LTS mais recente. **[REC] (a) com a decisão registrada**, desde que o Android Build Support instale |

## 2. Visão geral das camadas

```
 ┌───────────────────── Apresentação (MonoBehaviours, UI, VFX, áudio) ────────────┐
 │  MiningView · CrystalView · MinerView · HudView · ShopView · AdModalView · FX   │
 └────────▲──────────────────────────────▲──────────────────────────┬────────────┘
          │ eventos (somente leitura)    │ comandos                 │
 ┌────────┴──────────────────────────────┴──────────┐   ┌────────────▼─────────────┐
 │ Jogo (orquestração fina)                          │   │ Persistência             │
 │ GameSession · GameClock · Bootstrap               │◄──┤ SaveService · migrações  │
 └────────▲──────────────────────────────────────────┘   └──────────────────────────┘
          │ usa
 ┌────────┴──────────────────────────────────────────────────────────────────────┐
 │ Núcleo (C# puro, SEM UnityEngine) — regras determinísticas                      │
 │ GameState · MiningSimulation · RewardCalculator · UpgradeCalculator ·           │
 │ PurchaseService · CaveProgression · Penalty · Boost · InternStateMachine        │
 └────────▲──────────────────────────────────────────────────────────────────────┘
          │ lê
 ┌────────┴──────────────────────────────────────┐
 │ Dados (ScriptableObjects → definições imutáveis)│
 │ BalanceConfig · CaveDefinition · UpgradeDefinition│
 └────────────────────────────────────────────────┘
```

Dependências **só descem** (lição do Underworld Towers). O **Núcleo não referencia UnityEngine** (asmdef com *No Engine References*) → testável em EditMode, rápido, determinístico.

## 3. Decisões técnicas

### D-T1 · Separar regra de apresentação (Núcleo puro + Views)
| | |
|---|---|
| **Recomendado** | Toda regra (golpe, dano, crítico, recompensa, penalidade, compra, passagem, estagiário) mora em classes C# puras; MonoBehaviours só **escutam eventos** e **enviam comandos** (`Tap`, `Buy(id)`, `PassCave`). |
| **Por que serve** | O balanceamento do GDD é o coração; precisa de testes e simulador sem cena gráfica (RM §3 sem. 2). Também evita "o Animator decide o dano" (lição: o código é dono do tempo). |
| **Alternativas** | (a) lógica nos MonoBehaviours (rápido de começar, impossível de testar e simular); (b) ECS/DOTS (excesso: 5 cristais e 2 mineradores). |
| **Trade-offs** | Um pouco mais de "cola" (eventos, DTOs) e disciplina para não vazar `UnityEngine` no núcleo. |
| **Quando mudar** | Nunca justificaria ECS neste escopo; só reavaliar se virarem centenas de unidades. |

### D-T2 · Simulação orientada a tempo discreto e determinística
| | |
|---|---|
| **Recomendado** | `MiningSimulation.Tick(double dt)` acumula tempo de cada minerador e dispara golpes quando o acumulador ≥ intervalo (vários golpes por `Tick` se `dt` for grande). RNG injetada (`IRandom` com *seed*) para crítico e posições. Relógio injetado (`IClock`, UTC) para boost/offline. |
| **Por que serve** | Independe da taxa de quadros (celular varia), permite avançar horas num teste/simulador e fazer progressão offline sem duplicar regra. 100.000 golpes para o teste do 5% (RM §6) viram um loop de milissegundos. |
| **Alternativas** | Coroutines/`Invoke` por golpe (acumulam deriva, difícil testar); `Update` com `Time.deltaTime` embutido na regra (acopla à Unity). |
| **Trade-offs** | A animação precisa tolerar golpes "em atraso" após pausa de aplicativo (limitar `dt` máximo por quadro e usar a rota offline para o resto). |
| **Complexidade justificada quando** | Já é necessária: é a base do simulador econômico. |

### D-T3 · Dados em ScriptableObjects; estado em classes simples
| | |
|---|---|
| **Recomendado** | `BalanceConfig` (SO) com constantes e curvas (parâmetros, não tabelas coladas); `CaveDefinition[10]` (nome, cor, sprite de fundo, cristal, textos); `UpgradeDefinition[6]` (id, ícone, texto, limites, parâmetros de custo/efeito). **Estado de jogo** (`GameState`) é **POCO** serializável, **nunca** dentro de um SO. |
| **Por que serve** | RM §2 pede SO para separar balanceamento da lógica. Permite ter `Balance_GDD_v1` e `Balance_v2` lado a lado (REQ-PRO-06). |
| **Alternativas** | JSON/CSV de balanceamento externo (mais fácil de editar em planilha, perde *drag-and-drop* de arte no Inspector); constantes em código (rápido, pede recompilar). |
| **Trade-offs** | SO compartilhado **não** deve ser alterado em jogo (alteração persiste no Editor): leitura somente via `IBalance` (interface somente leitura). Alternativa leve: **exportar** o SO para JSON para o simulador. |
| **Quando adotar o plano B (CSV/planilha)** | Se o ajuste de balanceamento ficar tão frequente que o Inspector atrapalhe. |

### D-T4 · Números: `double`, sem biblioteca de números grandes
| | |
|---|---|
| **Recomendado** | `double` para vida, dano, custos e saldo [DOC]. Regras: **nunca** `float`; arredondar **só** na exibição; comparar custos com tolerância (`>= custo - 1e-9`) ou manter custos como inteiros em `double`; **um único formatador** de números (K/M/B/T, casas decimais, cultura). |
| **Por que serve** | O maior valor realista é 10¹⁰ (vida) / 10¹² (custos de Picareta) — dentro dos 15–16 dígitos exatos do `double`. Os 3,5·10⁹ da Mineração e ~10²³ da Velocidade (se mantidos) perdem precisão unitária, o que é irrelevante para custo. |
| **Alternativas** | `decimal` (lento, sem `Mathf`); `BigInteger`/tipo próprio estilo `BigDouble` (necessário só se o prestígio levar valores a 10³⁰⁰+ ou se a precisão unitária importar). |
| **Trade-offs** | Soma de frações (0,5 lascas penalizadas) acumula erro de ponto flutuante — aceitável para exibição de duas casas; **cobrir com teste** de acumulação longa. |
| **Quando migrar** | Se um dia entrar prestígio/escala infinita. Encapsular por trás de um alias (`using Quantity = System.Double;` ou `struct Amount`) **somente se** o plano pós-MVP o exigir — não agora. |

### D-T5 · Eventos C# simples (`event Action<...>`)
| | |
|---|---|
| **Recomendado** | Núcleo expõe eventos tipados (a lista de GDD §11.2 vira interface `IMiningEvents`: `HitLanded(MinerId, damage, isCrit)`, `CrystalBroken(...)`, `ShardsGained(...)`, `UpgradePurchased(...)`, `CaveChanged(n)`, `BoostStarted/Ended`, `PenaltyStarted`…). Views assinam em `OnEnable`, desassinam em `OnDisable` (lição #2 do we-had-a-plan). |
| **Por que serve** | RM sem. 5: "efeitos respondem a eventos sem decidir regras de jogo". |
| **Alternativas** | `UnityEvent` (inspetor, mais lento e propenso a referências mortas); event bus global/ScriptableObject Channels (mais indireção); MessagePipe/UniRx (dependência extra). |
| **Trade-offs** | Assinaturas esquecidas vazam; testar com "domain reload desligado". |
| **Quando subir para event bus** | Se mais de ~3 consumidores independentes por evento começarem a brigar por ordem. Provavelmente nunca. |

### D-T6 · Sem `GameManager` onipotente; composição em um `Bootstrap`
| | |
|---|---|
| **Recomendado** | Um `GameBootstrap` (cena `Bootstrap`) cria `GameState`, `SaveService`, `GameSession` e injeta por **construtor/campo** nas Views. **Sem singletons estáticos** para estado de jogo (lição de rede/estado, sem a rede). Ordem de inicialização explícita: Balanço → Save → Estado → Sessão → Views. |
| **Por que serve** | RM §2.1: "evitar um único GameManager". Dependências visíveis = testes fáceis. |
| **Alternativas** | Singleton `MonoBehaviour` (rápido, vira bola de neve); frameworks de DI (Zenject/VContainer: dependência extra sem necessidade). |
| **Trade-offs** | Um pouco de fiação manual. |
| **Quando justificar DI** | Se passarem de ~20 serviços ou houver *scenes* aditivas complexas. Não é o caso. |

### D-T7 · Estagiário como máquina de estados explícita
| | |
|---|---|
| **Recomendado** | `MinerAgent` (principal e estagiário usam a mesma classe com parâmetros) com estados `Idle → Striking → Waiting → Advancing`; `CrystalLot` (5 slots) com `Free/Reserved/Dead`; regras escritas e testadas em matriz (RM sem. 6): principal-primeiro, estagiário-primeiro, simultâneo, 5º cristal, troca de lote. **A especificação precisa ser escrita antes** (C-08) — documento de 1 página com diagrama. |
| **Por que serve** | É a parte mais frágil do GDD (alvo duplicado, "sem alvo"). |
| **Alternativas** | Corrotinas em cada minerador (de leitura simples, depuração ruim); fila de alvos global (menos expressiva). |
| **Trade-offs** | Mais um documento antes de codar. |
| **Quando simplificar** | Se for decidido que o estagiário não precisa "esperar" (reduz estados). |

### D-T8 · Persistência: JSON versionado, atrás de interface
| | |
|---|---|
| **Recomendado** | `SaveData { version, ... campos do GDD §11.1 + flags de tutorial + lastSeenUtc }` → `ISaveStore` → arquivo em `Application.persistentDataPath`. **Escrita atômica** (gravar `.tmp` → substituir) com **backup** `.bak`; carregamento tolerante (JSON inválido/versão futura → tentar `.bak` → senão estado novo + aviso). **Migrações** `Migrate_v1_to_v2` encadeadas, cada uma com teste. Save acionado em eventos-chave (compra, passagem, quebra do 5º), `OnApplicationPause(true)` e saída. Usar `JsonUtility` (suficiente) ou `System.Text.Json`/Newtonsoft só se precisar de dicionários. |
| **Por que serve** | RM §2 pede JSON versionado; o Underworld Towers fez o mesmo atrás de `ISaveStore`. Dados do GDD cabem em ~12 campos. |
| **Alternativas** | `PlayerPrefs` (frágil, sem versão); binário (ilegível); nuvem (fora do escopo). |
| **Trade-offs** | JSON é editável por usuário — aceitável (jogo offline, sem competição). Adicionar *checksum* opcional só se virar problema. |
| **Quando criptografar/assinar** | Quando houver compras reais ou ranking (fora do MVP). |

### D-T9 · Progressão offline analítica (se D-04 aprovado)
`taxa_média = f(GameState, BalanceConfig)` reaproveitando as mesmas fórmulas do simulador; aplica `min(Δt, teto) × eficiência`. Relógio: `DateTime.UtcNow` com proteção "se o relógio voltou, não ganhar nada". **Alternativa descartada:** simular golpe a golpe por horas (caro e desnecessário). Detalhes em [Economia §6](../design/Economia%20e%20Progressao.md).

### D-T10 · UI: Unity UI (uGUI) + TextMeshPro [DOC]
| | |
|---|---|
| **Recomendado** | Manter a decisão do Roadmap: *Canvas* (Screen Space – Overlay, **Canvas Scaler** com referência 270×480 × escala inteira), `SafeAreaController`, prefabs reutilizáveis (`UpgradeRowView`), TMP com fonte pixel (*Font Asset* com os caracteres acentuados). Números flutuantes e lascas voando no **mundo** (sprites/TMP em espaço do mundo, com *pool*). |
| **Por que serve** | Combina com o resto (sprites, câmera pixel perfect); abundante em exemplos. |
| **Alternativas** | **UI Toolkit** — adotado no Underworld Towers (D53) e mais forte para telas de formulário; porém mais atrito com pixel art e *world-space*. Escolher UI Toolkit só se o Bruno quiser reaproveitar o aprendizado dele. **D-13 (baixa prioridade).** |
| **Trade-offs** | uGUI pede cuidado com *draw calls* e *rebuilds*; manter poucas camadas e *atlas*. |

### D-T11 · Renderização e câmera 2D
URP 2D Renderer, **Pixel Perfect Camera** (verificar API no URP 17.6), sprites com *Point filter*, PPU 16 (ver [Arte](../design/Direcao%20de%20Arte%20e%20Assets.md) §2). **Luzes 2D opcionais** (testar custo em celular real; plano B: sprites aditivos). Sem pós-processamento no MVP (preserva a grade de pixels e a bateria).

### D-T12 · Entrada: Input System (novo)
`Touchscreen`/`Pointer` via Input System **desde o primeiro dia** (lição). Um adaptador `IMiningInput` gera `Tap(position)`; testes e o painel de debug disparam o **mesmo comando** (lição: testes jogam pelo mesmo caminho do jogador). Regras do toque (área, taxa máxima, crítico) em dados, decisão D-08.

### D-T13 · Object pooling — onde e quando
| | |
|---|---|
| **Recomendado** | **Sim, para:** números flutuantes (`xN`, "CRÍTICO"), lascas voando, partículas de faísca — volume alto a ≥ 6 toques/s. **Não, para:** 5 cristais e 2 mineradores (poucos objetos; reutilizar ao renovar o lote já basta). Implementação com `UnityEngine.Pool.ObjectPool<T>`. |
| **Por que** | Evita picos de GC no celular. |
| **Quando ampliar** | Se o *profiler* mostrar alocação por quadro > 0 B nas cenas de teste. |

### D-T14 · Áudio
`AudioMixer` com grupos `Master/SFX/(Music)`; ~8 `AudioSource` num *pool* de vozes para sons curtos com *pitch* aleatório pequeno e *pitch por caverna*; opção de silenciar vibração/tremor em Configurações.

### D-T15 · Estrutura de pastas e Assembly Definitions
```
Assets/_Project/
  Scripts/
    Core/          (MinersIdle.Core — C# puro, sem engine references)
    Game/          (MinersIdle.Game — orquestração, MonoBehaviours finos)
    Presentation/  (MinersIdle.Presentation — Views, FX, áudio)
    UI/            (MinersIdle.UI)
    Persistence/   (MinersIdle.Persistence)
    Editor/        (MinersIdle.Editor — ProjectSetup, menus, build)
  Data/            (SOs de balanceamento e cavernas)
  Prefabs/  Scenes/(Bootstrap, Game, Test/…)  Art/  Audio/  VFX/  Settings/
  Tests/
    EditMode/      (MinersIdle.Tests.EditMode)
    PlayMode/      (MinersIdle.Tests.PlayMode)
Assets/_Vendor/    (terceiros, intocados)
ArtSource/         (fora de Assets: .aseprite, PSD, referências de IA)
docs/  tools/{art,ci,build}
```
Dependências: `Presentation/UI → Game → Core` ; `Persistence → Core`; `Game → Persistence`; `Editor` e `Tests` enxergam tudo. **Nomes e pastas serão fixados na criação do projeto (M1).**

### D-T16 · Tratamento de erros e recuperação
Save inválido → backup → estado novo (com mensagem não bloqueante); valores fora da faixa são *clamped* na carga; `Debug.LogError` + tela de erro mínima em *release* apenas para falhas irrecuperáveis; nenhuma exceção silenciada; *asserts* de invariantes do núcleo (`Debug.Assert` em debug): "lote sempre com 5 posições", "um minerador nunca ocupa o alvo do outro", "compra nunca deixa saldo negativo".

### D-T17 · Estratégia de testes (resumo; detalhes em [Qualidade](Qualidade%20e%20Fluxo%20de%20Trabalho.md))
EditMode para tudo do Núcleo (fórmulas, limites, custo/efeito de todos os níveis, estagiário, penalidade, boost, save/migração, teste estatístico do crítico); PlayMode para fluxos (tocar → quebrar → coletar, loja bloqueia toque mas minera, pausa/retomada, passagem); testes em aparelho para input, Safe Area, desempenho.

### D-T18 · Desempenho e memória (metas provisórias)
60 fps em celular intermediário (a definir qual); alocações por quadro ≈ 0 no loop de mineração; atlas por grupo; ≤ ~100 MB de memória de textura; *build* Android < ~60 MB; consumo de bateria observado em sessão de 10 min. Verificar com Profiler a cada marco (lição).

### D-T19 · Build e CI
APK Android (*Development Build*) por marco; AAB para release; `Editor/ProjectSetup.cs` para configuração reprodutível (padrão do Underworld Towers); script de build em `tools/build/`. O **espelho de CI** (cópia do projeto para rodar testes com o Editor aberto) foi decisivo nos 2 outros jogos, mas aqui o projeto é pequeno: começar com **Test Runner + script batch** e só adotar o espelho se o Editor aberto atrapalhar.

## 4. Ordem de inicialização (proposta)

1. `Bootstrap` carrega `BalanceConfig`/definições → 2. `SaveService.Load()` (com migração e fallback) → 3. cria `GameState` e `GameSession` → 4. calcula **offline** (se aprovado) → 5. carrega cena `Game` e injeta Views → 6. inicia `Tick` do relógio → 7. `OnApplicationPause(true)` salva.

## 5. O que fica explicitamente de fora (evitar *overengineering*)

DI framework · ECS · event bus global · Addressables · localização por pacote oficial (usar tabela simples de strings em dados; Localization Package só se houver 2º idioma) · rede/nuvem/anúncios reais (SDK só pós-MVP) · telemetria remota · *shader graph* complexo.
