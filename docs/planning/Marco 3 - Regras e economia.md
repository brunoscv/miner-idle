# Marco 3 — Regras e economia (melhorias)

> **Plano aprovado pelo Bruno em 2026-10-21**, incluindo a interpretação do C-04 (nível = compras feitas).

> Plano de 2026-10-21, escrito depois da aprovação do M2 (D-33). Roadmap: [M3](Roadmap.md). Decisões que definem este marco: **D-35** (fórmulas do GDD com base ajustável), **D-36** (escopo), **D-37** (compras pelo debug), **D-34** (caminhada aprovada) — ver [Decisões e Riscos](Decisoes%20e%20Riscos.md). Requisitos: REQ-ECO-02, 03, 04, 07, 08, 10; REQ-INT-* (só a spec).

## Objetivo

As **melhorias** passam a existir nas regras: gastar lascas deixa o anão mais forte, mais rápido no golpe, mais rápido na caminhada e mais rentável. Tudo no núcleo, testado, com um **relatório de economia** que mede o ritmo. E o desenho de como um **estagiário** (2º minerador) vai dividir os alvos no mapa, para o M6.

**Para você:** abrir o jogo, juntar lascas, apertar **Comprar Picareta** no painel DBG e ver **DANO** subir no HUD e os cristais quebrando mais rápido. A loja de verdade é o M4.

## Decisões usadas

| # | O que fixa neste marco |
|---|---|
| D-35 | Formato das fórmulas do GDD; números no asset, mirando a 1ª compra em < 3 min; `Balance_GDD_v1` guardado para comparar |
| D-36 | Linhas: Picareta, Velocidade, Mineração, Picareta Dupla, **Botas** (nova); spec do estagiário só no papel |
| D-37 | Botões de compra e níveis/custos no painel DBG; HUD mostra DANO e VELOCIDADE atuais |
| D-34 | Caminhada base de 4 unidades/s mantida |
| C-04 (interpretação **confirmada pelo Bruno**, 2026-10-21) | **Nível = número de compras** (começa em 0). A próxima compra custa `custo inicial × crescimento^nível`. Ou seja, a 1ª compra custa o custo inicial. É a leitura usada na análise da economia |

## As cinco linhas de melhoria

Formato do GDD (REQ-ECO), com os números em asset. "Nível" = compras feitas.

| Linha | Efeito no nível *n* | Custo da próxima compra | Limite |
|---|---|---|---|
| **Picareta** | dano = dano base × 2ⁿ | inicial × 4ⁿ | máx. de compras |
| **Velocidade** | intervalo = max(mínimo, intervalo base − passo × *n*) | inicial × 3ⁿ | até chegar ao mínimo |
| **Mineração** | lascas por cristal × (1 + 1% × *n*) | inicial × 1,2ⁿ | máx. de compras (+100%) |
| **Picareta Dupla** | dano × 2 | preço único | 1 compra |
| **Botas** (nova, D-36) | caminhada = base × (1 + 10% × *n*) | inicial × 2ⁿ | máx. de compras |

**Valores (todos ajustáveis no asset; protótipo ≠ balanceamento):**

| Linha | `Balance_Prototipo` (ativo) | `Balance_GDD_v1` (referência) |
|---|---|---|
| Base do anão | dano 1 · golpe 1,0 s · caminhada 4 | dano 0,5 · golpe 5,0 s · caminhada 4 |
| Cristal | vida 10 · 1 lasca | vida 10 · 1 lasca |
| Picareta | 10 lascas, ×4, 19 compras | 50, ×4, 19 compras |
| Velocidade | 15, ×3, −0,05 s até 0,5 s (10 compras) | 200, ×3, −0,1 s até 0,5 s (45 compras) |
| Mineração | 10, ×1,2, +1%, 100 compras | 50, ×1,2, +1%, 100 compras |
| Picareta Dupla | 500 | 5.000 |
| Botas | 10, ×2, +10%, 10 compras | — (não existe no GDD: desligada) |

Ritmo esperado no protótipo: ~5 lascas/min no começo → **1ª compra em ~2 min**. O relatório da E4 mede de verdade.

## Como funciona (regras do núcleo)

- **Atributos do anão** (`MinerStats`): dano, intervalo, velocidade de caminhada e multiplicador de lascas são **calculados** a partir dos valores base + níveis. Nada fica "somado" em variável solta: muda um nível, recalcula tudo. Assim, nenhum bônus acumula duas vezes.
- **Compra** (`Purchase`): só conclui se houver lascas **e** o limite não foi atingido (REQ-ECO-08). Desconta, sobe o nível, recalcula os atributos e avisa (`UpgradePurchased`, `StatsChanged`, `ShardsChanged`). A resposta diz o motivo quando falha (sem lascas, no máximo). Uma chamada = no máximo uma compra; o bloqueio de duplo toque na interface é do M4.
- **Compra no meio da ação:** a simulação usa os atributos novos a partir do instante da compra. Se o novo intervalo já passou do tempo acumulado do golpe, o golpe sai no próximo `Tick`. Caminhada mais rápida vale do ponto onde o anão está.
- **Níveis no `GameState`** (junto de lascas, golpes, quebras): é o que o save vai guardar no M4.
- **Números:** `double`; o custo é comparado com uma folga mínima (`lascas ≥ custo − 1e-9`) para não falhar por arredondamento (Arquitetura, D-T4).

## Etapas

Cada etapa termina com algo que roda e só fica ✅ depois da sua validação.

| # | Etapa | Entrega | O que roda no fim |
|---|---|---|---|
| **E0** | **Testes pendentes da E5** (D-32) | Rodar EditMode e PlayMode no Unity, com o Editor fechado | 57 EditMode + 3 PlayMode verdes (ou correção do que falhar) |
| **E1** | **Melhorias no núcleo** | `UpgradeId`, `UpgradeDefinition` (custo inicial, crescimento, efeito, limite), `EconomyRules`, níveis no `GameState`, `MinerStats`, `Purchase` | Testes: custo e efeito de **todos os níveis de todas as linhas**, inclusive no limite e além dele; sem lascas = sem compra; saldo nunca negativo; Velocidade nunca abaixo do mínimo; Dupla só uma vez |
| **E2** | **Simulação usa as melhorias** | `MiningSimulation` lê `MinerStats`; eventos `UpgradePurchased` e `StatsChanged`; recompensa com Mineração | Testes: compra no meio do golpe e da caminhada; compras na mesma ordem dão o mesmo jogo (determinismo); 1 h simulada com compras, sem erro |
| **E3** | **Assets, HUD e debug** | `Balance_Prototipo` ganha as melhorias; novo `Balance_GDD_v1`; HUD atualiza DANO e VELOCIDADE; painel DBG com **Comprar Picareta / Velocidade / Mineração / Dupla / Botas** e níveis + próximo custo nas estatísticas | Play: juntar lascas (ou +100 no debug), comprar e ver o HUD e o ritmo mudarem |
| **E4** | **Relatório de economia** | Menu `Tools → Miners Idle → Debug → Relatório de economia`: simula 30 min com uma política "compra a mais barata que der", para os dois assets | Console: tempo da 1ª compra, lascas/min aos 5/10/30 min e níveis finais. Meta do protótipo: 1ª compra < 3 min |
| **E5** | **Spec do estagiário no mapa** | `docs/design/Spec - Estagiario no mapa.md` | Documento com diagrama de estados, regra de alvo com reserva, os casos da matriz (REQ-INT-01…05 adaptados ao mapa) e o que muda do GDD (a "espera" do REQ-INT-04) — para você aprovar |
| **E6** | **No celular** | `Rodar no celular` | Você joga 5–10 min comprando pelo DBG |

## Como testar (Bruno) — ao fim do marco

1. Play na `Bootstrap` (ou **Rodar no celular**). Abra o **DBG**.
2. Espere ~2 min (ou **Tempo ×5**): com 10 lascas, **Comprar Picareta**. LASCAS cai 10, **DANO** vai de 1 para 2, e cada cristal passa a cair em 5 golpes em vez de 10.
3. **Comprar Velocidade** (15): VELOCIDADE vai de 1,00s para 0,95s. **Comprar Botas** (10): o anão anda visivelmente mais rápido.
4. Sem lascas, o botão não faz nada e as estatísticas mostram o custo que falta.
5. **+100 lascas** algumas vezes e **Comprar Dupla** (500): DANO dobra; uma 2ª compra da Dupla não acontece.
6. `Tools → Miners Idle → Debug → Relatório de economia`: duas linhas no Console (protótipo e GDD v1) com a 1ª compra e lascas/min.
7. Test Runner: tudo verde.
8. Leia a spec do estagiário e aprove (ou ajuste) a regra.

## Critério de pronto

- [ ] E0: testes da E5 rodados no Unity.
- [ ] Custo e efeito de todos os níveis de todas as linhas cobertos por teste, inclusive limites.
- [ ] Compras pelo debug mudam o jogo e o HUD; nenhuma compra deixa saldo negativo ou passa do limite.
- [ ] Relatório de economia rodando; 1ª compra do protótipo < 3 min (senão, ajustar os valores do asset).
- [ ] Spec do estagiário aprovada.
- [ ] Rodou no Moto G75; README e registro de decisões atualizados; você validou o marco.

## Fora deste marco

Loja de verdade e save (M4) · toque, crítico, combo, feedback e arte (M5) · código do estagiário (M6) · Letrado, penalidade, cavernas e "Passar de caverna" (M7) · AD/boost (M8) · balanceamento fino (M10).

## Riscos

| Risco | Mitigação |
|---|---|
| Valores de protótipo viram "o balanceamento" sem querer | Estão marcados como protótipo (D-29/D-35); o `Balance_GDD_v1` fica ao lado para comparar; mudanças de verdade passam por decisão |
| Picareta ×2 por compra torna a caverna 1 trivial rápido | Esperado sem Letrado/cavernas (M7); o relatório mostra quando isso acontece |
| Compra no meio do golpe gera golpe duplo ou perdido | Teste específico na E2 |
| C-04 interpretado diferente do que você imaginava | Confirmar na aprovação deste plano |

## Esforço

**25–40 h** de desenvolvimento focado (o núcleo e o debug do M2 já existem; o grosso é a E1, os testes e a spec).
