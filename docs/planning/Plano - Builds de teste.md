# Plano — Builds de teste para o pessoal (Firebase App Distribution)

> Plano de 2026-10-24, a partir da **D-84** ([Decisões e Riscos](Decisoes%20e%20Riscos.md)). **É o marco M15 do [Roadmap](Roadmap.md)**, posto no fim do planejamento pelo Bruno (2026-10-24); pode ser puxado antes do playtest do M11. Quando começar: aprovar este plano e responder "Perguntas para você".

## Objetivo

**Um comando no seu PC e o pessoal recebe a versão nova no celular.** O script gera o APK, dá a ele um número de versão que sobe sozinho, monta as notas a partir dos commits e manda para um grupo de testadores no Firebase. Eles recebem um e-mail, instalam uma vez e, nas próximas versões, só atualizam: sem desinstalar e sem perder o save.

**Para você:** terminou uma etapa, commitou, roda `publish-test.sh` no Terminal, confere as notas, aperta `s` e vai tomar um café. Quando alguém disser "deu bug", a versão aparece no canto da tela e você sabe exatamente de qual commit ela veio.

## Por que local e não no GitHub Actions (por enquanto)

- **Os assets fora do git (D-65).** O repositório é público e os packs com redistribuição proibida (caverna, pedras, ícones) não estão nele. Um build no CI sairia em graybox. No seu PC eles estão lá.
- **Licença do Unity no CI.** Exige ativar a licença numa máquina do GitHub (GameCI) e confirmar que já existe imagem para a 6000.6.0f1.
- **Tempo.** O build IL2CPP numa máquina limpa leva de 20 a 40 min; no seu PC, com o `Library/` pronto, é bem menos.
- O caminho 2 (CI) continua possível depois: o script do Firebase e o versionamento desta etapa são reaproveitados lá.

## Decisões usadas

| # | O que fixa |
|---|---|
| D-84 | Script local + Firebase App Distribution; CI depois |
| D-65 | Assets com redistribuição proibida ficam fora do git; o build depende da sua pasta local |
| M4 (D-44) | O save tem versão: arquivo de versão desconhecida → jogo novo, e o arquivo ruim vira `save.json.bad` (não trava) |
| CLAUDE.md | Nada de keystore, senha ou token no git; o build por linha de comando roda no Terminal do app, não no shell do Claude |

## Perguntas para você

| # | Pergunta | Recomendação |
|---|---|---|
| P1 | O pessoal recebe build de **desenvolvimento** (com DBG e a marca "Development Build") ou **de verdade** (sem DBG, menor, desempenho real)? | **De verdade.** O DBG deixa pular tudo e o teste perde o sentido. O script terá `--dev` para mandar um build com DBG quando você quiser |
| P2 | Onde aparece a versão no jogo? | **Texto pequeno e discreto num canto da tela de jogo** (ex.: "v0.1.0-b45"). Assim ela sai em todo print de bug. O formato do texto fica em dados (regra: nenhum texto de interface no código) |
| P3 | Assinatura: continua com a **chave de debug do seu PC** ou já criamos uma chave própria (fora do git, senha em variável de ambiente)? | **Chave de debug por enquanto** (R-30): zero configuração, e os seus builds pelo menu e os do Firebase têm a mesma assinatura, então um instala por cima do outro. A chave de verdade entra antes do CI ou da Play Store (M12). Faça um backup de `C:\Users\brunocarvalho\.android\debug.keystore` |
| P4 | As notas da versão saem dos títulos dos commits (sem o "feat:"/"fix:") ou você escreve na hora? | **Dos commits, mostradas antes de enviar**, com `--notas "texto"` para escrever à mão quando quiser |

## Como vai funcionar

**Versão automática** (calculada no `BuildTools`, valendo para **todo** build: menu, `run-android.sh` e o script novo):

| Campo | Valor | Exemplo |
|---|---|---|
| `versionCode` (o número que o Android compara) | quantidade de commits do `main` (`git rev-list --count HEAD`) | 45 |
| `versionName` (o que aparece) | `VersionName` do `ProjectSetup` + build + commit | `0.1.0-b45+2b55e47` |

Por que a contagem de commits: só sobe, não precisa de arquivo guardando estado e é a mesma em qualquer máquina. E como vale para todo build, o build pelo menu nunca sai com número menor que o do Firebase (o Android recusaria instalar por cima). Hoje o `ProjectSetup` grava `bundleVersionCode = 1` em todo build; isso sai.

**O script** `tools/build/publish-test.sh`:

1. Confere: Editor fechado; nada modificado e não commitado em `Assets/_Project`, `ProjectSettings` e `Packages` (cada versão = um commit; as fontes do TextMesh Pro, que o Unity reescreve sozinho, ficam de fora da conferência); `firebase` instalado e logado.
2. Gera o APK de teste (sem Development, a não ser com `--dev`) em `tools/build/out/MinersIdle-teste-b45.apk`.
3. Monta as notas com os commits desde a última versão publicada (tag local `teste/b44`), mostra na tela e pergunta "Enviar para o grupo pessoal? (s/N)".
4. Envia: `firebase appdistribution:distribute <apk> --app <ID do app> --groups pessoal --release-notes-file <notas>`.
5. Cria a tag local `teste/b45`, que marca o que foi publicado e serve de ponto de partida para as próximas notas.

Opções: `--dry-run` (gera e mostra as notas, não envia), `--dev`, `--notas "texto"`, `--grupo <alias>` (ex.: mandar só para você antes), `--no-build` (reenvia o último APK).

**Configuração:** o ID do app no Firebase fica em `tools/build/firebase.conf`, que vai para o git (não é segredo: ele vem dentro de qualquer app que usa Firebase). **Os e-mails dos testadores nunca vão para o git**: ficam só no grupo, no console do Firebase. O login do Firebase fica na sua conta de usuário, fora do projeto.

**O jogo não ganha SDK do Firebase.** O App Distribution só distribui o APK; o aviso de versão nova chega por e-mail e pelo app Firebase App Tester.

## O que você faz uma vez (contas e termos são seus)

1. Em console.firebase.google.com: criar o projeto `miners-idle` (pode desligar o Google Analytics).
2. Adicionar um app **Android** com o pacote `com.brunocarvalho.minersidle`. Pode pular o download do `google-services.json` e os passos do SDK.
3. Menu **App Distribution** → Começar → aba **Testadores e grupos** → criar o grupo **pessoal** e pôr os e-mails. Vale criar também um grupo **eu** só com você, para a primeira prova.
4. No Terminal do app: `npm install -g firebase-tools` e depois `firebase login` (abre o navegador para você entrar).
5. Me passar o **ID do app** (Configurações do projeto → Seus apps; formato `1:…:android:…`).

## Etapas

| Etapa | Entrega | Como testar |
|---|---|---|
| **E1 Versão automática** | `BuildTools` calcula `versionCode` e `versionName` pelo git; o `ProjectSetup` para de gravar 1; texto da versão no canto da tela (formato em dados); build "de teste" sem Development (`BuildAndroidTestBatch`). Testes EditMode da função que monta o nome da versão; PlayMode: o texto mostra `Application.version` | 1. Editor fechado. 2. No Terminal: `& "C:\Program Files\Git\bin\bash.exe" tools/build/run-android.sh`. 3. No celular, o canto da tela mostra `v0.1.0-b45+…` (o número = commits do `main`). 4. Faça um commit qualquer e repita: o número sobe 1 e instala por cima, sem desinstalar e com o save mantido |
| **E2 Script de publicação** | `tools/build/publish-test.sh` com as conferências, as notas, o envio e a tag; `firebase.conf`; seção no README de como publicar | 1. Faça os passos de "O que você faz uma vez". 2. `& "C:\Program Files\Git\bin\bash.exe" tools/build/publish-test.sh --dry-run`: gera `tools/build/out/MinersIdle-teste-b45.apk` e mostra as notas, sem enviar. 3. Mexa num arquivo de `Assets/_Project` sem commitar e rode de novo: o script recusa e diz o motivo. 4. `... publish-test.sh --grupo eu`: o e-mail chega para você, você instala pelo link e o jogo abre sem DBG, com a versão no canto |
| **E3 Primeira rodada com o pessoal** | Primeiro envio para o grupo **pessoal**; anotar no README o que o testador precisa fazer (aceitar o convite, permitir instalar de fontes desconhecidas uma vez) | 1. `... publish-test.sh` (grupo pessoal). 2. Um amigo instala pelo e-mail. 3. Você faz um commit e publica de novo. 4. O amigo recebe o aviso, atualiza **sem desinstalar** e o progresso continua lá |

Esforço estimado: E1 ~2 h, E2 ~2–3 h, E3 ~1 h (depende do amigo responder).

## Critério de pronto

- [ ] Todo build (menu, `run-android.sh`, script) sai com versão que sobe a cada commit, e a versão aparece no jogo.
- [ ] `publish-test.sh` gera, mostra as notas, envia ao grupo e marca a tag; recusa publicar com mudança não commitada.
- [ ] Um testador de fora instalou pelo convite e atualizou para a versão seguinte sem desinstalar e sem perder o save.
- [ ] Nenhum e-mail, token ou chave no git. EditMode e PlayMode verdes.

## Riscos e cuidados

- **Chave de debug (R-30):** perdeu o PC ou a chave → os testadores desinstalam e perdem o save. Backup do `debug.keystore`.
- **Save entre versões:** se o formato do save mudar sem migração (a migração é do M8), o testador recomeça do zero (o jogo não trava, D-44). Vale avisar nas notas quando isso acontecer.
- **Commits viram notas:** os títulos dos commits são técnicos; o `--notas` resolve quando a mudança merecer um texto para o pessoal.
- **Testador precisa de conta Google** para aceitar o convite e baixar (a confirmar na E3).

## Fora de escopo

Build no GitHub Actions (caminho 2) · chave de assinatura definitiva e AAB para a Play Store (M12) · SDK do Firebase no jogo (aviso de versão nova dentro do app, Crashlytics) · teste interno da Play Store.
