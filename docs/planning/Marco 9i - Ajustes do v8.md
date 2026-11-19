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

### E4 — Apagar save e versão do save (#10)

- **Conferir o Apagar save:** um teste PlayMode que joga um pouco (compras, estagiário, descanso, permanentes, offline pendente), apaga pelo AJUSTES e confere que **tudo** voltou ao começo, inclusive o backup (`save.json.bak`). Os **ajustes** (som, tremor, avanço automático) continuam, como manda a D-200 (P2).
- **Versão do save (agora, para os testes):** o save grava a versão do app (a do build). Ao abrir um save de **versão mais velha** que a do app, aparece um modal: "Versão nova do jogo — para ver tudo o que mudou, recomendamos recomeçar.", com **RECOMEÇAR** (apaga, como o Apagar save) e **CONTINUAR** (P1).
- **Para produção (só o desenho, sem código):** uma seção no plano com o caminho: migrar o save de versão em versão (o save já tem número de versão, hoje 10) em vez de recomeçar, e um "o que há de novo" na primeira abertura depois de atualizar.

### E5 — Voltar do Android e a bomba por botão (#17, #18)

- **Voltar (#17):** o primeiro toque no Voltar mostra um aviso curto ("Toque de novo para sair"); o segundo, em até 2 s, abre o modal **"Sair do jogo?"** com o que o estagiário rende fora (as horas de offline de agora, ou o aviso de que ele não está contratado), **SAIR** e **FICAR**. SAIR grava o save e fecha o app. Com um modal aberto, o Voltar fecha o modal primeiro.
- **Bomba por botão (#18):** nos AJUSTES, **"Bomba: Deslizar / Tocar"**. No modo Tocar, um toque na bomba cheia lança. Padrão **Deslizar** (D-310, P4). Fica salvo com os ajustes.

### E6 — O resto dos ícones e a câmera nova (#12, #2–#5)

- **De-para dos ícones:** uma tabela com cada ícone do jogo e o candidato da pasta do Apellon (`icon_cave`, `icon_crystal`, `icon_crystal_red`, `icon_pickaxe`, `icon_speed`, `ui_button_rest`, `ui_rectangle`, `tab`, `tab_open`, `name_shop`, `loading`), para você aprovar antes de trocar.
- **Câmera nova:** os esteios somem aos poucos (#2); o corredor de trás ganha as marcas das quebras (#3, se der tempo); os cantos da transição (#4) e o recuo do DBG (#5).

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
- **E3…E6:** cada etapa traz o seu passo a passo aqui quando for feita.

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
| E2 Barra do andar, escada e transição | 🔶 feita (D-335), espera o seu teste | testes da barra, do Próximo Andar e da transição verdes |
| E3 Modais | ⏳ | mockup primeiro |
| E4 Apagar save e versão | ⏳ | |
| E5 Voltar do Android e bomba por botão | ⏳ | |
| E6 Ícones e câmera nova | ⏳ | de-para para aprovar |
| E7 Suíte de testes | ⏳ | sem as sessões longas |
