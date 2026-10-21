# miners-idle — regras de trabalho

Visão geral e status: [README.md](README.md). Índice dos documentos: [docs/README.md](docs/README.md).

## O projeto

- Jogo idle/clicker mobile (Android primeiro), Unity 6000.6.0f1, URP 2D, pixel art. Dono e único desenvolvedor: **Bruno** (~20 h/semana).
- Documentos e conversa em **português do Brasil**. Explicar o porquê das escolhas, não só fazer.
- **Decisões do Bruno** ficam em [Decisões e Riscos](docs/planning/Decisoes%20e%20Riscos.md), com data. **Valores de regra** (fórmulas, custos, tempos, números de balanceamento) **não mudam sem decisão registrada**.
- `we-had-a-plan` e `underworld-towers` (em `E:\Fabrica\Unity`) são **só leitura**: consultar como referência de aprendizado, nunca alterar nem copiar código/arte.
- Estilo visual: o do mockup `docs/Designer (2).png` (fantasia, mina de madeira). Resolução de referência provisória 270×480, PPU 16 (D-14, validar no Art Spike).

## Fluxo

1. **Jogável antes de sistema.** Um sistema só nasce quando o jogo jogável pede. Sem frameworks antecipados.
2. **Plano primeiro:** cada marco ganha `docs/planning/Marco N — nome.md` (objetivo, decisões, etapas, **como testar**, critério de pronto) antes do código. Marcos em [Roadmap](docs/planning/Roadmap.md); um marco só começa quando o anterior foi **aprovado pelo Bruno**.
3. **Cada etapa termina com algo que roda.** Uma etapa só é ✅ depois de validada pelo Bruno.
4. **Ao fim de cada implementação**, dizer ao Bruno exatamente onde testar: cena, caminho de menu, passos numerados, e se algum menu/cena foi criado.
5. **Todo recurso de gameplay novo** traz atalhos no painel de debug (quando existir) para pular a repetição.
6. README atualizado a cada marco; decisões novas no registro.

## Princípios técnicos

Detalhe em [Arquitetura Técnica](docs/planning/Arquitetura%20Tecnica.md). Em resumo:

- **Núcleo em C# puro** (`MinersIdle.Core`, *No Engine References*): regras determinísticas, tempo e RNG injetados. Apresentação só escuta eventos e envia comandos. **O código é dono do tempo e da regra; a animação só ilustra.**
- Dependências só descem: `UI/Presentation → Game → Core`; `Persistence → Core`.
- Dados em ScriptableObjects (definições imutáveis); estado de jogo em classes simples (nunca dentro de SO).
- `double` para números; arredondar só na exibição; um único formatador.
- Sem `GameManager` onipotente nem singletons estáticos de estado; composição no `GameBootstrap`.
- Input System desde o início; textos de jogo em dados (pt-BR); nenhum texto de interface escrito no código.
- Inscrição em eventos em `OnEnable`, desinscrição em `OnDisable`. Campo preenchido fora do Play Mode precisa de `[SerializeField]`.
- Nada de `Find`/`GetComponent`/alocação no `Update` do loop de mineração.
- Assets de terceiros em `Assets/_Vendor/`, registrados com a licença (`CREDITS.md`). **Mover asset sempre com o `.meta`.**
- Arquivos-fonte de arte fora de `Assets/` (`ArtSource/`).

## Testes

- **Núcleo:** testes EditMode (`Assets/_Project/Tests/EditMode`). Um teste quebrado pela mudança é **corrigido para medir o comportamento novo**, nunca apagado para passar.
- **Fluxos:** PlayMode (`Tests/PlayMode`). **Dispositivo:** Moto G75 a cada marco com mudança funcional.
- Rodar os testes (Editor fechado): `bash tools/ci/run-tests.sh` (EditMode e PlayMode, saída em `tools/ci/out/`).

## Ferramentas

- Unity: `C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe` (a mesma versão dos outros dois jogos).
- Configuração do projeto em código: `Assets/_Project/Scripts/Editor/ProjectSetup.cs` — menu `Tools → Miners Idle → Projeto → Aplicar configuração do projeto`, ou em batch:
  `Unity.exe -batchmode -nographics -projectPath <pasta> -executeMethod MinersIdle.EditorTools.ProjectSetup.RunBatch -logFile Logs/setup.log`. Depois, conferir as linhas `[Setup]` e procurar `error CS`.
- APK de desenvolvimento: `bash tools/build/build-android.sh` (saída em `tools/build/out/`, ignorada pelo git) ou menu `Tools → Miners Idle → Build`.
- **Rodar no celular num passo** (gera, instala e abre no aparelho conectado): menu `Tools → Miners Idle → Build → Rodar no celular (Android)` ou, com o Editor fechado, `bash tools/build/run-android.sh` (`--no-build` só reinstala o último APK). O build por linha de comando roda no Terminal do app, não no shell restrito do Claude (lá o Gradle falha na conexão local).
- **Nunca** commitar: `Library/`, `Temp/`, builds, **keystore/senhas** e **tokens de API** (PixelLab etc.). Segredos ficam em variável de ambiente ou na configuração do usuário.
