# Direção de Arte e Produção de Assets

> **2026-10-21 — D-05 decidida pelo Bruno: o estilo é o do mockup** (fantasia, mina de madeira), exatamente como no protótipo (`docs/Designer (2).png`). As menções abaixo a "industrial" ficam como histórico da discussão.
>
> Proposta de 2026-10-20. Referência visual única e prioritária: o **mockup embutido no GDD** (ver [Descoberta](../planning/Descoberta%20-%20Relatorio.md) §3), com a
> ressalva de que ele é **conceito de IA**, não sprite. Onde o texto do GDD (industrial/sci-fi) e o mockup (fantasia/madeira) divergem, esta proposta segue o
> mockup (**D-05, a aprovar**) e preserva do texto o que é funcional (âmbar = ação/habilitado, vermelho = alerta/penalidade, cristais saturados sobre rocha escura).
> Requisitos: REQ-ART-xx, REQ-AUD-xx em [Requisitos](Requisitos%20e%20Restricoes.md).

## 1. Identidade visual

| Tema | Direção [REC] |
|---|---|
| **Atmosfera** | Mina escura e quente: rochas em carvão/marrom, **lanternas** (laranja quente) como luz de ambiente, **cristais emissivos** como fonte de cor fria/saturada. Contraste quente × frio é a assinatura do mockup. |
| **Hierarquia de cor** | 1) cristal da caverna (mais saturado e brilhante da cena); 2) anão (capacete amarelo/barba ruiva — silhueta legível sobre o chão); 3) UI âmbar (ação); 4) cenário em baixa saturação. **Regra:** só **uma cor saturada de cristal por caverna** domina; as demais aparecem apenas nas paredes, esmaecidas. |
| **Cavernas (REQ-ART-06)** | Mesma "estrutura" de cenário em 10 paletas (piso, parede, luz), com mais **brilho e partículas** a cada nível: cave 1 clara e sem lava; cave 10 com magma e brasas. Tabela de cor/piso/sensação no GDD §6 vale. |
| **Cristais — "raridade" por cor e forma** | Cor fixa por caverna (GDD §6). **Forma escala com o nível:** cave 1–3 aglomerado de 3 facetas; 4–6 de 4–5 facetas; 7–10 de 5–7 facetas, com brilho interno pulsante nos 3 últimos. Nada de cristais de cores variadas no mesmo lote (corrige C-05 do mockup). |
| **Contorno e sombreamento** | Contorno **seletivo**: 1 px escuro (tom da paleta, nunca preto puro) externo; **sem** contorno interno nos cristais (as facetas definem). Sombreamento em **4 tons por rampa** (sombra, base, luz, brilho), luz **vinda de cima à esquerda**; cristais têm 1 faceta clara. Dithering: só em fundos, nunca em personagem/cristal. |
| **Escala e proporção** | Anão ≈ 2 cristais de altura (leitura de "grande" × "pequeno"). Cristal ocupa ≈ 70–80% da largura da faixa; 5 cristais + minerador cabem na altura de ≈ 50% da tela (como no mockup). |
| **Densidade de pixels** | Um único tamanho de "pixel" em **todo** o jogo (mundo **e** UI). Proibido escalar um sprite por fração, rodar sprite por ângulo livre ou usar *blur*. |
| **UI × mundo** | Mesma paleta/rampas; molduras da UI com a linguagem de madeira/metal escuro do mockup, chanfro de 1 px, cantos "robustos"; botões habilitados em âmbar `#F2A900`, desabilitados em cinza-carvão, alerta `#D84235`. |
| **VFX em pixel art** | Faíscas de 1–3 px, *flipbooks* curtos de 4–6 quadros, partículas com formas quadradas/retângulos pequenos; tremor de tela por **código** (deslocamento inteiro de pixels); brilho por **sprite aditivo** (não por *bloom* de pós-processamento) — mantém a grade. |
| **Luz** | Luzes 2D do URP apenas para lanternas e brilho de cristal (**opcional**; custo de performance em celular precisa de teste — plano B: sprites aditivos). |

**Áudio [REC]:** SFX secos e graves (rocha/metal/vidro), "timbre sobe" por caverna (variações de pitch); coleta com pitch crescente em sequência; vibração do aparelho curta no crítico (opção). Música é pós-MVP.
Fontes: [Freesound CC0, Kenney, OpenGameArt, jsfxr] — as pastas `E:\Fabrica\Unity\assets\Audio` e `UI` **já existem** no PC (verificar licenças antes de usar — [DOC]).

## 2. Resolução e escala — recomendação com trade-offs

**Decisão a validar na *Art Spike* (marco M1b), com base na análise abaixo.**

O mockup (605×1008, proporção ≈ 0,60) mostra detalhe de barba, olhos e facetas. Telas reais de Android têm proporção ≈ 9:19,5–9:21 (0,41–0,46), então o cenário precisa **estender-se na vertical**.

| Opção (largura×altura de referência) | Escala p/ 1080 px de largura | Prós | Contras | Leitura [INT] |
|---|---|---|---|---|
| 180 × 320 | ×6 | Muito barato de desenhar, "pixel" bem visível | Anão ≈ 24 px: sem rosto/barba; texto da UI sofre; lembra o mockup só de longe | Pouco detalhe para o mockup |
| **270 × 480** | **×4** | **Anão ≈ 32 px, cristal ≈ 40 px**; texto da UI legível com fonte pixel de 8–10 px; custo moderado | Precisa de redesenho fiel do mockup; letras acentuadas pequenas | **Recomendado (provisório)** |
| 360 × 640 | ×3 | Mais próximo do mockup; mais espaço para efeitos | ~+78% de pixels por sprite que 270×480 (arte e animação mais caras); 10 cristais × 4 estados é bastante trabalho | Plano B se a 270 parecer pobre |
| 540 × 960 | ×2 | Quase "HD", fácil usar a IA direto | Deixa de ser pixel art "de verdade"; perde a identidade | Não |

**Estratégia de escala [REC]:**
- Câmera ortográfica com **Pixel Perfect Camera** (componente do URP 2D; **verificar o componente exato no URP 17.6**) com resolução de referência **largura fixa 270 px**; altura variável por aparelho (arte do cenário com **margem vertical estendida** — a "tela segura" 270×480 contém HUD, mineração e botão; o resto é cenário repetível).
- **Pixels Per Unit (PPU) = 16** para **todo** sprite e para a UI em pixel (altura de referência 480 px = 30 unidades; cristal de 40 px ≈ 2,5 unidades). PPU é só convenção: o importante é **um valor único e documentado**.
- Escala inteira sempre (×3 ou ×4); em aparelhos com largura que não é múltiplo, aceita-se barra fina ou *crop* de ≤ 1 px por borda.

## 3. Especificação técnica dos sprites

| Item | Regra [REC] |
|---|---|
| **Import** | Texture Type: Sprite (2D and UI); **Filter Mode: Point (no filter)**; **Compression: None** (ou *High Quality* só após medir); **Generate Mip Maps: off**; Read/Write: off; **Wrap: Clamp**; PPU 16; **Pivot** definido por tipo (cristal e personagem: *Bottom Center* ou ponto custom no pé; ícone: *Center*) |
| **Slicing** | *Sprite Mode: Multiple*, *Grid By Cell Size* para *spritesheets* com células fixas (32×32, 32×48, 16×16); **células múltiplas de 8 px** quando possível; 1 px de margem transparente evita sangramento |
| **Atlas** | Sprite Atlas (v2) por grupo (UI, personagens, cristais+VFX, fundos de caverna); *Allow Rotation* off, *Tight Packing* off; Point filter |
| **Cores** | Cada sprite só usa **cores da paleta-mestre** (≈ 32–48 cores: âncoras do GDD + rampas de 4 tons por cristal + neutros). Script de auditoria `tools/art/` conferirá tamanho, paleta e transparência (pós-aprovação) |
| **Naming** | `categoria_assunto_variante_estado[_NN]`, minúsculo, ASCII: `crystal_glass_idle`, `crystal_glass_break_03`, `miner_main_swing_02`, `ui_btn_amber_pressed`, `bg_cave05_floor`, `vfx_spark_hit_01`, `icon_shard`, `sfx_hit_01.wav` |
| **Pastas** | `Assets/_Project/Art/{Characters,Crystals,Environment/CaveXX,UI,VFX}`; fontes brutas em `ArtSource/` **fora** de `Assets/` (`.aseprite`, PSDs, referências de IA com *prompt*); terceiros em `Assets/_Vendor/` intocados |
| **Animação** | Aseprite → exportar `.aseprite` e importar com o **importador nativo do Unity 2D (pacote `com.unity.2d.aseprite`; verificar versão no 6000.6)** ou *spritesheet* + JSON; **12 fps** (golpe, quebra, VFX), loops de ocioso a 6–8 fps; um clipe por estado; **a duração do golpe ≤ 0,35 s** (precisa caber em 0,5 s, o intervalo mínimo); **o código decide o tempo** (lição herdada): o golpe dispara a animação, o quadro de impacto é marcado como *Animation Event* só para efeito (faísca/som), **nunca** para decidir dano |
| **Estados dos cristais** | Barra de vida é a leitura oficial (GDD). Estados de dano opcionais: **3 *overlays* de rachadura genéricos** (66%, 33%, 10%) tingidos por cor — **P1**; no MVP bastam brilho/vibração/*flash* de acerto |
| **Picareta e níveis** | 20 níveis de "brilho" → **5 faixas visuais** (níveis 1–4, 5–8, 9–12, 13–16, 17–20) + intensidade de brilho por parâmetro de material; REQ-ART-04 respeitado; o Roadmap já aceita reduzir isso (RM §7.1) |

## 4. Inventário de assets do MVP

Contagens **provisórias [INT]**; a coluna "Marco" refere-se ao [Roadmap](../planning/Roadmap.md).

| Categoria | Uso | Escopo aproximado | Animação | Dependências | Marco / placeholder |
|---|---|---|---|---|---|
| **Cristais** (10) | Alvo principal | 10 *idle* (2 quadros de brilho p/ 7–10) + 10 sequências de quebra de 4–6 quadros (ou 1 genérica tingida + 10 *crumble* de 3 quadros) | idle sutil, quebra | Paleta, resolução | M3 placeholder (retângulos coloridos); **cave 1 final em M5**; caves 2–10 em M9 |
| **Lasca** (coletável/ícone) | Ícone do HUD + sprite que voa | 10 cores (1 por caverna) 8×8 ou 12×12 | rotação 4 quadros opcional | Cristais | M5 |
| **Minerador principal** | Protagonista | Idle, golpe (6–8 quadros), andar/descer (4–6), comemorar (opcional); **2ª picareta como camada** | sim | Resolução, 5 faixas de picareta | M3 placeholder; M5 final |
| **Estagiário** | Segundo minerador | Mesmos clipes, *recolor*/capacete/escala | sim (reuso) | Principal | M6 (reuso do principal em M3/M5) |
| **Picareta** | Mão do anão | 5 faixas visuais + brilho por material | sim | Principal | M5 |
| **Cenário** (10) | Fundo e piso | 1 estrutura-base + 10 paletas de piso/parede + **4–6 props únicos por caverna** (lanterna, viga, cristais de parede, lava) + sprite da **"parede"/passagem** (C-03) | parallax leve, brilho/oscilação | Paleta | M3 cave 1 provisória; M5 cave 1 final; M9 resto |
| **UI** | HUD, loja, modais | 4 painéis, botão âmbar (4 estados), botão loja/AD/engrenagem, linha de item de loja (reutilizável), modal, barra de vida (2 sprites), selos (notificação, penalidade, boost), ~14 ícones | transições de entrada | Fonte pixel com acentos | M3 caixas simples; M4 funcional; M9 *skin* |
| **VFX** | Feedback | Faíscas de acerto, explosão de crítico, ×10 do combo, rastro da lasca, raio do boost, seta da penalidade, poeira da quebra | flipbooks 4–6 quadros ou partículas | Paleta, Eventos | M5 (acerto, crítico, quebra, coleta); M9 resto |
| **Texto** | "CRÍTICO", `xN`, números | Fonte bitmap/pixel com ã ç é í õ ü; 2 tamanhos | pulsar por código | Licença da fonte | M3 |
| **Áudio** | SFX | ~12–16: golpe (3 var.), crítico, quebra, coleta, toque, combo, clique, compra, negado, passagem, boost início/fim, ambiente (opc.) | — | Pitch por caverna | M3 *beep* provisório; M5 final |

**Total aproximado de sprites originais:** ~150–220 quadros de personagem/VFX + ~40 de UI + ~10–20 peças por caverna reaproveitando estrutura — **plano de produção realista:** gerar **cave 1 completa** primeiro como "assinatura" e *estender* por *recolor* de rampas.

## 5. Ferramentas e pipeline

| Etapa | Ferramenta (recomendada) | Observação |
|---|---|---|
| Referência/conceito | **IA de imagem** (já usada no mockup) | **Não é arte de produção.** Serve para atmosfera/composição. Registrar *prompt*, ferramenta, data e licença/termos |
| Redução e paleta | Script (Python/Aseprite) ou Aseprite *Color Mode → Indexed* | Reduz para a grade 270×480 com *nearest-neighbour*, quantiza para a paleta-mestre |
| **IA de pixel art (candidata): PixelLab** | Serviço de IA focado em pixel art: personagens com 4/8 direções, animação por modelo (ex.: andar) ou por texto, geração com estilo consistente e *inpainting*; tem **MCP** (`https://api.pixellab.ai/mcp`, token de API no cabeçalho `Authorization: Bearer …`) | Ver §5-A abaixo: créditos grátis, limites de tamanho e **uso comercial não confirmados** |
| **Produção de sprites** | **Aseprite** (pago, ~US$ 20 [verificar]; também compilável gratuitamente) ou **LibreSprite** (gratuito, fork antigo do Aseprite) ou **Pixelorama** (gratuito, ativo) | **Nenhum deles está instalado hoje.** Krita serve para concept/paleta, mas é pior para animação em pixel |
| Animação | Aseprite (timeline + *tags*) | Importador nativo `.aseprite` do Unity reduz atrito |
| Auditoria | `tools/art/validate_sprites.py` (futuro) | Confere tamanho, paleta, PPU, transparência; roda antes do commit de arte |
| Áudio | Audacity + jsfxr/BFXR | Gratuitos |
| Licenças | `CREDITS.md` + tabela em `docs/planning/` | Padrão do Underworld Towers; exigência RM §5.1 |

### 5-A. PixelLab — pesquisa de 2026-10-21 (decisão do Bruno: testar para o anão)

**O que a pesquisa confirmou** (páginas do próprio PixelLab; ver fontes no fim): existe botão "Try for free" **sem cartão**; há **MCP** oficial e API; ferramentas `create_character` (4 ou 8 direções, tamanho, corpo humanoide/quadrúpede, modos standard/pro/v3), `create_character_state` (variantes de roupa/pose), `animate_character` (modelo como `walking` **ou descrição livre**), `get_character`. Existe também um servidor MCP **comunitário** (`pixellab-mcp` no npm) com `get_balance` para ver créditos.

**O que eu NÃO consegui confirmar** (a página de preços não carregou no meu acesso): **quantos créditos o plano gratuito dá**, se o **MCP/API está incluído no gratuito**, **limites de tamanho do sprite** (precisamos de ~32×48), e os **termos de uso comercial** das imagens geradas — **crítico**, porque o jogo será vendido (D-18). Uma fonte de terceiros sugere cobrança por "gerações" de assinatura, não créditos em dólar; não é documentação oficial.

**Como testar sem risco (T-M1B-06):**
1. Criar conta gratuita em pixellab.ai e **ler os Termos de Serviço (uso comercial e propriedade das imagens)**; registrar em `CREDITS.md`/tabela de fontes (REQ-ART-07).
2. Gerar o token em `api.pixellab.ai/mcp` e conectá-lo ao Claude Code **como MCP** (é um segredo: **nunca** commitar nem colar em arquivos do repositório; usar variável de ambiente ou configuração de usuário). *Eu só configuro o MCP quando você autorizar e fornecer o token — não vou pedir que você cole o token no chat.*
3. Gerar **1 anão** a partir da descrição do mockup (barba ruiva, capacete amarelo com lanterna, couro, picareta; **de frente**) no menor tamanho útil; ver se **segura o estilo** entre vários quadros.
4. Gerar **um ciclo de golpe** (6–8 quadros) e um ocioso; avaliar consistência quadro a quadro (o risco que o RM §5.2 já apontava).
5. **Retoque manual** num editor de pixel (D-10) para alinhar a paleta-mestre e o pivô; importar no Unity com Point filter.
6. **Critério de aprovação:** consistência em ≥ 6 quadros sem retoque pesado, licença comercial confirmada, custo de créditos compatível com ~10 cristais + 2 mineradores. **Plano B:** desenhar à mão sobre a saída do PixelLab, ou Aseprite do zero.

> Observação [INT]: o PixelLab gera personagens para 4/8 direções, mas **o anão do jogo é só frontal** (GDD §8.3) — gaste créditos apenas na direção que usamos, se a ferramenta permitir.

**Como transformar referência em sprite consistente [REC]:**
1. **Congelar** o estilo: escolher **1** imagem de referência da IA e **1** paleta; documentar na "folha de estilo" (anão, cristal da cave 1, painel da UI, lanterna).
2. **Redesenhar à mão sobre a referência** em Aseprite na grade final (270×480), não "limpar" a imagem da IA — a IA entrega pixels de tamanhos mistos que não se arrumam por filtro.
3. **Anão em *rig* simples:** corpo + braço + picareta em camadas, para animar por poucos quadros (evita a inconsistência de quadros gerados por IA — risco já anotado no RM §5.2).
4. **Cristais por rampa:** desenhar a cave 1 com a rampa "vidro"; criar as outras por **troca de rampa** (paleta indexada) e depois retocar formas/facetas das caves 4–10.
5. **Auditar** no aparelho (legibilidade a 1 braço de distância, em brilho baixo), não só no monitor.
6. **Fallback de custo:** se a produção própria passar do orçamento (RM §7), adotar pacote CC0/gratuito para ambiente (Kenney, OpenGameArt, itch.io — **conferir licença**) e manter **personagem + cristais** como arte própria, pois são a identidade.

## 6. Quando criar o quê (placeholders × polimento)

| Marco | Arte | Regra |
|---|---|---|
| M1b *Art Spike* | **Folha de estilo** (anão, 2 cristais, 1 painel de UI, paleta, fonte), **decisão 270×480 × 360×640** e industrial × fantasia | Obrigatório **antes** do vertical slice |
| M3 | Retângulos/círculos coloridos; texto padrão do TMP | Nada de arte "boa" ainda |
| M4 | UI funcional em caixas | Layout e fluxo, sem *skin* |
| **M5** | **Cave 1 final:** anão, picareta, cristal, lasca, VFX, SFX | **O momento em que o polimento vale a pena**: o *game feel* decide se o jogo vale a pena |
| M6–M8 | Estagiário reutiliza o anão; arte de passagem/AD provisória | — |
| M9 | Caves 2–10, UI final, VFX completos, áudio final, créditos | Primeiro corte de escopo (RM §7.1) se faltar tempo |
| M10–M11 | Só correções de legibilidade | Congelar arte antes do RC |

**Prioridade de feedback de mineração [DOC/REC]:** acerto (vibração, faísca, som) → barra de vida → quebra → lasca voando → crítico/combo → compra (brilho da picareta) → caverna nova → descoberta rara (P1).
