# Análise — o que o GDD v5 muda no plano

> Análise de 2026-10-24. Compara o [GDD v5](../gdd%20v5.pdf) (versão 5.0, "Idle Miner roguelite") com o [GDD v1](../GDD_Idle_Miner.docx), que foi a base de todo o planejamento, e com o que o jogo já faz.
>
> **Estado (2026-10-24):** o v5 é a referência (**D-85**). As perguntas G1–G10 e as decisões D-86…D-89 estão anotadas em [Decisões e Riscos §2.4](Decisoes%20e%20Riscos.md), cada uma com o marco em que precisa ser respondida; o [Roadmap](Roadmap.md) tem uma nota "GDD v5" no M6, M7, M8 e M9 e o marco proposto "Bônus temporários". Nenhum valor de regra mudou ainda.
>
> **Atualização (2026-10-31):** G1–G10 respondidas, viraram as decisões **D-92…D-101** ([Decisões e Riscos §1.7](Decisoes%20e%20Riscos.md)). Em resumo: vale a §3.1 (vida `10 × 8^(c−1)`, lascas `1 × 3^(c−1)`); a penalidade saiu; a Picareta Online saiu; a parede da passagem tem vida (2× o cristal da caverna nova); o offline mostra um modal de volta; a linha Crítico existe. O que este documento chama de "contradição" ou "não define" nas seções abaixo vale como histórico. Pontas ainda abertas: G11–G16.

## Resumo

- **Nada do que já roda quebra.** O jogo usa os valores de protótipo (`Balance_Prototipo`, D-29), não os do GDD; o v1 está guardado só para comparação (`Balance_GDD_v1`, D-35).
- **O v5 confirma escolhas que já fizemos:**
  - direção de arte em **fantasia medieval** (o v1 falava em ficção científica industrial à la Deep Rock Galactic; seguimos o mockup, D-05);
  - **câmera que anda com o minerador mostrando 5 cristais** em vez do "lote de 5 que se renova", que é o que a descida em corrida faz;
  - **parede para passar de caverna** (a C-03, conflito do v1 entre botão e parede, agora pende para a parede).
- **O que muda de verdade:** o **estagiário** foi redesenhado (mesmo cristal do anão, árvore própria de melhorias, mineração offline); entram **dois sistemas novos** (bônus do combo e cristal raro com escolha de melhoria temporária, o lado "roguelite"); a **economia** do anão e do Letrado mudou de números; a **penalidade** sumiu das regras.
- **O v5 tem contradições internas** (seção "Perguntas"), que precisam de resposta de quem escreveu o GDD antes dos planos do M7 e do M8.

## Mudanças, uma a uma

### 1. Números do anão e dos cristais

| Item | GDD v1 | GDD v5 | Hoje no jogo (protótipo) |
|---|---|---|---|
| Dano da Picareta | `0,5 × 2^(n−1)` | **`5 × 2^(n−1)`** (×10) | 1 inicial, ×2 por nível |
| Custo da Picareta | `50 × 4^(n−1)`, nível 20 | igual | `10 × 4^n` |
| Velocidade: intervalo | de 5,0 s a 0,5 s | **de 2,0 s a 0,1 s** | de 1,0 s a 0,5 s |
| Custo da Velocidade | `200 × 3^(n−1)` | **`50 × 5^(n−1)`** | `15 × 3^n` |
| Mineração | +1% por nível, máx. +100% | **+100% por nível, máx. +1000%** | +1% por nível |
| Toque | 10% do golpe | **33% do golpe** | 10% (D-47) |
| Letrado: custo | `10 × 10^(n−1)` | **`100 × 10^(n−1)`** | não existe |
| Picareta Dupla | 5.000, compra única | igual | 500 |
| Vida do cristal | `10 × 10^(c−1)` | **§3.1: `10 × 8^(c−1)`; tabela §6.1: `10 × 10^(c−1)`** (contradição) | 10 fixo |
| Lascas por cristal | `1 × 2^(c−1)` | **§3.1: `1 × 3^(c−1)`; tabela §6.1: `1 × 2^(c−1)`** (contradição) | 1 fixo |

**Efeito no plano:** a E5 do M6 (relatório de economia, pausada) passa a comparar com um `Balance_GDD_v5` novo, e não só com o v1. O toque de 33% é a única mudança que mexe num comportamento já aprovado (D-47); é só um valor no asset.

### 2. Combo com bônus (novo, mexe no M5 aprovado)

O v1 tinha combo só visual (D-48b). O v5 dá bônus temporários "ao alcançar valores específicos e parar":

| Combo | Bônus |
|---|---|
| ×10 | +10% de dano por 5 s |
| ×30 | +15% de dano por 10 s |
| ×50 | crítico garantido por 20 s |
| ×100 | "Frenesi": +50% de dano por 30 s |

O bônus precisa ficar visível na tela enquanto dura. É regra nova no núcleo (modificadores com tempo) e um indicador no HUD.

### 3. Cristal raro e melhoria temporária (novo sistema, o "10% roguelite")

Ao quebrar um **cristal raro**, abre uma janela com **2 opções de melhoria temporária com duração fixa**; o jogador escolhe uma. O v5 **não define** com que frequência o raro aparece, quais são as melhorias possíveis nem a duração. O `crystal_glass_rare` do Bruno já está guardado para isso (D-77).

**Efeito no plano:** não existe marco para ele. Combo com bônus, cristal raro e boost do AD são todos "bônus com tempo, visível na tela": faz sentido construí-los juntos (ver "Proposta para os marcos").

### 4. Estagiário redesenhado (M7)

| Ponto | GDD v1 (base do M7 e da spec) | GDD v5 |
|---|---|---|
| Alvo | nunca o cristal do anão; ataca o da frente, com barra própria; espera o anão | **ajuda no mesmo cristal** e avança junto com o anão |
| Onde nasce | no cristal à frente | no cristal atual do anão |
| Dano online | 10% × nível de uma picareta do anão | igual (10% × nível, até 100%) |
| Melhorias | só a linha Estagiário | **árvore própria**, comprada depois de contratar: Picareta Offline, Velocidade Offline, Mineração Offline, Picareta Dupla do Estagiário (20.000, dobra o dano dele), Picareta Online |
| Offline | não existia | **só o estagiário minera offline**, por até 4 h, com as melhorias offline |

O que isso simplifica: some a reserva de alvo, a matriz de ordem de quebra e o risco R-24. A [spec do estagiário no mapa](../design/Spec%20-%20Estagiario%20no%20mapa.md) fica obsoleta, e a posição "ao lado e um pouco atrás" (D-57) continua servindo.

O que isso aumenta: a loja ganha 5 linhas do estagiário (pede uma aba ou seção "Estagiário") e o offline passa a ser parte do estagiário.

### 5. Letrado, passagem e fim do jogo (M8)

| Ponto | GDD v1 | GDD v5 |
|---|---|---|
| Correspondência | Letrado *n* ↔ caverna *n* | **Letrado *n* ↔ caverna *n+1*** (o 1 leva à caverna 2); o 10 leva ao **"coração da montanha", o fim do jogo** |
| Exigência | nenhuma além do custo ("sem exigir 100 cristais") | **só pode comprar depois de quebrar 100 cristais na caverna atual** |
| Passagem | botão "Passar de Caverna" (e parede citada no loop: C-03) | botão ("Descer na Caverna" / "Passar de Caverna", com livro, âmbar) **e a parede que bloqueia a nova seção** |
| Efeito | dobra o dano só na caverna correspondente | igual |

Isso resolve em boa parte a **D-07** (parede ou só botão). Na descida, a parede entra naturalmente no fim do corredor.

### 6. Penalidade (M8)

O v5 **tirou a regra** ("após 100 quebras, lascas a 50%"): não está mais nos cristais nem nos estados de jogo. Mas ela **continua citada** no escopo (§1.3) e no feedback (seta vermelha, §9). Os "100 cristais" agora aparecem como condição para comprar o Letrado. Parece que a trava substituiu a punição, mas o documento não diz.

### 7. Offline (M9)

Resolve a **D-04**: há offline, feito só pelo estagiário, por até 4 h seguidas, com eficiência menor (as melhorias offline partem de 5,0 s por golpe e têm números próprios). Sem estagiário, não há ganho offline.

### 8. O que o v5 deixou de ter

O v5 não traz mais os critérios de aceite (§10 do v1), o estado persistente sugerido (§11), os riscos de balanceamento (§13), o checklist do MVP (§14) nem o resumo rápido (§15). Os nossos [Requisitos](../design/Requisitos%20e%20Restricoes.md) citam seções do v1 e precisam ser rebaseados no v5.

## Perguntas para o autor do GDD (antes dos planos do M7 e do M8)

| # | Pergunta | Onde |
|---|---|---|
| G1 | Vida e lascas por caverna: vale a fórmula da §3.1 (`10 × 8^`, `1 × 3^`) ou a tabela da §6.1 (`10 × 10^`, `1 × 2^`)? | §3.1 × §6.1 |
| G2 | A penalidade saiu de vez (e os 100 cristais viraram só a trava do Letrado) ou continua? Se continua, com que regra? | §1.3, §9 × §3.1 |
| G3 | Linha "Estagiário" (`500 × 5^(n−1)`, nível 10 / 100%) × "Picareta Online" (`100 × 2^(n−1)`, 10% × nível até 100%): a primeira só contrata e a segunda dá o percentual? Ou as duas dão dano? | §4 × §5.4 |
| G4 | Quando o estagiário dá o golpe final no cristal que os dois batem: a recompensa é uma só? Essa quebra conta nos 100 cristais da trava do Letrado? | §5.2 |
| G5 | Combo: "ao alcançar e parar" quer dizer que o bônus vem quando o combo acaba (vale o maior marco alcançado)? Bônus acumulam? | §3.3 |
| G6 | Cristal raro: com que frequência aparece? Quais melhorias temporárias existem e quanto duram? | §1.3, Sumário |
| G7 | A parede tem vida (anão e toque batem nela) ou é só a animação de quebrar ao apertar o botão? | §2.1, decisões consolidadas |
| G8 | Offline: além dos números próprios, há outra redução? Há tela de "enquanto você estava fora"? O nível do estagiário (online) conta offline? | §1.1, §2.3, §5.1 |
| G9 | "Sete linhas": Pesquisador = Letrado? E a linha Crítico (citada no §1.3 e no loop), existe? (Era a C-02 do v1 e continua) | §1.3, §2.1 |
| G10 | A Picareta Dupla do estagiário aparece no boneco (segunda picareta)? A §5.3 diz que a Dupla do anão não aparece nele | §5.3 × §5.4 |

## Proposta para os marcos (para você decidir)

1. **Agora, antes de seguir:** (a) registrar o GDD v5 como a referência no lugar do v1 (o v1 fica como histórico); (b) decidir o **toque de 33%** (só valor no asset, mexe na D-47); (c) criar o `Balance_GDD_v5` e incluir no relatório da E5 do M6. Os Requisitos são rebaseados no v5 quando as perguntas G1–G10 voltarem.
2. **M7 Estagiário:** replanejar pelo v5 (mesmo cristal, árvore própria, aba na loja). Fica **mais simples no alvo** e **maior na loja**. O offline em si fica no M9, junto do save completo e da proteção contra relógio mexido (R-14); o M7 já salva os níveis do estagiário.
3. **M8 Progressão:** Letrado pelo v5 (custo, *n* → *n+1*, trava dos 100 cristais, fim do jogo no 10), parede no fim do corredor, penalidade conforme G2. Os biomas (E8 do Art Spike) entram aqui.
4. **Marco novo, "Bônus temporários":** bônus do combo, cristal raro com a janela de 2 escolhas e, se fizer sentido, o boost do AD, que é o mesmo tipo de coisa (o M9 ficaria com save, offline e ajustes). Depende da G5 e da G6.
5. **M9:** offline pelo estagiário (resolve a D-04), save completo com caverna, estagiário e bônus, e ajustes.

A ordem M7 × M8 continua em aberto (pergunta anterior). Com o v5, o argumento pela progressão primeiro fica mais forte: o estagiário agora depende de economia de várias cavernas e do offline.
