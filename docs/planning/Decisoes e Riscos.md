# Decisões, questões em aberto e riscos

> Registro vivo iniciado em 2026-10-20. Convenção herdada do Underworld Towers: cada decisão tem **opções, uma recomendação e o marco que ela bloqueia**; quando o
> Bruno decide, a linha vai para a tabela "Decididas", **com a data**. Valores de regra não mudam sem decisão registrada aqui.

## 1. Já decididas (documentadas nos arquivos originais)

| # | Fonte | Decisão |
|---|---|---|
| DEC-01 | GDD, RM | Mobile, **vertical**, Android primeiro, uma tela principal |
| DEC-02 | RM §2 | Unity LTS + C# + URP 2D; Unity UI + TextMeshPro; ScriptableObjects; JSON versionado em `persistentDataPath`; Unity Test Framework; Git + Git LFS; AudioSource + AudioMixer; `double` para números |
| DEC-03 | GDD | **Uma moeda** (lasca); 10 cavernas; 5 cristais por lote; mineração automática + toque (10%); crítico 5%×2; combo; penalidade após 100 quebras (−50%); boost AD ×2,5/30 s (simulado); estagiário; Letrado libera a passagem; Picareta Dupla |
| DEC-04 | GDD §12 | Fora do MVP: prestígio, dinheiro real/anúncios reais, multiplayer/rankings/missões diárias/eventos, inventário/múltiplas moedas, narrativa/chefes, servidor/nuvem |
| DEC-05 | GDD §8 | Pixel art; paleta-âncora; anão frontal, bípede, barba; picareta de madeira/aço |
| DEC-06 | RM §5 | Arte de IA é **matéria-prima**; licença e origem de todo asset são registradas; Point filter |
| DEC-07 | RM §7.1 | **Não reduzir:** testes de fórmulas, persistência, matriz do estagiário, penalidade, progressão até a cave 10, mineração durante loja/modal, testes em aparelho. **Reduzir primeiro:** animações extra, partículas, fundos únicos, brilho de 20 níveis, sons extra, transições |
| DEC-08 | GDD §13 | Balanceamento só muda após telemetria/playtest, com uma versão-base para comparação **(entra em conflito com D-01)** |
| DEC-09 | Repositório | Documentos e conversa em **pt-BR** (padrão do Bruno nos outros projetos) |

### 1.1 Decididas pelo Bruno em 2026-10-21

| # | Decisão |
|---|---|
| **D-01** | **Não recalibrar a economia agora.** O jogo **não precisa chegar à caverna 10 neste momento**; as conquistas/progressão serão balanceadas ao longo do desenvolvimento. O achado C-01 (fórmulas do GDD não fecham) fica **registrado como observação**, não como bloqueio. Mantêm-se as fórmulas do GDD v1.0 como valores iniciais em `BalanceConfig`; o simulador do M2 continua útil para medir, mas não condiciona o avanço dos marcos. |
| **D-05** | **Estilo = o do mockup** (fantasia, mina de madeira), exatamente como em `Designer (2).png`. O texto "industrial/sci-fi" do GDD §8.1 fica **superado** nesse ponto; as cores funcionais (âmbar = ação, vermelho = alerta) e a paleta de cristais permanecem. |
| **D-12** | **Criar o projeto Unity** na 6000.6.0f1 + instalar Android Build Support (recomendação aceita). *A decisão está tomada; a criação em si só começa quando o Bruno autorizar a próxima fase.* |
| **D-02 / D-03** | **Só pt-BR no MVP** (textos em dados) e nome **Miners Idle**. |
| **D-20** | Aparelho de teste: **Motorola Moto G75**. **Não há ambiente Android instalado** no PC (SDK/módulo do Unity/depuração USB a configurar no M1). |
| **D-17** | **~20 horas por semana**; o Bruno é o desenvolvedor (e o responsável pelo produto). |
| **D-18** | O jogo será **comercializado e terá anúncios reais** (antes tratado como pós-MVP). O **MVP continua com AD simulado**; a integração de rede de anúncios entra como marco próprio antes da publicação (ver [Roadmap](Roadmap.md) M12). |
| **D-21** | **Anúncios fora do MVP.** O time ainda vai definir como serão os ads. Por ora o botão AD só dá o **boost de velocidade** (×2,5 por 30 s), **sem integração de rede de anúncios**. M12 deixa de incluir o SDK de anúncios (volta a ser só publicação); a integração fica como item **pós-MVP** quando os ads forem definidos (D-18 continua: o jogo será comercializado e deverá ter ads no futuro). O código mantém o boost atrás de uma interface para trocar depois. |
| **D-23** | **PixelLab:** aprovado seguir com o teste do anão (verificação de créditos e termos comerciais continua no M1b). |
| **D-24** | **Moto G75 é o aparelho-alvo mínimo de desempenho.** |
| **D-25** | **M1 aprovado** (fundação técnica; APK rodando no Moto G75 a 60 fps). |
| **D-26** | **Mineração no mapa.** O anão **anda** por um mapa visto de cima e minera **grupos de cristais** que **nascem continuamente** (até um máximo na tela). Substitui o lote fixo de 5 cristais com o minerador parado acima do alvo (REQ-LOOP-02 e REQ-LOOP-03 ficam **superados**). Cada grupo de cristais é **um alvo** (uma vida, uma recompensa). Referência de layout da tela: [Referencia de tela - mapa.jpg](../design/Referencia%20de%20tela%20-%20mapa.jpg) (HUD de 4 indicadores em cima, Loja/AD à direita, "Passar de caverna" embaixo). Cada caverna terá um **bioma** (gelo, fogo, lava, pântano…); o detalhe visual fica para o Art Spike. Os elementos do print sem regra definida (barras laterais, timers, "Modifiers", botões subir/descer/Auto) **não** estão decididos. |
| **D-27** | **Regra de prioridade de alvo: "lote, depois mais próximo".** Cada cristal nasce com um **número de lote**: os que estão na tela no início são o lote 1; cada nascimento posterior recebe o próximo número. O anão escolhe **o cristal de menor lote; entre os do mesmo lote, o mais próximo dele** no momento da escolha (empate exato: o que nasceu primeiro). O alvo **não muda** até quebrar. Sem cristal na tela, o anão fica parado onde está. Efeito: os cristais iniciais são quebrados de vizinho em vizinho; depois a fila segue a ordem de nascimento, mesmo que um cristal novo surja ao lado do anão. |
| **D-28** | **Roadmap reordenado: jogável antes do núcleo de economia.** O novo **M2 é o "Protótipo do loop no mapa"** (graybox jogável: andar, minerar, quebrar, juntar lascas, HUD e debug), com só as regras que esse loop pede. O antigo M2 (calculadores de melhoria, `BalanceConfig` v1, simulador econômico, spec do estagiário) passa a ser o **M3 "Regras e economia"**, adaptado ao mapa. O antigo M3 (vertical slice) fica absorvido pelo M2 e pelo M3. M4 em diante não muda de número. Referências a "M2" como marco de economia/simulador em documentos anteriores a esta data valem para o **M3**. |
| **D-29** | **Valores de protótipo no M2.** O M2 usa um conjunto de valores marcado **"protótipo"** (vida, recompensa, dano, intervalo, velocidade de caminhada, nascimento de cristais), num ScriptableObject separado. Esses números servem para sentir o loop, **não são balanceamento**, e podem ser ajustados durante o M2 sem nova decisão. Os valores de regra do GDD v1 (D-01) continuam valendo como referência do M3. |
| **D-32** | **E6 do M2 reduzida (2026-10-21).** O teste de 10 minutos no Moto G75 fica **dispensado** neste marco: o loop já rodou no aparelho na E4 e na E5. Continuam valendo os testes automáticos: os da E5 (EditMode/PlayMode) ainda não rodaram dentro do Unity e rodam **no início do M3**, na primeira vez que o Editor estiver fechado. O Profiler (alocação por quadro) passa para o M5, quando entra o feedback visual. |
| **D-33** | **M2 aprovado (2026-10-21)**, com a E6 reduzida (D-32). Próximo: plano do M3. A pergunta do *feel* da caminhada (R-23) continua aberta. |
| **D-34** | **Caminhada aprovada (2026-10-21):** o vaivém pela ordem do lote ficou bom; mantêm-se a regra D-27 e a velocidade de caminhada de protótipo (4 unidades/s). R-23 fica em observação até o playtest. |
| **D-35** | **Valores do M3: fórmulas do GDD com base ajustável.** O *formato* de cada fórmula vem do GDD (custo `base × crescimento^nível`, dano ×2 por nível, −Δ s por nível com mínimo, +1% de lascas por nível…), mas os **números** (custos iniciais, crescimentos, efeitos, limites) ficam no asset, com valores de protótipo mirando a **1ª compra em menos de 3 min**. O GDD v1 literal fica guardado como asset `Balance_GDD_v1` para comparação. Detalha o D-01 (o GDD v1 deixa de ser o valor ativo e vira a referência). |
| **D-36** | **Escopo do M3:** regras de **Picareta**, **Velocidade**, **Mineração**, **Picareta Dupla** e uma linha nova, **Botas** (velocidade de caminhada; nasce do mapa, D-26), mais a **especificação do estagiário no mapa** (só documento; código no M6). Fora do M3: Letrado, penalidade e cavernas (M7), toque/crítico/combo (M5; a D-08 passa a bloquear o M5). |
| **D-37** | **Compras do M3 visíveis pelo painel de debug** (botões "comprar" e níveis/custos nas estatísticas); o HUD já reflete DANO e VELOCIDADE. A loja de verdade continua no M4. |
| **D-38** | **C-04 resolvido (2026-10-21):** o nível de uma melhoria é o **número de compras feitas** (começa em 0); a próxima compra custa `custo inicial × crescimento^nível` (a 1ª compra custa o custo inicial). Plano do M3 aprovado. |
| **D-39** | **Spec do estagiário no mapa aprovada (2026-10-21)**, com as recomendações: **E-1** escolhe o próximo alvo na hora (sem a "espera" do REQ-INT-04, que fica superado no mapa); **E-2** Botas valem para o estagiário; **E-3** reserva vale para os dois (o principal não rouba o alvo do estagiário); **E-4** um estagiário no MVP. Resolve o C-08. Ver [Spec - Estagiario no mapa](../design/Spec%20-%20Estagiario%20no%20mapa.md). |
| **D-40** | **M3 aprovado (2026-10-21)**: melhorias (Picareta, Velocidade, Mineração, Picareta Dupla, Botas) nas regras, compras pelo DBG, relatório de economia e spec do estagiário; validado no Moto G75. Próximo: plano do M4. |
| **D-41** | **D-11 decidida (2026-10-21): números em pt-BR com abreviação.** Milhar com ponto e decimal com vírgula, casas decimais só quando há fração ("1.234,5"); a partir de 1 milhão, três algarismos e M/B/T ("1,23M", "12,3B", "123T"); tempo com duas casas ("1,00s"). É o `NumberFormatter` atual, que deixa de ser provisório. Supera o "2 casas decimais" do REQ-UI-01. |
| **D-42** | **Loja do M4 mostra Estagiário e Letrado como linhas bloqueadas** ("Em breve") até existirem (M6/M7), para o jogador já ver o que vem. |
| **D-43** | **Save mínimo do M4 guarda só o progresso:** lascas, níveis das melhorias, golpes e quebras. Ao reabrir, o mapa nasce de novo (cristais novos, lote 1). Posição de cristais e do anão não são salvas. |
| **D-44** | **Escrita segura do save desde o M4:** arquivo com número de versão, gravado num temporário e trocado no fim (escrita atômica). Backup, migração entre versões e progresso offline continuam no M8. |
| **D-45** | **D-13 decidida (2026-10-21): interface em uGUI + TextMeshPro** (o que o HUD e o DBG já usam). Plano do M4 aprovado. |
| **D-46** | **M5 em graybox, com o Art Spike (M1b) em paralelo (2026-10-21).** O M5 entrega toque, crítico, combo e feedback com as formas atuais; a arte da caverna 1 entra depois que o Art Spike for aprovado. |
| **D-47** | **D-08 decidida (2026-10-21): toque em qualquer ponto da tela que não seja menu ou botão** (HUD, LOJA, DBG e painéis não contam como toque no mapa). O toque **pode dar crítico** e **não tem limite** de toques por segundo. Dano do toque: 10% de um golpe do principal (REQ-MIN-02, com a Picareta Dupla). |
| **D-48** | **Detalhes do toque e do M5 (2026-10-21):** (a) o toque conta **mesmo com o anão andando**, no alvo atual dele; (b) o combo é **só visual**, não muda dano (como no GDD); (c) sons do M5 são **bipes gerados no código**, trocados pelos reais junto com a arte. Plano do M5 aprovado. |
| **D-49** | **M4 aprovado (2026-10-21)**: loja com prévia "de → para", Estagiário e Letrado bloqueados, save do progresso com escrita atômica; validado no Moto G75 (fechar e reabrir) e com testes verdes no Unity (EditMode 112/112, PlayMode 15/15). |
| **Ferramenta de arte** | Testar o **PixelLab** (IA que gera pixel art e animações por MCP) para o anão; viabilidade de créditos gratuitos **a confirmar** (ver [Arte](../design/Direcao%20de%20Arte%20e%20Assets.md) §5). |

### 1.2 Decididas pelo Bruno em 2026-10-22

> Contexto: o dev principal propôs mudar a visão do jogo para uma **descida em corrida** (câmera de frente, anão correndo na direção dela por um corredor de caverna). A proposta completa está em [Proposta - Descida em corrida](Proposta%20-%20Descida%20em%20corrida.md); o Bruno aprovou todas as recomendações (D-52…D-58) depois de validar a E1 no celular. O código do mapa aberto continua no git (último commit do M5: `7f5183c`) e numa cópia de segurança feita pelo Bruno.

| # | Decisão |
|---|---|
| **D-50** | **Câmera de frente da descida**, escolhida no simulador: altura **2,4 m**, inclinação para baixo **15°**, distância até o anão **4,8 m**, campo de visão vertical **40°**, corredor de **3,0 m** de largura (o anão tem ~1,3 m). O anão corre parado na tela e o mundo se move em volta dele. Ajuste fino permitido no celular, na E1 do M6. |
| **D-51** | **Adiantar a E1 do marco novo (M6 — Descida em corrida)** antes de aprovar o M5 e antes de aprovar o resto da proposta, para validar a câmera com o dev principal no aparelho. Exceção consciente à regra "um marco só começa quando o anterior foi aprovado": a E1 fica numa **cena separada** (`Descida`), sem tocar nas regras nem na cena do jogo, e o loop dela é **só visual** (como o simulador). O M5 continua com a E5 pendente (teste de 10 min no Moto G75). Plano: [Marco 6 - Descida em corrida](Marco%206%20-%20Descida%20em%20corrida.md). |
| **D-52** | **Nova visão: descida em corrida (aprovada em 2026-10-22).** O corredor de caverna visto de frente, com o anão correndo na direção da câmera, **substitui o mapa aberto da D-26**. Ficam superadas a D-27 (alvo "lote, depois mais próximo": o alvo passa a ser o próximo da fila), a D-31 (caminho em linha reta) e a D-34 (feel da caminhada no mapa). A paleta, o tema e o estilo do mockup (D-05) continuam. O código do mapa aberto fica na tag `v0.5-campo-aberto` (commit `7f5183c`) e na cópia de segurança do Bruno. |
| **D-53** | **Projeção 3D simples com cara de pixel art:** corredor 3D com texturas de pixel art, renderizado em baixa resolução (270 px, D-14) e ampliado sem suavizar; anão, estagiário e cristais como sprites 2D de frente. URP com o renderer Universal (o que o projeto já usa). Validada na E1 do M6. |
| **D-54** | **Os cristais continuam nascendo por tempo:** os mesmos valores (1 a cada 5 s, no máximo 12 na fila); o cristal novo entra no **fim da fila**. Nenhum valor de regra muda por esta decisão. |
| **D-55** | **Sem cristal à frente, o anão continua correndo:** a descida nunca para; ele só para para bater. Correr sem alvo não rende nada (é só visual). |
| **D-56** | **Roadmap: o M6 passa a ser "Descida em corrida"**, antes do Art Spike (M1b) e do Estagiário. Os marcos seguintes ganham +1: Estagiário = **M7**, Letrado/penalidade/cavernas = **M8**, AD/save completo/offline = **M9**, arte das cavernas 2–10 = **M10**, balanceamento = **M11**, regressão/RC = **M12**, publicação = **M13**, pós-lançamento = **M14**. Em documentos anteriores a 2026-10-22, "M6" = Estagiário e assim por diante. |
| **D-57** | **Estagiário corre ao lado do anão**, um pouco atrás (como na E1 do M6). Se ele bate no mesmo cristal (muda o REQ-INT-03) ou num cristal ao lado (cristais em pares) fica para o marco do estagiário (M7), com a spec revista. |
| **D-58** | **Escala da trilha: 1 unidade da regra = 0,5 m no corredor** (só apresentação). A corrida de 4 un/s vira 2 m/s na tela, a velocidade validada na E1. |
| **D-59** | **M5 aprovado (2026-10-22) com a E5 reduzida**, como a D-32 no M2: a parte automática (Profiler, testes de alocação, `FrameStats` no DBG) está pronta e commitada; o teste de 10 min no Moto G75 passa para a **E5 do M6**, que já mede fps e alocação na tela nova. |
| **D-60** | **Onde o cristal nasce na fila (2026-10-22):** no fim da fila, a pelo menos **12 un (6 m) à frente do anão** (fora da tela, atrás da câmera); substitui a distância mínima do anão do mapa (1,5 un). O espaço entre cristais continua **2 un**. Os **10 cristais iniciais** começam na tela: o primeiro a **3 un** do anão, os outros a cada 2 un. Plano da E2–E5 do M6 aprovado. |
| **D-61** | **Limpeza na E3 do M6:** a cena de teste `Descida`, o loop só visual e as views do mapa aberto (mapa, cristais espalhados, linha até o alvo, "Mostrar ordem") saem quando a cena `Game` passar a ser o corredor; os controles da câmera vão para o DBG do jogo. O código antigo fica na tag `v0.5-campo-aberto`. |

### 1.3 Decididas pelo Bruno em 2026-10-23

> Contexto: o Bruno pausou a E5 do M6 e quer a cara de jogo primeiro (UI, depois anão, cristais e biomas). Escolhas feitas sobre uma prévia com os sprites reais dos packs. Plano: [Marco 1b - Art Spike](Marco%201b%20-%20Art%20Spike.md).

| # | Decisão |
|---|---|
| **D-62** | **Art Spike (M1b) agora, começando pela interface.** A E5 do M6 (relatório da economia e teste de 10 min) fica pausada e o M6 continua sem aprovação; exceção consciente à regra "um marco só começa quando o anterior foi aprovado", como a D-51. A parte 2 (anão, cristais, biomas) vem depois da UI aprovada. |
| **D-63** | **Kit de UI:** base = *Pixel UI Kit* (REEBOO Labs, tema madeira, amostra grátis); do *Fantasy UI Kit Lite* (Willosuna) só o **botão rubi** de comprar e o selo **"!"**. Painéis do HUD com `panel` (opção A). Sprites em escala 1:1 com a referência 270 px. |
| **D-64** | **Fonte dos textos do jogo: Pixelify Sans** (Google Fonts, OFL), porque a fonte do kit não tem acentos. O DBG fica na fonte atual. |
| **D-65** | **Assets com licença que proíbe redistribuir ficam fora do git** em `Assets/_Vendor/AssetStore/` (o repositório é público). Backup dos originais com o Bruno; `CREDITS.md` registra todos. O jogo sem eles cai no visual graybox. |
| **D-66** | **Ícones:** HUD com os 4 ícones do Bruno; na loja, Picareta e Velocidade com os do Bruno, Picareta Dupla montada com duas picaretas dele cruzadas, Mineração/Botas/Estagiário/Letrado do pack *Anvil's RPG icons* (Ponk, baixado em 2026-10-23, fora do git). Os ícones da **LOJA** (lojinha com toldo) e do **AD** (raio) o Bruno faz na ferramenta dele, no formato dos outros (64×64, arte 32×32). |
| **D-67** | **Engrenagem e botão AD aparecem já**, desligados: o AD fica cinza e os dois mostram "Em breve" ao tocar. Funcionam de verdade no M9. Exceção consciente à regra *jogável antes de sistema*, para a tela já ter a cara final. A engrenagem sai do HUD e vira o botão **AJUSTES na coluna da direita, abaixo do AD**, do mesmo tamanho e estilo da LOJA e do AD, para os 4 painéis do HUD ganharem a largura toda (pedido do Bruno). Cada painel do HUD: **nome sozinho em cima, ícone e valor embaixo**. |
| **D-68** | **LASCAS aparece sempre como número inteiro** ("12.234"), arredondado para baixo, no HUD e na loja (saldo e preços); a fração continua no estado, só não é exibida. Ajusta a D-41 só para lascas; DANO, VELOCIDADE e os textos flutuantes de ganho seguem a D-41. |
| **D-69** | **Câmera da descida F1** (escolhida sobre prévias do próprio jogo): altura **3,6 m**, inclinação **30°**, distância **5 m**, campo de visão 40° e corredor 3 m. **Supera a D-50**: na D-50 os cristais da fila escondiam o anão. Vigas mais altas e sprites virados para a câmera. |
| **D-70** | **Cristal da fila = o `icon_crystal` do Bruno em 5 cores** (branco, dourado, azul, roxo, vermelho), geradas pelo setup como rampas de uma cor. |
| **D-71** | **Anão em recorte animado** a partir do `dwarf-idle` (peças animadas por código: correr e minerar, Picareta Dupla com a segunda picareta). A folha `dwarf-mining` gerada quadro a quadro fica descartada (inconsistente). O estagiário é o mesmo boneco recolorido. |
| **D-72** | **Biomas em dados** (`BiomeDefinition`): textura e tinta de chão/parede/teto, neblina, decoração, lanternas e nome por caverna; as 10 definidas no M1b, a troca jogando é do M8. |
| **D-73** | **Licenças da arte de `Art/Cave` (lidas nos zips do Bruno em 2026-10-23):** rocha, pilar de cristal e gemas vêm do *Nasha's Dark Dungeons* (amostra grátis) = **uso comercial OK** (sem redistribuir). Esteios/props/equipamentos vêm do *Afterwork* (sampler) e `free_walls&doors` do *Free Gold Rush* = **só não comercial**: não vão para a versão publicada (Afterwork só com a Full, US$ 4,99). Outros packs do Bruno com uso comercial OK: *Super Grotto Escape* (ansimuz, pode redistribuir), *MineAssets*, *Nature* (CraftPix), *Dungeon Depths* (IA). *PixelFantasy Caves* veio sem licença: conferir antes de usar. Tudo com redistribuição proibida fica fora do git. |
| **D-74** | **Caverna 1 = N2** (escolhida sobre prévias do jogo): parede e teto de **rocha escura** e chão de **calçamento**, texturas grandes geradas por carimbos sorteados do *Pixel Fantasy Caves* (Szadi art., uso comercial liberado) a 64 px/m, sem padrão repetido; **esteios = portal de madeira do Pixel Fantasy** (miolo removido), desenhados só do anão para o fundo. Substitui a escolha anterior (Tilecraft + `mine_support`, que ficou "pano de mesa" e era não comercial). Pack e texturas derivadas fora do git. |
| **D-75** | **O anão são as folhas do PixelLab do Bruno**, tocadas inteiras (`dwarf-walking`: corrida com os quadros 1–8 em ciclo; `dwarf-mining`: 13 quadros, levantar 1–3 logo antes do golpe, impacto 4 no instante do dano, 5–12 depois, cada quadro com no máximo 80 ms). **Supera a D-71** (recorte animado): o Bruno reprovou o movimento do recorte. O "feio" do teste anterior era o jogo tocando só os 4 primeiros quadros do golpe e esticando o anão em 2,4 × 2 m; agora o quadro é quadrado (1,94 m) e o pé toca o chão. |
| **D-76** | **Estagiário = o anão grisalho com capacete de aço** (o mesmo boneco do PixelLab recolorido; só barba e capacete mudam), ao lado e um pouco atrás do anão (D-57), ligado pelo DBG até o M7. **As variações de cor do anão (castanho, loiro, grisalho) ficam guardadas no jogo** (`Art/Character/Variants/`) para um **futuro seletor de cor do anão do jogador** (pedido do Bruno, 2026-10-23; ainda sem marco). |
| **D-77** | **Cristais da fila = os desenhados pelo Bruno** (`crystal_glass`, `crystal_sapphire`, `crystal_yellow`, com base de terra), em revezamento pelo id; cada um com a cor dos seus estilhaços (dado da cena). O `crystal_glass_rare` fica guardado para um futuro **cristal raro** (valeria mais: regra nova, precisa de decisão). Supera a D-70 (as 5 cores geradas do `icon_crystal` ficam no projeto, sem uso). |
| **D-78** | **Botões LOJA e AJUSTES desenhados pelo Bruno:** o ícone (com moldura) é a face do botão, 48 px; o nome em PNG fica embaixo; ao tocar, escurece. A coluna (LOJA, AD, AJUSTES) passa a 48 px. Para validação com o outro dev. O AD continua no estilo de madeira até ter ícone próprio. |
| **D-79** | **Pedras na beira do caminho** (`rock_t1`, do MineAssets: uso comercial OK, fora do git): dos dois lados, no encontro do chão com a parede, por todo o corredor, com tamanho e posição sorteados de forma fixa; disfarçam a emenda entre chão, parede e esteios. |
| **D-80** | **A fila de cristais nunca fica abaixo de 4** (pedido do Bruno em 2026-10-23: "precisa sempre ter cristais pra ele quebrar", mesmo numa partida de 50 min). Antes, só o relógio (1 a cada 5 s) repunha a fila; com o anão forte ele quebrava mais rápido e ficava correndo sem cristais. Agora, abaixo de `minCrystals` (= 4, no `Balance_Prototipo`), nasce um na hora, no mesmo lugar de sempre (fim da fila, ≥ 12 un à frente, fora da tela); o relógio continua enchendo até 12. Efeito na economia: o ritmo de lascas passa a ser o do anão, não o do relógio. |
| **D-81** | **Ajustes do Art Spike pedidos no Moto G75 (2026-10-23):** o crítico não pisca mais em amarelo (só a quebra pisca, em branco); as gemas da beira do caminho saíram (ficam só as pedras); o VOLTAR da loja é rubi como o "comprar"; a corrida usa os quadros 2-3-4 e 6-7-8 da folha (cada perna 3 quadros; o 5 deixava uma perna mais longa). |
| **D-82** | **Arte só não comercial fica fora do projeto** (pedido do Bruno em 2026-10-23). Conferido pelas GUIDs: nenhuma cena, prefab, material ou script usava o Afterwork Dungeon (`mine/`, `structures/`, `gear/`) nem o Free Gold Rush (`free_walls&doors_TileSheet`). Os 49 arquivos saíram de `Assets/_Vendor/AssetStore/CaveMix` e foram para `Downloads\assets\_tirados-do-miners-idle\CaveMix`, junto dos zips originais. Uma arte nova de terceiros só entra no projeto com a licença comercial lida e registrada no `CREDITS.md`. |

### 1.4 Decididas pelo Bruno em 2026-10-24

| # | Decisão |
|---|---|
| **D-83** | **A tela não pisca mais na quebra; o golpe do anão treme mais** (pedido do Bruno em 2026-10-24: o pessoal não gostou do clarão branco). Saiu o clarão inteiro (`Effects/Flash` e o código), então nada pisca, nem no crítico (D-81) nem na quebra. Tremor de câmera, em unidades de mundo (a tela tem ~17 de largura): golpe 0,12 por 0,08 s → **0,25 por 0,12 s**; quebra 0,3 → **0,5** (continua mais forte que o golpe, já que perdeu o clarão). Crítico (0,4) e marco do combo (0,25) não mudaram. Achado: os valores que valem são os da cena `Game`; o código dizia 1,0 / 0,8 / 0,6 desde a M6 E4, mas a cena nunca recebeu. Agora o código e a cena dizem o mesmo. Ajuste fino no aparelho: DBG → Câmera → Intensidade dos efeitos. |
| **D-84** | **Builds de teste para o pessoal: script local + Firebase App Distribution** (caminho 1, escolhido pelo Bruno em 2026-10-24). Um comando no PC do Bruno gera o APK com versão automática (sobe a cada commit) e manda para um grupo de testadores no Firebase, que recebem convite por e-mail e atualizam sem desinstalar. O build no GitHub Actions (caminho 2) fica para depois: esbarra na licença do Unity no CI, na keystore fixa e nos assets que ficam fora do git (D-65), que fariam o CI gerar o jogo em graybox. Vira o **marco M15**, no fim do Roadmap (o Bruno prefere cuidar das mecânicas antes; pode ser puxado antes do playtest do M11). Plano: [Builds de teste](Plano%20-%20Builds%20de%20teste.md). |
| **D-85** | **O GDD v5 ([`docs/gdd v5.pdf`](../gdd%20v5.pdf), versão 5.0, "Idle Miner roguelite") passa a ser a referência de design**, no lugar do v1 (pedido do Bruno em 2026-10-24). O v1 fica como histórico, e o `Balance_GDD_v1` continua guardado para comparação. Diferenças e impacto por marco: [Análise - GDD v5](Analise%20-%20GDD%20v5.md). **Nenhum valor de regra muda só por esta decisão:** cada mudança (toque de 33%, bônus do combo, estagiário, Letrado, cristal raro…) entra com decisão própria quando o marco dela começar. As contradições do v5 ficam em §2.4 (G1–G10, com as decisões D-86…D-89), para tirar com o autor do GDD no marco indicado. As decisões anteriores que contrariam o v5 valem até serem revistas. |

### 1.5 Decididas pelo Bruno em 2026-10-29

| # | Decisão |
|---|---|
| **D-90** | **Começa a troca da arte de terceiros por arte própria, pelo chão** (pedido do Bruno em 2026-10-29). O chão da caverna 1 passa do calçamento do *Pixel Fantasy Caves* (D-74, fora do git) para o `tileset_ground_01`, desenhado à mão pelo Bruno (16 px exportado em 32 px), usado como PNG de 32 px a 64 px/m (tile de 0,5 m). Os tons `02`…`04` (o mesmo desenho, mais escuros) e o calçamento antigo ficam num botão do DBG para escolher no Moto G75; o tom escolhido vira o padrão. É a etapa **E9 do M1b**, não um marco novo; parede, esteios, pedras e interface vêm em etapas seguintes. Plano: [Marco 1b](Marco%201b%20-%20Art%20Spike.md) (E9). |

### 1.6 Decididas pelo Bruno em 2026-10-31

| # | Decisão |
|---|---|
| **D-91** | **Os esteios do corredor passam a ser arte própria** (pedido do Bruno em 2026-10-31, segue a D-90). O portal de madeira do *Pixel Fantasy Caves* (D-74, fora do git) sai e entra o `mine_wood` (do Apellon; 96×128 px, `Art/Corridor/`), no mesmo tamanho de hoje (3,4 × 4,5 m: a proporção 3:4 é a mesma). A variação com lampião (`mine_wood_lamp`) e o lampião solto ficam guardados, sem uso. Junto, os ícones `icon_cave` e `icon_crystal` foram trocados pelas versões novas (mesmo arquivo, mesmo GUID). É a etapa **E10 do M1b**. Plano: [Marco 1b](Marco%201b%20-%20Art%20Spike.md) (E10). |

### 1.7 Respostas às perguntas do GDD v5 (G1–G10), trazidas pelo Bruno em 2026-10-31

> Cada resposta vira uma decisão. As pontas que ficaram abertas viraram as perguntas G11–G16 (§2.4). Nenhum código muda agora: cada decisão entra no marco indicado.

| # | Decisão | Marco |
|---|---|---|
| **D-92** | **Vida e lascas por caverna (G1):** vale a §3.1 do v5. Vida do cristal `10 × 8^(c−1)`, lascas por cristal `1 × 3^(c−1)`; a tabela da §6.1 (`10^`, `2^`) fica descartada. São os valores de caverna do `Balance_GDD_v5` (D-87). | E5 do M6 / M8 |
| **D-93** | **A penalidade sai do jogo (G2):** sem regra, sem estado e sem feedback (a seta vermelha da §9 sai). Os 100 cristais quebrados na caverna atual são **só a trava para comprar o Letrado**. Nada a tirar do código: a penalidade nunca foi implementada. Resolve a D-19. | M8 |
| **D-94** | **Sai a linha "Picareta Online" (G3):** o dano online do estagiário vem só da linha **Estagiário** (`500 × 5^(n−1)`, nível 10; 10% × nível de uma picareta do anão, até 100%). A árvore do estagiário fica com Picareta Offline, Velocidade Offline, Mineração Offline e Picareta Dupla do Estagiário. | M7 |
| **D-95** | **Uma recompensa por cristal (G4):** quebrou, paga uma vez, não importa quem deu o último golpe (anão, estagiário ou toque). Toda quebra conta nos 100 cristais da trava do Letrado. | M7 / M8 |
| **D-96** | **Bônus do combo (G5):** o bônus vem quando o jogador **para de tocar**, pelo marco alcançado no combo (×10, ×30, ×50, ×100, valores do v5). Os bônus são **cumulativos**. Detalhe do acúmulo: G11. | Bônus temporários (D-89) |
| **D-97** | **Cristal raro (G6):** **15% de chance**; a melhoria temporária escolhida dura **60 s**. A lista de melhorias e a base da chance: G12 e G13. | Bônus temporários (D-89) |
| **D-98** | **Parede da passagem com vida (G7):** depois de comprar o Letrado, o jogador aperta **"Passar de caverna"**; a **parede de cristais** aparece mais à frente no corredor (não surge do nada: o anão chega nela andando), o anão bate nela até quebrar e aí começa a caverna nova. Vida da parede = **2 × a vida do cristal da caverna nova** (entrando na caverna *k*: `2 × 10 × 8^(k−1)`). Resolve a D-07 (e a C-03) pela parede. Toque e lascas na parede: G14. | M8 |
| **D-99** | **Volta do offline (G8):** ao reabrir o jogo, um modal **"Bem-vindo de volta"** com o **tempo offline** e o **total de lascas coletadas**. O **nível do estagiário vale online e offline**. Resolve a D-04 (offline existe, só do estagiário, teto de 4 h do v5). A conta exata: G15. | M9 |
| **D-100** | **Linhas de melhoria (G9):** Pesquisador = Letrado = Conhecimento (é a mesma linha). A linha **Crítico** existe; custo e efeito ainda não definidos (G16). Com isso, as "sete linhas" do v5 são Picareta, Velocidade, Mineração, Picareta Dupla, Crítico, Letrado e Estagiário (as Botas, D-35, são nossas, a mais). Resolve a D-06. | M8 (loja) |
| **D-101** | **Picareta Dupla do estagiário aparece no boneco dele (G10):** uma segunda picareta na mão do estagiário. A Dupla do anão continua sem mudar o estagiário (§5.3). | M7 |

### 1.8 Plano do GDD v5, decidido pelo Bruno em 2026-10-31 (aprovou as quatro recomendações)

| # | Decisão | Marco |
|---|---|---|
| **D-86** | **Toque passa a 33% do golpe** (v5), no lugar dos 10% da D-47. Entra **junto** com o `Balance_GDD_v5`, nunca sozinho no protótipo; testar no celular. | E5 do M6 |
| **D-87** | **Criar o `Balance_GDD_v5`** com os números do v5 (dano `5 × 2^(n−1)`, intervalo 2,0 → 0,1 s, Velocidade `50 × 5^(n−1)`, Mineração +100%/nível até +1000%, Letrado `100 × 10^(n−1)`; vida e lascas por caverna da D-92) e pô-lo no relatório de economia, ao lado do v1. | E5 do M6 |
| **D-88** | **M8 (Letrado, parede, 10 cavernas) vem antes do M7 (Estagiário).** Os números dos marcos não mudam; só a ordem. Motivo: o M8 dá o objetivo de descer e usa os biomas; o estagiário do v5 depende do offline (M9) e da economia de várias cavernas. | Roadmap |
| **D-89** | **Marco novo "Bônus temporários"**, depois do M8: bônus do combo (D-96), cristal raro com janela de 2 escolhas (D-97) e o boost do AD (sai do M9, que fica com save completo, offline e ajustes). O plano espera as G11–G13. | Roadmap |
| **D-114** | **Vida da última parede** (a do coração da montanha, D-112): a fórmula da D-98 uma caverna além, `2 × 10 × 8^10` ≈ 21,5 bi (16 × o cristal da caverna 10). Proposta na E3 do M8; o Bruno aprovou em 2026-10-31 ("a vida tá ótima, mantém assim"). | M8 |
| **D-113** | **Fora do M8:** a linha Crítico (espera a G16); o nome na loja fica "Letrado" até a G16. (plano do M8 aprovado pelo Bruno em 2026-10-31.) | M8 |
| **D-112** | **Fim da descida:** o Letrado 10 leva a uma última parede e à tela "Coração da montanha" (tempo de jogo e lascas); depois o jogo continua na caverna 10, sem reset. (plano do M8 aprovado pelo Bruno em 2026-10-31.) | M8 |
| **D-111** | **Troca de bioma quando a parede quebra:** a tela escurece meio segundo e aparece a faixa "Caverna N · nome do bioma". A emenda de chão fica para o M10. (plano do M8 aprovado pelo Bruno em 2026-10-31.) | M8 |
| **D-110** | **Botão de passar de caverna** âmbar, com o livro, na coluna da direita abaixo de AJUSTES; só aparece com o Letrado da caverna atual comprado e some ao apertar. (plano do M8 aprovado pelo Bruno em 2026-10-31.) | M8 |
| **D-109** | **Progresso dos 100 cristais na linha do Letrado** da loja ("Quebre 100 cristais: 37/100", botão apagado); aos 100, o botão acende com o preço. Sem contador novo no HUD. (plano do M8 aprovado pelo Bruno em 2026-10-31.) | M8 |
| **D-108** | **Parede e toque, provisório até a G14:** o toque bate na parede (crítico e combo valem); a parede não dá lascas e não conta nos 100 cristais. (plano do M8 aprovado pelo Bruno em 2026-10-31.) | M8 |
| **D-107** | **Parede a 12 un à frente do anão** (onde os cristais nascem, fora da tela). Os cristais antes dela ele quebra; os de depois somem; nada nasce até ela cair. (plano do M8 aprovado pelo Bruno em 2026-10-31.) | M8 |
| **D-106** | **M6 aprovado** (Bruno, 2026-10-31: "achei a descida gostosa"), com o teste de 10 min pendente com o Apellon (D-105). Libera o plano do M8. | M6 |
| **D-105** | **O teste de 10 min do M6 passa para o Apellon** (Bruno, 2026-10-31: sem tempo para fazer). O Bruno já aprovou a sensação da descida na 1ª rodada no G75. O Apellon segue o roteiro da E5 do Marco 6 (DBG → Zerar medição, 10 min sem sair do app, print com fps, quadros lentos, pior quadro e GC). | E5 do M6 |
| **D-104** | **Valores na Press Start 2P** (escolha do Bruno, 2026-10-31, entre 6 candidatas numa comparação no tamanho real do G75): na Pixelify Sans o 2, o 5 e o 8 se confundem (o "50" da loja parecia "$0"). Muda só o que é número: o valor dos 4 painéis do HUD, o saldo da loja e o preço do botão Comprar. Títulos, nomes, níveis e efeitos continuam na Pixelify (D-64). Tamanho 6 (3 px de tela por pixel da fonte no G75), para "999.999" caber no HUD. Google Fonts, OFL, no git com o `OFL.txt`. | Teste da E5 do M6 |
| **D-103** | **O jogo passa a usar o `Balance_GDD_v5`** (escolha do Bruno para o teste de 10 min da E5 do M6, 2026-10-31; a recomendação era manter o protótipo até o M8). Entra junto o toque de 33% (D-86). O protótipo e o v1 ficam para comparação no relatório. Como as Botas não existem no v5 (limite 0), a linha some da loja. O save antigo carrega os níveis cortados no limite de cada linha, mas eles passam a valer pelos números do v5: para o teste, apagar o save pelo DBG. | E5 do M6 |
| **D-102** | **Biomas das 10 cavernas, opção B** (escolhida sobre a prévia, 2026-10-31): cavernas 1–4 com os tons `01`…`04` do Apellon; 5–10 com paletas novas de 4 cores no mesmo desenho, seguindo o piso do GDD v5 §6 (verde úmido, basalto violeta, ferrugem, obsidiana, azul abissal, brasa). Junto, as recomendações do plano: nomes da coluna "Sensação" do GDD; pedra/buraco sorteados ~1 a cada 10 tiles; lampião em 1 de cada 2 esteios na caverna 1, rareando até nenhum na 8–10; neblina na cor do piso. Fecha a escolha de tom da E9 (caverna 1 = `01`). As cores ficam numa tabela do `BiomeArtSetup` (o setup regenera os chãos quando ela muda); tinta, neblina e lampiões também podem ser ajustados nos assets `Data/Biomes/Caverna_NN`. | M1b E8 |

## 2. Aguardando aprovação do Bruno (ordenadas pelo que bloqueiam)

Legenda: **Bloq.** = bloqueia a partir de qual marco · **Rec.** = recomendação.

> **Atualização 2026-10-21 (tarde):** D-25…D-29 decididas (M1 aprovado, mineração no mapa, regra de alvo, roadmap reordenado, valores de protótipo); D-30 e D-31 abertas em §2.3.
>
> **Atualização 2026-10-21:** D-01, D-02, D-03, D-05, D-12, D-17, D-18 e D-20 foram **decididas** (tabela §1.1). A tabela abaixo é o histórico das perguntas; restam abertas as D-04, D-06…D-11, D-13…D-16 e D-19.

### 2.1 Bloqueantes (decidir antes de M1/M2) — *todas respondidas em 2026-10-21*

| # | Pergunta | Opções | Rec. | Bloq. |
|---|---|---|---|---|
| **D-01** | **Economia:** as fórmulas do GDD v1.0 não fecham (C-01). Aceitar a **recalibração orientada a metas de ritmo** (Economia §4) mantendo a *intenção* do GDD e guardando o GDD v1 como `Balance_GDD_v1` para comparação? | (a) Recalibrar [rec.] · (b) Manter literal e aceitar o jogo inalcançável · (c) Reescrever as regras do Letrado/penalidade | **(a)** | M2 |
| **D-02** | Idioma do jogo e dos documentos | pt-BR só (UI e docs) · pt-BR + en na UI desde já · outro | **pt-BR só para o MVP**, com **todos os textos em dados** para en depois | M1 |
| **D-03** | Nome do produto (e do pacote Android) | **Miners Idle** (repositório) · Idle Miner (documentos) · outro | **Miners Idle** (a confirmar disponibilidade na Play Store) | M1 |
| **D-05** | **Direção visual:** texto do GDD (industrial/sci-fi) × mockup (fantasia/mina de madeira) | (a) **Mockup** (fantasia de mina) com funções de cor do texto [rec.] · (b) Industrial puro · (c) Híbrido (madeira + metal de segurança) | **(a)** | M1b |
| **D-12** | **Criar o projeto Unity** em `miners-idle`: versão (**6000.6.0f1**, a dos outros jogos; confirmar se é LTS), instalar **Android Build Support**, template 2D URP | (a) 6000.6.0f1 + módulo Android [rec.] · (b) LTS mais recente | **(a)** | M1 |
| **D-20** | **Aparelho Android** de teste (modelo, versão) e depuração USB | — | Obter antes do M1 | M1 |

### 2.2 Importantes (decidir até o marco indicado)

| # | Pergunta | Opções / Rec. | Bloq. |
|---|---|---|---|
| D-04 | **Progressão offline** no MVP? | → **resolvida pela D-99** (offline só do estagiário, modal de volta; conta na G15) | M9 |
| D-06 | **Linhas de melhoria:** 6 (sem *Crítico*) ou 7? "Letrado" ou "Pesquisador"? | **6 linhas, nome "Letrado"**, crítico fixo 5% **[rec.]**; ou adicionar *Crítico* (custo/limite a definir). → **resolvida pela D-100** (7 linhas com Crítico; custo na G16) | M7 |
| D-07 | **"Quebrar a parede"** para passar de caverna, ou só o botão? | **Botão + animação de parede quebrando como efeito** (sem vida/estado) **[rec.]** · Parede com vida (novo sistema) · Só botão. → **resolvida pela D-98** (parede com vida) | M8 |
| ~~D-08~~ | ~~Regras do toque: área (qualquer ponto × só o cristal), taxa máxima, crítico no toque?~~ → **decidido em D-47** | **Qualquer ponto**, sem limite rígido, **crítico vale no toque**, tremor reduzido em alta taxa **[rec.]**; decidir após ver o simulador | M3 |
| D-09 | **Boost:** o mínimo de 0,5 s vale com boost? Recarga igual à duração é intencional? | **Boost pode passar do mínimo (piso 0,2 s)**; recarga **60 s** após o fim **[rec.]** (ou manter 30 s se for proposital) | M8 |
| D-10 | **Ferramenta de pixel art** e origem da arte | Aseprite (pago, ~US$ 20 [verificar]) **[rec.]** · LibreSprite/Pixelorama (grátis) · só IA+retoque; **fallback**: pacotes CC0 de ambiente | M1b |
| ~~D-11~~ | ~~Formato numérico (`1.234,00` pt-BR × `1,234.00` do mockup) e abreviações~~ → **decidido em D-41** | **pt-BR** com abreviações K/M/B/T acima de 1 M **[rec.]** | M4 |
| ~~D-13~~ | ~~**UI:** uGUI+TMP [DOC] × UI Toolkit (usado no Underworld Towers)~~ → **decidido em D-45** | **uGUI+TMP** **[rec.]** (mundo em pixel art, números flutuantes) | M4 |
| D-14 | **Resolução de referência e PPU** | **270×480, ×4, PPU 16** **[rec., a validar no Art Spike]**; plano B 360×640 ×3 | M1b |
| D-15 | **Rastreador de tarefas** | Checklist nos planos + GitHub Issues/Projects (mostrar a lista antes de criar) **[rec.]** · só docs | M1 |
| D-16 | **Git LFS e ramos:** LFS só para áudio/binários grandes (não para PNG pixel art, cenas ou prefabs); *main* + ramos curtos | **[rec.]** como em Qualidade §2–3 | M1 |
| D-17 | **Horas por semana** disponíveis e calendário | Informar; converte as faixas do Roadmap em semanas | M2 |
| D-19 | **Justificativa da penalidade** (−50% após 100 quebras): "anti-grind/empurrar ao Letrado" × "pena ao jogador parado" | Documentar a intenção e ajustar o limiar relativo ao *K* (Economia §4) **[rec.]**. → **resolvida pela D-93** (a penalidade saiu) | M8 |

### 2.3 Novas perguntas geradas pelas respostas

| # | Pergunta | Rec. | Bloq. |
|---|---|---|---|
| ~~D-21~~ | ~~Rede de anúncios real~~ → **decidido (2026-10-21): fora do MVP**, só o boost de velocidade (ver §1.1) | — | — |
| **D-22** | **Conformidade de anúncios** (quando houver ads): política de privacidade, consentimento, classificação etária. A política de privacidade e a conta do Google Play ainda são necessárias para publicar. | Planejar no M12; verificar regras vigentes na época | M12 |
| ~~D-23~~ | ~~PixelLab~~ → **aprovado** o teste; falta confirmar créditos grátis e **termos de uso comercial** no M1b | — | M1b |
| ~~D-24~~ | ~~Moto G75 aparelho-alvo~~ → **decidido** | — | — |
| **D-30** | **"Contar picaretas"** (pedido de 2026-10-21): é um **contador de golpes** (estatística) ou um **segundo recurso**? | **Contador de golpes**, exibido no painel de debug no M2; um segundo recurso contraria a "uma moeda" (DEC-03) e pediria decisão própria **[rec.]** | M2 (não bloqueia o início) |
| **D-31** | **Caminho do anão:** linha reta até o alvo (cristais não bloqueiam) ou desviar de cristais/paredes? | **Linha reta no M2** **[rec.]**; desvio só se o mapa ganhar obstáculos | M2 |

### 2.4 GDD v5: decisões e perguntas em aberto (D-85; tirar no marco indicado)

> Anotadas em 2026-10-24 a partir da [Análise - GDD v5](Analise%20-%20GDD%20v5.md); **G1–G10 respondidas e D-86…D-89 aprovadas em 2026-10-31 (§1.7, §1.8)**. As **G** são perguntas para o autor do GDD (contradições ou lacunas do v5); as **D** são decisões do Bruno sobre o nosso plano. Nada aqui muda o jogo antes de ser respondido.

| # | Pergunta | Rec. | Quando tirar |
|---|---|---|---|
| ~~D-86~~ | ~~Toque de 33% do golpe~~ → **aprovada em 2026-10-31** (§1.8) | — | — |
| ~~D-87~~ | ~~`Balance_GDD_v5`~~ → **aprovada em 2026-10-31** (§1.8) | — | — |
| ~~D-88~~ | ~~Ordem M7 × M8:~~ → **aprovada em 2026-10-31** (§1.8) | — | — |
| ~~D-89~~ | ~~Marco novo "Bônus temporários":~~ → **aprovada em 2026-10-31** (§1.8) | — | — |
| ~~G1~~ | ~~Vida e lascas por caverna~~ → **D-92** | — | — |
| ~~G2~~ | ~~Penalidade~~ → **D-93** (saiu) | — | — |
| ~~G3~~ | ~~Estagiário × Picareta Online~~ → **D-94** (sai a Picareta Online) | — | — |
| ~~G4~~ | ~~Recompensa quando o estagiário quebra~~ → **D-95** | — | — |
| ~~G5~~ | ~~Quando o bônus do combo dispara~~ → **D-96** (resta a G11) | — | — |
| ~~G6~~ | ~~Cristal raro~~ → **D-97** (restam a G12 e a G13) | — | — |
| ~~G7~~ | ~~Parede da passagem~~ → **D-98** (resta a G14) | — | — |
| ~~G8~~ | ~~Mineração offline~~ → **D-99** (resta a G15) | — | — |
| ~~G9~~ | ~~"Sete linhas"~~ → **D-100** (resta a G16) | — | — |
| ~~G10~~ | ~~Dupla do estagiário no boneco~~ → **D-101** | — | — |
| G11 | **Acúmulo do combo (D-96):** quem chega a ×50 ganha só o bônus do ×50, ou também os do ×10 e ×30? Dois bônus iguais seguidos somam o efeito, somam o tempo ou só renovam? | Só o maior marco alcançado; bônus de combos diferentes somam o efeito, cada um com o seu tempo | Bônus temporários |
| G12 | **Melhorias temporárias do raro (D-97):** quais podem sair na janela de 2 opções e quanto cada uma dá? | Começar com 4 ou 5 (ex.: +dano, +velocidade, +lascas, +crítico) | Bônus temporários |
| G13 | **Base dos 15% (D-97):** é a chance de cada cristal que nasce ser raro? A janela de escolha pausa a mineração? | 15% por cristal que nasce; janela sem pausar (como a loja, D-42). **Risco:** 15% ≈ 1 em cada 7 cristais; com quebras de poucos segundos, a janela pode abrir demais. Testar no celular | Bônus temporários |
| G14 | **Parede (D-98):** o toque bate nela? Ela dá lascas ao quebrar? Conta nos 100 cristais da caverna? | Toque bate; não dá lascas nem conta nos 100 (é passagem, não cristal) | M8 |
| G15 | **Conta do offline (D-99):** como o nível do estagiário entra no offline (os mesmos 10% × nível, multiplicados pelas melhorias offline)? O teto de 4 h do v5 continua? | — | M9 |
| G16 | **Linha Crítico e nome do Letrado (D-100):** custo, efeito por nível (sobe a chance? o multiplicador?) e nível máximo do Crítico; na loja, o nome fica "Letrado", "Pesquisador" ou "Conhecimento"? | — | M8 (loja) |
| G17 | **Mineração do v5 barata demais?** Custa `50 × 1,2^(n−1)` e dá +100% por nível: no relatório da E5 do M6, em 30 min o jogador compra os 10 níveis e passa a ganhar ×11 por cristal, enquanto Picareta e Velocidade ficam no nível 3. É intencional, ou o crescimento do custo deveria ser maior (como os ×4 e ×5 das outras linhas)? | Confirmar com o Apellon antes do M8; medir de novo com as cavernas | M8 |

### 2.5 Para depois do MVP
Prestígio ("Nova Descida") · música · iOS · idiomas adicionais · missões.

## 3. Questões abertas rápidas (para responder quando convier)

1. ~~`Designer (2).png`~~ **encontrado em `docs/`** (é o mockup do GDD). O `GDD.txt` original continua **não localizado**; se existir, pode trazer detalhes que o GDD consolidado omitiu.
2. ~~Quem é o desenvolvedor?~~ **Bruno** (também o responsável pelo produto).
3. ~~Publicar/monetizar?~~ **Sim**, com anúncios reais (D-18). Consequência: **licenças de assets e termos do PixelLab precisam permitir uso comercial**; evitar CC-BY-NC/SA.
4. ~~AD simulado?~~ **Simulado no MVP; real antes da publicação** (M12).
5. ~~Tema musical~~: sem preferência por enquanto (música segue pós-MVP).
6. ~~Jogos de referência de ritmo~~: sem preferência por enquanto; as metas de ritmo ficam para o balanceamento ao longo do desenvolvimento.

## 4. Registro de riscos

Probabilidade/Impacto: B(aixo) · M(édio) · A(lto). Dono: Bruno, salvo indicação.

| ID | Risco | Prob. | Imp. | Mitigação | Gatilho / acompanhamento |
|---|---|---|---|---|---|
| R-01 | **Economia do GDD v1 não fecha** (C-01) → progressão travada nas primeiras cavernas. **Rebaixado em 2026-10-21**: aceito como observação; balanceamento ao longo do desenvolvimento (D-01) | A (confirmado) | **M** (não bloqueia marcos) | Medir no simulador (M2) e **revisitar antes do playtest do M10**; não deixar o jogo "inalcançável" chegar a ser lançado | Cave 2 > ~1 h de jogo ativo |
| R-02 | Calibração demora várias rodadas | M | M | Metas de ritmo primeiro; 3 perfis de jogador; `Balance_v2.x` versionado | M2 passa de ~55 h |
| R-03 | Ambiguidade do estagiário gera estados travados | A | A | Spec escrita (M2) + matriz de testes (M6) | Qualquer P0 do estagiário |
| R-04 | **Arte de IA inconsistente** / pixel de tamanhos mistos | A | A | Art Spike; redesenhar à mão sobre a referência; paleta indexada; auditoria | Folha de estilo reprovada no aparelho |
| R-05 | Custo de arte (10 cavernas × estados) estoura | M | M | Estrutura reutilizável + troca de rampa; cortes de RM §7.1; pacotes CC0 de ambiente | M9 > 90 h |
| R-06 | Falta de ferramenta/versão (módulo Android, JDK, Aseprite) | M | M | Instalar no M1; checar no Hub; plano B de ferramenta grátis | APK não gera em M1 |
| R-07 | ~~Falta de aparelho Android~~ → **Moto G75 disponível**; falta o **ambiente Android** (módulo Unity, SDK, depuração USB, driver) | B | M | Configurar no M1 (T-M1-01/08) | APK não instala no Moto G75 |
| R-08 | Horas: o RM supõe 40 h/sem; o Bruno tem **~20 h/sem** (D-17) → o calendário **dobra** (≈ 22–33 semanas, 5–8 meses, para 450–660 h) | A | M | Não prometer datas; reestimar após M1 e M3; cortar escopo pelo RM §7.1 | M3 > 55 h |
| R-21 | **PixelLab:** créditos grátis insuficientes, inconsistência entre quadros, termos sem uso comercial (jogo será vendido) | M | A | D-23: testar 1 personagem; ler os termos; retoque manual em editor de pixel; plano B de arte (Art doc §5) | Anão sem consistência em 4+ quadros |
| R-22 | **Anúncios reais + venda**: políticas de loja, privacidade/LGPD, consentimento, taxas de conta; rejeição na publicação | M | A | M12 dedicado; D-21/D-22; manter anúncio só premiado e opcional | Início do M12 |
| R-09 | Overengineering (DI, event bus, ECS) | B | M | Lista "fora de escopo" (Arquitetura §5); regra "jogável antes de sistema" | Revisão por marco |
| R-10 | Tap domina cedo e some tarde → percepção de "jogo se joga sozinho" | M | M | D-08; simular perfis; ensinar automação por feedback | Playtest M5/M10 |
| R-11 | Penalidade percebida como punição injusta | M | M | Ícone + aviso + explicação (S10); revisar limiar | Playtester não entende a seta |
| R-12 | Números grandes ilegíveis em tela estreita | M | B | Formatador único (K/M/B/T), testes de largura | Texto transborda no aparelho |
| R-13 | Perda de save / duplicação de moeda | B | **A (P0)** | Escrita atômica, backup, migrações testadas; idempotência da compra | Qualquer ocorrência |
| R-14 | Relógio do aparelho manipulado (boost/offline) | M | B | UTC + "não ganhar se o relógio voltou"; teto offline | Playtester abusa |
| R-15 | Licença de asset mal registrada (CC-BY-NC, EULA) | M | A | `CREDITS.md` desde o 1º asset; evitar CC-BY-NC/SA; registrar *prompt*/ferramenta/data da IA | Asset sem linha na tabela |
| R-16 | Keystore/senha vazados ou perdidos | B | **A** | Nunca no git; backup offline do keystore | Antes do M12 |
| R-17 | Git LFS mal configurado trava o repositório | B | M | Escopo mínimo do LFS (D-16); testar clone limpo | Falha no `git push` |
| R-18 | Requisitos de loja/política mudam até M12 | M | B | Checar no momento; M12 é separado do MVP | Início do M12 |
| R-19 | Jogo "parece genérico" no mercado de idle mining | M | M | Investir em *game feel* e arte (M5), identidade da descida, polimento | Playtest sem "uau" |
| R-20 | Dois outros jogos em andamento disputam as horas do Bruno | A | M | Trabalhar 1 marco por vez; estimar com horas reais (D-17) | Marco atrasado 2× |
| R-23 | **Tempo de caminhada** (D-26/D-27) vira parte da economia: andar longe pela ordem do lote reduz lascas/min e pode parecer "burro" ao jogador | M | M | Caminhada no núcleo (medível no simulador); velocidade de caminhada como valor ajustável; avaliar o *feel* no M2 com o debug de ordem ligado | Bruno acha o vaivém irritante no M2 |
| R-24 | Mapa com nascimento contínuo + estagiário (M6) aumenta a chance de alvo duplicado ou espera infinita | M | A | Seletor de alvo isolado e testado desde o M2; reserva de alvo entra junto com o estagiário. **No GDD v5 (D-85) o estagiário bate no mesmo cristal do anão e avança junto: o risco cai muito se o M7 seguir o v5** | Qualquer P0 do estagiário |
| R-25 | Texturas de pixel art "dançam" (shimmer) na perspectiva em baixa resolução (D-53) | M | M | Render de 270 px com filtro *point* e mipmaps; densidade de pixel das texturas decidida no Art Spike | Bruno ou playtester nota cintilação no corredor |
| R-26 | O cristal na frente tapa o anão no golpe | M | B | Cristal baixo; o anão para a uma distância que deixa o cristal abaixo do peito; conferido na E1 | Rosto ou picareta escondidos no aparelho |
| R-27 | O cristal aparece por pouco tempo (menos de 1 s com a câmera da D-50) e o jogador não o vê chegando | M | M | Escala da trilha (D-58); câmera ajustável no DBG; aviso na borda de baixo antes de o cristal entrar | Playtester não percebe o cristal chegando |
| R-28 | O 3D com cara de pixel art se afasta do visual do mockup | M | M | Paleta e tema do mockup nas texturas; validar um quadro da caverna 1 no Art Spike antes de produzir as outras | Art Spike reprovado |
| R-29 | O cristal minerado (~69–90% da tela) fica colado no botão PASSAR DE CAVERNA | B | B | O toque vale na tela toda; subir ou reduzir o botão se atrapalhar | Toques no botão por engano |
| R-30 | Os builds de teste (D-84) saem assinados com a chave de debug do PC do Bruno: se o PC ou a chave se perderem, os testadores precisam desinstalar (e perdem o save) para instalar a versão nova | B | M | Backup de `~/.android/debug.keystore`; chave de verdade (fora do git) antes do CI ou da Play Store (M12) | Troca de PC ou de chave |

## 5. Premissas a validar

Lista em [Descoberta](Descoberta%20-%20Relatorio.md) §9. As três mais críticas: **D-01** (a economia pode ser recalibrada), **D-05** (o mockup manda), **D-17** (horas/semana).
