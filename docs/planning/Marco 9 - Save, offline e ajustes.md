# Marco 9 — Save completo, offline e ajustes

> Plano de 2026-11-10, **aprovado no mesmo dia** (P1…P8 → D-193…D-200; P9 → D-192). Base: GDD v7 ([`docs/gdd v7.pdf`](../gdd%20v7.pdf), §1.1, §1.2, §2.2, §5.1; D-132) e as decisões já tomadas: **D-99** (modal "Bem-vindo de volta" com o tempo e as lascas), **D-139** e **D-189** (offline só do estagiário, com tudo o que é dele, até 6 h, sem passar de nível e sem contar na trava do Mapa), **D-166** (a Mineração do estagiário rende no offline), **D-167** (ele não tem crítico), **D-181** (bônus ativos contam o tempo com o app fechado), **D-182** (o M9 vem antes dos Bônus temporários), **D-44** (save com escrita atômica) e **D-67** (AD e AJUSTES "Em breve" até aqui).
>
> O boost do AD **saiu deste marco** (D-89): vai para os Bônus temporários, junto com o combo e o cristal raro. O botão AD continua "Em breve".

## Objetivo

**Fechar e voltar sem perder nada, e voltar com algo a mais.** O jogador fecha o app; o estagiário continua minerando sozinho até 6 h; ao reabrir, um modal mostra quanto tempo passou e quantas lascas ele trouxe. O save aguenta app morto, arquivo estragado e relógio mexido. O botão **AJUSTES** deixa de ser "Em breve": volume, tremor da tela, vibração, apagar o save e créditos.

**Para você:** contratar o estagiário, comprar a Picareta dele, fechar o app por uns minutos e voltar com lascas; desligar o tremor nos AJUSTES e ver que ficou desligado depois de reabrir.

## O que já existe (e o que falta)

| Peça | Hoje | Falta no M9 |
|---|---|---|
| **Save** | versão 4 (run + permanente), escrita atômica (`.tmp` → troca, D-44), save ilegível ou de versão futura vai para `.bad` e começa jogo novo; grava a cada compra, troca de caverna, 30 s, ao minimizar e ao sair | a **hora em que foi gravado** (para o offline), um **backup** do último save bom (hoje um save estragado perde tudo), testes de app morto no meio da gravação |
| **Estagiário** | contratado, árvore dele, Treinar e Promoção, no save (M7) | minerar **sem o app aberto** |
| **AJUSTES** | botão "Em breve" (D-67) | a tela inteira |
| **Som** | `SoundBoard` com os sons do jogo | volume ajustável, guardado |
| **Tremor** | `ScreenShake` na câmera; no DBG liga e desliga | opção do jogador, guardada |
| **Vibração** | não existe | vibrar no crítico e na quebra da parede (P7) |

## As regras do offline (o que já está decidido)

- **Quem minera:** só o estagiário (D-139). Sem ele contratado, nada acontece offline (o modal não aparece).
- **Com o quê:** tudo o que é dele (D-189): dano (Picareta dele × Treinar), intervalo (Velocidade dele) e lascas com a **Mineração dele** (D-166). Sem crítico (D-167). Nada do anão (P2).
- **Onde:** nos cristais do nível em que o jogador parou, com a vida e as lascas desse nível. Ele **não passa de nível** e as quebras **não contam na trava dos 100 cristais do Mapa** (D-189).
- **Quanto tempo:** até **6 h**; o que passar disso se perde (GDD §1.1).
- **Eficiência reduzida (§1.2):** é só isso, minerar sozinho, sem corte a mais (D-189).
- **Bônus ativos ao fechar:** continuam contando o tempo e acabam (D-181). Os do combo e do AD não valem para ele (D-177); a carta do raro sim, até acabar (D-190). Como os bônus só nascem no marco seguinte, **o M9 deixa a conta do offline em trechos** (com e sem bônus), para o marco dos Bônus encaixar a carta sem refazer a conta.

### A conta (proposta, P1)

O mesmo ritmo do jogo aberto, sem o anão: o estagiário corre até o cristal, dá o primeiro golpe na chegada (A2, D-124/D-168) e os seguintes a cada intervalo dele.

- golpes por cristal `k = ⌈ vida do cristal ÷ dano dele ⌉`
- tempo por cristal `t = corrida entre cristais + (k − 1) × intervalo dele` (corrida = 2 un ÷ 4 un/s = **0,5 s**)
- cristais quebrados `N = ⌊ tempo offline (até 6 h) ÷ t ⌋`
- lascas `= N × lascas do cristal do nível × (1 + Mineração dele × n)`

Uma conta fechada, sem simular golpe a golpe: 6 h custam o mesmo que 1 min, e o resultado é igual ao de rodar a simulação (os testes comparam as duas).

**Exemplos** (nível 15: vida 22,7, lascas 27,2 por cristal):

| Estagiário | Golpes por cristal | Tempo por cristal | Lascas por hora | Em 6 h |
|---|---|---|---|---|
| recém-contratado (dano 5, 2,0 s; D-205) | 5 | 8,5 s | ~11,5 mil | ~69 mil |
| Picareta 5 (dano 160, D-205), Velocidade 10 (1,0 s), Mineração 3 (×4) | 1 | 0,5 s | ~785 mil | ~4,7 mi |

O segundo caso mostra um limite da conta: com um golpe por cristal, quem manda é a corrida de 0,5 s, a mesma do anão online. Ver os Riscos.

## Perguntas para o Bruno

> **Respondidas em 2026-11-10:** todas com a recomendação (P1…P8 → D-193…D-200; P9 → D-192).

**P1 — A conta do offline.** O ritmo do jogo aberto sem o anão, como acima: corrida de 0,5 s entre cristais, primeiro golpe na chegada, os outros a cada intervalo dele?
**Rec.:** sim. É a regra que o jogador já vê na tela; o DBG mostra a conta para o time avaliar.

**P2 — O Coração Ardente vale no offline?** Ele é uma permanente **do anão** que multiplica as lascas de cada quebra. A D-189 diz "tudo do estagiário".
**Rec.:** não vale. Offline só o que é dele: a Mineração dele e o Treinar.

**P3 — Minimizar conta como offline?** No Android, minimizar o app pausa o jogo. Ao voltar, o tempo minimizado vira offline (estagiário sozinho), como fechar e abrir?
**Rec.:** sim, o mesmo caminho. Com menos de **60 s** fora, nada de modal: as lascas entram direto, sem aviso.

**P4 — Relógio do aparelho.** O offline usa a hora do aparelho (em UTC). Se o relógio **voltou** (hora atual antes da hora do save), o que fazer? E se **adiantou**?
**Rec.:** voltou → offline 0, sem erro, e a hora nova passa a valer. Adiantou → não dá para saber sem um servidor; o teto de 6 h limita o ganho. Fica registrado como risco aceito (jogo sem ranking nem compra).

**P5 — O modal "Bem-vindo de volta" (D-99).** As lascas entram quando o jogador toca no botão, ou já entram ao abrir e o modal só informa?
**Rec.:** entram ao tocar **COLETAR** (o número subindo no HUD é a recompensa). O modal mostra o tempo fora (com "máx. 6 h" quando passou), as lascas e o estagiário. Contam nas lascas ganhas do dia (o resumo do Descansar). **Mockup em 270×480 antes do código**, como a LOJA.

**P6 — O que vai na tela AJUSTES.** Volume dos efeitos (deslizador), tremor da tela (liga/desliga), vibração (liga/desliga), **Apagar save** (com confirmação) e **Créditos** (a lista do `CREDITS.md`, em dados). Música não existe ainda (fica para o M10).
**Rec.:** essa lista. **Mockup em 270×480 antes do código.**

**P7 — Quando vibrar (com o jogo aberto).** Vibração ainda não existe no jogo. Offline nada vibra nem toca: é só a conta (D-191).
**Rec.:** curta no **crítico** e mais longa na **quebra da parede**. Não no toque comum (vibraria 15 vezes por segundo).

**P8 — Onde os ajustes ficam.** Num arquivo à parte (`settings.json`), como a escolha do DBG → Cálculos?
**Rec.:** sim. **Apagar save não mexe nos ajustes** (quem desligou o som não quer o som de volta).

**P9 — Começar o M9 antes de fechar o M7 e o 8b?** Falta a E4 do M7 (com a E5 do 8b: alternativas no DBG → Cálculos, sessão de 30 min e o teste no Moto G75) e a sua validação das etapas já feitas. A regra é um marco só começar com o anterior aprovado.
**Rec.:** fechar primeiro a E4 do M7 (3–6 h) e aprovar o M7 e o 8b juntos; o APK que o Apellon está testando ajuda nessa validação.
**Decidido (D-192):** o M9 começa já; a E4 do M7 e a E5 do 8b não entram agora. Depois do M9, os Bônus temporários.

## Etapas

### E1 — O núcleo do offline, save v5 e DBG → Offline

- **Núcleo:** `OfflineReport` em C# puro: recebe as regras, o estado da run, as permanentes e o tempo fora; devolve os cristais quebrados, as lascas e o tempo que contou (até 6 h). A conta da P1, em trechos (para o marco dos Bônus).
- **Save v5:** a hora em que foi gravado (`savedAtUtc`, UTC). Um save v4 abre com "sem hora": nenhum offline na primeira abertura. O relógio do jogo continua injetado (o núcleo não lê a hora do sistema).
- **Ao abrir e ao voltar do minimizado:** calcula o offline, credita (ou guarda para o modal da E2) e grava na hora.
- **DBG → Offline:** "Simular 1 min / 10 min / 1 h / 6 h / 8 h fora" (sem fechar o app; o de 8 h mostra o corte em 6 h), "Voltar o relógio 1 h" e a linha com a conta do último offline (cristais, golpes por cristal, tempo por cristal, lascas).
- **Testes EditMode:** a conta (com e sem estagiário, teto de 6 h, relógio voltado, nível alto), a conta fechada igual à simulação golpe a golpe, migração v4 → v5.

### E2 — O modal "Bem-vindo de volta"

- **Mockup** em 270×480 (P5) antes do código.
- O modal na volta, com o tempo fora, as lascas e COLETAR; em cima do jogo, bloqueando o toque no mapa, como a LOJA. Textos em dados.
- Fechar e abrir o app no meio do modal **não paga duas vezes** (as lascas pendentes vão no save e saem quando coletadas).
- **Testes PlayMode:** voltar com 10 min fora mostra o modal e paga uma vez; menos de 60 s não mostra; sem estagiário não mostra.

### E3 — Save que aguenta tudo

- **Backup:** antes de cada troca, o save bom anterior vira `save.json.bak`. Save principal ilegível → abre o backup (com aviso no log); os dois ruins → jogo novo, como hoje.
- Versão futura (um save gravado por um APK mais novo) continua indo para `.bad`, sem travar.
- **Testes:** app morto no meio da gravação (o `.tmp` sobra), save principal estragado com backup bom, os dois estragados, versão futura, minimizar e voltar várias vezes sem duplicar lascas nem compras.

### E4 — AJUSTES

- **Mockup** em 270×480 (P6) antes do código.
- O botão AJUSTES abre a tela (sai o "Em breve"): volume dos efeitos, tremor, vibração, Apagar save (confirmação em dois passos), Créditos. Textos em dados.
- `settings.json` à parte (P8), lido no início; o `SoundBoard`, o `ScreenShake` e a vibração (P7) obedecem.
- Apagar save pelos AJUSTES: run e permanentes zeradas, ajustes mantidos, o jogo recomeça no nível 1.
- **Testes PlayMode:** desligar o tremor e reabrir (continua desligado); Apagar save com confirmação.

### E5 — Sessão longa e o celular

- **Sessão de 2 h** em ×20 pelo DBG, com descansos, compras e offlines simulados no meio, sem travar nem perder nada (a Definição de Pronto do Roadmap).
- **Moto G75:** fechar com o estagiário contratado, esperar uns minutos, abrir; minimizar e voltar; mudar o relógio do aparelho para trás e para a frente; desligar o tremor e a vibração.

## Como testar (Bruno) — ao fim de cada etapa

- **E1:** cena `Bootstrap`, Play. DBG → Estagiário → Contratar. DBG → Offline → Simular 1 h fora: as lascas sobem, e a linha da conta mostra os cristais e as lascas. Sem contratar: Simular 1 h não dá nada. Simular 8 h: conta só 6 h. *(Desde a E2, com 60 s ou mais fora as lascas entram no COLETAR do "Bem-vindo de volta".)*
- **E2:** a mesma cena. Contratar, DBG → Offline → **Fora 10 min**: aparece o "Bem-vindo de volta" com "10 min 00 s", os cristais e as lascas; o HUD não muda. **COLETAR**: o HUD sobe e o modal fecha. **Fora 8 h**: "Tempo fora 8 h 00 min" e a linha "Contou 6 h (máx.)". **Fora 30 s**: sem modal, as lascas entram direto. Para o "não paga duas vezes": com o modal na tela, sair do Play e entrar de novo (pela cena `Bootstrap`): o modal volta com as mesmas lascas; depois do COLETAR, não volta mais.
- **E3:** sem passos na tela; os testes automáticos cobrem (`bash tools/ci/run-tests.sh`). Para ver: estragar o `save.json` à mão e abrir (abre o backup).
- **E4:** cena `Bootstrap`, Play. Tocar em **AJUSTES** (coluna da direita): a tela abre com Efeitos 80%, tremor e vibração ligados. Arrastar o volume: cada passo toca um golpe no volume novo. Desligar o **Tremor da tela**, fechar no X, tocar no cristal: a câmera não treme. Sair do Play e entrar de novo (pela `Bootstrap`): o tremor continua desligado e o volume onde ficou. **CRÉDITOS**: a lista; **VOLTAR** volta aos AJUSTES. **APAGAR SAVE**: pergunta antes; **CANCELAR** não apaga; **APAGAR** volta ao nível 1 com tudo zerado (permanentes também) e os ajustes continuam como estavam. A vibração só se sente no celular (E5): curta no crítico (DBG → Efeitos → Crítico sempre), longa ao quebrar a parede da passagem.
- **E5:** a sessão de 2 h é automática (`LongSessionTests`, roda com os outros testes). No Moto G75, `Tools → Miners Idle → Build → Rodar no celular (Android)` (ou `bash tools/build/run-android.sh` no Terminal, com o Editor fechado), e o roteiro abaixo.

### Roteiro do Moto G75 (E5)

Anote ✅/❌ em cada passo; um ❌ volta para mim com o que apareceu na tela.

1. **Instalação limpa:** desinstalar o jogo antes (apaga o save velho). Abrir: jogo novo no nível 1, sem "Bem-vindo de volta".
2. **Vibração e tremor (D-199):** DBG → Efeitos → Crítico sempre: cada golpe crítico dá uma vibração curta; tocar no cristal sem crítico não vibra. Quebrar a parede de uma passagem (DBG → Loja → +lascas, comprar o Mapa, PASSAR DE CAVERNA): vibração mais longa.
3. **AJUSTES:** desligar a vibração: os críticos param de vibrar. Desligar o tremor: a câmera para de tremer. Baixar o volume para 20%: os sons ficam baixos. Fechar o app pelo multitarefa (arrastar para fora) e abrir: os três continuam como você deixou.
4. **Fechar com o estagiário:** DBG → Estagiário → Contratar (ou comprar na LOJA). Fechar pelo multitarefa, esperar **5 minutos**, abrir: "Bem-vindo de volta" com uns 5 min e as lascas. COLETAR: o HUD sobe.
5. **Fechar no meio do modal:** repetir o passo 4, mas com o modal na tela fechar o app de novo e abrir: o modal volta com as mesmas lascas (somadas ao tempo novo); depois do COLETAR, fechar e abrir não traz nada.
6. **Minimizar e voltar:** botão Início do Android, esperar 2 min, voltar pelo multitarefa: o modal com uns 2 min. Minimizar e voltar logo (menos de 1 min): sem modal, as lascas entram direto.
7. **Relógio para trás:** Configurações do Android → Data e hora → desligar "automática" e voltar o relógio 1 h. Abrir o jogo: nada de lascas, nada trava. Voltar o relógio para o certo (automática de novo) e abrir: no máximo o tempo que de fato passou, nunca a hora "ganha".
8. **Relógio para a frente:** adiantar o relógio 10 h, abrir: o modal mostra 10 h fora e "Contou 6 h (máx.)". Voltar para a automática.
9. **Apagar save:** AJUSTES → APAGAR SAVE → CANCELAR (nada muda) → APAGAR SAVE → APAGAR: nível 1, tudo zerado, os ajustes continuam. Fechar e abrir: continua o jogo novo.
10. **Sessão curta de verdade:** jogar uns 10 min em ×20 (DBG) com compras e um descanso: sem travar, sem tela preta, sem queda de quadros que incomode.

## Critério de pronto

- Fechar ou minimizar com o estagiário contratado e voltar dá as lascas da conta (P1), até 6 h, uma vez só, com o modal.
- Sem estagiário, nada de offline; relógio voltado não dá lascas nem trava.
- O save guarda a hora, abre os saves v1…v4 sem perda e se recupera de arquivo estragado pelo backup.
- AJUSTES com volume, tremor, vibração, Apagar save e Créditos, guardados à parte do save.
- Os atalhos do DBG → Offline funcionam.
- Os testes EditMode e PlayMode passam, a sessão de 2 h não trava, e o fluxo foi testado no Moto G75.
- README, Roadmap e decisões atualizados.

## Riscos

- **O offline pode render tanto quanto jogar.** Com o estagiário forte (um golpe por cristal), o ritmo é o da corrida, 0,5 s por cristal, o mesmo do anão online sem toque. Em 6 h ele traz o que o jogador ganharia em 6 h jogando sem tocar, menos a Mineração e o Coração Ardente do anão. Se o time achar demais, a saída está no balanço (ex.: uma corrida offline mais lenta), não na conta. O DBG mostra os números para decidir.
- **Relógio adiantado** dá 6 h de offline a qualquer hora (P4). Aceito: sem servidor, não há como saber; o teto limita.
- **Android mata o app em segundo plano** sem chamar o "sair". O save no minimizar (já existe) e a hora gravada nele cobrem: o offline conta a partir do último save.
- **Pagar duas vezes** fechando o app durante o modal. As lascas pendentes vão no save e só saem no COLETAR (E2).

## Fora deste marco

- O boost do AD, o combo, o cristal raro e a coluna de buffs (Bônus temporários, D-89, D-182).
- Notificação no celular ("o estagiário encheu o saco de lascas"): não está no GDD.
- Música (M10) e créditos com arte final (M10).
- Save na nuvem e troca de aparelho.

## Esforço

| Etapa | Horas |
|---|---|
| E1 | 6–8 |
| E2 (com o mockup) | 5–6 |
| E3 | 4–5 |
| E4 (com o mockup) | 6–8 |
| E5 | 3–4 |

Total: **~24–31 h** (o Roadmap estimava 40–55 h com o AD, que saiu daqui).

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| Plano | ✅ aprovado em 2026-11-10: P1…P8 com as recomendações (D-193…D-200), P9 (D-192), offline sem som nem vibração (D-191). E1 ainda não começou (a pedido do Bruno) | — |
| E1 Núcleo do offline, save v5 e DBG → Offline | 🔧 feita em 2026-11-10, aguardando a validação do Bruno. EditMode 306/306, PlayMode 125/125 | `OfflineMining` no núcleo (conta fechada, em trechos; o modo do 1º golpe é o das regras), teto de 6 h no balanço (`offlineMaxHours`), `MiningSimulation.ApplyOffline` (lascas no saldo e nas ganhas do dia, sem quebra nem trava do Mapa), save v5 com `savedAtUtcTicks`, `MiningRunner` com relógio injetável: offline ao abrir pelo save e ao voltar do minimizado, gravando na hora com a hora nova; DBG → Offline (Fora 1 min…8 h, Relógio −1 h, a conta do último). Na E1 as lascas entram direto; o modal é a E2 |
| E2 Modal "Bem-vindo de volta" | ✅ validada pelo Bruno em 2026-11-10 (mockup aprovado no mesmo dia). EditMode 362/362, PlayMode 127/127 | `OfflineReturn` no núcleo: com 60 s ou mais contados (D-195) as lascas ficam pendentes no estado da run e no save v5 (`pendingOffline`) até o COLETAR (D-197); voltas sem coletar somam; `MiningRunner.CollectOffline` grava na hora. `OfflineReturnScreen` (UI) e `OfflineReturnSetup` (Editor), painel que se ajusta ao conteúdo; textos em `FeedbackTexts`. DBG → Offline ganhou "Fora 30 s". `NumberFormatter.FormatDuration(…, trimWholeHours)` para o "6 h". |
| E3 Save que aguenta tudo | ✅ aprovada pelo Bruno em 2026-11-10. EditMode 368/368, PlayMode 128/128 | Na mesma troca atômica, o save anterior vira `save.json.bak` (`File.Replace` com backup). Principal estragado, faltando (app morto entre os dois passos da troca) ou de versão futura: vai para `.bad` e abre o backup (`SaveLoadStatus.LoadedBackup`, aviso no log); os dois ruins: jogo novo. Apagar save apaga o backup também. Testes: backup, app morto no meio, os dois estragados, versão futura, gravar de novo depois do backup, minimizar e voltar várias vezes sem duplicar lascas nem compras. |
| E4 AJUSTES | ✅ aprovada pelo Bruno em 2026-11-10 (mockup aprovado no mesmo dia). EditMode 373/373, PlayMode 133/133 | `settings.json` à parte (`SettingsStore`, D-200), lido pelo `GameBootstrap`; `GameSettings` (Game) guarda volume, tremor e vibração e avisa quem obedece (`SettingsEffects` passa ao `SoundBoard.Volume`, ao `ScreenShake` e ao `Haptics`). Vibração (D-199): `Haptics` vibra 30 ms no crítico e 120 ms na quebra da parede, nada no toque comum, com intervalo mínimo de 0,12 s (no Android, `VibrationEffect`; no Editor só conta). `SettingsScreen` (UI) e `SettingsSetup` (Editor): o botão AJUSTES deixou de ser "Em breve"; deslizador de 5% em 5% (grava ao fechar), liga/desliga do kit de madeira, CRÉDITOS (lista em dados, do `CREDITS.md`) e APAGAR SAVE com confirmação, que chama o `MiningRunner.ResetProgress` (agora grava o jogo novo na hora). O "Tremor on/off" do DBG troca o ajuste. Captura real: `tools/ci/out/m9-e4-real.png`. |
| E5 Sessão longa e celular | 🔧 em andamento (2026-11-10): `LongSessionTests` escrito (compila; ainda não rodou: o Editor estava aberto); roteiro do Moto G75 abaixo, aguardando o Bruno | Sessão de 2 h em ×20 na cena Game, com save numa pasta temporária: um jogador robô toca, compra, contrata o estagiário, passa de caverna, descansa pelo botão (modal e resumo do dia), compra permanentes, minimiza e volta, simula offlines com COLETAR e fecha e abre o jogo a cada 40 min; confere que a reabertura traz tudo igual e que nenhum erro sai no log. |
