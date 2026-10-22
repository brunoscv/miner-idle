# Miners Idle — documentação

> Índice criado em 2026-10-20 na **fase de planejamento**. Nenhum código, cena, asset ou configuração do Unity foi criado. Os documentos originais foram **preservados**.
> O nome de trabalho no GDD é "Idle Miner"; o repositório e este índice usam **Miners Idle** (decisão D-03 pendente).

## Documentos originais (fonte histórica — não alterar)

| Arquivo | Conteúdo |
|---|---|
| [GDD_Idle_Miner.docx](GDD_Idle_Miner.docx) | Game Design Document do MVP v1.0 (inclui o mockup da tela) |
| [Designer (2).png](Designer%20(2).png) | Mockup da tela (referência visual **oficial** do estilo — D-05) |
| [Roadmap_MVP_Idle_Miner_Unity_CSharp.docx](Roadmap_MVP_Idle_Miner_Unity_CSharp.docx) | Proposta de roadmap de 11 semanas, stack e testes |

## Planejamento (novo)

| Tema | Documento | Fonte de verdade para |
|---|---|---|
| Descoberta | [planning/Descoberta - Relatorio.md](planning/Descoberta%20-%20Relatorio.md) | Inventário, achados, contradições, lições dos projetos anteriores |
| Requisitos | [design/Requisitos e Restricoes.md](design/Requisitos%20e%20Restricoes.md) | **IDs `REQ-*`** e restrições (rastreabilidade) |
| Visão e MVP | [design/Visao e Escopo do MVP.md](design/Visao%20e%20Escopo%20do%20MVP.md) | Conceito, pilares, candidatos a sistema, **escopo do MVP** |
| Economia | [design/Economia e Progressao.md](design/Economia%20e%20Progressao.md) | Análise e método de calibração da economia |
| Arte | [design/Direcao de Arte e Assets.md](design/Direcao%20de%20Arte%20e%20Assets.md) | Estilo, resolução, especificação e produção de assets |
| Interface | [design/Interface - Telas.md](design/Interface%20-%20Telas.md) | Inventário de telas e UX |
| Arquitetura | [planning/Arquitetura Tecnica.md](planning/Arquitetura%20Tecnica.md) | Decisões técnicas e estrutura do projeto Unity |
| Roadmap | [planning/Roadmap.md](planning/Roadmap.md) | Marcos, tarefas, caminho crítico, esforço |
| Planos dos marcos | [Marco 1](planning/Marco%201%20-%20Plano%20da%20fundacao.md) · [Marco 2](planning/Marco%202%20-%20Prototipo%20do%20loop.md) · [Marco 3](planning/Marco%203%20-%20Regras%20e%20economia.md) · [Marco 4](planning/Marco%204%20-%20Loja%20e%20save.md) | Etapas, como testar e critério de pronto de cada marco |
| Spec do estagiário | [design/Spec - Estagiario no mapa.md](design/Spec%20-%20Estagiario%20no%20mapa.md) | Como o 2º minerador divide os alvos no mapa (resolve C-08; implementação no M6) |
| Referência de tela | [design/Referencia de tela - mapa.jpg](design/Referencia%20de%20tela%20-%20mapa.jpg) | Layout da tela com o mapa (D-26), bioma de gelo |
| Qualidade | [planning/Qualidade e Fluxo de Trabalho.md](planning/Qualidade%20e%20Fluxo%20de%20Trabalho.md) | Git, testes, DoR/DoD, triagem |
| Decisões e riscos | [planning/Decisoes e Riscos.md](planning/Decisoes%20e%20Riscos.md) | **Decisões a aprovar**, perguntas, riscos |

**Convenções:** **[DOC]** = escrito nos documentos originais · **[INT]** = interpretação · **[REC]** = recomendação ainda sem decisão. Valores de economia marcados "provisório" **não são balanceamento**.

## Por onde começar a ler

1. [Decisões e Riscos §2](planning/Decisoes%20e%20Riscos.md) — o que precisa da sua aprovação.
2. [Descoberta §8](planning/Descoberta%20-%20Relatorio.md) — contradições e achados (a economia do GDD não fecha).
3. [Roadmap](planning/Roadmap.md) — o que fazer em seguida.
