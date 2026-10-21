# Visão do jogo e escopo do MVP

> Proposta de 2026-10-20, **[REC]** salvo onde marcado **[DOC]**. Baseada no GDD v1.0; não substitui o GDD enquanto o Bruno não
> aprovar (ver [Decisões e Riscos](../planning/Decisoes%20e%20Riscos.md)). Requisitos em [Requisitos e Restrições](Requisitos%20e%20Restricoes.md).

## 1. Conceito de alto nível

**Miners Idle** é um incremental vertical de mineração para celular. Um anão minerador desce por **dez cavernas** de cristais cada
vez mais intensos, quebra cinco cristais por lote, junta *lascas* e as investe em seis linhas de melhoria até conseguir o conhecimento (o
**Letrado**) para atravessar a próxima parede — até chegar ao coração do vulcão. [DOC, GDD §Sumário]

**Em uma frase (para o jogador):** *"Cada toque e cada compra fazem a mina gritar mais alto — e a descida muda a tela inteira."*

## 2. Público e experiência

- **Público [DOC]:** quem curte idle/clicker, prefere comandos simples e progressão numérica. **[REC]** adicionar: jogadores de celular em
  sessões de 1–3 minutos (fila, intervalo) que voltam várias vezes ao dia.
- **Experiência:** uma mão, polegar; poder crescente **legível** (barras, números, brilho da picareta); descoberta visual a cada caverna.
- **Não é:** um jogo de gestão, de combate ou de colecionar. Isso protege o escopo.

## 3. Pilares de design

Os cinco do GDD são mantidos [DOC] e ganham um **teste prático** (como saber se o pilar está sendo cumprido):

| Pilar [DOC] | Teste de aceite [REC] |
|---|---|
| **Progresso constante** — a mineração automática mantém a economia ativa, inclusive com loja/modal | Deixar o jogo 60 s sem tocar mostra lasca nova e barra descendo; abrir a loja não zera nem pausa nada |
| **Toque com impacto** | Qualquer toque tem resposta visual **e** sonora em < 100 ms; combo e crítico são percebidos sem ler texto |
| **Leitura imediata** | Quem nunca viu o jogo diz, sem ajuda, "o que" está atacando, "quanto falta" e "o que posso comprar" |
| **Descida perceptível** | Prints da caverna 1 e da 5, sem HUD, são distinguíveis a 1 m de distância (cor, chão, luz) |
| **Decisões de investimento** | Em qualquer momento da caverna 1–3 há ≥ 2 compras plausíveis com custo/benefício diferente |

**[REC] Pilar de contenção** (para o projeto, não para o jogador): **"Uma moeda, uma tela, dez cavernas."** Qualquer ideia que exija segunda
moeda, segunda tela principal ou conteúdo além de 10 cavernas é pós-MVP.

## 4. Loop principal e camadas de progressão

| Escala | O jogador faz | O jogo entrega |
|---|---|---|
| **Segundos** | Toca; vê a barra cair; recolhe a lasca | Impacto, crítico, combo, lasca voando ao HUD |
| **Minutos** | Compra Picareta/Velocidade/Mineração; contrata o Estagiário; compara compras | Cristais caem mais rápido, o lote de 5 passa mais depressa |
| **Sessão (10–30 min)** | Compra o Letrado e atravessa a parede | Nova caverna: cor, chão, luz e cristal novos; penalidade some |
| **Longo (dias, **[INT]**)** | Retorna para ver o que o progresso offline rendeu (se aprovado, D-04) e perseguir a Picareta nível 20 e a Mineração máxima | Chegada ao vulcão (caverna 10) e "fim" do MVP |

**Fontes de lasca:** cristal quebrado pelo principal e pelo estagiário (único). **Ralos:** as 6 linhas de melhoria + Letrado + Dupla.
Detalhes e a análise numérica em [Economia e Progressão](Economia%20e%20Progressao.md). **Ritmo pretendido:** **[REC]** definir *metas de tempo*
por caverna **antes** das fórmulas (ver Economia §4); o GDD não declara metas, só fórmulas.

## 5. O que torna o jogo interessante além de clicar no cristal

Analisando o que o GDD **realmente** propõe de diferente [INT]:

1. **Passagem por conhecimento, não por contagem.** O Letrado é um *gate* que o jogador **compra** (não uma barra que enche): pede planejamento.
2. **Dois mineradores com uma dança espacial.** Principal e estagiário percorrem o mesmo lote com regras de espera — gera momentos visuais
   e decisões de quando comprar o estagiário.
3. **Poder tátil que migra.** O toque (10% do golpe) domina no começo e se dilui quando a Velocidade cresce: o jogo *ensina* que automatizar vale
   a pena. Precisa ser **intencional** (C-06).
4. **A descida como recompensa audiovisual:** 10 identidades de cor/ambiente culminando no vulcão.
5. **Penalidade como ritmo (se o objetivo for esse):** ficar demais na mesma caverna rende menos — empurra o jogador a investir no Letrado. Hoje não está
   justificado no GDD (C-09).

**Franqueza:** esses diferenciais são **modestos** perante o mercado de idle mining. O que sustenta o jogo é a **qualidade do feedback e da arte** (por isso
o plano coloca o *game feel* e a arte da caverna 1 cedo) e o **acerto do ritmo**, não a quantidade de sistemas.

**Diferenciais pós-MVP candidatos [REC]:** cristal dourado raro (descoberta), cristais-chefe ao fim de cada caverna, um segundo minerador desbloqueável, ciclo de
"nova descida" (prestígio) com bônus permanente.

## 6. Identidade visual e sonora (resumo; detalhes em [Direção de Arte](Direcao%20de%20Arte%20e%20Assets.md))

- **Visual:** pixel art de mina escura com **cristais emissivos** como únicas fontes de cor saturada; lanternas quentes como contraponto; HUD em molduras
  robustas. A direção (industrial × fantasia) **precisa de decisão** (C-05) — recomendação: seguir o mockup.
- **Áudio:** SFX curtos, graves e "de impacto" (rocha/metal/vidro), com timbre que sobe a cada caverna; **música é "poderia ter"**.

## 7. Candidatos a sistema — veredito

Legenda: **Custo** S ≤ 8 h · M 8–25 h · L > 25 h (horas de desenvolvimento focado; [INT]). **Veredito:** MVP · MVP- (versão mínima) · P1 (primeira atualização) · P2 (talvez) · ✗ (descartar).

| Sistema | Propósito / valor | Custo | Dependências | Veredito |
|---|---|---|---|---|
| Mineração manual por toque | Impacto imediato; ensina o jogo | M | Input, feedback | **MVP** [DOC] |
| Vida, durabilidade e recompensa do cristal | Núcleo da economia | S | Fórmulas, dados | **MVP** [DOC] |
| Melhorias de picareta/toque | Sensação de poder | M | Loja | **MVP** [DOC] (Picareta, Dupla) |
| Mineração passiva/automação | Define "idle" | S | Timer | **MVP** [DOC] |
| Unidades de produção (minerador/máquina/drone) | Profundidade de automação | L | Estagiário | **MVP-**: **só o Estagiário** [DOC]; mais unidades **P1** |
| Tipos de recurso e raridade | Variedade de economia | L | Inventário | **✗ no MVP** (uma moeda, [DOC] §12); raridade como **evento** (cristal dourado) → P1 |
| Variedades de cristal / tiers | Identidade das cavernas | M (arte) | Dados, arte | **MVP** [DOC] (10 cristais) |
| Novas áreas / cavernas / biomas | Progressão visível | L (arte) | Letrado, arte | **MVP** [DOC] (10 cavernas) |
| Equipamentos, ferramentas, melhorias permanentes | Colecionismo | L | Inventário | **✗** [DOC §12]; "brilho da picareta" é só visual do nível |
| Prestígio / renascimento | Retenção longa | M–L | Economia fechada, save | **P1** (só após a economia comprovada); **✗ MVP** [DOC] |
| Progressão offline | Expectativa do gênero | M | Save, relógio UTC, economia analítica | **MVP- se aprovado (D-04)**: teto de horas e eficiência fixos; senão P1 |
| Missões, conquistas, marcos | Retenção, metas | M | UI, save | **MVP-**: apenas **celebração de marco** (nova caverna); lista de missões **P1** |
| Pesquisa / árvore tecnológica | Profundidade | L | UI | **✗** (o Letrado já faz o papel) |
| Inventário e gestão | — | L | — | **✗** [DOC] |
| Eventos especiais e descobertas raras | Surpresa | M | Fórmulas, VFX | **P1** (cristal dourado) |
| Cristais-chefe / grandes encontros | Clímax | M–L | Arte, balanceamento | **P1** [DOC: chefes fora do MVP] |
| Recompensa diária e retenção | Retorno diário | M | Relógio, save | **✗ MVP** [DOC]; **P2** |
| Save/load e persistência | Não perder progresso | M | JSON, migração | **MVP** [DOC] |
| SFX, partículas, música e feedback | Percepção de impacto | M–L | Eventos, arte/áudio | **MVP**: SFX + partículas [DOC]; música **P1** |
| Acessibilidade, ajustes, QoL | Alcance e conforto | S–M | UI | **MVP-**: volume (mestre/SFX), tremor de tela on/off, vibração on/off, apagar save com confirmação; daltonismo e idiomas **P1** |

## 8. Definição do MVP (escopo congelado para aprovação)

**Dentro [DOC + REC mínimo]:**

1. Tela vertical responsiva com Safe Area; HUD de 4 indicadores; Loja; modal de AD; botão "Passar de Caverna".
2. Lote de 5 cristais aleatórios e espaçados; minerador principal frontal e bípede; mineração automática e manual; crítico; combo.
3. Loja com **6 linhas** (Picareta, Velocidade, Mineração, Estagiário, Letrado, Picareta Dupla); *Crítico* fica **fora** (D-06).
4. Estagiário funcional com a máquina de estados especificada; Letrado e passagem; penalidade; Picareta Dupla visual e funcional.
5. **10 cavernas** com cristal, piso e luz próprios; vulcão ao final; fim do MVP com tela de "fim da descida" **[REC]**.
6. Boost de AD **simulado** (5 s, ×2,5, 30 s, recarga).
7. Save local versionado; mineração durante loja/modal; pausa/retomada.
8. Sons de golpe, crítico, quebra, coleta, compra, combo; partículas essenciais.
9. **[REC] Mínimo de qualidade de vida:** configurações (volume, tremor, vibração, apagar save), dicas contextuais de primeiro uso, tela de créditos
   (licenças são requisito do Roadmap).
10. Painel de debug interno (economia, pular caverna, dar lascas) **removido do build de release**.

**Fora (pós-MVP):** tudo o que o GDD §12 lista, mais o que a tabela do §7 marcou P1/P2/✗.

**Critério "MVP satisfatório" [REC]:** um jogador novo, sem instrução, **entende o loop em < 2 min**, **compra a 1ª melhoria em < 3 min** de jogo (a regra de ouro
do começo de um incremental — hoje o GDD dá ~95 s por cristal sem toques, ver Economia), **passa a caverna 1 em ~10–20 min** e deseja ver a 2ª. E o *build* roda a ≥ 60 fps em um Android intermediário.
