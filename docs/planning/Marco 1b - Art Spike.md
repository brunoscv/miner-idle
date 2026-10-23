# Marco 1b — Art Spike

> Plano de 2026-10-23. O Bruno pausou a E5 do M6 e quer dar **cara de jogo** ao jogo antes (D-62). O Art Spike do [Roadmap](Roadmap.md) (M1b) é esse trabalho: decidir o estilo de UI, a fonte, o anão e os cristais. Ele vem em duas partes:
>
> - **Parte 1 — Interface** (este plano): HUD, botão LOJA e loja com os packs de UI que o Bruno escolheu, e uma fonte pixel com acentos.
> - **Parte 2 — Anão, cristais e biomas**: plano escrito quando a parte 1 for aprovada.
>
> Decisões deste plano: **D-62…D-68** em [Decisões e Riscos](Decisoes%20e%20Riscos.md) §1.3. **Aguardando aprovação do Bruno.**

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
| Botão AJUSTES (abaixo do AD) | engrenagem | **Bruno vai fazer** junto com a loja e o raio (`icon_settings.png`), mesmo formato. Até lá, a engrenagem do kit de madeira ampliada 2× (pixel mais grosso que os outros ícones). |
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

### E2 — HUD, LOJA e AD

- Painéis do HUD em madeira (nome em cima, ícone e valor embaixo), ocupando toda a largura; LASCAS inteiro (D-68); na coluna da direita, de cima para baixo e do mesmo tamanho: LOJA (estado pressionado e selo "!"), AD desligado e AJUSTES.
- **Como testar:** rodar no celular; ver os 4 painéis com ícone; tocar LOJA (afunda e abre a loja); comprar algo para o "!" aparecer/sumir; tocar AD e a engrenagem ("Em breve"). Tocar nesses botões não conta como toque no cristal. O tremor do HUD no combo ×10 e o pulso de LASCAS continuam.
- **Pronto quando:** você aprova o HUD no aparelho.

### E3 — Loja

- Janela, linhas, molduras coloridas com os ícones, botão rubi/cinza, VOLTAR.
- **Como testar:** abrir a loja; uma melhoria que dá para comprar fica rubi, uma sem lascas fica cinza; comprar (botão afunda, preço sobe, linha atualiza); Picareta Dupla vira "comprada"; as duas bloqueadas aparecem escurecidas; VOLTAR fecha. Usar o DBG para ganhar lascas e testar rápido.
- **Pronto quando:** você aprova a loja no aparelho.

## Testes

- Os testes atuais continuam verdes (EditMode e PlayMode); os da loja e do HUD já cobrem compra, selo, pulso e tremor.
- Novo teste de Editor: todo sprite em `_Vendor/AssetStore/PixelUIKit` e `FantasyUIKitLite` está com filtro Point e sem compressão (fica *ignorado* se os packs não existirem, como na CI).
- Novo PlayMode: os botões da UI têm pelo menos 30 px de altura na referência.
- Conferência visual: um teste temporário que salva a tela em PNG (`tools/ci/out`), depois apagado, como no M6.
- Moto G75 ao fim de cada etapa.

## Critério de pronto da parte 1

- [ ] Packs fora do git, importados por código; `CREDITS.md` criado.
- [ ] Textos do jogo na Pixelify Sans, com acentos, nítidos no Moto G75.
- [ ] HUD com ícones, LOJA, AD e loja com a pele nova; estados (pressionado, sem lascas, comprada, máx., bloqueada, em breve) visíveis.
- [ ] Ícones da LOJA e do AD do Bruno encaixados (se chegarem durante o marco).
- [ ] Clone sem os packs ainda abre e roda (graybox).
- [ ] Testes verdes.
- [ ] Você aprova no aparelho.

## Esforço

10–16 h.
