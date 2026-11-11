# Marco 9b — Bônus temporários (começando pelo combo)

> Plano de 2026-11-10, **aprovado no mesmo dia** (P1…P8 → D-210…D-217). Base: GDD v7 ([`docs/gdd v7.pdf`](../gdd%20v7.pdf), §3.4 combo, §2.2 e §7.2 boosts, §8 efeitos; D-132) e as decisões já tomadas: **D-89** (o marco existe: combo, cristal raro e AD), **D-96** (o bônus vem quando o jogador **para de tocar**, pelos marcos alcançados; cumulativos), **D-177** (o bônus do combo não vale para o estagiário), **D-178** (o crítico garantido e os +% de dano valem no golpe automático e no toque), **D-179** (os +% do combo somam entre si e o total multiplica o resto do dano), **D-181** (bônus ativo continua contando o tempo com o app fechado), **D-182** (depois do M9), **D-183** (acúmulo em duas versões no DBG; padrão = o GDD) e **D-141** (coluna de ícones à esquerda, mockup antes).
>
> Este plano detalha o **combo** (E1–E3), que é o pedido de agora. O **cristal raro** (D-97, D-184…D-186, D-190) e o **boost do AD** (D-140, D-180, D-187, D-188) ficam esboçados nas E4 e E5, para usarem o mesmo mecanismo; o detalhe deles é escrito quando o combo estiver pronto (P1).

## Objetivo

**Tocar em rajadas vale a pena.** Hoje o combo só aparece na tela ("x37") e não muda nada (D-48b). Com este marco, parar o combo depois de passar de ×10, ×30, ×50 ou ×100 dá um bônus com tempo: mais dano ou crítico garantido. Os bônus ativos ficam numa coluna de ícones à esquerda, com o tempo correndo, e o HUD mostra o dano já com eles.

**Para você:** tocar até ×35, parar, e ver entrar +10% e +15% de dano (5 s e 10 s) na coluna da esquerda, o DANO do HUD subir em âmbar e voltar quando os bônus acabam.

## O que o GDD manda (os números não mudam)

| Combo alcançado | Bônus | Duração |
|---|---|---|
| ×10 | +10% de dano | 5 s |
| ×30 | +15% de dano | 10 s |
| ×50 | crítico garantido | 20 s |
| ×100 ("Frenesi") | +50% de dano | 30 s |

- O combo conta os toques com menos de **1 s** entre um e outro e **zera** sem toque por 1 s (já é assim desde o M5).
- O bônus vem **quando o combo zera** ("ao alcançar valores específicos e parar", D-96), pelos marcos que ele passou.
- **Acúmulo (D-183):** no padrão (o GDD, "bônus temporário acumulativo"), parar no ×57 dá **os três** (×10, ×30 e ×50) ao mesmo tempo. Na alternativa do DBG, dá **só o do maior marco** (o ×50).
- **Como entra no dano (D-179):** os +% somam (10 + 15 + 50 = +75%) e o total multiplica o resto: `dano = Picareta e melhorias × Sangue da Montanha × (1 + soma dos +%)`. Vale no golpe e no toque (o toque é 33% desse dano, D-178).
- **Crítico garantido (D-178):** todo golpe e todo toque saem críticos enquanto ele durar (como o "Crítico sempre" do DBG).
- **Estagiário (D-177):** nenhum bônus do combo vale para ele, nem online nem offline.
- **App fechado (D-181):** o tempo do bônus continua correndo; ao voltar, sobra o que sobrou, ou nada.

### Exemplos (padrão do GDD)

| O jogador parou no | Ganha | Dano nos primeiros 5 s | Depois |
|---|---|---|---|
| ×12 | +10% (5 s) | ×1,10 | normal aos 5 s |
| ×35 | +10% (5 s) e +15% (10 s) | ×1,25 | ×1,15 até os 10 s |
| ×57 | +10%, +15% e crítico garantido (20 s) | ×1,25 e todo golpe crítico (×2) = ×2,5 | crítico até os 20 s |
| ×104 | os quatro | ×1,75 e crítico = ×3,5 | +50% e crítico até os 20 s; +50% até os 30 s |

Na alternativa (só o maior marco), o ×104 dá só o Frenesi: ×1,5 por 30 s.

## O que já existe (e o que falta)

| Peça | Hoje | Falta |
|---|---|---|
| **Combo** | conta no núcleo (`MiningSimulation.Combo`), zera em 1 s, eventos `ComboChanged` e `ComboMilestone` (som e "x10!" a cada 10) | dar o bônus quando zera |
| **Dano e crítico** | `MinerStats` (Picareta, melhorias, Sangue da Montanha) e o sorteio do crítico, recalculados nos eventos | o multiplicador dos bônus e o crítico garantido |
| **Tempo do núcleo** | `Tick` por eventos (o próximo golpe, a corrida, o fim do combo) | o fim de cada bônus como evento |
| **Save** | v5 (run, permanente, hora, volta pendente) | v6 com os bônus ativos e o tempo que sobra |
| **HUD** | DANO, LASCAS, CAVERNA, VELOCIDADE | o DANO com os bônus, em âmbar enquanto houver bônus |
| **Coluna de buffs** | não existe | a coluna da D-141, que depois serve ao raro e ao AD |
| **DBG** | "Crítico sempre", "Toques auto" | DBG → Bônus (atalhos de cada marco, limpar) e o seletor do acúmulo |

## Perguntas para o Bruno

**P1 — O escopo deste plano.** O marco tem três partes (combo, cristal raro, AD) e um mecanismo comum (bônus com tempo e a coluna de ícones).
**Rec.:** o mecanismo nasce com o combo (E1–E3), que você valida sozinho; depois o raro (E4) e o AD (E5), cada um com o plano detalhado escrito quando chegar a vez, e uma sessão longa no fim (E6). Assim o combo não espera o resto.

**P2 — Dois bônus iguais ao mesmo tempo (padrão do GDD).** O jogador para no ×12 (+10% por 5 s) e, 2 s depois, para de novo no ×15 (outro +10%). A D-183 diz que no padrão "somam" e na alternativa "renovam", sem dizer o quê.
**Rec.:** no padrão, cada bônus é independente, com o próprio tempo: os dois +10% valem juntos (+20%) enquanto os dois durarem. O crítico garantido não tem como somar efeito, então **soma o tempo** que sobra. Na alternativa, o bônus novo de um marco **renova o tempo** do que já está ativo, sem somar efeito.

**P3 — Combo acima de ×100.** Parar no ×230 dá o Frenesi duas vezes (×100 e ×200)?
**Rec.:** não: cada marco vale uma vez por combo (×10, ×30, ×50, ×100). O GDD só fala desses quatro.

**P4 — Descansar com bônus ativo.**
**Rec.:** os bônus do combo **acabam no descanso**: a run nova começa sem nada do dia anterior, como as lascas e as melhorias base. (O que fazer com o raro e o AD fica para as E4 e E5.)

**P5 — Minimizar no meio do combo.** O jogador está no ×40 e minimiza. Com o app pausado, o combo não zera; quando volta, ele zera e o bônus só começa aí, inteiro.
**Rec.:** **minimizar ou fechar encerra o combo na hora**: os bônus entram naquele instante e o tempo deles corre com o app fechado (D-181). Ao voltar 3 s depois, sobram 2 s do +10%; 1 min depois, nada. É o que a D-181 descreve.

**P6 — O que o jogador vê quando ganha.**
**Rec.:** ao zerar o combo, um texto curto sobe do contador, com o som de marco que já existe: "+25% DANO" (a soma do que entrou), "CRÍTICO GARANTIDO", e, no ×100, "FRENESI!" maior, com um tremor leve (que obedece aos AJUSTES). Os ícones entram na coluna da esquerda. Nada de vibração (a D-199 é só crítico e parede). Tudo no mockup da E2.

**P7 — O DANO do HUD.**
**Rec.:** mostra o dano **com** os bônus de dano, em âmbar enquanto houver algum; volta à cor normal quando acabam. O crítico garantido não muda o número (aparece só na coluna).

**P8 — Onde fica o seletor do acúmulo (D-183).**
**Rec.:** no **DBG → Cálculos**, ao lado das outras variações (D-143): gravado no `calculations.json`, nunca num build de release, e o rótulo "Cálculo:" da tela avisa quando não é o GDD. Os atalhos de cada marco ficam num menu novo, **DBG → Bônus**.

## Etapas

### E1 — O núcleo dos bônus e o combo

- **Bônus com tempo no núcleo** (C# puro): uma lista de bônus ativos na run, cada um com o tipo (+% de dano, crítico garantido; depois o raro e o AD), o valor, de onde veio (combo ×10…×100) e os segundos que faltam. O fim de cada um é um evento do `Tick`, então o tempo bate com o do jogo (×20 do DBG incluído) sem olhar relógio.
- **Os quatro marcos nos dados do balanço** (`Balance_GDD_v7`): combo, tipo, valor, duração. Os textos ("Frenesi", "Crítico garantido") ficam nos textos do jogo, não no código.
- **Ao zerar o combo**, os marcos que ele passou entram, no padrão ou na alternativa (D-183, P2, P3). Evento `BonusesChanged` para a apresentação.
- **Dano e crítico:** `MinerStats` recebe a soma dos +% (D-179) e o sorteio do crítico respeita o garantido (D-178). O estagiário não muda (D-177).
- **Minimizar, fechar e voltar (P5, D-181):** a pausa encerra o combo; os bônus gravam o tempo que sobra; a volta desconta o tempo fora (o mesmo que o offline já mede).
- **Save v6:** os bônus ativos e o tempo que falta. O v5 abre sem bônus.
- **Descanso (P4)** limpa os bônus do combo.
- **DBG → Bônus:** "Combo ×10", "×30", "×50", "×100" (como se o jogador tivesse parado ali), "Limpar bônus" e a lista dos ativos com o tempo nas estatísticas.
- **Testes EditMode:** cada marco, o acúmulo nas duas versões, bônus iguais (P2), ×230 (P3), o dano do exemplo (×57 → ×2,5), o toque, o estagiário sem bônus, o fim exato pelo `Tick` (inclusive em ×20 e com um `Tick` grande), save e reabertura, app fechado por menos e por mais que a duração, descanso.

### E2 — A coluna de buffs e o feedback (mockup antes)

- **Mockup em 270×480** (D-141): a coluna à esquerda abaixo do HUD; cada ícone com o símbolo, um anel que esvazia e os segundos; pisca nos últimos 5 s; o mais recente entra embaixo; tocar mostra por 2 s o nome, o efeito e o tempo, sem pausar. Junto: o texto que sobe do contador (P6) e o DANO em âmbar (P7). Ícones do kit (raio, espada, estrela) ou desenhados, o que o mockup mostrar melhor.
- Depois da aprovação: a coluna (UI, só escuta o núcleo), o texto do ganho, o som, o tremor do Frenesi e o DANO do HUD. Toque na coluna não conta como toque no mapa. Nada de alocação por quadro (o anel é um `Image` com preenchimento).
- **Testes PlayMode:** parar no ×35 põe dois ícones e o DANO em âmbar; o tempo corre e eles saem na ordem; tocar no ícone mostra o detalhe; reabrir o jogo traz os ícones com o tempo certo.

### E3 — O seletor do acúmulo e o combo no celular

- **DBG → Cálculos:** o seletor do acúmulo (GDD × maior marco), gravado como as outras escolhas (P8).
- **Moto G75:** tocar em rajadas e sentir se o bônus "chega" ao parar (o risco abaixo); conferir que a coluna não atrapalha o toque nem cobre o cristal; minimizar no meio do combo (P5).

### E4 — Cristal raro (plano aprovado em 2026-11-10: P9…P16 → D-219…D-226)

**O que já está decidido (não muda):** 15% de cada cristal que nasce é raro (D-97, D-184); no DBG, o ritmo alternativo de 2%, sem raro novo enquanto houver janela aberta ou carta ativa (D-185). Arte `crystal_glass_rare` (D-77). Ao quebrar, a janela com **2 cartas diferentes** das 4: **dano ×2**, **golpe 2× mais rápido** (intervalo ÷ 2), **lascas ×2** e **crítico +25%**, **60 s** cada (D-190). Uma carta igual a outra ainda ativa só renova o tempo; diferentes valem juntas. **30 s** para escolher; sem escolha, o jogo sorteia uma das 2, aplica e deixa a janela aberta mostrando o que saiu, com **FECHAR** (D-186). A janela **não pausa** a mineração (D-184). As cartas valem também para o estagiário, e no offline enquanto durarem (D-181, D-190). Raro quebrado no offline não dá nada (D-186).

#### Perguntas da E4 (com a minha recomendação)

**P9 — O que cada carta faz, exatamente.**
**Rec.:**
- **Dano ×2:** o dano do anão (golpe e toque) depois dos +% do combo: Dano × (1 + Σ%) × 2. O do estagiário também ×2.
- **Golpe 2× mais rápido:** o intervalo do golpe ÷ 2, depois do piso de 0,1 s (como o AD, D-180), para o anão e para o estagiário.
- **Lascas ×2:** cada cristal quebrado paga o dobro, quem quer que quebre (golpe, toque, estagiário); no offline, as lascas do estagiário ×2 enquanto a carta durar.
- **Crítico +25%:** +25 pontos na chance de crítico do golpe e do toque (o golpe vai de 0% a 25%, o toque de 5% a 30%, mais a linha Crítico da loja). **Não vale para o estagiário**, que não tem crítico (D-167): para ele a carta não faz nada.
- A loja continua comparando sem os bônus (como o combo).

**P10 — Como é o cristal raro.**
**Rec.:** a mesma vida e as mesmas lascas de um cristal comum da caverna; o que ele dá a mais é a janela. Desenho `crystal_glass_rare` com estilhaços na cor dele, e um brilho leve piscando para se ver de longe na fila. A parede nunca é rara; os cristais do começo da run podem ser.

**P11 — Raro quebrado com uma janela já aberta (no padrão de 15%).**
**Rec.:** a janela nova **espera na fila**: ao escolher (ou FECHAR), abre a próxima. A janela mostra quantas esperam ("+1 raro"). Com 15% por cristal e quebras rápidas, a fila pode crescer; é o risco que o ritmo de 2% do DBG existe para comparar (D-185).

**P12 — Minimizar ou fechar com a janela aberta.**
**Rec.:** a janela **espera o jogador**: os 30 s só correm com o app aberto, e ela (as 2 cartas, o tempo que falta e a fila) vai para o save. Ao voltar, a janela está lá. Nenhuma carta entra sem ninguém ver, e não há o que explorar (a carta só vale depois de escolhida). As cartas já escolhidas correm o tempo com o app fechado (D-181).

**P13 — Toque com a janela aberta.**
**Rec.:** como a loja: o jogo segue (golpe automático, estagiário, tempo dos bônus), mas o toque fora da janela não minera nem conta no combo. A janela fica na metade de baixo da tela, para o anão e a coluna seguirem à vista.

**P14 — Descansar com carta ativa ou janela aberta.**
**Rec.:** como o combo (D-213): as cartas **acabam no descanso**, e uma janela aberta (e a fila) some. A run nova começa limpa.

**P15 — O que o jogador vê e ouve.**
**Rec.:** ao quebrar o raro, o som de marco que já existe e a janela abre. Ao escolher, a janela fecha, a carta sobe do anão como texto ("DANO ×2", em âmbar, como o ganho do combo) e entra na coluna da esquerda com o anel de 60 s. Sem vibração (D-199). No sorteio, a carta que saiu fica destacada, a outra apagada, e o texto "O jogo escolheu por você" acima do FECHAR.

**P16 — DBG.**
**Rec.:**
- **DBG → Cálculos:** botão **"Raro: 15%" ⇄ "Raro: 2%"** (o ritmo da D-185), gravado no `calculations.json` e no aviso "Cálculo:", como o Acúmulo (D-217).
- **DBG → Bônus:** **Próximo raro** (o próximo cristal que nascer é raro), **Janela agora** (abre a janela sem quebrar nada) e uma carta direta de cada tipo (**Dano ×2**, **Rápido**, **Lascas ×2**, **Crítico**).

#### Etapas da E4

- **E4a — Núcleo do raro.** O sorteio do raro e das cartas num gerador próprio, separado do crítico (a mesma semente repete a partida, e os críticos de hoje não mudam). Tipos novos de bônus (dano ×, intervalo ÷, lascas ×, chance de crítico +) e a origem "Raro" na mesma lista de bônus. A janela no núcleo: aberta, escolher, sorteio aos 30 s, fila, FECHAR. Os efeitos no anão, no estagiário e nas lascas. O offline em trechos (a carta vale para o estagiário até acabar). **Save v7** (a janela, a fila, as cartas). O ritmo de 2% e os atalhos do DBG. **Testes EditMode** (sorteio, cartas diferentes, renovar o tempo, 30 s e sorteio, fila, cada efeito, estagiário, offline em trechos, save, descanso, ritmo de 2%) e **PlayMode** (quebrar um raro abre a janela, escolher aplica).
- **E4b — A janela e o raro na tela (depois do mockup aprovado).** O cristal raro na fila (desenho e brilho), a janela das 2 cartas com a barra dos 30 s, o resultado do sorteio com FECHAR, o texto da carta, os ícones das cartas no `BuffCatalog` e na coluna, o som. **Testes PlayMode:** a janela abre e fecha, o toque fora dela não minera, reabrir o jogo traz a janela com o tempo que faltava.
- **E4c — Celular.** Roteiro do Moto G75 da E4.

### E5 — Boost do AD (plano aprovado em 2026-11-10: P17…P25 → D-227…D-235; P18 sem saída)

O botão AD deixa de ser "Em breve" (D-67): modal de 30 s "Visualizando AD" com ícone central e, no fim, o botão RECOMPENSA, que fecha o modal e põe o boost na coluna (D-218, no lugar do modal de 5 s com "Aprimorar" do §7.2); velocidade ×2,5 (intervalo ÷ 2,5, D-187) por 30 s, mesmo abaixo do piso de 0,1 s (D-180), o botão só volta quando o boost acaba (D-188), raio pulsante acima do anão (§8). Não vale para o estagiário (D-177). Sem rede de anúncios (D-21): o "assistir" fica atrás de uma interface para trocar depois. Mockup do modal: `tools/ci/out/m9b-e5-mockup.png`.

**Como fica por dentro (sem decisão nova):** o boost é mais um bônus da mesma lista (origem "AD", intervalo ÷ 2,5, 30 s), então a coluna, o save (os bônus já vão pelo nome, sem versão nova), o fim exato pelo `Tick` e o tempo correndo com o app fechado vêm de graça. Os números (2,5 e 30 s) entram no `Balance_GDD_v7`. O "assistir" é uma interface (`IRewardedAd`) com uma única implementação hoje, o anúncio simulado: o modal. Trocar por uma rede de anúncios depois (D-21) é escrever outra implementação, sem mexer no boost.

#### Perguntas da E5 (com a minha recomendação)

**P17 — O jogo durante os 30 s do modal.**
- **(a) Segue atrás, como na loja e na janela do raro (D-223):** o anão continua minerando, o toque no mapa não minera.
- (b) Pausa a mineração enquanto o "anúncio" passa (como um anúncio de verdade, que tira o jogo da tela).
**Rec.: (a).** É o mesmo comportamento das outras janelas, não cria o conceito de "pausa" só para isso, e o jogador não perde 30 s de mineração por ver o anúncio.

**P18 — Sair do modal antes dos 30 s.**
- **(a) Sem saída:** só o RECOMPENSA, no fim (como você descreveu).
- (b) Um "Cancelar" pequeno no canto: fecha sem recompensa e o botão AD fica livre de novo.
**Rec.: (b).** 30 s presos sem saída irritam, e os anúncios de recompensa de verdade deixam fechar perdendo a recompensa; quando a rede de anúncios entrar, o comportamento já é o mesmo.
**Bruno: (a), sem saída (D-228).** O "Cancelar" do mockup b) sai.

**P19 — Os 30 s do modal e o relógio.**
- **(a) Tempo real de tela:** não acelera com o ×20 do DBG; só corre com o app na frente (minimizar pausa a contagem, como um vídeo). Fechar o app no meio: perde, sem recompensa, e o modal não vai para o save.
- (b) Tempo do jogo (acelera com o DBG, corre com o app fechado).
**Rec.: (a).** É um vídeo, não uma regra do jogo; e um atalho do DBG (P24) pula a espera para testar.

**P20 — O botão AD.**
- **(a) Três estados:** livre (madeira normal, ícone do AD, "AD"); com o modal aberto, apagado; com o boost ativo, apagado com os segundos que faltam ("27s") no lugar de "AD" (D-188). Tocar apagado: nada.
**Rec.: (a).** O número no botão diz quando ele volta, sem precisar de outro aviso.

**P21 — O que se vê com o boost.**
- **(a)** Ao tocar em RECOMPENSA: o modal fecha, "VELOCIDADE ×2,5" sobe do anão em âmbar com o som de marco (como a carta do raro), o raio entra na coluna com o anel de 30 s ("AD · 28s" no detalhe), um raio pequeno pulsa acima da cabeça do anão (§8) e a VELOCIDADE do HUD fica âmbar com o valor já acelerado.
**Rec.: (a).** Tudo reaproveita o que o combo e o raro já têm. A VELOCIDADE âmbar também acende com a carta Rápido do raro, que hoje não acende.

**P22 — AD junto com a carta Rápido do raro.**
- **(a) Multiplicam:** ÷ 2 e ÷ 2,5 = ÷ 5 (com a Velocidade no máximo, 0,1 s vira 0,02 s).
- (b) Vale só o maior.
**Rec.: (a).** É a mesma regra das cartas (cada uma divide o intervalo, D-219); "só o maior" seria uma regra nova.

**P23 — Ícones.** O kit Anvil não tem raio nem tela.
- **(a)** Desenhar dois ícones do projeto em pixel art 32 px (como o anel da coluna): **o do AD** (uma telinha de madeira com o triângulo de "play"), usado no botão e no centro do modal, e **o raio** âmbar, usado na coluna e acima do anão. Você pode trocar pelos seus quando quiser (mesmo nome de arquivo).
**Rec.: (a).**

**P24 — Atalhos do DBG.** No menu Bônus: **AD agora** (o boost direto, sem modal) e **Pular espera** (com o modal aberto, mostra o RECOMPENSA já).
**Rec.: os dois.** O "AD agora" respeita a D-188 (com boost ativo, não faz nada).

**P25 — Descanso e app fechado.** O boost corre com o app fechado, como os do combo (D-181), e acaba no descanso (como o combo e o raro, D-213, D-224), deixando o botão livre. No offline ele não muda nada, porque não vale para o estagiário (D-177).
**Rec.: assim.**

#### Etapas da E5

- **E5a — Núcleo e o "assistir".** A origem "AD" na lista de bônus; os números no `Balance_GDD_v7` (2,5 e 30 s); `MiningSimulation.CanWatchAd` e `GrantAdBoost` (com boost ativo, não dá outro, D-188); o boost só no anão, depois do piso (D-180); a interface `IRewardedAd`. **Testes EditMode:** ÷ 2,5 no anão e não no estagiário, abaixo do piso, fim aos 30 s, sem acúmulo, junto com o Rápido (P22), save e reabrir, app fechado, descanso.
- **E5b — Na tela (depois do mockup aprovado).** O botão AD com os três estados (P20), o modal (P17…P19), o texto do ganho, o raio acima do anão, a entrada no `BuffCatalog` e a VELOCIDADE âmbar; os atalhos do DBG (P24). **Testes PlayMode:** tocar no AD abre o modal e apaga o botão; o toque no mapa não minera; aos 30 s o RECOMPENSA aparece e dá o boost; o botão mostra os segundos e volta no fim.
- **E5c — Celular.** Roteiro do Moto G75 da E5.

### E6 — Sessão longa e celular

A sessão de 2 h do M9 (`LongSessionTests`) ganha rajadas de toque, raros e AD; o roteiro do Moto G75 ganha os bônus.

## Como testar (Bruno) — ao fim de cada etapa

- **E1:** antes, com a cena `Game` aberta, `Tools → Miners Idle → Projeto → Aplicar configuração do projeto` (cria o menu DBG → Bônus e põe os quatro marcos no `Balance_GDD_v7`). Depois, cena `Bootstrap`, Play. DBG → Bônus → **Combo ×30**: o alto do menu lista +10% (5 s) e +15% (10 s), e o DANO do DBG sobe 25%; aos 5 s fica só o +15%, aos 10 s nada. **Combo ×50**: todo golpe sai crítico por 20 s. Tocar até ×35 e parar: o mesmo que o atalho ×30. Sair do Play no meio de um bônus de 30 s e voltar 10 s depois pela `Bootstrap`: sobram uns 20 s. DBG → Cálculos (na E3) troca para "maior marco": o ×35 dá só o +15%.
- **E2:** de novo `Tools → Miners Idle → Projeto → Aplicar configuração do projeto` com a cena `Game` aberta (cria o `BuffCatalog` e a coluna). Depois, a mesma cena, sem o DBG: tocar até ×35, parar, ver os dois ícones na coluna da esquerda esvaziando, o "+25% DANO" subindo do contador e o DANO do HUD em âmbar; tocar num ícone mostra o nome e o tempo. Tocar até ×100: "FRENESI!" e um tremor leve.
- **E3:** no Editor, antes, `Tools → Miners Idle → Projeto → Aplicar configuração do projeto` com a cena `Game` aberta (cria o botão "Acúmulo"). Cena `Bootstrap`, Play. DBG → Cálculos: o botão **Acúmulo: soma** (a do GDD). Tocar nele: vira **Acúmulo: maior**, e o aviso no pé da tela fica âmbar com "· combo só o maior marco". Tocar até ×35 e parar: **um** ícone só (o +15%). **Reiniciar run** mantém a escolha; **Padrão GDD** volta à soma. Depois, no Moto G75, `Tools → Miners Idle → Build → Rodar no celular (Android)` (ou `bash tools/build/run-android.sh` no Terminal, com o Editor fechado), e o roteiro abaixo.

### Roteiro do Moto G75 (E3)

Anote ✅/❌ em cada passo; um ❌ volta para mim com o que apareceu na tela.

1. **O ganho ao parar:** tocar em rajada até ×12 e parar. Aparece o "+10% DANO" subindo do contador, um ícone na coluna da esquerda e o DANO do HUD em âmbar. Dá para perceber sem procurar?
2. **Rajada longa (o risco do plano):** tocar sem parar até ×55 e parar. Entram três ícones de uma vez (+10%, +15%, crítico) e o "+25% DANO · CRÍTICO". Durante a rajada nada muda; o bônus "chega" ao parar, ou parece que veio tarde demais?
3. **A coluna e o toque:** tocar nos ícones da coluna: o detalhe abre ao lado por 2 s e o combo não sobe. Tocar no cristal com a coluna cheia: nada cobre o cristal nem rouba o toque.
4. **Leitura:** os segundos ao lado do ícone se leem bem no aparelho? Ficam nítidos ou borrados (hoje 5 pt; 6 pt é o tamanho nítido da fonte de números)?
5. **O fim:** nos últimos 5 s o número fica âmbar e o ícone pisca; ao acabar, o ícone sai e o DANO volta ao branco.
6. **FRENESI!:** tocar até ×100 (ou DBG → Bônus → Combo ×100): "FRENESI!" grande, tremor leve, quatro ícones. Com o tremor desligado nos AJUSTES, sem tremor.
7. **Minimizar no meio do combo (P5):** tocar até ×35, sem parar apertar o botão Início do Android, voltar em 3 s: o combo zerou e a coluna tem os ícones com ~2 s a menos. Repetir e voltar depois de 1 min: nada na coluna.
8. **Fechar com bônus:** ganhar o crítico (×50), fechar pelo multitarefa e abrir em 5 s: o crítico ainda está lá, com ~15 s.
9. **Acúmulo pelo DBG:** DBG → Cálculos → **Acúmulo: maior**. Rajada até ×55: só o crítico. Qual das duas versões parece melhor no dedo? (A resposta vira decisão; o GDD é a soma.)
10. **Descanso:** com um bônus ativo, descansar (DBG → Dia, ou o botão a partir do nível 10): a run nova começa sem ícones.

- **E4:** no Editor, antes, `Tools → Miners Idle → Projeto → Aplicar configuração do projeto` com a cena `Game` aberta (põe o raro no `Balance_GDD_v7`, as 4 cartas no `BuffCatalog`, a janela na cena, o desenho do raro na fila e os botões novos do DBG). Depois, cena `Bootstrap`, Play:
  1. **O raro na fila:** DBG → Bônus → **Próximo raro**. O próximo cristal que nasce tem o desenho `crystal_glass_rare` e pisca um brilho. Ao quebrar: o som de marco e a janela "CRISTAL RARO!" na metade de baixo, com 2 cartas diferentes e a barra dos 30 s.
  2. **Escolher:** tocar numa carta. A janela fecha, o nome da carta sobe do anão em âmbar ("DANO ×2") e o ícone entra na coluna da esquerda com 60 s. Com **Dano ×2**, o DANO do HUD fica âmbar. Tocar no ícone: "Cristal raro · 58s" no detalhe.
  3. **O mapa não minera:** DBG → Bônus → **Janela agora** e tocar no mapa fora da janela: o combo não sobe. O jogo segue atrás (o anão continua batendo).
  4. **Sem escolha:** **Janela agora** e esperar 30 s (o número fica âmbar nos últimos 10): "O jogo escolheu por você", a carta que saiu com a moldura destacada e a outra apagada, e **FECHAR**.
  5. **Fila:** **Janela agora** duas vezes: "+1 raro" no alto. Escolher abre a próxima com 30 s inteiros.
  6. **Fechar e abrir:** com a janela aberta (uns 20 s), sair do Play e voltar pela `Bootstrap` depois de um tempo: a janela volta com os mesmos ~20 s (o tempo fora não corre a janela).
  7. **Cartas pelo DBG:** os botões **Carta Dano ×2 / Rápido / Lascas ×2 / Crítico** dão a carta direto; o alto do menu Bônus lista as cartas e a janela. Com o estagiário (DBG → Estagiário), **Rápido** e **Dano ×2** valem para ele também; **Crítico** não.
  8. **Ritmo:** DBG → Cálculos → **Raro: 15%** vira **Raro: 2%** (o aviso no pé ganha "· raro 2%"): bem menos raros, e nenhum enquanto houver janela aberta ou carta ativa. **Padrão GDD** volta aos 15%.
  9. **Descanso:** com uma carta ativa e a janela aberta, descansar: a run nova começa sem carta e sem janela.

  Depois, no Moto G75, o roteiro abaixo.

### Roteiro do Moto G75 (E4)

Anote ✅/❌ em cada passo; um ❌ volta para mim com o que apareceu na tela.

1. **Ver o raro:** jogar normal no nível 1 por 2 min (15% dos cristais são raros). O raro se destaca na fila sem procurar? O brilho incomoda ou passa batido?
2. **A janela:** quebrar um raro. A janela abre na metade de baixo sem cobrir o anão nem o cristal da vez? As 2 cartas se leem (nome, efeito, "60 s", para quem vale)?
3. **Escolher no dedo:** tocar numa carta: o toque pega de primeira? A carta sobe do anão e entra na coluna.
4. **Toque fora:** com a janela aberta, tocar no mapa: não minera e não fecha a janela. Isso irrita no meio de um combo? (Com 15%, a janela aparece muito: anote se o ritmo pesa; o DBG → Cálculos → **Raro: 2%** é a alternativa.)
5. **Sorteio:** deixar os 30 s correrem: "O jogo escolheu por você" e FECHAR. Fica claro qual carta saiu?
6. **Fila:** quebrar dois raros seguidos (DBG → Bônus → **Janela agora** duas vezes): o "+1 raro" se vê?
7. **Minimizar e voltar:** com a janela aberta, botão Início, voltar em 1 min: a janela continua com o mesmo tempo. Com uma carta ativa, voltar em 2 min: a carta acabou.
8. **Leitura:** os textos da carta e o número da barra ficam nítidos no aparelho?

- **E5:** no Editor, antes, `Tools → Miners Idle → Projeto → Aplicar configuração do projeto` com a cena `Game` aberta (põe o boost no `Balance_GDD_v7`, o boost e os textos no `BuffCatalog`, troca o "Em breve" do AD pelo botão de verdade, cria o anúncio simulado, o raio acima do anão e os botões novos do DBG). Depois, cena `Bootstrap`, Play:
  1. **O botão:** o AD da coluna da direita está em madeira, com a TV e "AD" em âmbar (não mais o "Em breve").
  2. **O anúncio:** tocar no AD. Escurece, abre "VISUALIZANDO AD" com a TV pulsando, "O anúncio termina em" e o número descendo de 30s (âmbar nos últimos 5), a barra enchendo e "Recompensa: Velocidade ×2,5 por 30 s". O botão AD fica apagado. Não há como sair.
  3. **O jogo segue:** com o anúncio na tela, o anão continua batendo; tocar no mapa não minera.
  4. **Recompensa:** aos 30 s, "Anúncio concluído!" e o botão **RECOMPENSA**. Tocar: fecha, o som de marco, "VELOCIDADE ×2,5" sobe do anão, o raio pulsa acima dele, a VELOCIDADE do HUD fica âmbar e mais rápida, e o ícone do raio entra na coluna da esquerda. Tocar no ícone: "AD · 28s".
  5. **O botão conta:** o AD fica apagado com os segundos ("28s", "27s"…) no lugar do "AD". Tocar nele: nada. Aos 30 s o raio some, a VELOCIDADE volta ao branco e o AD fica livre de novo.
  6. **Atalhos do DBG:** DBG → Bônus → **AD agora** dá o boost sem o anúncio (o alto do menu mostra "AD ×2,5, 30s"); de novo com ele ativo, nada. Com o anúncio aberto, **Pular espera** mostra o RECOMPENSA já (o DBG fica por cima do anúncio).
  7. **Junto com o Rápido:** DBG → Bônus → **Carta Rápido** e **AD agora**: a VELOCIDADE do HUD vira 1/5 do normal.
  8. **Estagiário:** com o estagiário (DBG → Estagiário), o boost não muda a velocidade dele.
  9. **Fechar e abrir:** com o boost ativo (uns 20 s), sair do Play e voltar pela `Bootstrap` em 5 s: o boost volta com ~15 s. Voltar depois de 1 min: acabou. Sair com o **anúncio** aberto: ao voltar, nada de anúncio nem de boost.
  10. **Descanso:** com o boost ativo, descansar: a run nova começa sem ele.

  Depois, no Moto G75, o roteiro abaixo.

### Roteiro do Moto G75 (E5)

Anote ✅/❌ em cada passo; um ❌ volta para mim com o que apareceu na tela.

1. **Achar o AD:** o botão AD da direita se lê como algo que dá para tocar (madeira, TV, "AD")?
2. **O modal:** tocar no AD. O modal cabe na tela, o número e a barra se leem e ficam nítidos? A TV pulsando incomoda?
3. **30 s parado:** esperar os 30 s inteiros. Parece longo demais para um anúncio simulado? (O número vem do GDD; anote a impressão.)
4. **Recompensa no dedo:** tocar em RECOMPENSA: pega de primeira? O ganho, o raio e a VELOCIDADE âmbar aparecem juntos e dá para perceber que o anão ficou mais rápido?
5. **O raio:** o raio acima do anão fica longe do texto do combo e não cobre o cristal?
6. **O botão contando:** os segundos no lugar do "AD" se leem? Fica claro que não dá para tocar até acabar?
7. **Minimizar no anúncio:** com o anúncio aberto em ~20 s, botão Início, voltar em 10 s: o número continua de onde estava (não pulou os 10 s).
8. **Minimizar com o boost:** com o boost em ~25 s, botão Início, voltar em 10 s: sobram ~15 s (o boost corre com o app fechado).
9. **Fechar no anúncio:** com o anúncio aberto, fechar pelo multitarefa e abrir: sem anúncio e sem boost; o AD está livre.

## Critério de pronto (do combo, E1–E3)

- Parar o combo depois de cada marco dá os bônus do GDD, com o dano e o crítico certos no golpe e no toque, e nada para o estagiário.
- As duas versões do acúmulo funcionam e se trocam no DBG → Cálculos.
- Os bônus aparecem na coluna da esquerda com o tempo certo, sobrevivem a fechar e abrir com o tempo corrido, e acabam no descanso.
- Os atalhos do DBG → Bônus funcionam.
- Os testes EditMode e PlayMode do combo passam e o combo foi sentido no Moto G75.
- README, Roadmap e decisões atualizados.

## Riscos

- **O bônus só vem quando o jogador para.** Quem toca sem parar por muito tempo não ganha nada durante a rajada, só no fim, e o +10% de 5 s pode passar despercebido. A coluna e o texto do ganho existem para isso; se no celular ainda parecer pouco, a decisão é sua e do Apellon (o DBG já mostra as duas versões do acúmulo).
- **Os +% deixam o começo fácil demais.** ×3,5 por 20 s (o exemplo do ×104) é muito no nível 1. É o número do GDD; o relatório de economia (adiado, D-149) e a sessão longa mostram o efeito.
- **Muitos ícones de uma vez.** O combo dá até 4, o raro mais 1 ou 2, o AD mais 1: até 7 ícones na coluna. O mockup da E2 desenha o pior caso.
- **Tempo com o app fechado.** Desconta o tempo fora pelo mesmo relógio do offline (com as proteções do M9 para relógio voltado: nada volta a valer).

## Fora deste marco (por enquanto)

- O detalhe do cristal raro e do AD: só esboço aqui (E4, E5), plano detalhado quando chegar a vez (P1).
- Animação especial por marco além do "x10!" que já existe e do texto do ganho (arte final no M10).
- Rede de anúncios real (D-21, pós-MVP).

## Esforço

| Etapa | Horas |
|---|---|
| E1 núcleo dos bônus e o combo | 6–8 |
| E2 coluna de buffs e feedback (com o mockup) | 6–8 |
| E3 seletor do acúmulo e o celular | 2–3 |
| E4 cristal raro (com o mockup) | 8–10 |
| E5 boost do AD (com o mockup) | 5–7 |
| E6 sessão longa e celular | 2–3 |

Total: **~29–39 h**; o combo sozinho (E1–E3), **~14–19 h**.

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| Plano | ✅ aprovado em 2026-11-10 (P1…P8 → D-210…D-217) | — |
| E1 Núcleo dos bônus e o combo | 🔧 implementada em 2026-11-10 | Núcleo (`Bonuses.cs`, simulação, `StrikeRules`), marcos no `Balance_GDD_v7` pelo setup, save v6, pausa/fechar encerram o combo, DBG → Bônus. Testes `ComboBonusTests` (EditMode) e `ComboBonusFlowTests` (PlayMode) **só compilados**: rodam no fim, com a suíte toda. Ajustados para o comportamento novo: a versão do save nos `SaveServiceTests` (5 → 6) e o aquecimento do `AllocationTests` (limpa o +10% do x10). |
| E2 Coluna de buffs e feedback | 🔧 implementada em 2026-11-10 | Mockup aprovado (`tools/ci/out/m9b-e2-mockup.png`). `Data/BuffCatalog` (ícones Anvil 2/18/332, nomes, textos do ganho), `BuffColumn`/`BuffSlot` (anel `buff_ring`, piscar nos últimos 5 s, detalhe por 2 s), texto do ganho e "FRENESI!" com tremor no `MiningFeedbackView`, som de marco no `SoundBoard`, DANO âmbar no `HudView`. Setup: `BuffColumnSetup`. `BuffColumnTests` (PlayMode) **só compilados**. |
| E3 Seletor do acúmulo e o celular | 🔧 implementada em 2026-11-10 (EditMode 398/398, PlayMode 142/142); roteiro do Moto G75 aguardando o Bruno | `CalculationChoice.ComboStacking` (gravado no `calculations.json` como `comboHighestOnly`; arquivo antigo = soma), aplicado pelo `MiningRunner` a cada simulação nova (troca de cálculo, Reiniciar run, descanso, reabrir). DBG → Cálculos ganhou o botão "Acúmulo: soma ⇄ maior" (`CalculationsSetup`), que entra no aviso "Cálculo:". Testes novos: `CalculationTests.Store_KeepsTheComboStacking_AndAnOldFileSums` e `CalculationsPanelTests.ComboStacking_SwitchesToTheHighestMilestone_AndSurvivesANewRun`. |
| E4 Cristal raro | ✅ aprovada e testada pelo Bruno em 2026-11-10 (plano e mockup: P9…P16 → D-219…D-226; EditMode 422/422, PlayMode 148/148) | Núcleo: `RareRules`/`RareCard` e os bônus dano ×, intervalo ÷, lascas ×, crítico + (`Bonuses.cs`), `RareWindow` no `GameState` (save v7), sorteio do raro num gerador próprio (`MiningRunner.RareSeed`), janela/fila/sorteio aos 30 s como evento do `Tick`, offline em trechos para o estagiário. Tela: `RareWindowView` + `RareWindowSetup` (antes da loja), cartas no `BuffCatalog`, `crystal_glass_rare` com brilho no `DescentStage`, texto do ganho, som de marco, DANO âmbar também com o Dano ×2. DBG → Bônus (Próximo raro, Janela agora, 4 cartas) e DBG → Cálculos (Raro: 15% ⇄ 2%). Testes: `RareCrystalTests`, `RareCrystalFlowTests`, `CalculationTests.Store_KeepsTheRareRhythm_AndAnOldFileUsesTheGdd`. Mockup: `tools/ci/out/m9b-e4-mockup.png` |
| E5 Boost do AD | ✅ aprovada e testada pelo Bruno em 2026-11-10 (plano e mockup: P17…P25 → D-227…D-235, P18 sem saída; EditMode 432/432, PlayMode 150/150) | Núcleo: origem `Ad` nos bônus, `AdRules` no `StrikeRules` (2,5 e 30 s no `Balance_GDD_v7` pelo setup), `MiningSimulation.CanWatchAd`/`GrantAdBoost`/`AdBoostGranted` (só o anão, depois do piso; multiplica com o Rápido). Tela: `IRewardedAd` + `SimulatedAdScreen` (30 s de tela, sem saída), `AdButtonView` (livre, apagado, segundos), `AdBoostBolt` (raio acima do anão), VELOCIDADE âmbar no `HudView`, texto do ganho, som de marco, "AD · 28s" na coluna; ícones `icon_ad.png`/`icon_bolt.png`. Setup: `AdSetup`. DBG → Bônus: AD agora, Pular espera. Testes: `AdBoostTests`, `AdFlowTests`; `HudSkinTests` mede o AD de verdade (`SideButtons_AreNoLongerComingSoon`). Testes da E4 que falhavam às vezes, pela semente: `TapInputTests` e `TouchFingerTests` com o jogo parado; `MiningSceneTests` aceita a carta Lascas ×2. Mockup: `tools/ci/out/m9b-e5-mockup.png` |
| E6 Sessão longa e celular | ⏳ | — |
