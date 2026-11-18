# Marco 9f — Cristal rachando

> Plano de 2026-11-18, **aguardando as respostas do Bruno (P1…P7)**. É o **Bloco B** da [Análise do GDD v8](Analise%20-%20GDD%20v8.md) §5, depois do [Marco 9e](Marco%209e%20-%20Raro%20e%20anuncios%20do%20v8.md) (aprovado em 2026-11-18, D-297). Segue o processo do v8 (D-298): as melhorias vistas nos testes vão para a [lista de ajustes](Ajustes%20do%20v8%20-%20lista.md). Base: GDD v8.2, "vida exibida: sem barra, o cristal racha conforme perde vida" (Análise §2).

## Objetivo

**A barra de vida sai, e o próprio cristal mostra quanto falta.** Cada cristal tem quatro desenhos: inteiro e três rachaduras. Conforme perde vida, passa de um para o outro, com um brilho e umas lascas a cada rachadura nova. Na quebra, os estilhaços têm a cor do desenho.

**Para você:** descer alguns andares e ver cada cristal rachar e quebrar sem a barra verde; num cristal que aguenta vários golpes, ver as três rachaduras uma depois da outra; ver as cinco cores trocando com o bioma.

## A arte (o que o Bruno mandou em 2026-11-18)

- Uma folha de **4 colunas × 5 linhas**: as colunas são os estágios (inteiro, racha 1, racha 2, racha 3) e as linhas são as cores (azul, amarelo, verde, anil, vermelho).
- Cada quadro tem **32×32 pixels de verdade**. A imagem enviada está ampliada 2× (256×320, cada pixel virou um bloco 2×2 exato), com fundo transparente e alfa só 0 ou 255. É pixel art limpa, pronta para o jogo.
- Hoje o cristal da fila é **um desenho por bioma** (`BiomeDefinition.crystalTexture`, D-116), com três desenhos do Bruno revezando nos 10 biomas (vidro, safira e amarelo, D-77), e o raro é o `crystal_glass_rare` piscando (D-220).

### Como exportar (resposta ao Bruno)

**[REC]** **A mesma folha, em tamanho real (100%):**

| Item | Valor |
|---|---|
| Arquivo | **um PNG só**, `crystals_v8.png`, **128×160** (4 × 32 por 5 × 32) |
| Organização | **colunas = estágios** (esquerda → direita: inteiro, racha 1, racha 2, racha 3); **linhas = cores** (de cima para baixo) |
| Escala | **1×**: um pixel do desenho é um pixel do arquivo, sem ampliar |
| Fundo | transparente (RGBA), sem espaço nem borda entre os quadros, sem "trim" |
| No Aseprite | *File → Export Sprite Sheet*, layout por linhas/colunas (ou a tela como está), *Border*, *Spacing* e *Inner padding* em 0, *Trim* desligado, escala 100% |
| Onde | `Assets/_Project/Art/Crystals/crystals_v8.png` (ou me manda que eu ponho). O `.aseprite` vai em `ArtSource/crystals/` (fora do git, como o resto da arte-fonte) |

**Por que assim:**

- **Uma folha só, sem cortar em 20 arquivos.** O código da descida já desenha "o quadro *n* de uma folha" (`DescentPiece.SetFrame`, o mesmo do anão): só muda o recorte da textura no material, sem criar textura nem material por quadro. Trocar de estágio custa quase nada, e todos os cristais usam a mesma textura.
- **Cor nova é linha nova.** Uma 6ª cor (ou o dourado do raro, P4) entra embaixo, e a folha vira 128×192. Nada mais muda: o bioma aponta para a linha.
- **1× em vez de 2×.** O cristal ocupa uns 20–30 pixels na tela de 270 de largura. Com 32 pixels na textura, ele fica perto de 1 texel por pixel e cintila menos ao se afastar. Com 64, metade dos texels some de um quadro para o outro. As cores e o desenho são os mesmos.
- **A importação fica por conta do setup** (Tools → Miners Idle → Projeto → Aplicar configuração do projeto), como a dos outros cristais: filtro *Point*, sem compressão, sem *mipmaps*, *Clamp*.

A imagem que você mandou já é uma ampliação 2× exata, então eu consigo tirar dela a versão 1× sem perder nada. Mesmo assim, o melhor é exportar do arquivo original, que é a fonte.

## O que já existe (e o que muda)

| Peça | Hoje | No 9f (v8) |
|---|---|---|
| **Vida do alvo** | barra verde sobre o cristal (REQ-CRY-03), encolhendo | **sem barra**: o estágio do desenho mostra a vida |
| **Desenho** | um por bioma, três revezando | a **folha v8**, uma linha por bioma (P1) |
| **Rachar** | — | inteiro → racha 1 → racha 2 → racha 3, pela vida (P2) |
| **A cada rachadura** | — | brilho de um instante e lascas pequenas na cor do cristal (P5) |
| **Quebra** | estilhaços na cor do bioma | igual, com a cor tirada do desenho |
| **Raro** | `crystal_glass_rare` piscando | o mesmo cristal, rachando, em dourado (P4) |
| **Núcleo** | `Crystal.Health` / `MaxHealth` | **não muda**: o estágio é só desenho, calculado a partir da vida. A regra continua no código; o desenho só ilustra |

## Perguntas para o Bruno

**P1 — Qual cor em qual bioma.** São 5 cores para 10 biomas.
**[REC]** Cada cor em **dois biomas**, escolhida pelo **contraste com o fundo**: cristal frio em caverna quente e cristal quente em caverna fria.

| Andares | Bioma | Fundo | Cristal |
|---|---|---|---|
| 1–10 | Entrada da mina | marrom quente | **azul** |
| 11–20 | Galeria rochosa | marrom avermelhado | **verde** |
| 21–30 | Veio profundo | vinho escuro | **anil** |
| 31–40 | Zona metálica | roxo escuro | **amarelo** |
| 41–50 | Câmara subterrânea | verde | **vermelho** |
| 51–60 | Fenda cristalina | roxo | **amarelo** |
| 61–70 | Profundidade térmica | vermelho | **azul** |
| 71–80 | Abismo mineral | violeta | **verde** |
| 81–90 | Núcleo profundo | azul | **vermelho** |
| 91–100 | Coração do vulcão | laranja | **anil** |

A E0 mostra os 10 lado a lado. Trocar depois é mudar um número no bioma (`crystalRow`).

**P2 — Quando cada rachadura aparece.** Um cristal que morre em um golpe nunca racha. Os que aguentam mais precisam mostrar o primeiro golpe.
**[REC]** **Inteiro só com a vida cheia.** Depois, por terços: **racha 1** acima de 2/3 da vida, **racha 2** acima de 1/3, **racha 3** no último terço. Assim todo golpe que não quebra já mostra uma rachadura. Com quartos iguais, um golpe de 10% passaria sem mudar nada. O golpe do estagiário e o toque também racham, porque é a mesma vida.

**P3 — A barra de vida.** O GDD tira a barra.
**[REC]** **Sai**, e fica um botão no DBG → Caverna, **"Barra de vida: não / sim"**, para comparar durante os testes. Ele some no marco de ajustes, se você não quiser manter.

**P4 — O raro.** A folha não tem a cor dourada do raro.
**[REC]** **Agora:** o setup gera uma linha dourada a partir da linha azul, recolorida numa rampa de um matiz só, como os cristais da D-70. Ela vai num arquivo à parte (`crystal_v8_rare.png`, para não mexer no seu), com o mesmo brilho piscando de hoje, e racha igual aos outros. **Depois:** se você desenhar a linha dourada (a 6ª, embaixo), ela substitui a gerada sem mudar código.

**P5 — O efeito a cada rachadura.**
**[REC]** No instante em que o estágio muda:
- um **brilho branco** de ~0,08 s no cristal (o mesmo truque do brilho do raro: cor acima de 1);
- **3 a 4 lascas** pequenas na cor do cristal, saltando do ponto do golpe (as mesmas partículas da quebra, em menor número).

O tremor e a parada do golpe continuam como hoje. **Sem som novo:** o som do golpe já toca. Um "crac" próprio vai para a lista de ajustes, se você quiser.

**P6 — Os cristais antigos.** São o `icon_crystal` em 5 cores (D-70) e o vidro, a safira e o amarelo (D-77).
**[REC]** A folha v8 vale em **todos os biomas**, no v8 e no v7 (o bioma é só desenho). Os arquivos antigos ficam no projeto, sem uso, até o marco de ajustes decidir se saem. O `crystalTexture` do bioma fica como reserva: um bioma sem linha na folha usa ele.

**P7 — A sessão longa deste marco.**
**[REC]** Como na D-282 e na D-291: vai para os **testes longos do fim**, e o marco fecha com a E2.

## Etapas

### E0 — A folha no jogo e a prévia (P1, P4)

- `crystals_v8.png` importado pelo setup (Point, sem compressão, sem mipmaps, Clamp).
- `BiomeDefinition` ganha `crystalSheet` e `crystalRow`. O setup preenche os 10 biomas pela tabela da P1, migrando os biomas que ainda estão com o desenho antigo.
- A linha dourada do raro gerada (P4).
- **Prévia** (`tools/ci/out/m9f-e0-biomas.png`): os 10 biomas com o cristal nos 4 estágios, no corredor de verdade, para você aprovar as cores e o tamanho. Hoje o cristal é 0,84 × 0,80 m e a arte é quadrada; a prévia mostra os dois tamanhos.

### E1 — Rachar na tela (P2, P3, P6)

- `DescentStage` desenha cada cristal com `SetFrame(folha, linha × 4 + estágio, 4)`, com o estágio pela vida (P2).
- A conta do estágio fica numa função pura (`CrackStage.Of(vida, vidaMáxima)`), testada em EditMode: vida cheia → 0; 99% → 1; 2/3 → 2; 1/3 → 3; um cristal de 1 golpe nunca passa de 0.
- A barra de vida sai, e o DBG → Caverna ganha **"Barra de vida"** (P3).
- O raro em dourado, rachando e piscando.
- **DBG → Caverna → "Estágios"**: a fila mostra os 4 estágios em sequência (o cristal *i* no estágio *i* mod 4), para conferir a arte parada.
- **Testes PlayMode:** um golpe que não quebra muda o quadro do alvo; a barra não aparece e volta com o DBG; o raro usa a linha dourada; a troca de bioma troca a linha.

### E2 — O efeito (P5)

- Brilho e lascas a cada rachadura nova; nada na quebra além do que já existe.
- Os estilhaços da quebra com a cor média da linha do cristal, calculada pelo setup e guardada no bioma (`crystalShardColor`, que já existe).
- Sem alocação no `Update`: as lascas usam o mesmo pool da quebra.
- **Testes PlayMode:** a rachadura nova dispara o efeito uma vez só; o mesmo estágio não dispara de novo; a contagem de alocação continua zero.

### E3 — Sessão longa e celular

- Vai para os **testes longos do fim** (P7).

## Como testar (Bruno) — ao fim de cada etapa

- **E0:** abrir `tools/ci/out/m9f-e0-biomas.png` e dizer se as cores e o tamanho estão bons.
- **E1:** cena `Game`, Play. DBG → Andar → **Ir ao andar 20** (os cristais aguentam vários golpes): ver o alvo rachar a cada golpe e sumir a barra. DBG → Caverna → **Barra de vida** para comparar; **Estágios** para ver os 4 desenhos parados. DBG → Andar → ir ao 50 e ao 100 para ver outras cores. DBG → Bônus → **Raro agora** no andar 10 para ver o dourado rachar.
- **E2:** a mesma descida, olhando o brilho e as lascas a cada rachadura, e os estilhaços da quebra na cor do cristal.

## Critério de pronto

- Nenhuma barra de vida no jogo (só no DBG), e todo cristal que leva um golpe sem quebrar mostra uma rachadura.
- As 5 cores nos 10 biomas, pela tabela aprovada; o raro dourado racha igual.
- Brilho e lascas em cada rachadura nova; estilhaços da quebra na cor do desenho.
- Testes EditMode e PlayMode das etapas passando; sessão longa nos testes longos do fim.

## Riscos

- **Cristal pequeno na tela.** A 20–30 pixels, a diferença entre racha 1 e racha 2 pode não aparecer. A prévia da E0 mostra isso; se for o caso, o ajuste é na arte (rachaduras mais grossas) ou no tamanho do cristal, e entra na lista de ajustes.
- **Cristal que morre em um golpe** (o começo de cada run, com a Picareta forte) nunca racha: é o esperado (P2).
- **A câmera de 60° (Bloco G)** muda o tamanho do cristal na tela; o tamanho final se acerta lá.

## Fora deste marco

- A Bomba e a saída do toque e do combo (Blocos E e F), a câmera de 60° (Bloco G).
- Um som de rachadura próprio (P5), se o Bruno quiser: lista de ajustes.

## Esforço

| Etapa | Horas |
|---|---|
| E0 | 2–3 |
| E1 | 3–5 |
| E2 | 3–4 |

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| Plano | 📝 escrito em 2026-11-18, aguardando P1…P7 | — |
| E0 A folha no jogo e a prévia | ⏳ | espera a folha 1× (ou uso a enviada, reduzida) |
| E1 Rachar na tela | ⏳ | — |
| E2 O efeito | ⏳ | — |
| E3 Sessão longa e celular | ⏭️ testes longos do fim (P7) | — |
