# Roadmap de desenvolvimento

> Proposta de 2026-10-20 (**[REC]**), derivada do `Roadmap_MVP_Idle_Miner_Unity_CSharp.docx` (RM) e do GDD, **ajustada** pelo que a descoberta encontrou.
> **Nenhuma data é compromisso.** As estimativas são faixas de **horas de desenvolvimento focado** (não calendário), válidas só sob as premissas do §7.
> Rastreabilidade: cada tarefa cita os `REQ-*` de [Requisitos](../design/Requisitos%20e%20Restricoes.md); cada marco tem Definição de Pronto verificável.
> **Regra de ouro (herdada):** *jogável antes de sistema*; um marco só começa depois que o anterior foi **aprovado pelo Bruno** (padrão do Underworld Towers).

> **Atualizações de 2026-10-21 (decisões do Bruno):** economia **não bloqueia** o M2 (D-01) · estilo = mockup (D-05) · aparelho **Moto G75**, **sem ambiente Android instalado** (entra no M1) · **~20 h/semana** → as faixas de horas abaixo equivalem a **≈ 22–33 semanas** (5–8 meses), não às 11 semanas do RM · o jogo será **vendido com anúncios reais** → M12 passa a incluir a integração de anúncios e a conformidade (D-18, D-21, D-22) · teste do **PixelLab** para o anão no M1b (D-23).

> **Atualização de 2026-10-21 (tarde) — D-25…D-29:** **M1 aprovado.** O jogo passa a ter **mineração no mapa** (o anão anda e minera grupos de cristais que nascem continuamente; regra de alvo "lote, depois mais próximo"). **Novo M2 = Protótipo do loop no mapa** (graybox jogável); o antigo M2 (regras e economia) vira **M3**; o antigo M3 (vertical slice) fica absorvido pelos dois. M4 em diante mantém o número. Nas tabelas de outros documentos anteriores a esta data, "M2" como marco de economia/simulador = **M3**.

> **Atualização de 2026-10-22 — D-50…D-59:** **nova visão aprovada: descida em corrida** (câmera de frente, anão correndo por um corredor de caverna; substitui o mapa aberto da D-26). **M5 aprovado** (D-59). O **novo M6 = Descida em corrida**, antes do Art Spike e do Estagiário; **os marcos seguintes ganham +1** (D-56): Estagiário M7, Letrado/cavernas M8, AD/save/offline M9, arte 2–10 M10, balanceamento M11, RC M12, publicação M13, pós-lançamento M14. Em documentos anteriores a esta data, "M6" = Estagiário e assim por diante. O texto dos marcos abaixo que fala em "mapa" vale para o corredor.

> **Atualização de 2026-10-23 — D-62…D-67:** a E5 do M6 fica pausada e o **Art Spike (M1b)** começa, primeiro pela **interface** (packs de UI em madeira, ícones, fonte Pixelify Sans, botões LOJA e AD) e depois anão, cristais e biomas. Plano: [Marco 1b - Art Spike](Marco%201b%20-%20Art%20Spike.md).

> **Atualização de 2026-10-24 — D-84, D-85:** o **GDD v5** passa a ser a referência de design (o v1 vira histórico). Os textos do M7, M8 e M9 abaixo ainda descrevem o v1; cada um tem uma nota "GDD v5" com o que muda, e o plano de cada marco é escrito pelo v5. Perguntas em aberto (G1–G10) e decisões pendentes (D-86 toque 33%, D-87 `Balance_GDD_v5`, D-88 ordem M7 × M8, D-89 marco "Bônus temporários") em [Decisões e Riscos §2.4](Decisoes%20e%20Riscos.md); diferenças completas em [Análise - GDD v5](Analise%20-%20GDD%20v5.md). Builds de teste pelo Firebase viram o **M15** (D-84).

> **Atualização de 2026-10-31 — D-92…D-101:** as perguntas **G1–G10 do GDD v5 foram respondidas** ([Decisões e Riscos §1.7](Decisoes%20e%20Riscos.md)). Valem sobre as notas "GDD v5" dos marcos abaixo: penalidade fora do jogo (D-93), parede da passagem com vida (D-98), Picareta Online fora (D-94), linha Crítico (D-100), modal de volta do offline (D-99). Pontas abertas: G11–G16. **D-86…D-89 aprovadas no mesmo dia:** toque de 33% e `Balance_GDD_v5` entram juntos na E5 do M6; **o M8 vem antes do M7** (os números ficam); o marco **"Bônus temporários"** entra depois do M8 e leva o boost do AD.

## 1. Como este roadmap se relaciona com o do RM

| RM (semanas) | Aqui | Mudança e motivo |
|---|---|---|
| 1 Fundação Unity/Android | **M1** | + *Art Spike* (M1b) em paralelo: decidir estilo/resolução **antes** do slice (C-14) |
| 2 Economia e testes | **M2** | + **recalibração da economia** e simulador (C-01 bloqueia o resto); + especificação da máquina do estagiário (C-08) |
| 3 Vertical slice cave 1 | **M3** | igual (Marco B) |
| 4 HUD, loja, melhorias | **M4** | + **save mínimo** (o RM já pede "compras persistidas" na sem. 4) |
| 5 Crítico, combo, feedback | **M5** | + **arte final da caverna 1** aqui (RM só na sem. 9): é onde o *game feel* é avaliado |
| 6 Estagiário | **M6** | igual |
| 7 Progressão e 10 cavernas | **M7** | igual, com arte provisória por *recolor* |
| 8 AD simulado e persistência | **M8** | + progressão offline (se D-04), configurações, tutorial mínimo |
| 9 Assets e integração visual | **M9** | arte das cavernas 2–10 (estende a assinatura da caverna 1) |
| 10 Balanceamento/perf/playtest | **M10** | igual |
| 11 Regressão e RC | **M11** | igual |
| — | **M12** | **Preparação de publicação** (o RM termina no APK/AAB; publicar exige loja, política de privacidade, assinatura) |
| — | **M13** | Pós-lançamento (backlog) |

## 2. Visão geral

```
M0  Descoberta e aprovação ──┐
                             ▼
M1  Fundação técnica ✅ ───────┬────────────────────────────┐
M1b Art Spike (paralelo) ─────┤                            │
                              ▼                            ▼
M2  Protótipo do loop no mapa (graybox) ✅ ← 1º jogável  [arte: caverna 1 assinatura]
                              ▼
M3  Regras e economia (calculadores, simulador, spec do estagiário) ✅ ← Marco B
                              ▼
M4  Loja, melhorias, HUD, save mínimo ✅
                              ▼
M5  Toque, crítico, combo e feedback (graybox) ✅
                              ▼
M6  Descida em corrida (câmera de frente, corredor 3D) ← nova visão, D-52
                              ▼ ◄─── M1b Art Spike (depois da E1 do M6, para o corredor)
M8  Letrado, parede, 10 cavernas  (antes do M7, D-88)
                              ▼
M7  Estagiário  ·  Bônus temporários + boost do AD (D-89)  (ordem entre os dois: a definir)
                              ▼
M9  Save completo, offline, config ← Marco C
                              ▼ ◄─── (paralelo) M10 arte das cavernas 2–10 e UI final
M11 Balanceamento, desempenho, playtest
                              ▼
M12 Regressão e Release Candidate ← Marco D
                              ▼
M13 Publicação  →  M14 Pós-lançamento

M15 Builds de teste pelo Firebase (ferramenta, D-84; no fim, pode ser puxado antes do M11)
```

## 3. Marcos

Formato por marco: **Objetivo · Resultado para o jogador · Tarefas (ver §5) · Dependências · Entregáveis · Técnica · Arte/áudio · Testes e validação · Definição de Pronto · Riscos · Fora deste marco · Esforço**.

### M0 — Descoberta e aprovação
- **Objetivo:** transformar o GDD+RM em plano aprovado. **Jogador:** nada.
- **Entregáveis:** estes documentos; lista de decisões ([Decisões e Riscos](Decisoes%20e%20Riscos.md)).
- **Dependências:** nenhuma. **Testes:** revisão do Bruno.
- **DoD:** Bruno aprovou (ou ajustou) os itens **bloqueantes**: D-01, D-02/D-03, D-05, D-12, D-17, D-20 — **respondidos em 2026-10-21** (ver [Decisões](Decisoes%20e%20Riscos.md) §1.1). Falta apenas a **autorização explícita para iniciar a fase de implementação (M1)**.
- **Riscos:** decisões ficarem em aberto e travarem M1/M2. **Fora:** qualquer código ou asset. **Esforço:** ~8–12 h (já feito aqui) + revisão.

### M1 — Fundação técnica
- **Objetivo:** projeto Unity reprodutível que gera APK no aparelho. **Jogador:** nada (tela vazia).
- **Tarefas:** T-M1-01…T-M1-09. **Dependências:** D-12 ✔, aparelho **Moto G75** ✔ (D-20); **a instalar:** Android Build Support no Unity (SDK/NDK/JDK embutidos), **opções de desenvolvedor + depuração USB** no celular, driver USB no Windows.
- **Entregáveis:** projeto (URP 2D, portrait, Input System, TMP, Test Framework), `.gitignore`/`.gitattributes` ajustados (ver [Qualidade](Qualidade%20e%20Fluxo%20de%20Trabalho.md) §3), asmdefs, `ProjectSetup.cs`, cenas `Bootstrap` e `Game` vazias, `SafeAreaController`, `CLAUDE.md`/`README.md` do projeto, APK de desenvolvimento instalado.
- **Técnica:** versão do Unity (D-12), nome do pacote Android, *Scripting Backend* IL2CPP/ARM64 só no release (Mono no dev para iterar rápido), *Orientation: Portrait* travada.
- **Arte/áudio:** nenhum. **Testes:** APK instala/abre sem erros no aparelho; um teste EditMode trivial roda (pipeline de testes vivo).
- **DoD:** REQ-TEC-01, REQ-TEC-08, REQ-PLT-01/02/05 verificados em aparelho; commit inicial no GitHub; **marco A parcial**.
- **Riscos:** módulo Android/JDK/SDK; assinatura/keystore (**nunca** commitar); Git LFS mal configurado. **Fora:** qualquer regra de jogo. **Esforço:** 25–40 h.

### M1b — Art Spike (paralelo ao M1/M2)
- **Objetivo:** provar o estilo e a escala **antes** de gastar arte. **Jogador:** nada (é documento + imagens).
- **Tarefas:** T-M1B-01…05 (+ **T-M1B-06: teste do PixelLab** — criar conta, ver créditos gratuitos e termos de uso comercial, gerar o anão e uma animação de golpe; ver [Arte](../design/Direcao%20de%20Arte%20e%20Assets.md) §5). **Dependências:** D-05 ✔ (mockup), ferramenta de pixel art (D-10/D-23).
- **Entregáveis:** **folha de estilo** (paleta-mestre ~32–48 cores, rampa de cristal, anão em 1 pose + 1 quadro de golpe, cristal da cave 1, painel e botão da UI, fonte), teste de escala em 270×480 × 360×640 num aparelho real, `CREDITS.md` inicial e tabela de fontes.
- **Arte:** sim (essencial). **Testes:** olhar a folha **no celular** em brilho baixo; Bruno aprova o estilo.
- **DoD:** D-05 e D-14 (resolução/PPU) decididas e registradas; paleta e regras do [Art doc](../design/Direcao%20de%20Arte%20e%20Assets.md) validadas.
- **Riscos:** IA inconsistente (já anotado); perfeccionismo → **limitar a 1–2 sessões**. **Fora:** animação completa, outros cristais. **Esforço:** 12–25 h.

### M2 — Protótipo do loop no mapa (graybox) — *novo, D-28*
- **Objetivo:** primeiro jogável: o anão anda pelo mapa, minera grupos de cristais pela regra D-27, soma lascas; HUD e painel de debug. **Jogador:** assiste o loop rodando sozinho no celular.
- **Plano detalhado:** [Marco 2 - Prototipo do loop.md](Marco%202%20-%20Prototipo%20do%20loop.md) (etapas E1–E6, como testar, critério de pronto).
- **Dependências:** M1 ✔ (D-25); D-26, D-27, D-29 ✔. **Fora:** loja, toque, crítico, combo, estagiário, save, arte. **Esforço:** 30–45 h.

### M3 — Regras e economia (antigo M2, adaptado ao mapa — D-28)
- **Plano detalhado (2026-10-21):** [Marco 3 - Regras e economia.md](Marco%203%20-%20Regras%20e%20economia.md). Escopo reduzido pelas D-35…D-37: Picareta, Velocidade, Mineração, Picareta Dupla e **Botas** (nova), relatório de economia e spec do estagiário; compras pelo painel de debug. O texto abaixo é o escopo original (histórico).
- **Objetivo:** todas as fórmulas e estados em C# puro, testados e simulados; economia calibrada. **Jogador:** nada (ferramenta).
- **Tarefas:** T-M2-01…T-M2-12. **Dependências:** M1 (asmdefs/testes). **D-01 (decidida):** a economia **não é calibrada agora** — `Balance_GDD_v1` entra como valores iniciais; o simulador **mede e reporta**, sem bloquear (T-M2-12 vira "relatório de observações").
- **Entregáveis:** `GameState`, calculadores (Reward/Upgrade/Cost), `MiningSimulation`, `PurchaseService`, `IClock`/`IRandom`, `BalanceConfig` com **`Balance_GDD_v1`** e **`Balance_v2`**, **simulador econômico** (relatório tempo×caverna para 3 perfis), **especificação do estagiário** (diagrama), painel de debug (esqueleto), suíte EditMode.
- **Técnica:** [Arquitetura](Arquitetura%20Tecnica.md) D-T1…T5; `double`; determinismo por *seed*.
- **Testes:** custo/efeito de **todos** os níveis de **todas** as linhas (inclui nível máximo e fora do limite); crítico: 100.000 golpes ≈ 5% (REQ-PRO-02); penalidade em 100 quebras; reward = f(caverna, Mineração, penalidade); compra uma vez; simulador alcança a caverna 10 em tempo dentro da meta nos 3 perfis.
- **DoD:** **marco A** (RM): fórmulas cobertas e reproduzíveis sem cena; economia aprovada pelo Bruno (D-01); spec do estagiário aprovada.
- **Riscos:** recalibração é problema de design (pode levar mais de uma rodada); ambiguidades de C-04. **Fora:** qualquer cena, arte, UI. **Esforço:** 35–55 h.

### ~~M3 — Vertical slice da caverna 1 (graybox)~~ — absorvido (D-28)
> O loop jogável em graybox foi para o **M2**; os itens que dependem de regra (toque, recompensa com economia, testes de fluxo completos) vão para o **M3** e o **M4**. O texto abaixo fica como histórico; o "lote de 5" foi substituído pelo mapa (D-26).
- **Objetivo:** loop completo jogável no celular com formas simples. **Jogador:** toca, vê a barra cair, quebra 5 cristais, o lote se renova.
- **Tarefas:** T-M3-01…T-M3-10. **Dependências:** M2.
- **Entregáveis:** cena `Game` com 5 cristais em posições aleatórias espaçadas, minerador principal (sprite provisório), timer automático, toque, barra de vida, lasca somada ao HUD básico, avanço e novo lote, lasca "voa" **opcional**.
- **Arte/áudio:** retângulos/círculos coloridos, *beep* provisório. **Testes:** PlayMode: toque→dano; intervalo exato (REQ-MIN-01); quebra→recompensa **sem duplicar** (RM Marco B); lote sempre com 5 posições (REQ-LOOP-02); 10 minutos em aparelho sem erro; input responsivo.
- **DoD:** **marco B** (RM): caverna 1 jogável em dispositivo, sem duplicação de recompensa, HUD provisório; Bruno joga e responde "é divertido o bastante para seguir?".
- **Riscos:** posicionamento aleatório sobrepondo (requisito de espaçamento); *Safe Area*; "o 1º cristal demora 100 s" (C-01) — usar balanço v2. **Fora:** loja, crítico, combo, estagiário, arte final. **Esforço:** 40–55 h.

### M4 — Loja, melhorias, HUD e save mínimo ✅ (aprovado em 2026-10-21, D-49)
- **Plano detalhado (2026-10-21):** [Marco 4 - Loja e save.md](Marco%204%20-%20Loja%20e%20save.md). Decisões D-41…D-44: números pt-BR com M/B/T; Estagiário e Letrado bloqueados na loja; save só do progresso, com versão e escrita atômica. O texto abaixo é o escopo original (histórico).
- **Objetivo:** gastar lascas e sentir o efeito. **Jogador:** compra Picareta/Velocidade/Mineração/Picareta Dupla; HUD reflete.
- **Tarefas:** T-M4-01…T-M4-09. **Dependências:** M3, M2.
- **Entregáveis:** `ShopView` + `UpgradeRowView` reutilizável, botão Loja com selo, **camada de bloqueio de toque** que **não** pausa a mineração, HUD completo (4 indicadores), compras com validação/limite, **save mínimo** (estado do GDD §11.1 sem migração), atualização imediata do HUD.
- **Testes:** compra processada uma vez (duplo-toque); sem lascas = sem compra; limites; mineração continua com a loja aberta (REQ-UI-04, P0); estado salvo e relido; HUD atualiza imediatamente. **Estagiário e Letrado entram nos itens da loja desabilitados** (placeholder).
- **DoD:** REQ-ECO-02…05,07,08; REQ-UI-01,02,04,06 verificados. **Riscos:** `Canvas` e *rebuilds*; formato de números (D-11). **Fora:** estagiário, Letrado, AD, polimento. **Esforço:** 40–50 h.

### M5 — Feedback de mineração e arte final da caverna 1 ✅ (aprovado em 2026-10-22, D-59)
- **Plano detalhado (2026-10-21):** [Marco 5 - Toque e feedback.md](Marco%205%20-%20Toque%20e%20feedback.md). Pela D-46, o M5 é em **graybox** (toque, crítico, combo, feedback, som provisório); a **arte da caverna 1 sai deste marco** e entra depois do Art Spike (M1b), que corre em paralelo. Regras do toque: D-47. O texto abaixo é o escopo original (histórico).
- **Objetivo:** **fazer bater gostoso.** **Jogador:** crítico, combo, impacto, lasca voando, caverna 1 bonita.
- **Tarefas:** T-M5-01…T-M5-12. **Dependências:** M3/M4, M1b (estilo aprovado).
- **Entregáveis:** crítico (REQ-MIN-03), combo `xN` + ×10, vibração do cristal e do cenário, *shake* de tela, lasca que voa lentamente ao HUD, faíscas, quebra do cristal, SFX finais da caverna 1, **arte final da caverna 1** (anão, picareta [5 faixas], cristal "Vidro", lasca, fundo, UI base), dicas de primeiro uso (toque), *pooling* dos números flutuantes.
- **Testes:** latência toque→resposta < 100 ms (medir), teste estatístico do crítico, *profiler* sem alocação por quadro; **playtest informal de *feel*** com 2–3 pessoas sem explicação.
- **DoD:** pilares "Toque com impacto" e "Leitura imediata" passam nos testes de aceite do [Vision](../design/Visao%20e%20Escopo%20do%20MVP.md) §3; Bruno aprova o *game feel*. **Riscos:** polir cedo demais (limitar à caverna 1); sincronia animação×golpe (código manda). **Fora:** estagiário, outras cavernas. **Esforço:** 45–65 h.

### M6 — Descida em corrida ← *novo, D-52/D-56*
- **Objetivo:** trocar o mapa aberto pela **descida em corrida**: câmera de frente (D-50), o anão correndo na direção dela por um corredor de caverna 3D com cara de pixel art (D-53), parando para bater nos cristais da fila. **Jogador:** o mesmo loop (golpe, toque, crítico, combo, loja, save), na tela nova.
- **Plano detalhado:** [Marco 6 - Descida em corrida.md](Marco%206%20-%20Descida%20em%20corrida.md) (E1 câmera e render ✅ · E2 fila no núcleo · E3 jogo no corredor · E4 feedback e DBG · E5 economia e celular). Proposta e motivos: [Proposta - Descida em corrida](Proposta%20-%20Descida%20em%20corrida.md).
- **Dependências:** M5 ✅ (D-59). **Fora:** arte de verdade (Art Spike, depois), estagiário (M7). **Esforço:** 40–60 h.
- **GDD v5 (D-85):** a E5 (relatório de economia) compara também com o `Balance_GDD_v5` (D-87, aprovada; vida e lascas da D-92) e aplica o toque de 33% junto com ele (D-86, aprovada).

### M7 — Estagiário
- **Objetivo:** segundo minerador sem estados quebrados. **Jogador:** contrata, vê o estagiário atacar o cristal à frente.
- **Tarefas:** T-M6-01…T-M6-07. **Dependências:** M5 (arte do principal), **spec do M2**.
- **Entregáveis:** `MinerAgent` + `CrystalLot` com estados, `InternController`, barra de vida do estagiário, silhueta/capacete distinto, regras REQ-INT-01…06.
- **Testes:** **matriz** (principal primeiro, estagiário primeiro, simultâneo, 5º cristal, troca de lote, compra no meio do lote, pausa); nenhum alvo duplicado; nenhuma espera infinita (P0); quebra do estagiário não conta p/ penalidade; Dupla não altera estagiário (REQ-INT-05).
- **DoD:** matriz verde + 30 min de sessão sem travar; **Bruno testa**. **Riscos:** C-08 mal resolvido. **Fora:** mais mineradores. **Esforço:** 35–50 h.
- **GDD v5 (D-85) — muda bastante:** o estagiário **bate no mesmo cristal do anão** e avança junto (somem a reserva de alvo, a matriz de ordem de quebra, o R-24 e a [spec do mapa](../design/Spec%20-%20Estagiario%20no%20mapa.md)); nasce no cristal atual do anão; dano online 10% × nível de uma picareta. Ganha **árvore própria** de melhorias (Picareta Offline, Velocidade Offline, Mineração Offline, Picareta Dupla do Estagiário 20.000, Picareta Online), compradas depois de contratar: a loja precisa de uma aba ou seção "Estagiário". O **offline** é dele (só ele minera fora do jogo), mas o cálculo offline fica no M9; o M7 já salva os níveis do estagiário. Perguntas: G3, G4, G9, G10. Ordem com o M8: D-88.

### M8 — Progressão: Letrado, penalidade, passagem e 10 cavernas
- **Objetivo:** descer de verdade. **Jogador:** compra o Letrado, passa de caverna, vê a cor mudar; sente a penalidade.
- **Tarefas:** T-M7-01…T-M7-09. **Dependências:** M4, M6, **D-07 (parede?)**.
- **Entregáveis:** `CaveDefinition[10]` com *recolor* provisório, `Letrado`, `Penalty` (ícone), passagem (botão âmbar ou parede), reset da penalidade, bloqueio após a cave 10, tela "fim da descida", dica do Letrado/penalidade.
- **Testes:** nova caverna remove a penalidade e gera 5 cristais novos; Letrado *n* só dobra o dano na caverna *n*; bônus não acumula (REQ-ECO-06); progressão 1→10 **alcançável** no simulador e jogando; botão desabilitado/habilitado.
- **DoD:** **marco C parcial:** caverna 1→10 funcional com arte provisória; REQ-CAVE-01…03. **Riscos:** C-01/C-09 mal calibrados fazem o final inatingível — rodar o simulador em cada alteração. **Fora:** arte final das caves 2–10. **Esforço:** 35–45 h.
- **GDD v5 (D-85):** Letrado custa `100 × 10^(n−1)`; **Letrado *n* leva à caverna *n+1*** e o nível 10 leva ao **"coração da montanha", o fim do jogo**; só pode ser comprado **depois de quebrar 100 cristais na caverna atual**. A passagem depende do Letrado **e da parede** (D-07, G7); na descida, a parede fica no fim do corredor. A **penalidade** saiu das regras do v5 (G2, D-19). Vida e lascas por caverna dependem da G1. Os biomas (E8 do Art Spike, D-72) entram aqui.

### M9 — AD simulado, persistência completa, offline e ajustes
- **Objetivo:** fechar os sistemas. **Jogador:** assiste (simulado) para ganhar boost; fecha e volta sem perder nada.
- **Tarefas:** T-M8-01…T-M8-10. **Dependências:** M7.
- **Entregáveis:** modal de AD (5 s, ×2,5, 30 s, recarga), selo do boost, `SaveService` completo (JSON versionado, escrita atômica, backup, migração), pausa/retomada, **offline (se D-04)**, Configurações (volume, tremor, vibração, apagar save com confirmação), créditos mínimos.
- **Testes:** fechar/minimizar não duplica bônus/compra/moeda (RM sem. 8, P0); save corrompido recupera; versão futura tratada; teste de migração; manipular o relógio do aparelho; sessão longa (2 h).
- **DoD:** **marco C** (RM): loja, estagiário, penalidade, progressão, boost e save integrados; testes de estados simultâneos feitos. **Riscos:** relógio do aparelho, *backgrounding* do Android. **Fora:** SDK de anúncios real. **Esforço:** 40–55 h.
- **GDD v5 (D-85):** o offline existe e é **só do estagiário**, por até 4 h, com as melhorias offline dele (resolve a D-04; detalhes na G8). Pela D-89 (aprovada), o boost do AD vai para o marco "Bônus temporários" e este marco fica com save completo, offline e ajustes.

### Bônus temporários (D-89, aprovado em 2026-10-31) ← *GDD v5*
- **Objetivo:** o lado "10% roguelite" do v5. **Jogador:** combo longo dá bônus (×10: +10% de dano por 5 s · ×30: +15% por 10 s · ×50: crítico garantido por 20 s · ×100: "Frenesi", +50% por 30 s), sempre visível na tela; quebrar um **cristal raro** abre uma janela com **2 melhorias temporárias** para escolher.
- **Técnica:** modificadores com tempo no núcleo (o mesmo mecanismo serviria ao boost do AD); indicador de bônus ativo no HUD; o `crystal_glass_rare` do Bruno (D-77) como arte do raro.
- **Posição:** depois do M8 (D-89). **Já decidido:** bônus quando o combo para, cumulativos (D-96); raro com 15% e bônus de 60 s (D-97); leva também o boost do AD. **Pendências:** G11 (acúmulo), G12 (lista de melhorias), G13 (base dos 15% e pausa). **Esforço:** a estimar quando a G12 for respondida.

### M10 — Arte, áudio e integração visual das cavernas 2–10
- **Objetivo:** os 10 ambientes e a UI final. **Jogador:** cada caverna parece outro lugar; interface com cara de jogo.
- **Tarefas:** T-M9-01…T-M9-10. **Dependências:** M5 (assinatura), pode **começar em paralelo desde M5** e rodar até M10.
- **Entregáveis:** 9 cristais, 9 pisos/paredes/props, UI final (*skin*), VFX completos (boost, penalidade, combo ×10), áudio final, `CREDITS.md` completo, auditoria de paleta/tamanho.
- **Testes:** prints de cavernas distinguíveis sem HUD (pilar "Descida perceptível"); legibilidade no aparelho; sem *draw calls* explosivos.
- **DoD:** assets aprovados e licenças registradas (marco do RM sem. 9). **Riscos:** custo de arte (cortar conforme RM §7.1). **Esforço:** 60–90 h.

### M11 — Balanceamento, desempenho e playtest
- **Objetivo:** "dar gosto" e rodar bem. **Jogador:** progressão compreensível, sem travadas.
- **Tarefas:** T-M10-01…T-M10-08. **Entregáveis:** relatório de balanceamento (simulador + playtest), ajustes em `Balance_v2.x` com **registro de cada mudança**, Profiler em aparelho intermediário, **playtest com 5–8 pessoas** (20–30 min), lista de bugs P0–P2.
- **DoD:** sem P0; loop e progressão compreendidos; metas de ritmo do [Econ](../design/Economia%20e%20Progressao.md) §4 atingidas ou renegociadas. **Riscos:** ajustes em cascata. **Esforço:** 35–50 h.

### M12 — Regressão e Release Candidate
- **Objetivo:** build confiável. **Entregáveis:** AAB/APK RC, regressão completa, instalação limpa e atualização, sessão longa, remoção do debug, *checklist* do MVP (GDD §14) atendido.
- **DoD:** **marco D**: P0 zerados, P1 críticos zerados, *build* reproduzível, documentação mínima. **Esforço:** 25–40 h.

### M13 — Publicação
- **Objetivo:** chegar à loja (D-18). **Anúncios reais ficam fora (D-21, 2026-10-21):** o botão AD continua só dando o boost de velocidade; o time ainda vai definir os ads. O boost fica atrás de uma interface (`IBoostSource`) no M8 para a rede de anúncios entrar depois, **sem mexer na regra**.
- **Entregáveis:** conta de desenvolvedor Google Play (taxa única — **conferir valor/regras atuais**), política de privacidade, ícone, *screenshots*, descrição, classificação etária, assinatura do app (keystore com backup), teste interno → fechado → aberto.
- **Riscos:** requisitos de loja mudam (verificar na época — D-22); rejeição na revisão. **Esforço:** 15–30 h (voltou à faixa original).
- **Pós-MVP (quando os ads forem definidos):** SDK da rede, consentimento/LGPD, testes de falha/sem rede, 25–40 h adicionais **[estimativa]**.

### M14 — Pós-lançamento
Backlog em §6 (P1/P2). Reavaliar com dados reais.

### M15 — Builds de teste para o pessoal (Firebase App Distribution) ← *D-84, 2026-10-24*
- **Objetivo:** um comando no PC do Bruno gera o APK com versão automática e manda para um grupo de testadores no Firebase; eles atualizam sem desinstalar e sem perder o save. **Jogador:** o pessoal recebe as versões novas por e-mail.
- **Plano detalhado:** [Plano - Builds de teste](Plano%20-%20Builds%20de%20teste.md) (E1 versão automática e versão na tela · E2 script `publish-test.sh` · E3 primeira rodada com o pessoal; perguntas P1–P4 em aberto).
- **Posição:** posto no fim do planejamento pelo Bruno (2026-10-24). Não depende de nenhum marco e **pode ser puxado antes**: o lugar natural é logo antes do playtest do M11 (5–8 pessoas). Até lá, APK de desenvolvimento passado à mão (Drive). **Fora:** build no GitHub Actions, chave definitiva e Play Store (M13). **Esforço:** 5–7 h.

## 4. Caminho crítico e paralelismo

**Caminho crítico:** aprovações (D-01, D-12) → **M1 ✅ → M2 ✅ → M3 ✅ → M4 ✅ → M5 ✅ → M6 (descida) → M7 → M8 → M9 → M11 → M12 → M13** (renumerado pela D-56).

**Em paralelo (fora do caminho crítico, mas com prazo):**

| Trilha | Começa | Termina | Observação |
|---|---|---|---|
| Art Spike → arte da caverna 1 | M1b | M5 | **Bloqueia M5** |
| Arte das cavernas 2–10, UI final, VFX | M5 | M9/M10 | Cortável (RM §7.1) |
| Áudio (coleta, edição, *licenças*) | M3 | M9 | Provisório até M5 |
| Registro de licenças | M1b | M11 | Obrigatório (RM §5.1) |
| Spec do estagiário | M2 | M2 | Bloqueia M6 |
| Documentação de decisões | contínuo | — | Padrão do Underworld Towers |

## 5. Detalhamento de tarefas (priorizadas)

Prioridade: **P0** = caminho crítico/obrigatório · **P1** = importante · **P2** = desejável. Tamanho: S ≤ 3 h · M 3–8 h · L > 8 h.

### M1 — Fundação
| ID | Tarefa | REQ | Pri | Tam | Depende | Validação |
|---|---|---|---|---|---|---|
| T-M1-01 | Aprovar versão do Unity e instalar **Android Build Support** | REQ-TEC-01 | P0 | S | D-12 | Hub mostra módulo |
| T-M1-02 | Criar projeto 2D URP em `miners-idle` **sem sobrescrever `docs/`** | REQ-TEC-01 | P0 | S | T-M1-01 | Abre sem erros |
| T-M1-03 | `.gitignore` Unity + `.gitattributes` (Force Text, LFS só onde faz sentido — Qualidade §3) | REQ-TEC-08 | P0 | S | T-M1-02 | `git status` limpo após abrir |
| T-M1-04 | Estrutura de pastas + asmdefs (`Core` sem engine references) | REQ-TEC-06 | P0 | M | T-M1-02 | Compila; `Core` rejeita `using UnityEngine` |
| T-M1-05 | `ProjectSetup.cs` reproduzível (portrait, URP 2D, Input System, TMP, nome/pacote) | — | P1 | M | T-M1-04 | Reaplicar não muda nada |
| T-M1-06 | Cenas `Bootstrap` e `Game` vazias + `SafeAreaController` | REQ-PLT-05 | P0 | M | T-M1-04 | Safe Area correta no aparelho |
| T-M1-07 | Assembly de testes EditMode/PlayMode + 1 teste trivial | REQ-TEC-07 | P0 | S | T-M1-04 | Test Runner verde |
| T-M1-08 | APK *Development* instalado no aparelho | REQ-PLT-02 | P0 | M | T-M1-06 | Abre sem erros (marco A parcial) |
| T-M1-09 | `CLAUDE.md` e `README.md` do projeto (regras de trabalho herdadas) | — | P1 | S | — | Revisado pelo Bruno |

### M1b — Art Spike
| ID | Tarefa | REQ | Pri | Tam | Depende |
|---|---|---|---|---|---|
| T-M1B-01 | Decidir ferramenta de pixel art e instalar | REQ-ART-07 | P0 | S | D-10 |
| T-M1B-02 | Paleta-mestre (âncoras GDD + rampas) | REQ-ART-02 | P0 | M | T-M1B-01 |
| T-M1B-03 | Anão (pose + 1 golpe), cristal cave 1, painel/botão, fonte com acentos | REQ-ART-03 | P0 | L | T-M1B-02 |
| T-M1B-04 | Teste de escala 270×480 × 360×640 no aparelho | REQ-PLT-01 | P0 | M | M1 (T-M1-08) |
| T-M1B-05 | `CREDITS.md` + tabela de fontes/licenças | REQ-ART-07 | P1 | S | — |

### M3 — Regras e economia (antigo M2; resumo; o plano detalhado será escrito no início dele. Os IDs `T-M2-*` abaixo são históricos)
T-M2-01 spec do estagiário (diagrama) · T-M2-02 `GameState`/`IClock`/`IRandom` · T-M2-03 calculadores de custo/efeito de todas as linhas · T-M2-04 `RewardCalculator` (+ penalidade/Mineração) · T-M2-05 `MiningSimulation` (tick determinístico, crítico, toque) · T-M2-06 `PurchaseService` · T-M2-07 `BalanceConfig` (v1 literal + v2) · T-M2-08 **simulador econômico** (3 perfis) · T-M2-09 **teste estatístico do crítico** · T-M2-10 testes de limites/fora-do-limite · T-M2-11 painel de debug (esqueleto) · T-M2-12 **relatório de calibração** para aprovação (D-01). **P0** todas, exceto T-M2-11 (P1).

### M3–M9 (granularidade de recurso; cada marco ganha plano próprio em `docs/planning/Marco N — …` antes de começar, como nos projetos anteriores)
- **M3:** cena + câmera + `MiningView` · geração do lote (5 posições aleatórias espaçadas, *seeded*) · `CrystalView` + barra · minerador provisório · `IMiningInput` · HUD básico · avanço/renovação · build + teste em aparelho. (P0)
- **M4:** `ShopView`/`UpgradeRowView` · bloqueio de toque sem pausar · compras e limites · HUD completo · save mínimo · selo "novo". (P0; selo P1)
- **M5:** crítico · combo · *shake* · lasca voando · VFX · SFX · arte caverna 1 · dicas · *pooling*. (P0 exceto dicas/pooling P1)
- **M7:** `MinerAgent` · `CrystalLot` · `InternController` · matriz de testes · arte do estagiário. (P0)
- **M8:** `CaveDefinition[10]` · Letrado · Penalidade + ícone · passagem · fim da descida · reset. (P0)
- **M9:** AD · save completo · offline (D-04) · configurações · créditos. (P0, offline P1 se D-04 = não)

### M10–M13 (resumo)
Arte cavernas 2–10 · UI final · VFX/áudio finais · `CREDITS.md` · auditoria · balanceamento · Profiler · playtest · regressão · RC · loja.

## 6. MVP × pós-MVP

| MVP (obrigatório) | MVP- (mínimo, depende de aprovação) | Pós-MVP (P1) | Talvez (P2) |
|---|---|---|---|
| Tudo de [Visão §8](../design/Visao%20e%20Escopo%20do%20MVP.md) item 1–8 | Offline (D-04), tutorial mínimo, configurações, créditos | Prestígio ("Nova Descida"), cristal dourado, cristais-chefe, música, missões/conquistas, daltonismo, idiomas, mais unidades de produção, anúncios reais, iOS | Recompensa diária, eventos sazonais, ranking, nuvem |

## 7. Esforço e premissas

| Marco | Horas (foco) |
|---|---|
| M0 | 8–12 |
| M1 | 25–40 |
| M1b | 12–25 |
| M2 (protótipo do loop) | 30–45 |
| M3 (regras e economia) | 35–55 |
| M4 | 40–50 |
| M5 | 45–65 |
| M6 (descida em corrida, D-52) | 40–60 |
| M7 | 35–50 |
| M8 | 35–45 |
| M9 | 40–55 |
| M10 | 60–90 |
| M11 | 35–50 |
| M12 | 25–40 |
| M13 | 15–30 |
| M15 (builds de teste, D-84) | 5–7 |
| **Total** | **≈ 440–650 h** (≈ **22–33 semanas a 20 h/sem**; recalculado em 2026-10-21 com o M2 novo) (o RM estima 420–460 h para M1–M11, sem M1b, M12 e sem a recalibração) |

**Premissas (a validar — C-18, D-17):** (1) horas de desenvolvimento **focado com o Claude escrevendo o código** e o Bruno revisando/testando — em 2026-10-20 não há medida de velocidade neste projeto; (2) **uma pessoa**;
(3) o aparelho Android e as ferramentas existem; (4) a arte é produzida (não comprada); (5) **sem calendário**: converter em semanas só depois de o Bruno informar quantas horas/semana tem para este projeto (hoje há 2 projetos irmãos em andamento);
(6) refazer estas faixas **após M1** e **após M3** (padrão do RM).

## 8. Ordem de execução recomendada para o início

1. **Aprovar** D-01, D-05, D-12, D-02/D-03 e revisar estes documentos (M0).
2. **M1** (Unity + Android + APK vazio) **em paralelo com M1b** (Art Spike — não depende do Unity). ✅ M1 aprovado em 2026-10-21.
3. **M2** (protótipo do loop no mapa) — primeiro marco em que o Bruno **vê o jogo rodando** (D-28).
4. **M3** (regras e economia, adaptadas ao mapa) com a **spec do estagiário** e o simulador.
5. A partir daí, 1 marco por vez, **plano do marco** (`docs/planning/Marco N — …`) → implementação → testes → "Como testar" para o Bruno → aprovação.
