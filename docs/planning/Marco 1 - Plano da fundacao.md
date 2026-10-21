# Marco 1 — Plano da fundação técnica

> Iniciado em 2026-10-21, com autorização do Bruno. Roadmap: [M1](Roadmap.md). Requisitos: REQ-TEC-01, REQ-TEC-06, REQ-TEC-07, REQ-TEC-08, REQ-PLT-01, REQ-PLT-02, REQ-PLT-05, REQ-PLT-07.

## Objetivo
Um projeto Unity reprodutível que gera um APK e abre no Moto G75, com estrutura de pastas, assemblies, testes e Safe Area prontos. **Nenhuma regra de jogo.**

## Decisões usadas
D-12 (Unity 6000.6.0f1 + Android) · D-03 (nome Miners Idle, pacote `com.brunocarvalho.minersidle`) · D-02 (pt-BR) · D-20/D-24 (Moto G75) · D-16 (LFS só para áudio/binários grandes — recomendação adotada, **ainda pendente de confirmação**) · D-14 provisório (270×480, PPU 16: câmera ortográfica tamanho 15 e Canvas Scaler 270×480, a validar no M1b).

## O que foi feito (2026-10-21)

| Tarefa | Resultado |
|---|---|
| T-M1-01 Android Build Support | Instalado pelo Unity Hub: Android Player, OpenJDK 17, Android SDK/NDK r27c e ferramentas. A primeira tentativa falhou por *timeout* de rede; a segunda completou |
| T-M1-02 Projeto Unity | Criado em `miners-idle` **sem tocar em `docs/`**; pacotes: URP 17.6.0, Input System 1.20.0, uGUI 2.6.0 (inclui TextMeshPro), Test Framework 1.8.0 |
| T-M1-03 `.gitignore`/`.gitattributes` | Padrão Unity; **keystore e tokens ignorados**; cenas/prefabs como texto; LFS só para áudio, PSD/KRA e fontes |
| T-M1-04 Pastas e asmdefs | `Core` (sem referência à UnityEngine), `Persistence`, `Game`, `Presentation`, `UI`, `Editor`, `Tests.EditMode`, `Tests.PlayMode` |
| T-M1-05 `ProjectSetup.cs` | Identidade, retrato travado, IL2CPP + ARM64, min API 26, URP com **Renderer 2D**, Input System único, TMP Essentials, cenas de base. Reaplicável (só cria o que falta) |
| T-M1-06 Cenas e Safe Area | `Bootstrap` (carrega `Game`) e `Game` (câmera 2D, Canvas, painel Safe Area âmbar translúcido, rótulo de diagnóstico com versão/resolução/Safe Area/FPS) |
| T-M1-07 Testes | EditMode: guarda de arquitetura (Core não referencia Unity) + 3 testes do cálculo do Safe Area; PlayMode: a cena `Game` abre e tem Safe Area. **5/5 verdes** |
| T-M1-08 APK no aparelho | Ver "Estado" abaixo |
| T-M1-09 `CLAUDE.md` e `README.md` | Criados |

## Como testar (Bruno)

**No Editor:**
1. Unity Hub → Add → Add project from disk → `E:\Fabrica\Unity\miners-idle` (versão 6000.6.0f1). Na primeira abertura, aceite importar.
2. Abra `Assets/_Project/Scenes/Bootstrap.unity` e aperte Play: ela troca sozinha para `Game`. Você deve ver fundo carvão, um painel âmbar translúcido (o Safe Area) e o texto "Miners Idle / v0.1.0 (M1) / resolução / FPS".
3. Menu `Tools → Miners Idle → Projeto → Aplicar configuração do projeto` reaplica a configuração (não deve mudar nada).
4. `Window → General → Test Runner`: rode EditMode e PlayMode — tudo verde. (Em linha de comando: `bash tools/ci/run-tests.sh`, com o Editor fechado.)

**No Moto G75:** ver a seção "Estado" (instalação do APK).

*Criados:* menus `Tools → Miners Idle → Projeto` e `Tools → Miners Idle → Build`; cenas `Bootstrap` e `Game`. Nenhuma sala de teste ainda.

## Critério de pronto (do Roadmap)
- [x] Projeto abre e compila sem erros; testes verdes.
- [x] APK instala e abre **sem erros no Moto G75**, a 60 fps (ver Estado).
- [x] Commit inicial (0a1fba3).
- [x] Bruno valida o marco A parcial — **aprovado em 2026-10-21 (D-25)**.

## Fora deste marco
Qualquer regra de jogo, arte, economia, UI funcional, save, anúncios.

## Estado (2026-10-21)

- **APK gerado:** `tools/build/out/MinersIdle-dev.apk` (~41 MB, IL2CPP ARM64, 0 erros), com `bash tools/build/build-android.sh` ou pelo menu.
- **Atenção — Gradle e rede local:** dentro do shell restrito da sessão do Claude, o Gradle falhou com `Unable to establish loopback connection` (a compilação IL2CPP passou; só o empacotamento falhou). **Rodando fora desse shell (Terminal do app ou o seu PowerShell) o build funciona.** No seu uso normal (Editor ou terminal seu) isso não deve acontecer; se acontecer, é firewall/antivírus bloqueando o Java em `127.0.0.1`.
- **Verificado no Moto G75 (2026-10-21):** APK instalado e aberto sem erros. Tela 1080×2388; Safe Area 1080×2278 (desconta ~110 px do recorte superior); texto TMP renderizando; **60 fps** (cap em `GameBootstrap`; sem o cap o Unity rodava a **30 fps** no Android, achado do M1). O único erro no logcat é o aviso benigno `AssetPackManager` (Play Asset Delivery opcional; some no AAB). A tela atual é só diagnóstico (fundo âmbar translúcido sobre carvão).
- **Correção no meio do marco:** o primeiro APK exibia erro de TMP (Essentials não importados em batch). `ProjectSetup` agora importa o pacote e espera o término antes de criar a cena.
- ~~Falta: instalar e abrir no Moto G75.~~ (feito)
- **Sobrou (sem urgência):** o painel âmbar ficou mais claro que o esperado (mistura em espaço linear); trocar o diagnóstico por HUD real no M3. O `adb devices` ainda não lista o aparelho. Passos: Configurações → Sobre o telefone → toque 7× em "Número da versão" → Opções do desenvolvedor → **Depuração USB** ligada; cabo USB **com dados**; aceitar o aviso "Permitir depuração USB" no celular. Depois: `adb install -r tools\build\out\MinersIdle-dev.apk`.
- **O que verificar no celular:** abre sem fechar; fundo carvão; painel âmbar translúcido respeitando o notch/barra de gestos; texto com versão 0.1.0, resolução, Safe Area e FPS (anote o FPS: baseline do Moto G75).
