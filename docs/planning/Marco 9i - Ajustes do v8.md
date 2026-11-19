# Marco 9i — Ajustes do v8

> Plano de 2026-11-18, aberto pelo Bruno ("pode fechar o marco 9h e vamos pros ajustes") depois do [Marco 9h](Marco%209h%20-%20Camera%20de%2060%20graus.md) (aprovado em 2026-11-18, D-328). É o **marco de ajustes da D-298**: com todos os blocos do GDD v8 no jogo, as melhorias anotadas na [lista de ajustes do v8](Ajustes%20do%20v8%20-%20lista.md) viram as etapas deste marco. Depois dele vêm os **testes longos** (sessões longas e Moto G75: M9b E6, M9d E4, M9e E4, M9g E4, M9h E3).

## Objetivo

**Polir o jogo do v8 antes dos testes longos.** Nenhum bloco novo: só o que o Bruno e o Claude notaram jogando os blocos A a G, em ordem de prioridade.

**Para você:** passar a sua lista de melhorias (as que você viu no M9e e nos outros), dizer a ordem, e testar cada etapa como nos outros marcos.

## Como o marco funciona (D-298)

1. **E0 — A lista fechada:** o Bruno passa as melhorias que anotou; elas entram na [lista](Ajustes%20do%20v8%20-%20lista.md) ao lado das do Claude. Juntos, a lista ganha prioridade (**alta**: o jogador sente toda hora; **média**: aparece às vezes; **baixa**: acabamento).
2. **As etapas:** cada grupo de ajustes do mesmo lugar do jogo vira uma etapa (E1, E2…), da prioridade alta para a baixa, cada uma terminando com algo que roda e com o "como testar".
3. Ajuste que mexe em **valor de regra** (custo, tempo, fórmula, balanceamento) só entra com **decisão registrada**, como sempre.
4. Um ajuste novo que aparecer durante o marco entra na lista; entra neste marco se for alta, senão fica para depois dos testes longos.
5. O marco fecha quando a lista estiver feita ou adiada com o Bruno, e a suíte de testes (sem as sessões longas) estiver verde.

## A lista hoje (2026-11-18)

| # | De | Onde | Ajuste | Prioridade sugerida |
|---|---|---|---|---|
| — | Bruno | M9e e outros | as "algumas melhorias" que você viu (a anotar) | — |
| 1 | Claude | Bomba (M9g) | arte própria do tier 2 (Dinamite, Pacote, Caixa, Barril); hoje o `icon_bomb` recolorido (D-315). **Depende do seu desenho** | média |
| 2 | Claude | Câmera de 60° (M9h) | os esteios somem de uma vez 0,6 m atrás do anão: o arco some no meio da tela. Sumir aos poucos | média |
| 3 | Claude | Câmera de 60° (M9h) | a metade de cima mostra ~9 m de corredor já minerado e vazio: marca das quebras, cacos, poeira | baixa |
| 4 | Claude | Transição de andar (D-327) | os cantos de cima escurecem quando a câmera sobe; só se incomodar | baixa |
| 5 | Claude | DBG → Câmera (M9h) | o "Recuo" é da F1 e na câmera do v8 leva a uma câmera misturada: tirar ou contar da câmera em uso | baixa (só DBG) |

## Etapas

### E0 — A lista fechada e ordenada

- O Bruno manda as melhorias dele (pode ser em texto solto: "no descanso, X"; "a barra do andar, Y"). O Claude escreve cada uma na lista com o lugar do jogo, propõe a prioridade e agrupa por lugar.
- Para ajuste de tela, o mockup vem antes do código, como nos blocos.
- **Pronto quando:** a lista tem prioridade em todas as linhas e as etapas E1… estão escritas aqui.

### E1… — definidas na E0

## Como testar (Bruno)

- **E0:** ler a lista ordenada e as etapas neste plano e aprovar a ordem.
- **E1…:** cada etapa traz o seu passo a passo aqui, como nos outros marcos.

## Critério de pronto

- Todos os ajustes de prioridade alta feitos; os outros feitos ou adiados com o Bruno (com o motivo na lista).
- Suíte de testes verde, sem as sessões longas (EditMode e PlayMode, como na rodada de 2026-11-18).
- README, Roadmap e lista atualizados; decisões novas registradas.

## Fora deste marco

- Os testes longos (sessões de horas e o Moto G75): vêm logo depois.
- Os dois `AdFlowTests` do anúncio antigo, ignorados desde o M9e: continuam pendentes (pedido do Bruno, 2026-11-18).
- Blocos novos do GDD e a [ideia guardada](Ideia%20guardada%20-%20Turma%20e%20conquistas.md).

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| Plano | 📝 escrito em 2026-11-18 | espera a lista do Bruno |
| E0 A lista fechada | ⏳ espera as melhorias do Bruno | 5 sugestões do Claude já na lista |
