# Miners Idle

Idle/clicker de mineração para celular (Android primeiro). Um anão minerador desce por dez cavernas de cristais, quebra cinco por lote, junta lascas e compra melhorias. Pixel art de fantasia (mina de madeira). Unity 6 (URP 2D).

Projeto do Bruno. Regras de trabalho: [CLAUDE.md](CLAUDE.md). Documentação: [docs/README.md](docs/README.md).

## Status

| Marco | Estado |
|---|---|
| M0 Descoberta e planejamento | ✅ aprovado em 2026-10-21 |
| **M1 Fundação técnica** | ✅ implementado; **aguarda validação do Bruno** (APK rodando no Moto G75 a 60 fps) — ver [plano do M1](docs/planning/Marco%201%20-%20Plano%20da%20fundacao.md) |
| M1b Art Spike | ⏳ |
| M2 Núcleo de regras e economia | ⏳ |

Roadmap completo: [docs/planning/Roadmap.md](docs/planning/Roadmap.md).

## Como abrir

Unity Hub → **Add** → **Add project from disk** → esta pasta. Versão: **6000.6.0f1** (módulo Android instalado).

## Configuração e build

- Reaplicar a configuração do projeto: menu `Tools → Miners Idle → Projeto → Aplicar configuração do projeto`.
- APK de desenvolvimento: menu `Tools → Miners Idle → Build → APK de desenvolvimento (Android)`; saída em `tools/build/out/`.
- Aparelho de teste: Motorola Moto G75 (depuração USB ligada).
