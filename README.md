# Miners Idle

Idle/clicker de mineração para celular (Android primeiro). Um anão minerador anda pelo mapa de cada caverna quebrando grupos de cristais (na ordem em que nasceram), junta lascas, compra melhorias e desce por dez cavernas, cada uma com seu bioma. Pixel art de fantasia (mina de madeira). Unity 6 (URP 2D).

Projeto do Bruno. Regras de trabalho: [CLAUDE.md](CLAUDE.md). Documentação: [docs/README.md](docs/README.md).

## Status

| Marco | Estado |
|---|---|
| M0 Descoberta e planejamento | ✅ aprovado em 2026-10-21 |
| M1 Fundação técnica | ✅ aprovado em 2026-10-21 — [plano do M1](docs/planning/Marco%201%20-%20Plano%20da%20fundacao.md) |
| M1b Art Spike | ⏳ (em paralelo) |
| M2 Protótipo do loop no mapa | ✅ aprovado em 2026-10-21 (E6 reduzida, D-32; testes da E5 no Unity rodam no início do M3) — [plano do M2](docs/planning/Marco%202%20-%20Prototipo%20do%20loop.md) |
| M3 Regras e economia | ✅ aprovado em 2026-10-21 — [plano do M3](docs/planning/Marco%203%20-%20Regras%20e%20economia.md) |
| M4 Loja, HUD e save mínimo | ✅ aprovado em 2026-10-21 — [plano do M4](docs/planning/Marco%204%20-%20Loja%20e%20save.md) |
| M5 Toque, crítico, combo e feedback (graybox) | ✅ aprovado em 2026-10-22 (E5 reduzida, D-59: o teste de 10 min no Moto G75 vai para a E5 do M6) — [plano do M5](docs/planning/Marco%205%20-%20Toque%20e%20feedback.md) |
| **M6 Descida em corrida** (nova visão, [proposta](docs/planning/Proposta%20-%20Descida%20em%20corrida.md)) | 🔨 proposta aprovada (D-52…D-58); E1 (câmera e render, cena separada) ✅ validada no Moto G75 em 2026-10-22; E2 (fila no núcleo) ✅; E3 (jogo no corredor) ✅; E4 (feedback) em andamento — [plano do M6](docs/planning/Marco%206%20-%20Descida%20em%20corrida.md) |

Roadmap completo: [docs/planning/Roadmap.md](docs/planning/Roadmap.md).

## O que já roda (graybox)

- **Descida em corrida (M6):** a câmera de frente (D-50) mostra o anão correndo na sua direção por um corredor de caverna 3D com cara de pixel art; ele para em cada cristal da fila, bate até quebrar e volta a correr. Cristais nascem no fim da fila, até 12.
- HUD de cima: LASCAS, DANO, CAVERNA e VELOCIDADE.
- Melhorias: Picareta, Velocidade, Mineração, Picareta Dupla e Botas, compradas na **LOJA** (botão à direita, com o selo "!"); Estagiário e Letrado aparecem como "Em breve".
- **Save** do progresso (lascas e níveis): fechar e reabrir o app mantém tudo; a fila nasce de novo.
- Painel **DBG** (só em build de desenvolvimento): tempo ×1/×5/×20, gerar cristal, quebrar alvo, +100 lascas, reiniciar com semente, compras, toques auto, crítico sempre, tremor, som, medição e estatísticas.
- Menus em `Tools → Miners Idle → Debug`: **Simular 10 minutos** e **Relatório de economia**.
- Tudo desenhado por código (graybox); a arte de verdade vem do Art Spike.
- DBG → **Câmera** ajusta a câmera da descida ao vivo (altura, inclinação, distância, campo de visão, largura do corredor, resolução).

## Como abrir

Unity Hub → **Add** → **Add project from disk** → esta pasta. Versão: **6000.6.0f1** (módulo Android instalado).

## Configuração e build

- Reaplicar a configuração do projeto: menu `Tools → Miners Idle → Projeto → Aplicar configuração do projeto`.
- APK de desenvolvimento: menu `Tools → Miners Idle → Build → APK de desenvolvimento (Android)`; saída em `tools/build/out/`.
- **Rodar no celular** (gera, instala e abre num passo, com o aparelho no USB): menu `Tools → Miners Idle → Build → Rodar no celular (Android)`, ou `bash tools/build/run-android.sh` com o Editor fechado.
- Aparelho de teste: Motorola Moto G75 (depuração USB ligada).
