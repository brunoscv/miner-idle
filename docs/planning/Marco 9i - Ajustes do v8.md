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

## A lista (fechada em 2026-11-19)

A [lista](Ajustes%20do%20v8%20-%20lista.md) tem 18 linhas: 5 do Claude (2026-11-18) e as 13 do Bruno (2026-11-19). Duas já estão respondidas pelo GDD e uma é ideia:

- **#8 (Bruno 3), cristais distantes no começo:** é do GDD. São 3 m entre cristais, e o **Atalho**, comprado no descanso, tira 0,5 m por nível (D-252, D-277, D-325). Como pedido, desconsiderado.
- **#14 (Bruno 9), a Velocidade no alto da tela:** é do GDD v8. O HUD mostra só lascas e vermelhas (§7), e Dano e Velocidade foram para a LOJA (D-281). Nada a fazer.
- **#16 (Bruno 11), bônus do último golpe da bomba:** fica registrada como **ideia**, para o Bruno conversar com o Apellon. Não entra no marco.

| Prioridade | Linhas | Por quê |
|---|---|---|
| **Alta** | #6 barra do andar, #7 bônus à direita, #9 botões do HUD, #11 texto da transição, #13 modais, #10 apagar save e versão | o jogador vê toda hora; a versão do save protege os testes de agora |
| **Média** | #12 ícones do Apellon, #15 Descansar apagado, #17 voltar do Android, #18 bomba por botão | aparecem às vezes ou são opção |
| **Baixa** | #2 esteios, #3 corredor vazio, #4 cantos da transição, #5 recuo do DBG | acabamento da câmera nova e do DBG |
| Espera arte | #1 bombas do tier 2 | depende do desenho |

## Etapas

Cada etapa de tela começa com **mockup** sobre captura real (270×480), aprovado antes do código.

### E1 — HUD e coluna da direita (#9, #7, #15, #12 da coluna)

- **LASCAS e VERMELHAS:** mais altura nos painéis, para o texto não encostar na borda de dentro (proposta: +4 px na tela de 270, 2 em cima e 2 embaixo).
- **AD (Boost):** a mesma folga embaixo.
- **LOJA e AJUSTES:** saem do fundo preto. O DBG ganha um botão para trocar entre as opções no jogo, e você escolhe vendo.
- **Ícones da coluna:** `icon_settings`, `icon_shop` e `icon_boost` do Apellon no lugar dos de hoje.
- **O que o mockup mostrou (2026-11-19):** os três ícones do Apellon já vêm **num tile de botão** laranja (96 px no PNG, pixel de 2 px: **48 px de arte, 1:1 na tela de 270**), com o desenho em cima e espaço para o rótulo embaixo. Por isso as duas opções do mockup ficaram:
  - **A, o tile do Apellon inteiro:** o tile é o botão, o rótulo em marrom escuro na parte de baixo; os três botões iguais (o AD também). A coluna fica uns 30 px mais alta na tela de 270.
  - **B, a moldura de hoje:** o fundo das lascas na LOJA e no AJUSTES, o AD como está, e só o desenho do Apellon (sem o laranja do tile).

  Recomendação: **A**, porque é o botão como o Apellon desenhou, na escala certa, e a coluna fica de uma cor só. Mockup: `tools/ci/out/m9i-e1-mockup.png` (e `-coluna.png`, o detalhe).
- **Escolha (2026-11-19, D-334):** **A, o tile do Apellon.** Com a escolha feita no mockup, o botão do DBG para trocar o fundo não é feito.
- **Como ficou no código:** `Editor/HudColumnV8Setup.cs` (roda por último no setup) aumenta o HUD, troca os três botões pelos tiles (`Art/UI/tile_shop.png`, `tile_boost.png`, `tile_settings.png`, cópias dos PNGs do Apellon), leva a coluna dos bônus para a direita e dá ao `RestButton` o jeito trancado (`Art/UI/icon_lock.png`, um cadeado de 8×10 px de arte desenhado aqui; textos `restLockedFormat` e `restLockedNotice` nos `FeedbackTexts`). O núcleo ganhou `MiningSimulation.IsRestLocked` e `RestLevel`.
- **Bônus (#7):** a coluna dos bônus vai para a **direita, abaixo do AJUSTES**, crescendo para baixo.
- **Descansar (#15):** sempre na barra de baixo. Antes do andar 10 (o `restLevel` do v8), **apagado**, com um cadeado e "Andar 10" embaixo; tocar mostra um aviso curto ("Descanse a partir do andar 10"). A escada (Próximo Andar) e a bomba já aparecem sempre.

### E2 — Barra do andar, escada e transição (#6, #12, #11)

- **Barra do andar:** **~40% menor** na altura. A borda `cave_meter` do Apellon por cima (12×128 px no desenho, 24×256 no PNG), a seta `cave_meter_arrow` no lugar da nossa marca do andar, e as cores dos andares de hoje pintadas por cima do resto de fundo amarelo dentro da borda.
- **Próximo Andar:** a **escada** (`stair`, 64×64) no lugar da seta.
- **Texto da transição (#11):** três propostas no mockup, todas com o texto ~2× maior:
  - **A, placa de madeira:** "ANDAR 12" numa placa pendurada, no estilo do mockup do jogo, com o nome da caverna embaixo;
  - **B, faixa:** uma faixa escura atravessando a tela, com o número grande e as bordas douradas das janelas do jogo;
  - **C, modal:** o quadro dos modais do jogo, com o título "ANDAR 12" e o nome da caverna.

  Recomendação: **A**, porque conversa com a mina de madeira (D-05) e não parece uma janela que pede toque.
- **O que o mockup mostrou (2026-11-19):** `tools/ci/out/m9i-e2-mockup-barra.png` (com o detalhe em `-barra-detalhe.png`) e `m9i-e2-mockup-transicao.png`, sobre capturas reais.
  - **Barra:** a `cave_meter` a 2 px de tela por pixel de arte: **24×256 px na tela de 270** (hoje são ~18×470 no 9:20 do G75, então fica ~45% menor; no 9:16, ~27%). A 1 px por pixel de arte ela teria só 128 px, pequena demais para ler os 10 biomas. Por dentro, o miolo de 4 px de arte recebe o âmbar até o andar de agora e as cores dos biomas por cima do amarelo; a seta `cave_meter_arrow` na mesma escala aponta o andar, com a etiqueta do número ao lado.
  - **Escada:** a `stair` a 1 px por pixel de arte (32×32 na tela de 270, como os tiles) no lugar da seta do DESCER. O desenho do Apellon já traz uma setinha vermelha ao lado da escada.
  - **Transição:** hoje o "ANDAR 13" tem ~5 px de altura na tela de 270. As três opções usam ~16 px (o título) e ~8 px (o bioma), no mesmo lugar do cartão de hoje (58% da altura): **A**, uma placa com a madeira dos tiles do Apellon pendurada por duas cordas; **B**, uma faixa escura com bordas âmbar de lado a lado; **C**, o quadro de madeira dos modais.
- **Ajustes do Bruno no mockup (2026-11-19):** a barra "muito top"; a placa A, com o texto como está, mas maior, perto do tamanho do quadro C (176×60 px na tela de 270); a barra **centrada** no espaço livre entre o HUD e a faixa da bomba (o mesmo vão em cima e embaixo); a **seta** do andar do tamanho da marca antiga (1 px de tela por pixel de arte, metade da escala da barra: o triângulo fica com 7×9 px); a **escada** 25% menor (24×24 px: no G75, 4 px de aparelho por px de 270, ficam 3 px por pixel de arte, sem distorcer; 30% daria pixels tortos).

### E3 — Modais (#13)

- Em todos os modais (descanso, LOJA, AJUSTES, raro, Boost, anúncio, offline, apagar save): **título mais alto** e o **X maior**, com área de toque de pelo menos 32 px na tela de 270. Mockup de um modal antes, e o mesmo padrão nos outros pelo setup.
- **O que o mockup mostrou (2026-11-19):** `tools/ci/out/m9i-e3-mockup-hoje.png` (os cinco modais de hoje), `m9i-e3-mockup-novo.png` (a proposta nos cinco) e `m9i-e3-mockup-detalhe.png` (a barra do AJUSTES a 2×, com a área de toque do X tracejada), sobre capturas reais.
  - **Hoje:** a barra do título é a faixa da janela do kit de madeira, com **8 px** de altura na tela de 270; o título tem 8,25 px; só o AJUSTES tem X, de 16 px. LOJA e Boost fecham pelo VOLTAR embaixo; o Descansar, pelo CONTINUAR.
  - **Proposta:** a faixa com **20 px** (a mesma madeira, esticada no desenho), o título em **11 px** (o próximo tamanho da Pixelify que fica nítido) e o **X de 24 px** (o `icon_close` a 1,5×: no G75 são 6 px de aparelho por pixel de arte, sem distorcer), com **área de toque de 32 px**. O texto da direita (LASCAS na LOJA, DIA no Descansar) fica do mesmo tamanho, antes do X.
  - **A janela cresce 12 px:** para baixo nos modais do meio da tela; o raro, preso em cima da bomba, cresce para cima; a LOJA, que já vai do HUD ao VOLTAR, fica do mesmo tamanho e a lista perde 12 px (ela rola).
  - **X onde hoje não há:** LOJA, Boost e Descansar ganham o X no canto, e o botão de baixo (VOLTAR, CONTINUAR) continua: o X é o lugar que o jogador procura, o botão grande é o mais fácil de alcançar com o polegar. O **raro não ganha X**: enquanto a escolha corre, não há o que fechar (o FECHAR aparece depois, como hoje). Anúncio, offline e apagar save seguem o mesmo padrão da barra; o X entra só onde já existe um jeito de fechar sem escolher.
  - Os nomes desenhados pelo Apellon (`name_settings`, `name_shop`) existem só para AJUSTES e LOJA: ficam de fora, para os títulos serem todos iguais.

### E4 — Apagar save e versão do save (#10)

- **Conferir o Apagar save:** um teste PlayMode que joga um pouco (compras, estagiário, descanso, permanentes, offline pendente), apaga pelo AJUSTES e confere que **tudo** voltou ao começo, inclusive o backup (`save.json.bak`). Os **ajustes** (som, tremor, avanço automático) continuam, como manda a D-200 (P2).
- **Versão do save (agora, para os testes):** o save grava a versão do app (a do build). Ao abrir um save de **versão mais velha** que a do app, aparece um modal: "Versão nova do jogo — para ver tudo o que mudou, recomendamos recomeçar.", com **RECOMEÇAR** (apaga, como o Apagar save) e **CONTINUAR** (P1).
- **Para produção (só o desenho, sem código):** uma seção no plano com o caminho: migrar o save de versão em versão (o save já tem número de versão, hoje 10) em vez de recomeçar, e um "o que há de novo" na primeira abertura depois de atualizar.
- **Como ficou no código (2026-11-19, D-337):**
  - **A versão do app:** cada build grava `0.1.0.AAMMDDhhmm` (a versão do projeto e a data e a hora do build) no `Application.version`; no fim do build a versão volta para `0.1.0`, e o `ProjectSettings` não muda no git. No Editor a versão é `0.1.0`. Assim todo APK novo é mais novo que o anterior, sem ninguém lembrar de subir número.
  - **No save:** o campo `appVersion` vai em cada gravação. O número do save continua 10: um save sem o campo (de antes da E4) abre igual, com a versão vazia, que conta como mais velha. A comparação é por partes numéricas (`AppVersion.IsOlder`).
  - **O aviso:** `VersionNoticeScreen`, uma cópia da confirmação do Apagar save (a mesma janela da E3), logo acima do "Bem-vindo de volta". Mostra "Seu save: x · Jogo: y" ("anterior" para um save sem versão). RECOMEÇAR é o mesmo `ResetProgress` do Apagar save (os ajustes ficam, D-200); CONTINUAR grava o save na hora, já com a versão nova, e o aviso não volta. Um jogo novo ou um save desta versão não avisa.
  - **DBG → Offline → "Aviso de versão"** abre o aviso a qualquer hora (os botões funcionam de verdade).
  - **O Apagar save** já apagava tudo e o backup; o teste novo confirma, jogando um pouco de tudo antes (recorde, descanso, permanentes, compra, estagiário, offline esperando o COLETAR).

### O save em produção (desenho, sem código)

Hoje o aviso sugere recomeçar porque o jogo ainda muda os números de um build para o outro. Na loja, ninguém pode perder o progresso por atualizar. O caminho:

1. **Migrar sempre, nunca recomeçar.** Cada mudança no que o save guarda sobe o número do save (hoje 10) e ganha uma migração no `SaveService`, como já é feito da 1 à 10: o campo novo nasce com um valor que não estraga o jogo, o campo que saiu é lido uma última vez e convertido. Um teste EditMode por versão abre um save de exemplo daquela versão (guardado em `Tests/EditMode/Saves/`) e confere o resultado.
2. **Mudança de balanço não apaga o save.** Se uma regra muda (custo, vida, recompensa), o save guarda o estado (níveis, andar, lascas), e os números são recalculados com as regras novas ao abrir. Quando uma mudança deixaria o jogador muito acima ou abaixo da curva, a migração compensa (por exemplo, devolve lascas de um nível que ficou mais caro), em vez de recomeçar.
3. **Versão do app separada da versão do save.** A versão do app (`0.1.0.AAMMDDhhmm` hoje; na loja, `1.2.0` e o `versionCode` que só cresce) serve para o "o que há de novo"; a do save, para a migração.
4. **"O que há de novo".** Na primeira abertura com a versão do app mais nova que a do save, um modal com as novidades daquela versão (texto em dados, pt-BR), com um único botão OK. Abre depois do "Bem-vindo de volta". Substitui o aviso de recomeçar.
5. **Save do futuro.** Um save gravado por um app mais novo (o jogador voltou para uma versão velha) continua abrindo o backup, como hoje; nunca apaga.
6. **Na nuvem (depois do lançamento):** o save local continua o principal; o da nuvem entra só como cópia, com a mesma versão do save e a mesma migração.

### E5 — Voltar do Android e a bomba por toque (#17, #18)

- **Voltar (#17):** o primeiro toque no Voltar mostra um aviso curto ("Toque de novo para sair"); o segundo, em até 2 s, abre o modal **"Sair do jogo?"** com o que o estagiário rende fora (as horas de offline de agora, ou o aviso de que ele não está contratado), **SAIR** e **FICAR**. SAIR grava o save e fecha o app. Com um modal aberto, o Voltar fecha o modal primeiro.
- **Bomba por botão (#18):** nos AJUSTES, **"Bomba: Deslizar / Tocar"**. No modo Tocar, um toque na bomba cheia lança. Padrão **Deslizar** (D-310, P4). Fica salvo com os ajustes.

### E6 — O resto dos ícones e a câmera nova (#12, #2–#5)

**O levantamento (2026-11-19).** Comparei a pasta do Apellon (`Downloads\svg`, desenhada pixel a pixel em SVG) com o que a cena e os catálogos usam. Quase tudo dele **já está no jogo**: o cristal das lascas, a picareta, a velocidade, a bomba (na LOJA e na faixa: a da faixa é o mesmo desenho), a escada, a barra e a seta do andar, os três tiles, o esteio, o lampião, os cristais e o chão. Sobram estas peças, e o de-para é sobre elas:

| # | Peça do Apellon | Onde entra | Hoje | Proposta |
|---|---|---|---|---|
| 1 | `icon_crystal_red` (cristal vermelho com azul) | VERMELHAS no HUD, saldo da LOJA, Descansar (aberto), modal do descanso, loja das permanentes | o cristal roxo das lascas pintado de vermelho | **Entra** |
| 2 | `ui_button_rest` (botão laranja, 130×66) | os dois botões de baixo: **Descansar** e **Próximo andar / DESCER**, em 9-slice no tamanho de hoje (94×36) | o botão de madeira escura do kit | **Entra.** Aceso: laranja com o texto em marrom, como os tiles. Trancado (Descansar antes do 10) e enchendo (Próximo andar): o laranja apagado, e o que já encheu acende |
| 3 | `loading` (moldura com o verde) | as barras de tempo: os 30 s do anúncio e os 30 s da escolha do raro (212 e 186 × 12 px; o desenho do Apellon tem os mesmos 12 px de altura) | a barra do kit de madeira | **Entra** |
| 4 | `cave_meter_t` (ponto laranja de 3×3) | as marcas dos andares que liberam algo, ao lado da barra do andar | tracinhos cinza de 4×1 | **Entra** |
| 5 | `ui_rectangle` (moldura laranja, fundo azul-acinzentado) | a etiqueta do andar ao lado da seta | caixa escura com o número em âmbar | **Opcional; recomendo não:** o azul-acinzentado tira o contraste do número e destoa da coluna |
| 6 | `tab` / `tab_open` (gaveta lateral com 5 vagas) | não tem lugar no jogo de hoje: parece uma gaveta que sai da borda e abre com 5 espaços (a coluna dos bônus?) | — | **Fica de fora:** é recurso novo, não ícone; perguntar ao Apellon para que foi pensada |
| 7 | `icon_cave` (montanha) | o HUD do v8 não mostra mais a caverna (§7, D-281) | — | **Fica de fora** |
| 8 | `name_shop`, `name_settings` | os títulos dos modais são texto, todos iguais (E3) | — | **Fica de fora** |

**Ainda sem desenho do Apellon** (ficam como estão; é a lista para pedir a ele): o ícone do AD (`icon_ad`), o raio dos bônus (`icon_bolt`), o X (`icon_close`), o alto-falante dos AJUSTES, o selo "!", os ícones da LOJA e das permanentes do pacote Anvil RPG (13), os dos bônus, o raro dourado (`icon_golden_rare`) e a picareta dupla.

**Mockups:** `tools/ci/out/m9i-e6-mockup-andar1.png` (Descansar trancado, Próximo andar enchendo), `m9i-e6-mockup-andar12.png` (os dois acesos, com a etiqueta opcional do #5), `m9i-e6-mockup-barras.png` e `m9i-e6-depara.png` (as peças). Feitos por cima das capturas reais; a captura no jogo (`TempHudCaptureTests.CaptureIconMockup`) fica para quando o Editor estiver fechado.

**A câmera nova (#2–#5):**

- **#2, os esteios:** hoje o esteio mais perto da câmera que é desenhado fica 0,6 m além do anão da F1 (5,6 m da câmera), porque na F1 ele cobria o anão. Na câmera do v8 esse corte cai no meio da tela, e o arco aparece ou some de uma vez ali. Proposta: **os postes passam sempre** (ficam nas bordas, não cobrem o anão nem os cristais) e **a viga some aos poucos** no último 1,5 m antes do corte, num **pontilhado de pixels** (o shader do corredor já recorta por alfa; o pontilhado é o jeito pixel art de "transparência", sem fila de transparentes). Alternativa: o arco inteiro some no pontilhado.
- **#3, o corredor atrás do anão:** pede desenho (marca da quebra, cacos). **Recomendo não fazer agora:** vai para a lista de pedidos ao Apellon.
- **#4, os cantos escuros na transição:** fica como a **D-327** decidiu (só aparece no cartão escuro). Nada a fazer.
- **#5, o "Recuo" no DBG → Câmera:** é da F1 (D-148). Proposta: **o botão só aparece com a câmera F1**; na do v8 ele some, em vez de mostrar "Recuo ?".

### E7 — Suíte de testes

- EditMode e PlayMode sem as sessões longas, tudo verde (como em 2026-11-18). Os `AdFlowTests` continuam pendentes.

## Perguntas (aprovadas em 2026-11-19, D-329…D-333)

| | Pergunta | Recomendação |
|---|---|---|
| P1 | Save de versão mais velha: modal com escolha ou recomeçar sozinho? | **Modal com RECOMEÇAR e CONTINUAR** |
| P2 | O Apagar save zera também os ajustes (som, tremor, avanço automático)? | **Não** (D-200: ficam) |
| P3 | Texto da transição: placa, faixa ou modal? | **A, a placa**, decidida no mockup |
| P4 | Bomba por botão: o padrão continua Deslizar? | **Sim** (D-310) |
| P5 | A ordem das etapas: E1 HUD → E2 barra e transição → E3 modais → E4 save → E5 Android e bomba → E6 ícones e câmera | **Esta ordem**: o que se vê primeiro, o save logo depois |

## Como testar (Bruno)

- **E0:** ler a lista e este plano e responder P1…P5.
- **E1 — HUD e coluna da direita** (feita em 2026-11-19, D-334). Nada de menu ou cena novos: o setup monta tudo na cena Game.
  1. Abrir o Unity e rodar `Tools → Miners Idle → Projeto → Aplicar configuração do projeto` (no Console, as linhas `[Setup]` com "M9i E1").
  2. Abrir a cena `Assets/_Project/Scenes/Game.unity` e dar Play (Game view em 9:16 ou 9:20).
  3. **HUD:** LASCAS e VERMELHAS um pouco mais altas, o nome e o número sem encostar na borda de dentro.
  4. **Coluna da direita:** LOJA, AD e AJUSTES são os tiles do Apellon, com o nome em marrom embaixo do desenho. Tocar em cada um: a LOJA abre, o AD abre o menu do Boost, o AJUSTES abre a tela; o botão escurece no toque. O "!" da LOJA continua no canto.
  5. **Bônus:** `DBG → Bônus → AD curto` (ou uma carta do raro). O ícone aparece à direita, embaixo do AJUSTES, com o tempo embaixo dele; tocar no ícone abre o detalhe à esquerda.
  6. **Descansar trancado:** no andar 1, o Descansar está na barra, cinza, com o cadeado e "ANDAR 10". Tocar: aparece "Descanse a partir do andar 10" acima do botão por ~1,6 s, e o modal não abre.
  7. `DBG → Andar → Ir ao andar` até o 10: o Descansar acende (madeira, o rubi e as vermelhas). Tocar abre o modal como antes.
  8. Descansar e COMEÇAR: na run nova (andar 1), o Descansar volta a ficar trancado.
- **E2 — Barra do andar, escada e transição** (feita em 2026-11-19, D-335). Nada de menu ou cena novos.
  1. Rodar `Tools → Miners Idle → Projeto → Aplicar configuração do projeto` (no Console, as linhas `[Setup]` com "M9i E2").
  2. Abrir `Assets/_Project/Scenes/Game.unity` e dar Play (Game view em 9:16 e depois em 9:20).
  3. **Barra:** à esquerda, a borda do Apellon, com o mesmo vão até o HUD e até a faixa da bomba (nas duas proporções). Por dentro, o âmbar até o andar de agora e as cores dos biomas; a setinha do Apellon aponta o andar, com o número ao lado.
  4. `DBG → Andar → Ir ao andar` (5, 10, 20…): a seta e o âmbar descem; com o recorde acima do andar de agora, o traço branco do recorde aparece.
  5. `DBG → Andar → Encher a barra`: o Próximo Andar vira DESCER, com a escada do Apellon no lugar da seta.
  6. Tocar em DESCER: no escuro da transição, a placa de madeira pendurada por duas cordas, com "ANDAR n" e o nome do bioma; ela aparece e some junto com o texto.
  7. Ir até o andar 11 (bioma novo) e conferir a placa com o nome do bioma novo.
- **E3 — Modais** (feita em 2026-11-19, D-336). Nada de menu ou cena novos.
  1. Rodar `Tools → Miners Idle → Projeto → Aplicar configuração do projeto` (no Console, a linha `[Setup]` "Modais com a barra do título de 20 px…: 10 janelas"; rodando de novo, ela não aparece mais).
  2. Abrir `Assets/_Project/Scenes/Game.unity` e dar Play.
  3. **AJUSTES:** a barra do título mais alta, "AJUSTES" maior e o X de 24 px; tocar no X (e também um pouco fora do desenho, a área de toque é de 32 px) fecha. Em CRÉDITOS, o mesmo X; em APAGAR SAVE, a confirmação com a barra nova (sem X: CANCELAR fecha).
  4. **LOJA:** "LOJA" e o saldo na barra nova, o X depois do saldo; o X fecha, o VOLTAR continua fechando. A lista rola até o Estagiário.
  5. **AD (Boost):** a barra nova e o X; o X e o VOLTAR fecham.
  6. **Descansar:** `DBG → Andar → Ir ao andar` até 10 ou mais, tocar em DESCANSAR: barra nova, "DIA n" antes do X; o X e o CONTINUAR fecham sem descansar.
  7. **Raro:** `DBG → Bônus → Janela agora` (o atalho do raro): a janela cresce para cima, o cristal e "CRISTAL RARO!" na barra nova, sem X.
  8. **Offline e anúncio:** `DBG → Offline → Fora 10 min` (o "Bem-vindo de volta") e ASSISTIR num anúncio do AD: as duas janelas com a barra nova.
- **E4 — Apagar save e versão do save** (feita em 2026-11-19, D-337). Nada de cena nova; um botão novo no DBG.
  1. Rodar `Tools → Miners Idle → Projeto → Aplicar configuração do projeto` (no Console, as linhas `[Setup]` do aviso de versão e do "Aviso de versão" no menu Offline).
  2. Abrir `Assets/_Project/Scenes/Game.unity` e dar Play. **Na primeira vez, o aviso "VERSÃO NOVA" aparece sozinho:** o save do Editor é de antes da E4, sem versão ("Seu save: anterior · Jogo: 0.1.0"). Tocar em CONTINUAR: o jogo segue com o seu progresso. Parar e dar Play de novo: o aviso não volta.
  3. `DBG → Offline → Aviso de versão`: o aviso abre. CONTINUAR fecha sem mudar nada.
  4. Jogar um pouco (comprar, contratar o estagiário, descer uns andares), `DBG → Offline → Aviso de versão` e tocar em RECOMEÇAR: tudo volta ao andar 1, sem lascas, sem estagiário, dia 1, sem vermelhas nem permanentes; os AJUSTES (som, tremor) ficam como estavam.
  5. **Apagar save:** jogar de novo, AJUSTES → APAGAR SAVE → APAGAR: o mesmo começo do zero.
  6. **No G75:** com o APK desta etapa, o save de lá é de antes da E4, então o aviso aparece na primeira abertura. Gerando outro APK depois, o aviso aparece de novo (o build novo tem a hora nova); CONTINUAR segue com o save.
- **E5 — Voltar do Android e a bomba por toque** (feita em 2026-11-19, D-338). Nada de cena nova nem de menu novo; uma linha nova nos AJUSTES. No Editor, a tecla **Esc** faz o papel do Voltar.
  1. Rodar `Tools → Miners Idle → Projeto → Aplicar configuração do projeto` (no Console: `[Setup] "Sair do jogo?"…`, `[Setup] Voltar: 11 telas respondem a ele.` e `[Setup] A bomba obedece aos AJUSTES…`).
  2. Abrir `Assets/_Project/Scenes/Game.unity`, dar Play e clicar na janela Game (para ela receber o teclado).
  3. **Sair:** apertar Esc uma vez: embaixo, acima da bomba, "Toque de novo para sair", que some em 2 s. Esperar sumir e apertar de novo: só o aviso volta. Apertar duas vezes seguidas: abre "SAIR DO JOGO?". Sem estagiário, o texto diz que nada é minerado com o jogo fechado; Esc ou FICAR fecham.
  4. Contratar o estagiário (LOJA, ou o DBG) e repetir: o texto diz "até 4 h de offline" (as horas de agora). No Editor, SAIR grava e não fecha nada (o `Application.Quit` só fecha o app no celular).
  5. **Fechar telas:** abrir a LOJA e apertar Esc: fecha, sem aviso. O mesmo no AD, no AJUSTES (com a confirmação do APAGAR SAVE aberta, o primeiro Esc só cancela; nos CRÉDITOS, só fecha os créditos) e no Descansar (`DBG → Andar → Ir ao andar` 10 ou mais).
  6. **Telas que seguram:** `DBG → Offline → Fora 10 min` e Esc: o "Bem-vindo de volta" fica (as lascas esperam o COLETAR). O mesmo no anúncio do AD e em `DBG → Offline → Aviso de versão`. No raro (`DBG → Bônus → Janela agora`), o Esc não fecha na escolha; depois de escolher, fecha.
  7. **Bomba:** AJUSTES → "Lançar a bomba" → tocar em DESLIZAR, que vira TOCAR. Fechar: a faixa diz "TOQUE" sem as setas. Com a bomba pronta, um toque em qualquer lugar da faixa lança. Voltar para DESLIZAR: só o arrastar lança, como antes. Parar e dar Play: o modo escolhido continua.
  8. **No G75:** o Voltar do aparelho (botão ou gesto) faz o mesmo que o Esc, e SAIR fecha o app; abrir de novo: o progresso está lá.
- **E6:** traz o seu passo a passo aqui quando for feita.

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
| Plano | ✅ aprovado em 2026-11-19 (D-329…D-333) | P1…P5 com as recomendações |
| E0 A lista fechada | ✅ 18 linhas com prioridade e as etapas | |
| E1 HUD e coluna da direita | ✅ aprovada pelo Bruno em 2026-11-19 ("Testei, pode seguir") | opção A (D-334) |
| E2 Barra do andar, escada e transição | ✅ aprovada pelo Bruno em 2026-11-19 ("Aprovada, segue") | placa A (D-335) |
| E3 Modais | ✅ aprovada pelo Bruno em 2026-11-19 ("Aprovada, segue") | D-336 |
| E4 Apagar save e versão | ✅ aprovada pelo Bruno em 2026-11-19 ("Aprovada, segue") | D-337 |
| E5 Voltar do Android e bomba por toque | ✅ aprovada pelo Bruno em 2026-11-19 ("Aprovada, segue") | D-338 |
| E6 Ícones e câmera nova | 📝 de-para e propostas da câmera esperando aprovação (2026-11-19) | |
| E7 Suíte de testes | ⏳ | sem as sessões longas |
