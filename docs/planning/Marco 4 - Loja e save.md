# Marco 4 — Loja, HUD e save mínimo

> **Plano aprovado pelo Bruno em 2026-10-21**, incluindo uGUI + TextMeshPro (D-45).

> Plano de 2026-10-21, escrito depois da aprovação do M3 (D-40). Roadmap: [M4](Roadmap.md). Decisões que definem este marco: **D-41** (formato de números), **D-42** (Estagiário e Letrado bloqueados na loja), **D-43** (o save guarda só o progresso), **D-44** (escrita segura desde já) — ver [Decisões e Riscos](Decisoes%20e%20Riscos.md). Tela: S03 em [Interface - Telas](../design/Interface%20-%20Telas.md). Requisitos: REQ-UI-01, 02, 04, 06; REQ-ECO-08; REQ-TEC-04 (parte do progresso).

## Objetivo

Duas coisas que fazem o protótipo virar jogo:
1. **Uma loja de verdade**, no lugar dos botões do DBG: o botão **LOJA** à direita, como no mockup, abre uma tela com cada melhoria, o efeito "de → para", o custo e o botão Comprar. A mineração continua por trás.
2. **Não perder o progresso:** fechar o app e voltar mantém lascas e melhorias.

**Para você:** jogar, abrir a Loja, comprar Picareta, fechar o app pelo celular, abrir de novo e ver as lascas e a Picareta como estavam.

## Decisões usadas

| # | O que fixa neste marco |
|---|---|
| D-41 | Números em pt-BR com M/B/T (o `NumberFormatter` atual vira o oficial) |
| D-42 | Estagiário e Letrado aparecem na loja apagados, com "Em breve" |
| D-43 | O save guarda lascas, níveis, golpes e quebras; o mapa nasce de novo ao reabrir |
| D-44 | Arquivo com versão e escrita atômica (temporário → troca); backup e migração no M8 |
| D-45 (era a D-13) | Interface em **uGUI + TextMeshPro**, que já é o que o HUD e o DBG usam desde o M1/M2 |

## A loja (S03)

```
┌──────────────────────────────┐
│ LOJA              LASCAS 312 │  ← saldo atualizado ao vivo
├──────────────────────────────┤
│ [ícone] Picareta     nv 2/19 │
│  Dano 4 → 8          [ 160 ] │  ← botão Comprar com o custo
│ [ícone] Velocidade   nv 1/10 │
│  0,95s → 0,90s       [  45 ] │
│ [ícone] Mineração    nv 8/100│
│  +8% → +9%           [  43 ] │
│ [ícone] Picareta Dupla  0/1  │
│  Dano ×2             [ 500 ] │  ← apagado se faltar lasca
│ [ícone] Botas        nv 2/10 │
│  Caminhada 4,8 → 5,2 [  40 ] │
│ [ícone] Estagiário  Em breve │  ← bloqueado (D-42)
│ [ícone] Letrado     Em breve │
├──────────────────────────────┤
│           [ VOLTAR ]         │
└──────────────────────────────┘
```

- **Estados do botão Comprar:** comprável · sem lascas (apagado, mostra o custo) · no limite ("máx.") · compra única já feita (Dupla: "comprada") · bloqueado ("Em breve").
- **O mundo não pausa:** a loja escurece o mapa por trás e bloqueia toques nele (REQ-UI-04, P0), mas o anão continua minerando e o saldo continua subindo enquanto ela está aberta.
- **Botão LOJA** à direita, com selo **"!"** quando há alguma melhoria comprável (como no mockup).
- **Um toque = uma compra** (REQ-ECO-08). Tocar de novo compra de novo, se der: em jogo idle, comprar várias vezes seguidas é desejado. Nenhum toque é processado duas vezes.
- **Textos em dados** (pt-BR): nome, rótulo do efeito e ordem de cada linha num ScriptableObject `ShopCatalog`; ícones provisórios (graybox) até a arte.
- Os botões **Comprar …** do DBG continuam: são o atalho rápido de teste.

## O save mínimo

- **O que guarda (D-43):** versão do arquivo, lascas, golpes, quebras e nível de cada melhoria (pelo nome da linha, não pela posição, para o arquivo sobreviver a uma linha nova).
- **Onde:** `Application.persistentDataPath/save.json` (JSON legível).
- **Como (D-44):** grava em `save.json.tmp` e só então troca pelo `save.json`. Se o app morrer no meio, o save anterior continua inteiro.
- **Quando grava:** depois de cada compra, a cada 30 s de jogo, ao minimizar o app (`OnApplicationPause`) e ao sair.
- **Ao abrir:** carrega o progresso e começa um mapa novo com ele. Sem arquivo → jogo novo. **Arquivo ilegível ou de versão desconhecida → jogo novo**, o arquivo ruim é renomeado para `save.json.bad` (para não ser sobrescrito sem chance de análise) e sai um aviso no log. Valores fora do lugar são corrigidos ao carregar: lascas negativas viram 0, nível acima do limite da linha cai para o limite.
- **Reiniciar no DBG** passa a manter o progresso (só refaz o mapa). Atalhos novos: **Salvar agora** e **Apagar save** (zera o progresso e começa de novo).

## Estrutura do código

| Camada | Peças |
|---|---|
| **Core** | `UpgradePreview` (por linha: nível, limite, custo da próxima, estado do botão, valor do efeito agora e depois da compra) · `GameState` ganha uma forma de ser recriado a partir do save, com validação |
| **Persistence** (C#, sem cena) | `SaveData` (v1) · `ISaveStore` + `FileSaveStore` (escrita atômica) · `SaveService` (carrega → `GameState`; salva) |
| **Game** | `MiningRunner` recebe o estado carregado e salva nos momentos acima · `ShopCatalog` (ScriptableObject com os textos e a ordem da loja) |
| **UI** | `ShopView` (tela, saldo, Voltar, camada que escurece e bloqueia toques), `UpgradeRowView` (prefab reutilizável de uma linha, REQ-UI-06), `ShopButtonView` (botão LOJA e o selo "!"), DBG com os atalhos novos |

A loja só se atualiza quando chega um evento (`ShardsChanged`, `StatsChanged`, `UpgradePurchased`), nunca por quadro: menos reconstrução de Canvas no celular.

## Etapas

| # | Etapa | Entrega | O que roda no fim |
|---|---|---|---|
| **E1** | **Prévia das melhorias** | `UpgradePreview` no núcleo | Testes: estado do botão em cada situação (comprável, sem lascas, no limite, Dupla comprada), efeito "de → para" de cada linha em vários níveis |
| **E2** | **Tela da loja** | `ShopCatalog`, `ShopView`, `UpgradeRowView`, botão LOJA com selo; setup monta tudo na cena Game | Play: abrir a loja, comprar, ver a linha e o saldo mudarem, fechar. Teste PlayMode: a mineração não para com a loja aberta (o tempo de jogo e as quebras avançam) |
| **E3** | **Save mínimo** | Assembly Persistence: `SaveData`, `FileSaveStore`, `SaveService`; carregar ao abrir e salvar nos momentos definidos; DBG: Salvar agora, Apagar save | Testes EditMode: ida e volta, arquivo ausente, corrompido, versão desconhecida, valores fora do lugar, `.tmp` sobrando. PlayMode: comprar, salvar, recarregar a cena e achar o mesmo progresso |
| **E4** | **No celular** | Rodar no celular | Você compra, fecha o app (deslizando para fora), abre de novo e o progresso está lá |

## Como testar (Bruno) — ao fim do marco

1. **Rodar no celular** (ou Play na `Bootstrap`).
2. No lado direito, o botão **LOJA**. Com 10 lascas, aparece o selo **"!"**.
3. Abra a loja: o mapa escurece por trás e o anão continua minerando (o saldo no topo da loja sobe sozinho).
4. Compre **Picareta**: o saldo cai, a linha passa para "nv 1" e "Dano 2 → 4", e o DANO do HUD vira 2. Linhas sem lasca ficam apagadas; Estagiário e Letrado mostram "Em breve".
5. **Voltar** fecha a loja.
6. Compre mais alguma coisa, **feche o app** (deslize para fora nos recentes), abra de novo: LASCAS e os níveis estão como antes; o mapa é novo.
7. DBG → **Apagar save**: o jogo recomeça do zero. DBG → **Reiniciar**: o mapa muda, o progresso fica.
8. Test Runner: tudo verde.

## Critério de pronto

- [ ] Loja com as 5 linhas e os 5 estados do botão; Estagiário e Letrado bloqueados.
- [ ] A mineração não pausa com a loja aberta (teste PlayMode, P0).
- [ ] Um toque = no máximo uma compra; sem lascas = sem compra; limites respeitados.
- [ ] Save com versão e escrita atômica; progresso volta ao reabrir; arquivo ruim não trava o jogo.
- [ ] Atalhos do DBG: Salvar agora, Apagar save; Reiniciar mantém o progresso.
- [ ] Rodou no Moto G75 (fechar e reabrir o app); README e decisões atualizados; você validou o marco.

## Fora deste marco

Progresso offline (M8, D-04) · backup e migração do save (M8) · engrenagem e Configurações (M8) · botão AD e boost (M8) · Estagiário (M6) e Letrado (M7) de verdade · toque, crítico, combo e feedback (M5) · arte e skin final da loja (M5/M9) · animações de entrada/saída do painel.

## Riscos

| Risco | Mitigação |
|---|---|
| R-13: perder o save ou duplicar lascas | Escrita atômica, arquivo ruim guardado como `.bad`, testes de ida e volta e de arquivo corrompido; salvar depois de cada compra |
| Loja cobrindo o HUD ou cortada no Moto G75 (tela ~9:20) | Layout dentro do Safe Area; conferir no aparelho na E4 |
| Reconstrução de Canvas a cada lasca | Atualizar só por evento; saldo é um texto só |
| Linha nova de melhoria quebrar saves antigos | Níveis salvos pelo nome da linha; linha desconhecida é ignorada ao carregar |

## Esforço

**30–45 h** de desenvolvimento focado (a regra de compra já existe desde o M3; o grosso é a tela e o save).

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| E1 Prévia das melhorias | ✅ validada pelo Bruno em 2026-10-21 (testes verdes no Editor) | Core: `UpgradePreview` (nível, limite, custo da próxima, estado do botão Comprável/Sem lascas/Máx., valor do efeito agora e depois da compra; `IsSinglePurchase` para a Dupla mostrar "comprada"; `IsEnabled` para linha desligada) e `MiningSimulation.Preview(id)`. `MinerStats` calcula também "com esta linha um nível acima"; `Purchases.CanAfford` é a mesma folga da compra, então a loja nunca mostra "sem lascas" quando a compra aceitaria. Testes: `UpgradePreviewTests` (6), incluindo "o que a prévia promete é o que a compra entrega" em todos os níveis de todas as linhas. Rodado fora do Unity: 82/82 do núcleo; compila. **No Unity ainda não rodou** (Editor aberto) |
| E2 Tela da loja | ⏳ | |
| E3 Save mínimo | ⏳ | |
| E4 No celular | ⏳ | |

**Como testar a E1:** Test Runner → EditMode → Run All (ou `bash tools/ci/run-tests.sh EditMode` com o Editor fechado): os 6 de `UpgradePreviewTests` verdes, junto com os anteriores. Nada visível no jogo ainda: a tela da loja é a E2.

