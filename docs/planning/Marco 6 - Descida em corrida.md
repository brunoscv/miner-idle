# Marco 6 — Descida em corrida

> Plano de 2026-10-22. Proposta e motivos: [Proposta - Descida em corrida](Proposta%20-%20Descida%20em%20corrida.md). Decisões: **D-50** (câmera de frente) e **D-51** (E1 adiantada, numa cena separada), em [Decisões e Riscos](Decisoes%20e%20Riscos.md) §1.2.
> **Só a E1 está autorizada** (D-51). As etapas E2–E5 seguem o rascunho da proposta (§8) e só começam depois de o Bruno aprovar a proposta inteira (D-52…D-58) e o M5.

## Objetivo

Ver no Moto G75 o jogo com a câmera nova: o anão correndo na direção da câmera por um corredor de caverna, as paredes passando, os cristais vindo pela frente, ele parando para bater e voltando a correr. A ideia é validar a câmera e a sensação de descida com o dev principal, no aparelho, antes de mexer nas regras.

## E1 — Câmera e render da descida (cena separada)

### O que entra

| Parte | Como fica |
|---|---|
| **Cena `Descida`** | Nova, gerada pelo setup do projeto. O jogo atual (cena `Game`) não muda; troca-se de cena pelo DBG. |
| **Renderer** | Nada muda: o renderer do projeto, apesar do nome `URP_2D_Renderer`, já é o **Universal** (3D) desde o M1 (achado de 2026-10-22). A câmera da descida usa o mesmo; o setup só acrescentaria um Universal se faltasse. |
| **Câmera** | De perspectiva, com os valores da D-50: altura 2,4 m, inclinação 15°, distância 4,8 m, campo de visão 40°, corredor de 3,0 m. O campo de visão vale para a tela **9:16** do simulador; num celular mais alto (o Moto G75 é ~9:20), a largura vista é a mesma e a sobra vai para cima e para baixo, como no jogo atual (`CameraWidthFit`). |
| **Render pixelado** | A câmera desenha numa textura de **270 px de largura** (D-14), ampliada sem suavizar; a altura acompanha a proporção da tela. No DBG dá para trocar para 360 px ou para a resolução do aparelho e comparar. |
| **Corredor** | Chão, paredes e teto em 3D, com texturas de pedra em pixel art geradas por código, que **rolam** conforme o anão corre (a malha não se move). Esteios e vigas de madeira a cada 4 m, lanternas e cristais incrustados nas paredes, reciclados quando saem de vista. Neblina escura no fundo. |
| **Anão e estagiário** | Sprites de frente gerados por código (o mesmo desenho do simulador): 4 quadros de corrida e 4 de golpe. O estagiário corre ao lado, com outro capacete e outra barba. |
| **Cristais** | Sprites de frente nas 5 cores do mockup; entram por baixo, o anão para, bate 3 vezes (faíscas, cristal treme) e quebra (estilhaços). |
| **HUD** | Uma maquete do HUD (LASCAS, DANO, CAVERNA, VELOCIDADE, LOJA, AD, PASSAR DE CAVERNA) só para julgar o enquadramento; não funciona. |
| **DBG da descida** | Botões − e + para altura, inclinação, distância, campo de visão, largura do corredor e velocidade; Estagiário on/off, HUD on/off, Pausar, Resolução (270/360/nativa), Padrão D-50 e Voltar ao jogo. Uma linha mostra onde o anão fica na tela (% do topo), quanto chão se vê à frente dele e o fps. |
| **DBG do jogo** | Botão novo **Descida (teste)**: salva o progresso e abre a cena `Descida`. "Voltar ao jogo" reabre o jogo pelo caminho normal (com o save). |

### O que fica de fora (e por quê)

- **Regras.** O loop da cena é **só visual**, como no simulador (D-51): não há lascas, compras nem save. A fila de verdade é a E2.
- **Toque.** Tocar no mapa ainda não faz nada nesta cena; entra com as regras (E3).
- **Arte de verdade.** Tudo é gerado por código, no espírito do graybox; a arte é do Art Spike, depois da validação.

### Valores da cena

Ficam no componente `DescentStage` da cena (Inspector) e no DBG. São de **apresentação**: não são valores de regra (CLAUDE.md), e por isso podem ser ajustados sem decisão nova. A câmera validada é a D-50.

| Valor | Padrão | Origem |
|---|---|---|
| Altura · inclinação · distância · campo de visão · largura | 2,4 m · 15° · 4,8 m · 40° · 3,0 m | D-50 |
| Velocidade de corrida na tela | 2,0 m/s | 4 un/s da regra × 0,5 m/un (D-58, proposta) |
| Espaço entre cristais | 3,6 a 5,2 m | o mesmo do simulador |
| Golpes para quebrar · ritmo | 3 golpes · 0,55 s | o mesmo do simulador |
| Resolução interna | 270 px de largura | D-14 |

### Testes

- PlayMode `DescentTests`:
  - a cena abre e a câmera usa o renderer Universal e a textura de baixa resolução;
  - com o tempo acelerado, o anão corre, para, quebra cristais e volta a correr;
  - o enquadramento da D-50 numa tela 9:16 põe a cabeça do anão a ~44% e os pés a ~78% do topo;
  - um quadro da descida não aloca memória (corrida, golpe, quebra e nascimento).
- Os testes atuais continuam verdes (EditMode e PlayMode).
- No Moto G75: 60 fps com a cena rodando.

### Como testar (Bruno)

1. **Rodar no celular** (`Tools → Miners Idle → Build → Rodar no celular (Android)`; o build aplica o setup). Linhas `[Setup]` esperadas: "Cena criada: Assets/_Project/Scenes/Descida.unity", "Painel de debug de versão anterior removido para ser refeito".
2. No jogo, abra o **DBG** e toque **Descida (teste)**.
3. Olhe a cena por uns segundos: o anão corre na sua direção, as paredes correm para o fundo, um cristal entra por baixo, ele para, bate 3 vezes e o cristal quebra.
4. Abra o **DBG** da descida e mexa nos − e + (altura, inclinação, distância, campo de visão, largura, velocidade). Tudo muda na hora. **Padrão D-50** volta aos valores validados.
5. Teste **Resolução** (270 → 360 → nativa): é a diferença entre o "pixelado" e o "liso".
6. **Estagiário**, **HUD** e **Pausar** ligam e desligam cada coisa.
7. Mostre ao dev principal. Se mudarem a câmera, me mandem os números da linha de baixo do DBG (ou um print).
8. **Voltar ao jogo** volta ao jogo normal, com o progresso.

No Editor: setup pelo menu, abrir `Assets/_Project/Scenes/Descida.unity` e dar Play. (Pela `Bootstrap`, vai pelo DBG como no celular.)

### Critério de pronto da E1

- [ ] Cena `Descida` rodando no Moto G75 a 60 fps, igual ao simulador.
- [ ] Controles do DBG mudam a câmera na hora; Padrão D-50 volta aos valores validados.
- [ ] Ida e volta entre o jogo e a descida sem perder o progresso.
- [ ] Testes verdes (os novos e os de antes).
- [ ] Você (e o dev principal) confirmam a câmera no aparelho, ou mandam os números novos.

### Esforço

10–16 h (proposta, §8).

## E2–E5 (rascunho, aguardando a aprovação da proposta)

E2 a fila de cristais no núcleo · E3 o jogo de verdade no corredor · E4 feedback e DBG · E5 economia e celular. Detalhe na [proposta](Proposta%20-%20Descida%20em%20corrida.md) §8; o plano completo destas etapas é escrito quando a proposta for aprovada.

## Estado

| Etapa | Estado | Notas |
|---|---|---|
| E1 Câmera e render da descida | ✅ validada pelo Bruno no Moto G75 em 2026-10-22 | Presentation/Descent: `DescentStage` (câmera da D-50, render de 270 px com filtro *point*, corredor parado com a textura rolando no shader, esteios, lanternas e cristais de parede reciclados, partículas em pool), `DescentPreview` (loop só visual, fila circular sem alocação), `DescentArt` (anão, estagiário, cristais e texturas desenhados por código, o mesmo desenho do simulador), `DescentMeshes`, `DescentFraming` (enquadramento e as medidas da tela). Shader `MinersIdle/DescentUnlit` (corte por alfa, rolagem e neblina). UI: `DescentScreen` (imagem na tela inteira) e `DescentDebugPanel` (− e + da câmera e da velocidade, Estagiário, HUD, Pausar, Resolução, Padrão D-50, Voltar ao jogo, linha de medidas). Setup: `DescentSetup` cria a cena `Descida` e a põe no build; o DBG do jogo ganhou **Descida (teste)** (22 botões, 8 fileiras). **Achados:** (1) o renderer do projeto (`URP_2D_Renderer`) já é o Universal desde o M1, então nada mudou no URP; (2) a neblina saía fraca e cinza: `Shader.SetGlobalColor` não converte a cor para o espaço linear, e a mistura em espaço linear parece lenta ao olho; corrigido (cor convertida, mistura em sRGB, fim em 25 m), conferido em capturas renderizadas de verdade. Teste PlayMode `DescentTests` (6). Em batch: setup ok, EditMode **112/112**, PlayMode **33/33** |
