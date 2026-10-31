# Miners Idle

Idle/clicker de mineração para celular (Android primeiro). Um anão minerador anda pelo mapa de cada caverna quebrando grupos de cristais (na ordem em que nasceram), junta lascas, compra melhorias e desce por dez cavernas, cada uma com seu bioma. Pixel art de fantasia (mina de madeira). Unity 6 (URP 2D).

Projeto do Bruno. Regras de trabalho: [CLAUDE.md](CLAUDE.md). Documentação: [docs/README.md](docs/README.md).

## Status

| Marco | Estado |
|---|---|
| M0 Descoberta e planejamento | ✅ aprovado em 2026-10-21 |
| M1 Fundação técnica | ✅ aprovado em 2026-10-21 — [plano do M1](docs/planning/Marco%201%20-%20Plano%20da%20fundacao.md) |
| M1b Art Spike | 🔨 parte 1 (interface: fonte pixel, HUD, LOJA/AD/AJUSTES, loja) ✅ aprovada em 2026-10-23; parte 2 em andamento: corredor, cristais, anão e estagiário feitos; chão e esteios próprios (E9, E10) e biomas das 10 cavernas (E8, D-102) aguardando validação no Moto G75 — [plano do M1b](docs/planning/Marco%201b%20-%20Art%20Spike.md) |
| M2 Protótipo do loop no mapa | ✅ aprovado em 2026-10-21 (E6 reduzida, D-32; testes da E5 no Unity rodam no início do M3) — [plano do M2](docs/planning/Marco%202%20-%20Prototipo%20do%20loop.md) |
| M3 Regras e economia | ✅ aprovado em 2026-10-21 — [plano do M3](docs/planning/Marco%203%20-%20Regras%20e%20economia.md) |
| M4 Loja, HUD e save mínimo | ✅ aprovado em 2026-10-21 — [plano do M4](docs/planning/Marco%204%20-%20Loja%20e%20save.md) |
| M5 Toque, crítico, combo e feedback (graybox) | ✅ aprovado em 2026-10-22 (E5 reduzida, D-59: o teste de 10 min no Moto G75 vai para a E5 do M6) — [plano do M5](docs/planning/Marco%205%20-%20Toque%20e%20feedback.md) |
| **M6 Descida em corrida** (nova visão, [proposta](docs/planning/Proposta%20-%20Descida%20em%20corrida.md)) | ✅ aprovado em 2026-10-31 (D-106); o jogo usa o balanço GDD v5 (D-103); o teste de 10 min no Moto G75 ficou com o Apellon (D-105) — [plano do M6](docs/planning/Marco%206%20-%20Descida%20em%20corrida.md) |
| **M8 Letrado, parede e 10 cavernas** (antes do M7, D-88) | 🔨 plano aprovado em 2026-10-31 (D-107…D-113); E1 em andamento — [plano do M8](docs/planning/Marco%208%20-%20Letrado,%20parede%20e%2010%20cavernas.md) |

Roadmap completo: [docs/planning/Roadmap.md](docs/planning/Roadmap.md).

## O que já roda (graybox)

- **Descida em corrida (M6):** a câmera de frente (D-50) mostra o anão correndo na sua direção por um corredor de caverna 3D com cara de pixel art; ele para em cada cristal da fila, bate até quebrar e volta a correr. Cristais nascem no fim da fila, até 12.
- HUD de cima em placas de madeira: LASCAS (sempre inteiro), DANO, CAVERNA e VELOCIDADE, cada um com seu ícone; textos na fonte pixel Pixelify Sans.
- Coluna da direita: **LOJA** (com o selo "!"), **AD** e **AJUSTES**; os dois últimos mostram "Em breve" até o M9.
- Melhorias: Picareta, Velocidade, Mineração, Picareta Dupla e Botas, compradas na **LOJA** (janela de madeira, ícone em moldura colorida, botão rubi quando dá para comprar); Estagiário e Letrado aparecem como "Em breve".
- **Save** do progresso (lascas e níveis): fechar e reabrir o app mantém tudo; a fila nasce de novo.
- Painel **DBG** (só em build de desenvolvimento): tempo ×1/×5/×20, gerar cristal, quebrar alvo, +100 lascas, reiniciar com semente, compras, toques auto, crítico sempre, tremor, som, medição e estatísticas.
- Menus em `Tools → Miners Idle → Debug`: **Simular 10 minutos** e **Relatório de economia**.
- A interface usa packs de terceiros que **não vão para o git** (licença proíbe redistribuir, D-65): ficam em `Assets/_Vendor/AssetStore/` e precisam ser copiados do backup numa máquina nova. Créditos em [CREDITS.md](CREDITS.md).
- O corredor, o anão e os cristais ainda são desenhados por código (graybox); a arte deles é a parte 2 do Art Spike.
- DBG → **Câmera** ajusta a câmera da descida ao vivo (altura, inclinação, distância, campo de visão, largura do corredor, resolução).

## Como abrir

Unity Hub → **Add** → **Add project from disk** → esta pasta. Versão: **6000.6.0f1** (módulo Android instalado).

## Configuração e build

- Reaplicar a configuração do projeto: menu `Tools → Miners Idle → Projeto → Aplicar configuração do projeto`.
- APK de desenvolvimento: menu `Tools → Miners Idle → Build → APK de desenvolvimento (Android)`; saída em `tools/build/out/`.
- **Rodar no celular** (gera, instala e abre num passo, com o aparelho no USB): menu `Tools → Miners Idle → Build → Rodar no celular (Android)`, ou `bash tools/build/run-android.sh` com o Editor fechado.
- Aparelho de teste: Motorola Moto G75 (depuração USB ligada).
