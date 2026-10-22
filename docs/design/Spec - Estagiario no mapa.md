# Spec — Estagiário no mapa

> Escrita em 2026-10-21 no Marco 3 (E5). **Aprovada pelo Bruno em 2026-10-21 com as recomendações E-1…E-4 (D-39).** Implementação no **M6**. Resolve o achado **C-08** (a máquina de estados do estagiário não estava especificada) e adapta os REQ-INT-01…06 do GDD ao mapa (D-26, D-27).
> Convenção: **[DOC]** = vem do GDD · **[REC]** = recomendação, precisa da sua decisão.

## 1. Em uma frase

O estagiário é um **segundo anão** que anda pelo mesmo mapa e escolhe alvo pela **mesma regra** do principal ("lote, depois mais próximo"), **pulando o cristal que o outro já pegou**. Ele bate mais fraco, no mesmo ritmo, e as lascas dele vão para o mesmo saldo.

## 2. O que o GDD pede e o que muda no mapa

| ID | GDD (lote de 5, minerador parado) | No mapa |
|---|---|---|
| REQ-INT-01 | A 1ª compra cria o estagiário no cristal à frente; as seguintes só melhoram | **Igual.** A 1ª compra faz o estagiário aparecer ao lado do principal; as seguintes aumentam o dano dele |
| REQ-INT-02 | Mesmo intervalo de golpe do principal (inclui Velocidade) | **Igual** [DOC] |
| REQ-INT-03 | Nunca ocupa o cristal do principal; barra de vida própria; mesmas lascas | **Igual**, e vale para os dois lados: **ninguém pega o alvo do outro** (§4) |
| REQ-INT-04 | Depois de quebrar o seu alvo, **espera**; só avança quando o principal avança | **Muda** [REC]: no mapa não há "avançar". Ele escolhe o próximo alvo na hora, pela mesma regra (§5, decisão E-1) |
| REQ-INT-05 | Dano = 10% × nível de **uma** picareta do principal; a Picareta Dupla não afeta; o bônus do Letrado pode valer | **Igual** [DOC]; Letrado só no M7 |
| REQ-INT-06 | Silhueta parecida, com variação de cor/capacete | **Igual** [DOC]; no graybox, outra cor |
| REQ-CRY-06 | Quebras do estagiário não contam para a penalidade | **Igual** [DOC]; penalidade só no M7 |

**Por que a "espera" do GDD não serve no mapa:** no GDD os 5 cristais ficavam numa fila vertical, e a espera impedia o estagiário de passar à frente do principal. No mapa não existe "à frente": os dois andam livres. Se ele esperasse pelo principal, ficaria parado sem motivo visível, e é aí que o C-08 temia estados travados.

## 3. Atributos

| Atributo | Estagiário | Origem |
|---|---|---|
| Dano por golpe | dano de **uma** picareta do principal (Picareta, **sem** a Dupla) × 10% × nível do Estagiário (nível 10 = 100%) | REQ-INT-05 |
| Intervalo entre golpes | **o mesmo** do principal (Velocidade vale para os dois) | REQ-INT-02 |
| Caminhada | **a mesma** do principal (Botas valem para os dois) [REC, decisão E-2] | nova |
| Lascas por cristal | as mesmas do principal (Mineração vale) | REQ-INT-03 |
| Crítico (M5) | a definir no M5 | — |

Custo da linha Estagiário (GDD: `500 × 5^nível`, 10 compras) e os valores de protótipo entram no asset no **M6**.

## 4. Reserva de alvo (a regra nova)

- Cada anão tem no máximo **um alvo**, e enquanto o tem, o alvo está **reservado** para ele.
- Ao escolher alvo, cada anão aplica a regra D-27 (**menor lote; empate: mais próximo dele; empate: o que nasceu primeiro**) **sobre os cristais não reservados**.
- O alvo não muda até quebrar (D-27). Ninguém "rouba" o alvo do outro, nem o principal [REC, decisão E-3].
- **Mesma hora, mesma decisão:** quando os dois ficam sem alvo no mesmo instante, o **principal escolhe primeiro**, depois o estagiário. A ordem é fixa, e por isso o jogo continua determinístico.

Consequência: na prática, os dois limpam o lote mais antigo juntos, cada um no cristal mais perto de si. Quando o lote está acabando, quem ficar sem cristal livre daquele lote já vai para o próximo lote. Isso continua respeitando a ordem dos lotes, porque o próximo lote é o menor que sobrou livre.

## 5. Estados

Os mesmos três do principal (a mesma classe `Miner`):

```
              ┌────────── nenhum cristal livre ──────────┐
              ▼                                           │
 [Parado] ──(há cristal livre: escolhe e reserva)──► [Andando] ──(chegou ao alcance)──► [Batendo]
     ▲                                                                                    │
     └──────────── o alvo quebrou: libera a reserva, soma lascas, escolhe de novo ◄──────┘
```

- **Parado** só acontece quando **não há cristal livre**: o mapa está vazio, ou o único cristal é o alvo do outro. Ele fica onde está, sem seguir o principal, e sai desse estado no instante em que um cristal fica livre (nasceu um novo, ou o outro quebrou o dele e escolheu outro). **Não existe espera infinita:** cristais continuam nascendo até o máximo, e todo alvo reservado está sendo batido.
- **E-1 (decisão):** depois de quebrar, ele escolhe na hora (recomendado). A alternativa B ficaria mais perto do GDD: ele só pegaria cristais do **mesmo lote** do alvo do principal e, sem nenhum, ficaria parado esperando. B deixa o estagiário parado com mais frequência e pede um estado a mais, sem ganho claro.

## 6. Eventos (mudança de interface no M6)

Os eventos que hoje falam "do anão" passam a dizer **qual** anão: `TargetChanged(minerador, cristal)` e `HitLanded(minerador, cristal, dano)`. As views desenham um anão por minerador, e a barra de vida aparece no alvo de cada um (REQ-CRY-03). `CrystalBroken` e `ShardsChanged` continuam iguais. No debug: "Quebrar alvo do estagiário" e a linha de ordem para os dois (cores diferentes).

## 7. Matriz de testes (M6)

Do RM (semana 6), adaptada ao mapa. Todas em EditMode, com semente e cristais posicionados à mão:

| # | Caso | Esperado |
|---|---|---|
| 1 | Principal escolhe primeiro (os dois sem alvo no mesmo instante) | O principal fica com o melhor cristal; o estagiário, com o melhor entre os que sobraram |
| 2 | Estagiário comprado com o principal no meio de um cristal | Ele aparece, escolhe outro cristal e nunca o alvo do principal |
| 3 | Os dois quebram no mesmo instante | Cada um recebe a sua recompensa uma vez; o principal escolhe primeiro |
| 4 | Só 1 cristal no mapa | Um bate, o outro fica Parado; quando nasce outro cristal, o parado o pega na hora |
| 5 | Mapa vazio | Os dois Parados; nenhum erro; o primeiro cristal que nasce vai para o principal |
| 6 | Troca de lote | Com o lote 1 acabando, quem fica sem cristal do lote 1 pega o lote 2; nenhum cristal do lote 2 é pego enquanto há cristal **livre** do lote 1 |
| 7 | Compra de Velocidade/Botas com os dois em ação | Os dois passam a usar o atributo novo a partir do instante da compra |
| 8 | Compra de Picareta Dupla | O dano do principal dobra; o do estagiário não (REQ-INT-05) |
| 9 | Determinismo | O mesmo estado com `dt` pequeno ou grande, com dois mineradores |
| 10 | 30 min simulados | Nenhum alvo duplicado (invariante checada a cada evento), nenhum anão parado com cristal livre por mais de 0 s, lascas = soma das recompensas |

## 8. Decisões pedidas

| # | Pergunta | Recomendação |
|---|---|---|
| **E-1** | Depois de quebrar, o estagiário escolhe o próximo alvo **na hora** (A) ou só dentro do lote do principal, esperando se não houver (B)? | **A** |
| **E-2** | Botas valem para o estagiário? | **Sim** (os dois andam no mesmo ritmo; menos coisa para explicar) |
| **E-3** | O principal pode "roubar" o alvo do estagiário quando esse alvo seria o melhor para ele? | **Não** (reserva vale para os dois; sem roubo não há corrida nem alvo abandonado) |
| **E-4** | Quantos estagiários? | **Um** no MVP (GDD); mais mineradores ficam como P1 (Visão §7) |

## 9. Fora desta spec

Valores da linha Estagiário (entram no asset no M6) · crítico do estagiário (M5) · Letrado e penalidade (M7) · arte do estagiário (M6/M9) · mais de um estagiário (P1).
