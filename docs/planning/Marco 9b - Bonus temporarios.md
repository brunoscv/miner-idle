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

### E4 — Cristal raro (esboço; plano detalhado quando chegar a vez)

15% de cada cristal que nasce (D-184; no DBG, o ritmo alternativo de 2%, D-185); arte `crystal_glass_rare` (D-77); ao quebrar, a janela com 2 das 4 cartas (dano ×2, golpe 2× mais rápido, lascas ×2, crítico +25%; 60 s cada; D-190), 30 s para escolher, e sem escolha o jogo sorteia (D-186); as cartas valem para o estagiário, também no offline enquanto durarem (D-181, D-190); raro quebrado no offline não dá nada (D-186). Usa a mesma lista de bônus e a mesma coluna. Mockup da janela antes.

### E5 — Boost do AD (esboço; plano detalhado quando chegar a vez)

O botão AD deixa de ser "Em breve" (D-67): modal de 5 s com "Aprimorar" (§7.2), velocidade ×2,5 (intervalo ÷ 2,5, D-187) por 30 s, mesmo abaixo do piso de 0,1 s (D-180), o botão só volta quando o boost acaba (D-188), raio pulsante acima do anão (§8). Não vale para o estagiário (D-177). Sem rede de anúncios (D-21): o "assistir" fica atrás de uma interface para trocar depois. Mockup do modal antes.

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
| E4 Cristal raro | ⏳ | — |
| E5 Boost do AD | ⏳ | — |
| E6 Sessão longa e celular | ⏳ | — |
