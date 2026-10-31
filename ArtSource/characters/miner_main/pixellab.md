# Anão principal — prompts do PixelLab

Referência: o anão do mockup (`docs/Designer (2).png`), recortado em `ref_mockup_dwarf.png` (82×105 px, nativo) e `ref_mockup_dwarf_x4.png` (ampliado sem filtro, só para olhar).
No mockup, o anão ocupa ≈ 14% da largura e 13% da altura da tela; na resolução de referência 270×480 (D-14) isso dá **≈ 40×60 px**, por isso o canvas sugerido é **64×64**.

Orçamento: 40 gerações gratuitas. Antes de gerar, **ler os Termos de Serviço** (uso comercial — o jogo será vendido, D-18) e conferir quanto cada ação custa (alguns modos cobram por direção ou por quadro).

## 1. Personagem base (só de frente)

**Descrição (inglês, o modelo responde melhor):**

```
stocky fantasy dwarf miner, full body, facing the viewer. Brass-yellow mining helmet with a big glowing round headlamp on the front. Huge bushy bright red beard covering his chest, thick red eyebrows, big round nose, small blue eyes. Brown leather tunic, darker brown leather shoulder pads, wide dark belt with a square brass buckle, brown leather gloves, dark brown boots. Holds a steel pickaxe with a wooden handle in his right hand, raised next to his head. Short legs, wide body, cheerful and tough.
```

**Negativo (se o campo existir):**

```
background, ground, floor, scenery, text, blurry, anti-aliasing, gradient, two pickaxes, axe, hammer, sword, shield, horns, side view, back view, cape, beard braids, white beard, brown beard, missing headlamp
```

**Configuração:**

| Opção | Valor | Por quê |
|---|---|---|
| Tamanho / canvas | 64×64 | ≈ 40×60 px do mockup em 270×480; abaixo disso o rosto e a barba somem |
| Direção | só **sul (de frente)**; se obrigar 4, use 4 (nunca 8) | o anão do jogo é só frontal (REQ-ART-03) |
| Câmera / vista | *low top-down* (de frente, levemente de cima) | igual ao mockup |
| Contorno | *selective outline* (ou *single color outline*) | Arte §1: contorno escuro seletivo, não preto puro |
| Sombreamento | *medium* / *detailed shading* | 4 tons por rampa |
| Detalhe | *high detail* | rosto, fivela e lanterna |
| Fundo | transparente | sprite vai para o Unity |
| Imagem de referência | `ref_mockup_dwarf.png` (se houver campo de referência/estilo; força baixa a média) | o recorte tem fundo de caverna: força alta pode trazer o fundo junto |

**Instrução de uso da referência (campo "como usar a imagem"):**

```
Use the reference image only for the character design: same dwarf identity, same brass-yellow helmet with glowing headlamp, same bright red bushy beard covering the chest, same brown leather outfit with brass belt buckle, same stocky proportions, pickaxe in his right hand raised beside his head. Ignore the cave background, the floor, the orange lighting and the glow around him. Redraw him as a clean pixel art sprite, full body, facing the viewer, isolated on a transparent background, crisp pixels with no blur.
```

**Checklist antes de animar (a animação herda tudo):**

- [ ] De frente, os dois pés visíveis e inteiros dentro do canvas.
- [ ] Picareta na mão direita **dele** (lado esquerdo de quem olha), cabeça de aço acima do ombro.
- [ ] Lanterna do capacete visível e brilhante; barba lida como **vermelha** em zoom 1×.
- [ ] Fundo transparente, sem sombra no chão.
- [ ] Silhueta legível em 100% (sem zoom).

## 2. Animação de minerar (só sul)

**Pré-requisito:** o personagem base precisa estar **segurando a picareta**. A animação parte dele; se o base estiver de mãos vazias, a picareta "aparece e some".

**Descrição da ação (v2 — golpe com uma mão, recomendada):**

```
one-handed mining strike. The pickaxe stays in his right hand in every frame: it never leaves his hand, never switches hands, never disappears. Frame 1: standing, holding the pickaxe in his right hand beside his head. Frame 2: raises the pickaxe high above his helmet. Frame 3: top of the swing, pickaxe behind his head. Frame 4: swings it down fast in front of his body. Frame 5: impact, the pickaxe head hits the ground right in front of his feet. Frame 6: lifts it back to the starting pose of frame 1. His left hand stays empty, closed in a fist at his side. He keeps facing the viewer, feet planted, body stays in place.
```

**Alternativa (golpe com as duas mãos na mesma picareta):** conflita com a Dupla (2ª picareta na mão esquerda, REQ-ART-03), que exigiria outra animação.

```
two-handed mining strike with a single pickaxe that is visible in every frame and never disappears. Frame 1: standing, holding the pickaxe in his right hand beside his head. Frame 2: his left hand also grabs the wooden handle, both hands hold it. Frame 3: raises it high above his helmet with both hands. Frame 4: swings it down fast in front of his body. Frame 5: impact, the pickaxe head hits the ground right in front of his feet. Frame 6: lets go with the left hand and returns to the pose of frame 1, pickaxe in the right hand only. He keeps facing the viewer, feet planted, body stays in place.
```

**Negativo da animação (se houver):**

```
pickaxe disappearing, empty hands, pickaxe in the left hand only, two pickaxes, throwing the pickaxe, walking, turning sideways, jumping
```

**Configuração:** 6–8 quadros, só a direção sul, primeiro quadro = personagem base, último quadro voltando à pose inicial (para emendar quando o intervalo for curto).

**Tempo:** a velocidade quem decide é o código no Unity. Com 6 quadros a 12 fps o golpe dura 0,5 s, igual ao intervalo mínimo; deve rodar a ~18 fps (≈ 0,33 s, Arte §3).

## Registro das gerações

| # | Etapa | O que mudou | Resultado | Seed |
|---|---|---|---|---|
| 1 | | | | |
