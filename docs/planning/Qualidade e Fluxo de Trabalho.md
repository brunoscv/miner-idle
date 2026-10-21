# Qualidade e fluxo de trabalho

> Proposta de 2026-10-20 (**[REC]**), com práticas **herdadas** dos projetos irmãos marcadas **[herdado]** e as do Roadmap original **[DOC]**.
> Nenhuma configuração foi aplicada; `CLAUDE.md`, `.gitignore` e scripts só serão criados com a aprovação do M1.

## 1. O que reaproveitar e o que adaptar dos projetos anteriores

| Prática | Origem | Decisão para o Miners Idle |
|---|---|---|
| Jogável antes de sistema; vertical slice cedo | we-had-a-plan → Underworld Towers | **Adotar** (M3 é o 1º marco jogável) |
| Plano por marco em `docs/planning/`: objetivo, decisões, etapas, critério de pronto | idem | **Adotar** |
| Decisões registradas **com data**; valores de regra não mudam sem decisão | idem | **Adotar** — e é mais crítico aqui (a economia é regra) |
| **Cada etapa termina com algo que roda**; marco termina com critério de pronto verificado | Underworld Towers | **Adotar** |
| README atualizado a cada marco | idem | **Adotar** (curto; o do we-had-a-plan chegou a ~490 KB) |
| Assembly Definitions por área; dependências só descem | Underworld Towers | **Adotar** |
| Config do projeto em código (`ProjectSetup.cs`) reaplicável em batch | idem | **Adotar** (M1) |
| Testes que **jogam pelos mesmos comandos do jogador** + atalhos F10 por feature | Underworld Towers, we-had-a-plan | **Adaptar:** `IMiningInput`/comandos compartilhados por teste e painel de debug; atalhos de debug por feature (dar lascas, pular caverna, forçar crítico, avançar tempo) |
| **Espelho de CI** (rodar testes numa cópia do projeto) | idem | **Adiar:** só se o Editor aberto atrapalhar; começar com Test Runner + script batch |
| "Como testar" ao fim de cada implementação (cena, menu, passos numerados) | feedback do Bruno (memória) | **Adotar** |
| Nunca editar `Assets/` durante um *batch* com *sync-back* | feedback | Só vale se o espelho for adotado |
| Teste quebrado pela mudança é **corrigido**, nunca apagado | ambos | **Adotar** |
| Assets de terceiros em `_Vendor/`, **mover sempre com o `.meta`**, licenças registradas | Underworld Towers | **Adotar** |
| Textos em dados; Input System desde o dia 1 | lições | **Adotar** |
| Playtest sem explicar nada, assistindo calado | Boas práticas | **Adotar** (M5 e M10) |
| Board no GitHub, responsável sempre o dono, **mostrar a lista antes de criar** | we-had-a-plan | **Adotar se aprovado (D-15)** |
| O que **não** trazer: gerador procedural, "pronto para rede", Blender/rig, cartas, 3D | — | Não se aplicam |

## 2. Git e ramificação

- **Repositório:** `main` estável; trabalho em ramos curtos `feat/m3-vertical-slice`, `fix/…`, `docs/…`, `art/…`; **um marco = uma sequência de commits pequenos**; PR/merge para `main` ao fechar o marco (PR é opcional trabalhando sozinho, mas ajuda a revisar o diff).
- **Commits (convencionais, em pt-BR ou inglês — coerente com o repositório):** `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `art:`, `chore:`, `balance:` (mudança de valor de regra, **sempre** com a decisão citada). Primeira linha ≤ 72 caracteres.
- **Valores de balanceamento:** `balance: <o quê> (D-nn)` — nenhum valor de regra muda sem decisão registrada [herdado].
- **Nunca commitar:** `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Builds/`, `*.apk`, `*.aab`, **keystore (`*.keystore`, `*.jks`) e senhas**, `*.csproj/*.sln` gerados.

## 3. `.gitignore` e `.gitattributes` para Unity (proposta)

- **`.gitignore`:** padrão Unity (como o do Underworld Towers: `[Ll]ibrary/`, `[Tt]emp/`, `[Oo]bj/`, `[Bb]uild*/`, `[Ll]ogs/`, `[Uu]serSettings/`, `*.csproj`, `*.sln*`, `*.apk`, `*.aab`, `*.keystore`, `*.jks`, `.vs/`, `.idea/`) **+** `ArtSource/_cache/` se existir.
- **Manter versionados:** `Assets/`, `Packages/manifest.json` e `packages-lock.json`, `ProjectSettings/`, **todos os `.meta`**; Editor em *Version Control = Visible Meta Files* e *Asset Serialization = Force Text*.
- **`.gitattributes`:** normalização de fim de linha; `*.unity`, `*.prefab`, `*.asset`, `*.meta` como **texto** (diffs legíveis; **não** colocar cenas/prefabs em LFS — o RM sugere "LFS protege cenas/prefabs", mas isso piora diffs e *merges*; **[REC] contra**, D-16).
- **LFS:** áudio (`*.wav`, `*.ogg`, `*.mp3`), fontes brutas de arte (`*.psd`, `*.kra`, `*.aseprite` só se ficarem grandes) e vídeos. **PNG de pixel art** são pequenos (KB): **[REC] manter fora do LFS** para não pagar atrito/cota à toa — decisão do Bruno (D-16); o Underworld Towers usa LFS para PNG por causa de texturas 3D.

## 4. Documentação

- **Idioma: pt-BR** (consistente com GDD, Roadmap e os dois projetos; o Bruno prefere explicações do porquê).
- **Fontes de verdade (uma por assunto):**

| Assunto | Documento |
|---|---|
| Requisitos e restrições (IDs) | [Requisitos e Restrições](../design/Requisitos%20e%20Restricoes.md) |
| Visão, pilares, MVP | [Visão e Escopo do MVP](../design/Visao%20e%20Escopo%20do%20MVP.md) |
| Economia e progressão | [Economia e Progressão](../design/Economia%20e%20Progressao.md) |
| Arte e produção de assets | [Direção de Arte e Assets](../design/Direcao%20de%20Arte%20e%20Assets.md) |
| Telas e UX | [Interface — Telas](../design/Interface%20-%20Telas.md) |
| Arquitetura | [Arquitetura Técnica](Arquitetura%20Tecnica.md) |
| Marcos e tarefas | [Roadmap](Roadmap.md) |
| QA, git, processo | este documento |
| Decisões, riscos, questões | [Decisões e Riscos](Decisoes%20e%20Riscos.md) |
| Descoberta (histórico) | [Descoberta — Relatório](Descoberta%20-%20Relatorio.md) |

- Os `.docx` originais **permanecem** como fonte histórica; quando uma decisão substituir um número do GDD, o **registro de decisões** manda e o GDD não é editado (nem convertido) sem autorização.
- **Plano por marco:** `docs/planning/Marco N — nome.md` com Objetivo · Decisões · Etapas · **Como testar** · Critério de pronto.
- **README do projeto:** status por marco, como abrir o projeto, links. Curto.

## 5. Rastreamento de tarefas

- **Padrão proposto:** tarefas `T-Mx-nn` do [Roadmap](Roadmap.md) viram **checklist no plano do marco**; opcionalmente espelhadas em **GitHub Issues/Projects** (D-15). Uma etapa só é marcada ✅ depois de **validada pelo Bruno** (padrão do we-had-a-plan).
- **Rastreabilidade:** `REQ → T-Mx-nn → teste/validação → marco`. Cada commit de implementação cita o `T-` ou `REQ-`.

## 6. Definition of Ready / Done

**Pronto para começar (Ready):** plano do marco escrito e aprovado · decisões bloqueantes resolvidas · requisitos citados com `REQ-` · critério de pronto verificável · dados/arte necessários existem ou há *placeholder* aprovado.

**Pronto (Done) — por tarefa:** compila sem erros nem avisos novos · testes do que mudou criados/atualizados e **verdes** · testado no Editor **e**, quando toca input/UI/performance, **no aparelho** · sem alocação por quadro nova no loop de mineração · valores de regra em dados (não em código) · documentos tocados atualizados · "Como testar" escrito · commit convencional.

**Pronto — por marco:** critério de pronto do [Roadmap](Roadmap.md) verificado **pelo Bruno** · regressão (§7) verde · APK do marco instalado · README/Decisões atualizados · riscos revistos.

## 7. Estratégia de testes e regressão

| Camada | O que cobre | Ferramenta | Quando |
|---|---|---|---|
| **EditMode (núcleo)** | Fórmulas (custo/efeito em **todos** os níveis e limites), recompensa, penalidade, crítico (100.000 golpes ≈ 5% **com *seed* fixa**), compra única, Letrado por caverna, estagiário (matriz), boost, save/migração, **acumulação de ponto flutuante** | Unity Test Framework | Desde M2; roda antes de todo commit |
| **Simulador econômico** | Curva tempo×caverna por perfil; "progressão alcançável"; compara `Balance_GDD_v1` × `v2` | Código de editor/teste | M2 em diante; roda a cada mudança de balanceamento |
| **PlayMode** | Tocar→dano→quebra→lasca; loja bloqueia toque mas minera; pausa/retomada; passagem; save/continuar | UTF + `IMiningInput` | M3–M11 |
| **Teste de regressão "dourado"** | Simulação com *seed* fixa e estado conhecido compara com um *snapshot* versionado (economia e eventos); qualquer diferença exige `balance:` + decisão | Teste EditMode com arquivo de referência | Desde M2 |
| **Dispositivo** | Input real, Safe Area, performance, áudio, sessão longa, retomada do app, instalação limpa/atualização | Android físico + Profiler | Toda semana com mudança funcional (RM §6) |
| **Manual (playtest)** | Entendimento e prazer; protocolo: jogar **sem instrução**, observar calado, anotar travas | 2–3 pessoas (M5), 5–8 (M10) | M5, M10 |

**Não automatizar tudo:** *juice* (animação, partículas), arte e som são validados por olho/ouvido e no aparelho; automatizar só **regra**.

**Prevenir regressões:** testes do que mudou **sempre**; `PlayMode` mínimo ("fumaça": abre, minera 30 s, compra, salva, reabre) em todo commit de marco; teste dourado; *checklist* manual por marco (GDD §14).

**Validação de save/load [REQ-TEC-03]:** ida e volta; JSON truncado/inválido; versão futura; campos fora da faixa; migração *vN→vN+1* (um teste por migração); *kill* do app no meio da escrita (escrita atômica); relógio do aparelho para trás/frente; apagar progresso.

**Performance (metas provisórias):** 60 fps; 0 B alocados por quadro no loop; memória de textura ≤ ~100 MB; APK/AAB < ~60 MB; sessão de 30 min sem crescimento de memória; Profiler a cada marco.

**Verificação de build:** APK dev por marco; AAB no RC; instalar por cima da versão anterior (atualização com save antigo); instalar limpo; remover painel de debug; checar *Scripting Backend/ABI* e ícone/nome corretos.

## 8. Triagem de bugs e riscos

| Prioridade | Exemplos [DOC, RM §6.1] | Regra |
|---|---|---|
| **P0** | Perda de save, progressão impossível, moeda duplicada, cristal inalcançável, estagiário travado | **Bloqueia entrega** |
| **P1** | Cálculo errado, penalidade incorreta, layout quebrado, boost inválido | Corrigir antes do RC |
| **P2** | Polimento de partículas, transições, variações de áudio | Pós-MVP |

Fluxo: reproduzir → classificar → registrar (tabela do plano do marco ou *issue*) → corrigir com teste que falha antes e passa depois. Riscos: **registro vivo** em [Decisões e Riscos](Decisoes%20e%20Riscos.md) §4, revisado a cada marco.
