# Análise — o que o GDD v6 muda no plano

> Análise de 2026-11-02. Compara o [GDD v6](../gdd%20v6.pdf) (versão 6.0, 31/10/2026, "Idle Miner Roguelite") com o [GDD v5](../gdd%20v5.pdf) e com as decisões já tomadas (D-92…D-126), que é o que o jogo faz hoje.
>
> **Estado:** o v6 passa a ser a referência (**D-127**, pedido do Bruno em 2026-11-02). **Nenhum valor de regra muda só por isso**, igual ao que a D-85 fez com o v5. As mudanças que mexem no que já está pronto viraram decisões pendentes (**D-128…D-131**) e as lacunas do v6 viraram perguntas para o autor do GDD (**G18…G25**), todas em [Decisões e Riscos §2.4](Decisoes%20e%20Riscos.md). O [Roadmap](Roadmap.md) tem uma nota "GDD v6" no M7, no M8, no M9, no Bônus temporários e no marco novo proposto.

## Resumo

- **A grande mudança é o lado roguelite, que virou um ciclo de runs.** A montanha se regenera todo dia; cada run é um dia. No fim do dia, as melhorias somem e o jogador leva só as **lascas vermelhas** (do coração da montanha), que compram **melhorias permanentes**. Até o v5, o "roguelite" era só o cristal raro. O v6 cria uma segunda moeda, um reset e uma árvore permanente. Nenhum marco nosso cobre isso.
- **O Letrado virou Mapa e a descida passou de 10 cavernas para 100 níveis.** O Mapa custa **zero** (só pede os 100 cristais), **não dobra mais o dano** e tem 100 níveis; o nível 100 leva ao coração da montanha. Os 10 cristais e biomas agora são faixas de 10 níveis (1–9 Vidro, 10–19 Solar… 90–100 Magma).
- **Os números mudaram todos, para uma curva bem mais suave.** Cristal com vida `1 × 1,25^(nível−1)` (era `10 × 8^`), Picareta `1 × 1,25^`, até o nível 50. A Mineração ficou cara (`20 × 5^`): isso responde a G17.
- **O estagiário tem as próprias melhorias, online e offline.** O dano dele não é mais "10% de uma picareta do anão". A árvore perdeu o "Offline" no nome e a Picareta Dupla dele. Offline de **6 h** (era 4 h).
- **O que o jogo já faz e o v6 confirma:** o primeiro golpe sem espera ("a primeira picaretada não tem contador" = o A2 da D-124), a penalidade fora (D-93) e o combo com bônus **acumulativos** (D-96, que agora está escrito no GDD).
- **O M8 já implementado precisa de ajuste antes da E4**: nome, custo e efeito do Letrado/Mapa, 100 níveis, números novos. A parede, a trava dos 100, os biomas, o save e o fim continuam servindo.

## Mudanças, uma a uma

### 1. Run, lascas vermelhas e melhorias permanentes (novo, sem marco)

| Ponto | GDD v6 |
|---|---|
| História | A montanha se regenera todo dia; cada run equivale a um dia |
| Fim da run | "Sempre que finalizar o dia" as melhorias são perdidas; **não diz o que encerra o dia** (G21) |
| Lascas vermelhas | Níveis 1–50: **5**; 51–100: **10**. **Não diz por quê** (por nível passado? no fim da run?) (G22) |
| Objetivo | Chegar ao coração da montanha (Mapa 100), o que só dá com melhorias permanentes acumuladas |

Melhorias permanentes (custo em lascas vermelhas):

| Melhoria | Custo | Efeito | Limite |
|---|---|---|---|
| Memória | `5 + 2 × nível` | −10 cristais na trava do Mapa | nível 10 (aos 10, a trava chega a **0**: G23) |
| Sangue da Montanha | `2 + 2 × nível` | +10% de dano por nível | nível 20 |
| Coração Ardente | `2 + 2 × nível` | +10% de lascas por nível | nível 20, "máximo 100%" (**20 × 10% = 200%**: G23) |
| Alma dos Anões | `2 + 2 × nível` | a run começa com +1 nível de Picareta por nível | nível 10 |
| Treinar Estagiário | `3 + 2 × nível` | +10% de dano do estagiário por nível | nível 10 |
| Promoção do Estagiário | 50 | a run já começa com o estagiário (pede Treinar 10) | compra única |

Para ter uma ideia (pela nossa convenção da D-38, nível = compras feitas): a árvore inteira custa 140 (Memória) + 420 (Sangue) + 420 (Coração) + 110 (Alma) + 120 (Treinar) + 50 = **1.260 lascas vermelhas**. Se fossem 5 por nível passado, uma run até o nível 50 daria 250.

**Contradição:** o §10 ("Fora do escopo do MVP") **ainda proíbe** "prestígio, renascimento ou meta-progressão além das dez cavernas" e "múltiplas moedas", exatamente o que o §1.3 e o §4.2 criam (G25). Também contraria a D-112 (fim: o jogo continua na caverna 10, sem reset).

**Efeito no plano:** pede um marco próprio (D-131), e o save passa a separar o que é da run (lascas, melhorias, nível, estagiário) do que é permanente (lascas vermelhas, melhorias permanentes, recordes). Isso mexe no save v2 do M8 e no save completo do M9.

### 2. Letrado → Mapa, 10 cavernas → 100 níveis (M8, já implementado)

| Ponto | GDD v5 / jogo hoje | GDD v6 |
|---|---|---|
| Nome | Letrado (D-113; G16 em aberto) | **Mapa**. A §7.3 ainda fala em "Letrado" e no ícone de livro, e o §2.1 em "conhecimento" (resto do v5) |
| Custo | `100 × 10^(n−1)` | **zero**: só pede os 100 cristais da caverna atual |
| Efeito | libera a passagem **e dobra o dano** | **só libera a passagem** |
| Limite | 10 (o 10 leva ao coração) | **100** (o 100 leva ao coração) |
| Trava | 100 cristais (D-93) | 100 cristais, **reduzidos pela Memória** |
| Parede | D-98 (2 × a vida do cristal da caverna nova) | o loop continua com "quebrar a parede que bloqueia a nova seção"; o resto é nosso |

**Consequência:** sem o ×2 de dano e com custo zero, o Mapa vira só a trava dos 100 cristais. Uma run até o fim pede **10.000 cristais** (100 × 100), sem contar a Memória; o relatório da E4 mede quanto tempo isso dá.

**Efeito no que já roda (M8 E1–E3):** o `UpgradeId.Letrado`, o `×2` do `MinerStats`, o custo no `Balance_GDD_v5`, `caveCount = 10`, a vida da última parede (D-114), a tela do coração (D-112) e o save v2 (caverna de 1 a 10). A parede, a trava, a faixa da caverna, o botão PASSAR DE CAVERNA e os biomas continuam como estão.

### 3. Cavernas em faixas de 10 níveis (M8, M10)

| Níveis | Cristal | Sensação (bioma, D-102) |
|---|---|---|
| 1–9 | Vidro | Entrada da mina |
| 10–19 | Solar | Galeria rochosa |
| 20–29 | Safira | Veio profundo |
| … | … | … |
| 90–100 | Magma | Coração do vulcão |

(A faixa 1–9 tem 9 níveis e a 90–100 tem 11, por causa do jeito de contar; vale a regra "o bioma troca nos múltiplos de 10".)

Os 10 biomas da D-102 servem do jeito que estão: cada um cobre 10 níveis. O que muda é a troca: hoje o escurecimento e a faixa "Caverna N · bioma" acontecem a cada passagem; com 100 níveis, a maioria das passagens não troca de bioma (D-129). A tabela de números por caverna (§6.1) ficou vazia no v6.

### 4. Números do anão e dos cristais (Balance)

| Item | GDD v5 (`Balance_GDD_v5`, o jogo hoje) | GDD v6 |
|---|---|---|
| Vida do cristal | `10 × 8^(c−1)` (D-92) | **`1 × 1,25^(nível−1)`** |
| Lascas por cristal | `1 × 3^(c−1)` (D-92) | **vida × 0,5** |
| Dano da Picareta | `5 × 2^(n−1)`, nível 20 | **`1 × 1,25^(n−1)`, nível 50** |
| Custo da Picareta | `50 × 4^(n−1)` | **"Dano Base × 2"** (não fica claro: G18) |
| Velocidade | `50 × 5^(n−1)`; 2,0 → 0,1 s | **`50 × 2^(n−1)`**; 2,0 → 0,1 s; nível 20 (2,0 − 20 × 0,1 = 0: G19) |
| Mineração | `50 × 1,2^(n−1)`, +100%/nível, até +1000% | **`20 × 5^(n−1)`**, igual no resto. **Responde a G17** |
| Picareta Dupla | 5.000, compra única | **sumiu da tabela**, mas o §3.3, a nota do §4.1 e o §8.3 ainda falam dela (G20) |
| Crítico (linha) | existe, sem custo (D-100, G16) | o §2.1 cita "chance de crítico"; não há linha na tabela (G16 continua) |
| Toque | 33% do golpe (D-86) | igual |
| Primeiro golpe | A2, sem espera (D-124) | "a primeira picaretada não tem contador": **confirma a D-124** |

Escala dos números do v6:

| Nível | Vida do cristal | Lascas | Parede (2 × vida, D-98) |
|---|---|---|---|
| 1 | 1 | 0,5 | 2 |
| 10 | 7,5 | 3,7 | 15 |
| 20 | 69 | 35 | 139 |
| 50 | 56.052 | 28.026 | 112.104 |
| 100 | ~3,93 bi | ~1,96 bi | ~7,85 bi |

**O que os números mostram:** a Picareta no nível 50 dá 56.052 de dano, a vida do cristal do nível 50. Sem as melhorias permanentes, a partir do meio da descida cada cristal pede cada vez mais golpes: é o desenho do roguelite (só se chega ao fundo com várias runs). No nível 1, o cristal tem vida 1 e paga **meia lasca**: o anão quebra cada um com um golpe. A LASCAS do HUD aparece inteira (D-68); a fração fica no estado.

### 5. Estagiário (M7)

| Ponto | GDD v5 + D-94/D-101 | GDD v6 |
|---|---|---|
| Dano online | 10% × nível × uma picareta do anão (linha Estagiário) | **das próprias melhorias**, online e offline |
| Linha Estagiário | `500 × 5^(n−1)`, nível 10 / 100% | igual no custo; o efeito virou "atributos base do jogo" (G24: o que ela faz depois de contratar?) |
| Árvore | Picareta/Velocidade/Mineração **Offline** + Dupla do Estagiário (20.000) | **Picareta, Velocidade, Mineração** (sem "Offline"); a **Dupla do Estagiário sumiu** (D-101 em xeque: G24) |
| Números da árvore | Picareta `50 × 4^`, dano `5 × 2^`, nível 20; Velocidade `50 × 5^`, 5,0 → 0,5 s; Mineração `50 × 1,2^` | iguais (os números do anão de antes, não os do anão novo) |
| Permanentes | — | Treinar Estagiário, Promoção do Estagiário (§1) |
| Alvo e avanço | mesmo cristal, avança junto | igual |

### 6. Offline (M9)

Teto de **6 h** (o v5 dizia 4 h; a D-99 usou os 4 h). Continua só do estagiário, com as melhorias dele. Entra na G15.

### 7. Bônus temporários

- **Combo:** os bônus agora são **acumulativos** no texto ("bônus temporário acumulativo"; "os bônus precisam estar visíveis"). Bate com a D-96. Resta a G11: o ×50 dá também o ×10 e o ×30? Iguais somam ou renovam?
- **Saiu** "a cada múltiplo de 10 toques, uma animação especial" (o §9 ainda tem "Combo ×10: animação especial"). O jogo já faz isso desde o M5 e não precisa mudar.
- **Cristal raro:** o próprio GDD marca a mecânica como "[ainda a definir]". A D-97 (15%, 60 s) veio das respostas do Apellon de 31/10; confirmar se continua (vai junto da G12/G13).

### 8. O que saiu do v6

O §1.3 "Escopo do MVP" (cinco cristais visíveis, dez cavernas, sete linhas), o §2.2 "Fluxo espacial" (cinco cristais aleatórios; já superado pela descida, D-52), a tabela de números por caverna (§6.1) e a "penalidade removida" na transição de caverna (já fora, D-93).

## Perguntas para o autor do GDD

| # | Pergunta | Onde |
|---|---|---|
| G18 | Custo da Picareta "Dano Base × 2": é 2 × o dano atual, em lascas (2 lascas no nível 1, 2,5 no 2…)? E o cristal do nível 1 com vida 1 e **0,5 lasca** é intencional? | §4.1, §3.1 |
| G19 | Velocidade: com 20 níveis de −0,1 s a partir de 2,0 s chega a 0. O limite é 19 níveis (para no 0,1 s) ou o último nível não faz nada? | §4.1 |
| G20 | Picareta Dupla do anão continua (5.000, compra única)? E a linha Crítico (custo, efeito, limite, a antiga G16)? | §3.3, §4.1, §2.1 |
| G21 | **O que encerra o dia (a run)?** Tempo real, o jogador escolhe "voltar para casa", chegar a um nível? O que zera: lascas, melhorias, Mapa (volta ao nível 1), estagiário? | §1.3 |
| G22 | **Lascas vermelhas:** 5 (níveis 1–50) ou 10 (51–100) **por quê**: por nível passado, por parede quebrada, uma vez no fim da run pelo nível mais fundo? | §3.1 (o segundo "3.1") |
| G23 | Melhorias permanentes: o "nível" do custo `2 + 2 × nível` é o atual (1ª compra custa 2) ou o próximo (custa 4)? Memória 10 zera a trava dos 100 cristais: intencional? Coração Ardente com 20 níveis de 10% passa do "máximo 100%": qual vale? | §4.2 |
| G24 | Estagiário: depois de contratar, o que a linha Estagiário (níveis 2–10, "atributos base") ainda dá, se o dano vem da árvore dele? A Picareta Dupla do Estagiário saiu? | §4.1, §5.4 |
| G25 | O §10 ainda põe fora do MVP o prestígio, a meta-progressão e as múltiplas moedas, que o §1.3 e o §4.2 criam. O §10 está desatualizado? | §10 × §1.3, §4.2 |

## Proposta para os marcos (para você decidir: D-128…D-131)

1. **Agora (D-127, feita):** o v6 vira a referência; o v5 fica como histórico.
2. **M8 (D-128, D-129, D-130):** uma etapa nova, **"E3b — O M8 no GDD v6"**, antes da E4: `Balance_GDD_v6` com os números do v6 (o v5 fica para comparação), Letrado → **Mapa** (custo zero, sem ×2 de dano, 100 níveis), cavernas como **níveis** (1–100, o bioma troca a cada 10), save v3 (nível até 100) e o coração no Mapa 100. A E4 (relatório 1→100) passa a medir até onde uma run chega **sem** melhorias permanentes, que é o número que o marco novo vai precisar.
3. **Marco novo "Dia e melhorias permanentes" (D-131):** fim da run, lascas vermelhas, tela de melhorias permanentes (as 4 do anão), reset, save separado em run × permanente. Entra **depois do M8 e antes do M7**: é o novo loop principal, e o M8 dá os níveis que ele usa. As 2 permanentes do estagiário entram no M7. Depende de G21–G23 e G25.
4. **M7 Estagiário:** árvore própria (Picareta, Velocidade, Mineração) com dano dele mesmo, online e offline; Treinar e Promoção. Espera a G24.
5. **Bônus temporários:** sem mudança de escopo; G11 em parte respondida, cristal raro a confirmar.
6. **M9:** offline com teto de 6 h; o save completo já nasce com a separação run × permanente.
