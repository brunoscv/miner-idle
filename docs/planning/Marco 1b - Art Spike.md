# Marco 1b — Art Spike

> Plano de 2026-10-23. O Bruno pausou a E5 do M6 e quer dar **cara de jogo** ao jogo antes (D-62). O Art Spike do [Roadmap](Roadmap.md) (M1b) é esse trabalho: decidir o estilo de UI, a fonte, o anão e os cristais. Ele vem em duas partes:
>
> - **Parte 1 — Interface** (este plano): HUD, botão LOJA e loja com os packs de UI que o Bruno escolheu, e uma fonte pixel com acentos.
> - **Parte 2 — Anão, cristais e biomas**: plano escrito quando a parte 1 for aprovada.
>
> Decisões deste plano: **D-62…D-68** em [Decisões e Riscos](Decisoes%20e%20Riscos.md) §1.3. **Plano aprovado pelo Bruno em 2026-10-23.**

## Objetivo (parte 1)

Trocar os retângulos coloridos do graybox por **molduras e botões de pixel art em madeira**, no espírito do mockup (`docs/Designer (2).png`), sem mudar nenhuma regra nem o layout geral.

**Para você:** abrir o jogo no celular e ver o HUD em placas de madeira com os seus ícones, os botões LOJA e AD na lateral (como no mockup), e a loja como uma janela de madeira com um ícone por melhoria, tudo numa fonte pixel que tem acento.

## Os packs

| Pack | Autor | O que tem | Licença |
|---|---|---|---|
| **Pixel UI Kit — amostra grátis, tema madeira** (`pixel_ui`) | REEBOO Labs | 67 sprites: painéis, janela, botões (3 tamanhos + quadrado, cada um com normal/hover/pressionado/desabilitado), molduras de item com 5 cores de raridade, barras, 10 ícones (engrenagem, +, estrela…), fonte bitmap | Uso comercial livre; **proíbe redistribuir os arquivos**; crédito opcional |
| **Fantasy UI Kit Lite** (`fantasy-ui`) | Willosuna | 30 peças em creme/rubi: botão rubi, fechar, painel creme, molduras, marcador "!", corações, chaves de teclado | Uso comercial livre; **proíbe redistribuir os arquivos**; crédito opcional |

| **Anvil's RPG icons** (baixado em 2026-10-23) | Ponk | 400 ícones 32×32 de RPG (armas, armaduras, botas, livros, bolsas, gemas), contorno escuro | CC BY 4.0 segundo a página; uso comercial livre; **proíbe redistribuir**; crédito apreciado |
| **Ícones do HUD** (cristal, picareta, caverna, cronômetro) | Bruno | Arte 32×32 salva ampliada 2× (64×64) | Do projeto |

O tema madeira é a base (combina com a mina e com o mockup). Do fantasy entram só dois detalhes: o **botão rubi** de comprar e o **"!"** de novidade (D-63). A fonte do kit de madeira não tem acentos (ã, ç, é), por isso a Pixelify Sans (D-64).

### Ícones (D-66)

| Onde | Ícone | De onde |
|---|---|---|
| HUD: LASCAS · DANO · CAVERNA · VELOCIDADE | cristal · picareta · caverna · cronômetro | Bruno (`Art/Crystals/icon_crystal`, `Art/UI/icon_pickaxe-export`, `icon_cave`, `icon_speed`) |
| Botão AJUSTES (abaixo do AD) | engrenagem | **Bruno vai fazer** junto com a loja e o raio (`icon_settings.png`), mesmo formato. Até lá, a engrenagem do kit de madeira em 16 px, no tamanho original (ampliada 2× ela invadia o nome; achado da E2). |
| Loja: Picareta · Velocidade | picareta · cronômetro | Bruno (os mesmos do HUD) |
| Loja: Mineração | saco de lascas (`Icon288`) | Ponk |
| Loja: Picareta Dupla | duas picaretas cruzadas | montado a partir da picareta do Bruno (cópia espelhada por cima) |
| Loja: Botas | bota de couro (`Icon224`) | Ponk |
| Loja: Estagiário · Letrado | capacete (`Icon165`) · livro (`Icon299`) | Ponk (aparecem apagados enquanto "Em breve") |
| Botão **LOJA** · botão **AD** | lojinha com toldo · raio | **Bruno vai fazer**, na mesma ferramenta e no mesmo formato dos 4 do HUD: 64×64, arte 32×32 ampliada 2×, fundo transparente, mesmo contorno. Nomes: `Assets/_Project/Art/UI/icon_shop.png` e `icon_ad.png`. Até chegarem, o botão mostra só o texto. |

**Escala dos ícones:** a arte de 32 px aparece com **16 px da referência no HUD** (ao lado do valor), **28–32 px nos botões da coluna** e **32 px na loja**. No Moto G75 (4 px de tela por px da referência) isso dá 2, 3–4 e 4 px de tela por pixel de arte: números inteiros, sem pixel torto. No HUD os ícones ficam com pixels mais finos que as molduras, como no mockup.

### Números do HUD (D-68)

- **LASCAS sempre inteiro** (`12.234`), arredondado para baixo: a fração continua contando por baixo, só não aparece, e a tela nunca mostra mais lascas do que você tem. Vale também para o saldo e os preços da loja.
- DANO, CAVERNA e VELOCIDADE seguem a D-41: `12,5`, `3`, `2,30s`. Os textos flutuantes de ganho (`+0,6`) também, senão um ganho pequeno apareceria como `+0`.
- Mudança no `NumberFormatter` (um método novo para lascas), com teste EditMode; entra na E2.

## Onde entra cada peça

| Lugar na tela | Peça | Detalhe |
|---|---|---|
| 4 painéis do HUD (LASCAS, DANO, CAVERNA, VELOCIDADE) | madeira `panels/panel` | 9-slice (borda 8 px), altura 38. Layout do Bruno: **o nome sozinho em cima** (âmbar, centralizado) e **embaixo o ícone e o valor lado a lado**, centralizados juntos. Ex.: `LASCAS` / 💎 `12.234`. Escolha do Bruno (A). |
| Botão **AJUSTES** (engrenagem) | madeira `buttons/button_square_*` | **Na coluna da direita, abaixo do AD** (pedido do Bruno: o HUD fica só com os 4 painéis, que ganham a largura toda). Mesmo tamanho e estilo da LOJA e do AD: ícone em cima, "AJUSTES" embaixo. Ao tocar, mostra "Em breve" (a tela de configurações é do M9, S07). |
| Botão **LOJA** | madeira `buttons/button_square_*` | 46×46 à direita abaixo do HUD (onde já está): ícone em cima, "LOJA" embaixo; troca para `pressed` ao tocar. Os três botões da coluna (LOJA, AD, AJUSTES) têm o mesmo tamanho; 46 px é o que cabe "AJUSTES". |
| Selo de novidade da LOJA | fantasy `markers/quest_available` | O "!" roxo no canto do botão, no lugar da bolinha vermelha. |
| Botão **AD** (D-67) | madeira `buttons/button_square_disabled` | Logo abaixo da LOJA, mesmo tamanho, **cinza**; ao tocar, mostra "Em breve". Liga de verdade no M9. |
| Fundo escurecido da loja | — | Continua uma cor (preto 60%). |
| Painel da loja | madeira `panels/window` | Janela com barra de título: **LOJA** à esquerda, saldo à direita. |
| Linha de cada melhoria | madeira `panels/panel_inset` | 9-slice (borda 4 px). As bloqueadas ("Em breve") ficam escurecidas. |
| Moldura do ícone da melhoria | madeira `slots/slot_rarity_*` | Uma cor por melhoria (dado do catálogo): Picareta comum (cinza), Velocidade raro (azul), Mineração épico (roxo), Picareta Dupla lendário (dourado), Botas incomum (verde); bloqueadas `slot_disabled`. Dentro, o ícone da melhoria (tabela de ícones acima). |
| Botão comprar — **dá para comprar** | fantasy `buttons/button_ruby_*` | Escolha do Bruno: rubi, para saltar aos olhos. |
| Botão comprar — sem lascas, comprada, máx. | madeira `buttons/button_medium_disabled` | Cinza: lê-se "não dá agora" sem precisar ler o texto. |
| **VOLTAR** | madeira `buttons/button_large_*` | Normal e pressionado. |
| Todos os textos do jogo (HUD, loja, textos flutuantes) | **Pixelify Sans** | O painel DBG continua na fonte atual: é ferramenta de desenvolvimento e tem textos miúdos. |

Prévia feita com os sprites reais sobre a tela atual (escolhas acima): HUD **A**, loja **2** (madeira + rubi); a segunda prévia já com os ícones e os botões LOJA e AD.

O "Em breve" da engrenagem e do AD é um texto curto sobre o botão, que some sozinho; o texto fica em dados (o mesmo `lockedLabel` do catálogo da loja).

### O que fica de fora (e por quê)

- **PASSAR DE CAVERNA.** O Letrado e a passagem são do M8 (S05). O estilo fica reservado: `button_large` (desabilitado cinza, habilitado normal) com o livro do Ponk.
- **Tela de configurações e anúncio de verdade.** Engrenagem e AD só aparecem (D-67); o que eles abrem é do M9.
- **Barra de vida do cristal.** Ela é desenhada dentro do corredor 3D, não na UI; muda com os cristais, na parte 2.
- **Animações de painel** (entrar/sair deslizando). Polimento, depois que o visual estiver aprovado.

## Técnica

- **Onde os packs ficam (D-65).** O repositório no GitHub é público e as duas licenças proíbem redistribuir os arquivos. Os packs vão (com os `.meta`) de `Assets/_Project/Art/UI/` para `Assets/_Vendor/AssetStore/PixelUIKit/` e `Assets/_Vendor/AssetStore/FantasyUIKitLite/`, pasta que o `.gitignore` já deixa fora do git. Os zips originais ficam no backup do Bruno. O `CREDITS.md` é criado com os dois packs, a fonte e a arte de abertura.
- **Clone limpo / CI sem os packs.** O setup procura os sprites; se faltarem, mantém o visual graybox e escreve `[Setup] Packs de UI ausentes: ...`. Nenhum teste depende da arte existir.
- **Importação por código** (no `ProjectSetup`, como o resto): Sprite, filtro **Point**, sem compressão, sem mipmap, e as bordas de 9-slice tiradas do `atlas_sample.json` (madeira) e do `manifest.json` (fantasy, 6 px). PPU dos sprites de UI = `referencePixelsPerUnit` do Canvas (100), para **1 px do sprite = 1 px da referência 270**. No Moto G75 (1080 px de largura) isso dá escala **4× exata**, sem pixel torto.
- **A pele é aplicada por um passo novo do setup (`UiSkinSetup`)**, que atualiza os objetos que já existem na cena `Game` e no prefab `UpgradeRow`. O setup atual só cria o que falta, então sem esse passo a cena não mudaria. O passo é idempotente: rodar duas vezes não muda nada.
- **Moldura e ícone por melhoria em dados:** o `ShopCatalog` ganha, por linha, o sprite da moldura e o sprite do ícone (no lugar da cor do quadrado). O código não decide cor nem ícone. A Picareta Dupla montada é um PNG gerado pelo setup a partir da picareta do Bruno, em `Assets/_Project/Art/UI/` (vai no git: é arte do projeto).
- **Ícones do Ponk** já estão em `Assets/_Vendor/AssetStore/AnvilRpgIcons/` (os 400, com `LICENSE.txt` resumindo a página); o catálogo aponta para os quatro usados.
- **Botões com estado:** `Button` com *Sprite Swap* (normal/pressionado/desabilitado). A `UpgradeRowView` troca o sprite de fundo entre rubi e cinza conforme dá ou não para comprar; os textos (preço, "comprada", "máx.") continuam vindo do catálogo.
- **Fonte (D-64):** Pixelify Sans (Google Fonts, licença OFL, pode ir no git) em `Assets/_Vendor/Fonts/PixelifySans/` com o `OFL.txt`. Um *font asset* do TextMeshPro gerado pelo setup (`Art/Fonts/PixelifySans Bitmap.asset`), com os caracteres do português, em atlas estático.
  - **Achados da E1:** a fonte usa um grid de 11 "pixels" por em, e cada pixel é um quadrado que se sobrepõe um pouco ao vizinho. Em **SDF** (o padrão do TMP) essa sobreposição saía oca e fechava letras ("C" virava "O"); rasterizada **1:1** (tamanho 11), o espaçamento entre letras ficava irregular; com **hinting**, as letras deformavam. O que funcionou: **raster sem hinting a 44** (4 px de atlas por pixel da fonte), filtro **Point**.
  - **Tamanhos:** os textos da UI vão para o múltiplo de 2,75 px da referência mais próximo (5,5 · 8,25 · 11 · 13,75), para cada pixel da fonte cair num número inteiro de pixels de tela no Moto G75. Os textos flutuantes (mundo) mantêm o tamanho, porque mudam de escala na animação.
  - **Seta:** a fonte não tem "→"; os textos de efeito da loja passam a usar "»" ("Dano 1 » 2"), no catálogo (dado).
  - O "C" da Pixelify tem só uma aberturinha no meio do lado direito: de longe, "LASCAS" pode parecer "LASOAS". É o desenho da fonte; julgar no aparelho.
- **Alvo de toque:** botões com pelo menos **30 px de altura na referência** (≈ 48 dp no Moto G75, o mínimo do Android).
- Nenhum texto novo no código; nenhum valor de regra muda.

## Etapas

### E1 — Packs no lugar e fonte nova ✅ (validada no Moto G75 em 2026-10-23)

- Mover os packs para `_Vendor/AssetStore/` (com `.meta`), configurar a importação, criar o `CREDITS.md`.
- Baixar a Pixelify Sans do Google Fonts e trocar a fonte de todos os textos do jogo (DBG fica como está).
- **Como testar:** rodar no celular; ler "Mineração", "Estagiário" e os números do HUD: acentos certos, letras nítidas, nada cortado. Abrir a loja e o DBG.
- **Pronto quando:** você aprova a fonte no aparelho (ou pede outro tamanho).

### E2 — HUD, LOJA e AD ✅ (validada no Moto G75 em 2026-10-23)

- Painéis do HUD em madeira (nome em cima, ícone e valor embaixo), ocupando toda a largura; LASCAS inteiro (D-68); na coluna da direita, de cima para baixo e do mesmo tamanho: LOJA (estado pressionado e selo "!"), AD desligado e AJUSTES.
- **Como testar:** rodar no celular; ver os 4 painéis com ícone; tocar LOJA (afunda e abre a loja); comprar algo para o "!" aparecer/sumir; tocar AD e a engrenagem ("Em breve"). Tocar nesses botões não conta como toque no cristal. O tremor do HUD no combo ×10 e o pulso de LASCAS continuam.
- **Pronto quando:** você aprova o HUD no aparelho.
- **Achado:** o teste `DescentTests.Dwarf_RunsStopsBreaksAndRunsAgain` falhava às vezes (corrida entre a partida criar um cristal e a descida desenhar); corrigido para parar a partida e esperar um quadro antes de comparar.

### E3 — Loja ✅ (validada no Moto G75 em 2026-10-23)

- Janela, linhas, molduras coloridas com os ícones, botão rubi/cinza, VOLTAR.
- **Como testar:** abrir a loja; uma melhoria que dá para comprar fica rubi, uma sem lascas fica cinza; comprar (botão afunda, preço sobe, linha atualiza); Picareta Dupla vira "comprada"; as duas bloqueadas aparecem escurecidas; VOLTAR fecha. Usar o DBG para ganhar lascas e testar rápido.
- **Pronto quando:** você aprova a loja no aparelho.
- **Achados:** a tábua da barra de título da janela tem só 8 px (linhas 6 a 13 do sprite): "LOJA" e o saldo vão em 8,25 px e em cor clara (âmbar sobre essa madeira tinha pouco contraste). A descrição longa das linhas bloqueadas quebra em duas linhas antes do botão. O catálogo aponta para ícones e molduras dos packs; num clone sem eles, as referências ficam vazias e a linha volta ao quadrado colorido.

## Testes

- Os testes atuais continuam verdes (EditMode e PlayMode); os da loja e do HUD já cobrem compra, selo, pulso e tremor.
- Novo teste de Editor: todo sprite em `_Vendor/AssetStore/PixelUIKit` e `FantasyUIKitLite` está com filtro Point e sem compressão (fica *ignorado* se os packs não existirem, como na CI).
- Novo PlayMode: os botões da UI têm pelo menos 30 px de altura na referência.
- Conferência visual: um teste temporário que salva a tela em PNG (`tools/ci/out`), depois apagado, como no M6.
- Moto G75 ao fim de cada etapa.

## Critério de pronto da parte 1

> **Parte 1 concluída e aprovada pelo Bruno em 2026-10-23** (E1 `96707c3`, E2 `318d43a`, E3).

- [x] Packs fora do git, importados por código; `CREDITS.md` criado.
- [x] Textos do jogo na Pixelify Sans, com acentos, nítidos no Moto G75.
- [x] HUD com ícones, LOJA, AD e loja com a pele nova; estados (pressionado, sem lascas, comprada, máx., bloqueada, em breve) visíveis.
- [ ] Ícones da LOJA, do AD e de AJUSTES do Bruno: **ainda não chegaram**. Quando estiverem em `Art/UI` (`icon_shop.png`, `icon_ad.png`, `icon_settings.png`), o próximo setup encaixa.
- [ ] Clone sem os packs: **não verificado, e não cai mais no graybox colorido.** Depois da pele, a cena e o prefab apontam para sprites dos packs com cor branca: sem eles, molduras e botões aparecem como retângulos brancos (o jogo roda). Para trabalhar noutra máquina, copiar `Assets/_Vendor/AssetStore/` do backup.
- [x] Testes verdes (EditMode 123, PlayMode 42).
- [x] Você aprova no aparelho.

## Esforço

10–16 h.

---

# Parte 2 — Corredor, cristais, anão, estagiário e biomas

> Plano de 2026-10-23, depois de prévias tiradas do próprio jogo (câmera da descida em 270×600) com a arte candidata. Decisões do Bruno: **D-69…D-74** em [Decisões e Riscos](Decisoes%20e%20Riscos.md) §1.3. **Aguardando aprovação do Bruno.**

## Objetivo

Trocar o desenho por código do corredor, dos cristais e do anão por arte de verdade, no estilo do mockup, e deixar os biomas das 10 cavernas prontos como **dados**, sem mudar nenhuma regra.

**Para você:** abrir o jogo e ver o anão do seu desenho correndo e minerando de verdade, num corredor de rocha com esteios, lanternas e gemas nas paredes, com a fila de cristais em coluna (como no mockup). No DBG, trocar o bioma para ver como cada caverna vai ficar.

## O que as prévias mostraram

| Tema | Achado |
|---|---|
| **Corredor** | A rocha do pack de `Art/Cave` (`cave_rock_floor`/`cave_rock_wall`, 32 px, tileável) no chão, parede e teto já deixa o jogo muito melhor que o tijolo gerado por código. A lanterna (`gear/hand_lantern`) e as gemas (`rough_amethyst`, `rough_ruby`, `pyrite_cube`) funcionam como decoração das paredes. |
| **Câmera** | Na câmera D-50, os cristais da fila ficam entre a câmera e o anão e **escondem o anão** (com arte de verdade, fica gritante). Na **F1** (3,6 m, 30°) a fila aparece em coluna e o anão fica à vista. Câmeras mais altas atravessam as vigas e achatam os cristais (placas em pé vistas de cima). |
| **Cristais** | O seu `icon_crystal` em 5 cores (uma rampa por cor, mantendo o claro/escuro do desenho) é o que mais lembra o mockup. Girar o matiz não serve (o desenho mistura rosa e ciano). |
| **Anão** | O `dwarf-idle` (parado) é ótimo. A folha `dwarf-mining` não serve para animar: os 13 quadros mudam de ângulo (frente/3-4), de tamanho e de lugar, um tem uma picareta gigante. Não existe anão grátis de frente com correr e minerar (os bons, como o do Elthen, são de lado e pagos). |
| **Biomas** | A mesma rocha, tingida, com a neblina do fundo na cor do bioma, já dá 4 climas bem diferentes (mina, gelo, lava, fungos): barato para 10 cavernas. |
| **Tiles** | `free_walls&doors_TileSheet` é vista de cima: não serve para o corredor. O PK texturepack (CC0) não é pixel art (ruído/pintado): destoa. |
| **Parede × chão** | Pedra em tudo cansa, e tiles pequenos repetidos (Tilecraft, 32 px) parecem "pano de mesa". O que funcionou: **texturas grandes (8 m de corredor) montadas por carimbos sorteados**, com emenda nas bordas, a partir dos blocos naturais do **Pixel Fantasy Caves** (Szadi art.), a **64 px por metro** (perto da densidade do anão). Escolhida a **N2**: rocha escura na parede e calçamento de pedra no chão. |
| **Esteios** | Sprite de portal no lugar das caixas de madeira é muito mais bonito. O `mine_support` do Afterwork é só não comercial; o **portal de madeira com cintas de ferro do Pixel Fantasy Caves** (com o miolo escuro removido) faz o mesmo papel e é liberado. **Nenhum esteio pode ficar entre a câmera e o anão** (o mais próximo cobria o anão inteiro): só do anão para o fundo. |

## Decisões (D-69…D-74)

- **D-69 — Câmera F1:** altura 3,6 m, inclinação 30°, distância 5 m (campo de visão 40° e corredor 3 m como na D-50). Supera a D-50. As vigas sobem para não cruzar a tela e os sprites (cristais, anão) passam a ficar virados para a câmera.
- **D-70 — Cristal da fila:** o `icon_crystal` do Bruno em 5 cores (branco, dourado, azul, roxo, vermelho), geradas pelo setup em `Art/Crystals/` (rampas de uma cor). Os estilhaços da quebra usam a cor do cristal.
- **D-71 — Anão em recorte animado:** a partir do `dwarf-idle`, o anão é separado em peças (corpo, cabeça, braço com picareta, pernas) e animado por código: correr = passada alternando as pernas + balanço; minerar = a picareta gira para trás e desce no instante do golpe (o relógio do golpe é do núcleo). A Picareta Dupla põe a segunda picareta na mão esquerda. O estagiário é o mesmo boneco recolorido.
- **D-72 — Biomas em dados:** cada caverna é um asset (`BiomeDefinition`): textura e tinta de chão/parede/teto, cor da neblina, decoração das paredes, cor das lanternas e nome. As 10 ficam definidas aqui; a troca de caverna no jogo é do M8 (passagem de caverna).
- **D-74 — Caverna 1 (N2):** parede e teto de **rocha escura** e chão de **calçamento de pedra**, as duas texturas geradas pelo setup com carimbos sorteados do *Pixel Fantasy Caves* (blocos de rocha 169/193; calçamento 201 girado e espelhado), 256 px a 64 px/m; esteios = **portal de madeira do Pixel Fantasy** (sem miolo), só do anão para o fundo. O pack e as texturas derivadas ficam fora do git (`_Vendor/AssetStore/PixelFantasyCaves`). Tilecraft e Nature ficam como fonte para os outros biomas.
- **D-73 — Pack de `Art/Cave` = Afterwork Dungeon Pixel Assets (bbig0104):** o sampler grátis é **só não comercial**; para publicar, comprar a Full library (US$ 4,99+) ou trocar a arte. Arte gerada com IA (declarado pelo autor). Fica fora do git.

## Etapas

### E4 — Corredor de rocha e câmera F1 ✅ (validada no Moto G75 em 2026-10-23)
- Rocha escura na parede e no teto, calçamento no chão, geradas pelo setup a partir do Pixel Fantasy Caves (D-74; o gerador fica no `Editor/`, as texturas geradas em `_Vendor`); portais de madeira do Pixel Fantasy como esteios, só do anão para o fundo; lanterna e gemas de parede como sprites; câmera F1 (D-69); sprites virados para a câmera. Rocha um pouco mais clara (tinta do material), porque na F1 a parede aparece pouco atrás dos esteios.
- O pack de `Art/Cave` vai para `Assets/_Vendor/AssetStore/AfterworkDungeon/` (fora do git, D-73): mesmo com a Full library, a licença proíbe redistribuir os arquivos.
- O trabalho em andamento no `DescentStage` (textura única de cristal, quadros de anão) é absorvido aqui e substituído pelo que as próximas etapas pedem.
- Importação dos PNGs de `Art/Cave` e `Art/Character` como pixel art (Point, sem compressão; repetição nas texturas de chão/parede).
- **Testes:** os de enquadramento da descida passam a medir a F1 (onde ficam cabeça e pés do anão na tela); um quadro continua sem alocação.
- **Como testar:** rodar no celular; ver o corredor de rocha, a fila em coluna e o anão sempre à vista; 60 fps no Moto G75.
- **Como ficou:** `CorridorArtSetup` (Editor) gera `rock_dark`, `cobble` e `support_portal` em `_Vendor/AssetStore/PixelFantasyCaves/Generated/` e preenche os campos novos do `DescentStage` (texturas de chão/parede/teto a 64 px/m, tinta da parede 1,35, esteio em sprite 3,4 × 4,5 m, gemas do Nasha). A cena migrou da D-50 para a F1 e o botão do DBG virou "Padrão F1". `Art/Cave` foi para `_Vendor/AssetStore/CaveMix` (com `LICENSES.txt` das três origens).

### E5 — Cristais ✅ (validada no Moto G75 em 2026-10-23)
- 5 cores geradas do `icon_crystal`; cada cristal da fila usa a cor do seu tipo; barra de vida acima; estilhaços e faíscas na cor do cristal.
- **Como testar:** ver as 5 cores na fila; bater e quebrar; conferir que o cristal não esconde o anão.
- **Como ficou:** `CrystalArtSetup` (Editor) gera `Art/Crystals/crystal_{white,gold,blue,purple,red}.png` do `icon_crystal` (rampas de uma cor, no git) e põe na lista `crystalTextures` do `DescentStage`, na ordem de `DescentArt.CrystalColors`: a cor do cristal é a mesma dos estilhaços da quebra. Cristal de 0,7 m. O campo de textura única do trabalho em andamento saiu.

### E6 — Anão com as folhas do PixelLab (D-75; substitui o recorte animado da D-71) ✅ (validada no Moto G75 em 2026-10-23)
- O recorte animado foi testado em GIF e reprovado pelo Bruno: o movimento certo é o das folhas do PixelLab.
- `DescentStage` toca as folhas em grade (`SetFrame` só muda o recorte da textura: sem alocação): corrida 1–8 em ciclo (10 qps); mineração com o impacto (quadro 4) no instante do dano, o levantar (1–3) logo antes e 5–12 depois, no máximo 80 ms por quadro (com golpes rápidos, comprime). Quadro quadrado de 1,94 m (o anão mede 1,32 m) descido 13% para o pé tocar o chão.
- `DwarfArtSetup` importa as folhas como pixel art e preenche a cena. Os campos `Sprite[]` do trabalho em andamento saíram.
- **Como testar:** ver o anão correr entre os cristais e minerar; o impacto da picareta bate junto com as faíscas e o número; DBG ×5 para golpes rápidos.
- **Observações:** no golpe o anão agacha e gira um pouco para a direita (é a arte); a barra de vida fica na altura da barriga dele. A Picareta Dupla (segunda picareta) depende de uma folha nova do PixelLab.

### E7 — Estagiário (D-76) ✅ (validada no Moto G75 em 2026-10-23)
- `DwarfVariantsSetup` gera 3 variações de cor do anão (castanho, loiro, grisalho; capacete de aço) das folhas `dwarf-idle`, `dwarf-walking` e `dwarf-mining`, em `Art/Character/Variants/` (no git): ficam guardadas para o **futuro seletor de cor do anão** (ideia do Bruno, sem marco ainda).
- O estagiário é o **grisalho**: ao lado (0,85 m) e um pouco atrás (0,6 m) do anão, com a corrida defasada 3 quadros e o golpe junto com o do anão (o golpe próprio dele é do M7).
- **Como testar:** DBG → Câmera → **Estagiário** liga e desliga.

### Ajustes pedidos pelo Bruno durante a E7 (D-77…D-81) ✅ (validados no Moto G75 em 2026-10-23)
- **Cristais:** a fila reveza os cristais desenhados por ele (glass, sapphire, yellow); o raro fica guardado.
- **Botões:** LOJA e AJUSTES com ícone e nome desenhados por ele (para validação com o outro dev).
- **Pedras:** fileira de pedras nas duas beiras do caminho, por todo o corredor.
- **Botões com o nome dentro do quadrado:** o setup gera `button_shop`/`button_settings` (moldura do ícone esticada, desenho em cima, nome embaixo); 52×40 px.
- **Picaretada de despedida:** se um toque quebra o cristal antes da picareta descer, o anão faz um golpe completo e rápido (só ilustra).
- **Corrida fluida:** o ciclo é só a passada (quadros 3–8); o 1 é a pose parada e o 2, a saída dela (davam a "pausinha").
- **Figuras de frente para a câmera:** anão, estagiário, cristais, barra de vida e gemas se inclinam com a câmera (`spriteLean`); antes a câmera de cima os achatava e o anão parecia largo.
- **Gemas:** saíram da parede e depois do chão (D-81); ficam só as pedras.
- **Barra de vida mais discreta** (0,46 × 0,05 m, verde mais suave) e **"CRÍTICO!" ~20% menor.**
- **Segunda rodada no Moto G75 (D-80, D-81):**
  - o chão e a parede rolavam **o dobro** da velocidade das pedras e dos esteios (a rolagem entrava depois da escala da textura no shader); agora andam juntos;
  - o anão e o estagiário se inclinam **em volta dos pés**, em cima da sombra (antes os pés iam para trás dela e ele parecia flutuar);
  - as pedras descem as linhas transparentes de baixo do desenho e variam menos de lugar e tamanho;
  - corrida com cada perna 3 quadros (2-3-4 e 6-7-8); crítico sem clarão amarelo; VOLTAR rubi;
  - o botão "Estagiário" (DBG → Câmera) cabe no painel: os 4 botões numa linha;
  - a fila de cristais nunca fica abaixo de 4 (D-80).

### Experimento: anão com menos pixels (2026-10-24) ✅ (validado no Moto G75 em 2026-10-24)
- Folhas `low-dwarf-walking` e `low-dwarf-mining` do Bruno (quadros de 64 px, mesma grade e mesmos quadros das de 256), para validar a ideia com o outro dev. **O anão atual continua o padrão**; nada fica salvo.
- Mesma altura e pé no mesmo lugar que o anão atual (o desenho de 64 px enche 61 px do quadro; o de 256, 174). O estagiário continua com as folhas de 256.
- **Como testar:** DBG → Câmera → **Anão 64px** liga e desliga.

### E8 — Biomas
- `BiomeDefinition` (D-72) com as 10 cavernas (nomes e cores propostos para você aprovar); a caverna 1 é a mina. DBG → **Bioma** troca na hora para ver cada uma.
- **Como testar:** DBG → Bioma, passar pelas 10.

### E9 — Arte própria, começando pelo chão (D-90) — plano aprovado em 2026-10-29; implementada, aguardando validação no Moto G75

**Por quê:** o calçamento de hoje vem do *Pixel Fantasy Caves* (Szadi art.), que fica fora do git (D-65) e não pode ser redistribuído. Com arte própria, o chão entra no repositório: quem clonar vê o jogo como ele é, e os builds do M15 deixam de depender do pack nessa parte. O chão é o teste do caminho; o resto da troca vem depois (lista no fim).

**O que o Bruno mandou** (`Assets/_Project/Art/Tile/`, desenhados à mão): desenho de 16 px exportado em PNG de 32 px (cada pixel do desenho = 2×2) e o SVG de origem.

| Arquivo | O que é |
|---|---|
| `tileset_ground_01` | O desenho em 4 tons de areia, o mais claro |
| `tileset_ground_02`, `_03`, `_04` | **O mesmo desenho**, cada um um tom mais escuro (o `04` vai ao roxo quase preto) |
| `tileset_ground_01_02` | Transição: as 7 linhas de cima são o `01`, as 9 de baixo, o `02` (emenda com o `01` em cima e o `02` embaixo) |

Os quatro tons emendam sem costura com eles mesmos. Na prévia ao lado da parede de rocha, o `01` é **bem mais claro** que tudo na caverna e que o chão do mockup; o `02`/`03` ficam mais perto do mockup.

**Decisões (D-90):** o `01` vira o chão padrão da caverna 1; o PNG de 32 px a **64 px/m** (o mesmo `texturePixelsPerMeter` de hoje: tile de 0,5 m, pedras do tamanho das do calçamento, pixel do desenho 2× maior que o da parede); a cor final sai do teste no Moto G75 pelo DBG; o trabalho é esta etapa do M1b, não um marco novo.

**Passos:**
1. **Organizar:** os PNG ficam em `Art/Tile/`; os SVG vão para `ArtSource/tiles/` (arquivo-fonte fora de `Assets/`).
2. **Setup** (`CorridorArtSetup`): importa os tiles como pixel art (Point, sem compressão, sem mipmap, Repeat) e põe o `01` no `floorTexture` do `DescentStage` **no lugar do calçamento**. Hoje o setup só preenche campo vazio; a regra nova é "se o campo tem o calçamento do pack, troca pelo `01`" (algo posto à mão continua intocado). O calçamento continua sendo gerado só até o fim desta etapa, para a comparação no DBG.
3. **DBG → Câmera → Chão:** um botão alterna `01 → 02 → 03 → 04 → calçamento antigo → 01`, na hora, com o nome do tom no botão. Só visual: não salva nada (segue o padrão do "Anão 64px"). Sem o pack no PC, o "antigo" é pulado.
4. **CREDITS.md:** "Chão da caverna 1 (tiles `ground_01`…`04`), Bruno Carvalho, à mão" na Arte do jogo; o calçamento do Pixel Fantasy passa a "comparação no DBG; sai na E9".
5. **Depois da escolha no G75:** o tom escolhido vira o padrão (se não for o `01`, registro na D-90); sai a geração do calçamento, o botão do DBG fica para os próximos testes de arte.

**Riscos a olhar no aparelho:**
- **Repetição:** o tile repete a cada 0,5 m e não dá para girar nem espelhar (quebra a emenda), diferente do calçamento, que é montado sorteado. Nas prévias da E4, tile pequeno repetido pareceu "pano de mesa". Se aparecer: 2 ou 3 variações do `01` com **as mesmas bordas** e o miolo diferente, que o setup sorteia numa textura grande (como o calçamento faz hoje).
- **Cristal branco sumindo:** o `crystal_glass` é branco; num chão claro pode perder contraste.
- **Pixel 2×:** o chão fica mais "grosso" que a parede e o anão. Se incomodar, redesenho em 32 px de verdade.

**Testes:** nada muda no núcleo; rodar `bash tools/ci/run-tests.sh` para garantir que EditMode e PlayMode seguem verdes, e conferir as linhas `[Setup]` (sem `error CS`).

**Como testar:**
1. Unity: `Tools → Miners Idle → Projeto → Aplicar configuração do projeto`; abrir a cena `Game` e dar Play: o chão é o `01`.
2. DBG → Câmera → **Chão**: passar pelos 5 e voltar ao `01`.
3. Moto G75: `Tools → Miners Idle → Build → Rodar no celular (Android)`; repetir o passo 2 com o brilho de sempre e olhar repetição, costura, contraste com os cristais e o pixel 2×.
4. Escolher o tom.

**Critério de pronto:** o Bruno escolheu o tom no Moto G75, o chão é arte própria versionada no git, o calçamento não é mais gerado nem usado, e o CREDITS.md está em dia.

**Esforço:** 3–5 h.

**Próximas trocas (cada uma uma etapa, em ordem de quanto aparece na tela):** parede e teto (rocha escura, Pixel Fantasy) · esteios (portal, Pixel Fantasy) · pedras da beira (`rock_t1`, MineAssets) · interface (Pixel UI Kit, Fantasy UI Kit Lite, ícones Anvil). A fonte Pixelify Sans (OFL) pode ficar.

## O que fica de fora
- **Troca de caverna jogando** (Letrado, PASSAR DE CAVERNA): M8.
- **Novas regras:** nada muda em valores de regra; o espaço entre cristais (2 un) continua.
- **Animações extras** (comemorar, sofrer penalidade): depois.
- **Seletor de cor do anão** (usar as variações guardadas na E7): ideia do Bruno para depois, ainda sem marco.

## Esforço
25–40 h (E6, o anão, é a maior: 10–16 h).
