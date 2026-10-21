# Miners Idle

Idle/clicker de mineração para celular (Android primeiro). Um anão minerador anda pelo mapa de cada caverna quebrando grupos de cristais (na ordem em que nasceram), junta lascas, compra melhorias e desce por dez cavernas, cada uma com seu bioma. Pixel art de fantasia (mina de madeira). Unity 6 (URP 2D).

Projeto do Bruno. Regras de trabalho: [CLAUDE.md](CLAUDE.md). Documentação: [docs/README.md](docs/README.md).

## Status

| Marco | Estado |
|---|---|
| M0 Descoberta e planejamento | ✅ aprovado em 2026-10-21 |
| M1 Fundação técnica | ✅ aprovado em 2026-10-21 — [plano do M1](docs/planning/Marco%201%20-%20Plano%20da%20fundacao.md) |
| M1b Art Spike | ⏳ (em paralelo) |
| **M2 Protótipo do loop no mapa** | 🚧 plano aprovado em 2026-10-21; E1–E3 ✅; **E4 (HUD) em andamento** — [plano do M2](docs/planning/Marco%202%20-%20Prototipo%20do%20loop.md) |
| M3 Regras e economia | ⏳ |

Roadmap completo: [docs/planning/Roadmap.md](docs/planning/Roadmap.md).

## Como abrir

Unity Hub → **Add** → **Add project from disk** → esta pasta. Versão: **6000.6.0f1** (módulo Android instalado).

## Configuração e build

- Reaplicar a configuração do projeto: menu `Tools → Miners Idle → Projeto → Aplicar configuração do projeto`.
- APK de desenvolvimento: menu `Tools → Miners Idle → Build → APK de desenvolvimento (Android)`; saída em `tools/build/out/`.
- Aparelho de teste: Motorola Moto G75 (depuração USB ligada).
