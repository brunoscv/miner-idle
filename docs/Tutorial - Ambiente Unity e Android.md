# Tutorial: montar o ambiente Unity + Android e rodar o Miners Idle

> Para quem **nunca mexeu** com isso. Siga na ordem, sem pular. Tempo total: umas 2 a 3 horas (a maior parte é só esperar downloads).
> Espaço livre no disco: **pelo menos 30 GB**. Internet: de preferência boa, os downloads são grandes.

## O que vamos fazer (visão geral)

| Parte | O que é | Para quê |
|---|---|---|
| 1 | **Git** | Programa que baixa o projeto e as atualizações dele |
| 2 | **Unity Hub + Unity 6000.6.0f1** | O "programa de fazer jogos" onde o projeto abre e roda |
| 3 | **Módulo Android** (dentro da Unity) | Permite gerar o jogo para celular |
| 4 | **Visual Studio** | Editor onde se lê/escreve o código |
| 5 | **Baixar o projeto** (clone) | Trazer o jogo para o seu PC |
| 6 | **Abrir e rodar no PC** | Testar pela própria Unity |
| 7 | **Celular em modo desenvolvedor** | Deixar o celular aceitar o jogo do PC |
| 8 | **Rodar no celular** | Instalar e abrir o jogo no aparelho |
| 9 | **Problemas comuns** | Se algo der errado |

Uma dica de ouro: **a versão da Unity tem que ser exatamente a do projeto: `6000.6.0f1`**. Versão diferente pode dar erro ou mudar arquivos do projeto.

---

## Parte 1 — Instalar o Git

O Git é o programa que "puxa" o projeto da internet (do GitHub) e guarda o histórico de mudanças.

1. Abra o navegador e entre em **https://git-scm.com/download/win**.
2. O download do "**64-bit Git for Windows Setup**" começa sozinho (se não começar, clique nele).
3. Abra o arquivo baixado (vai estar em *Downloads*). Se o Windows perguntar "Deseja permitir...?", clique **Sim**.
4. Vai aparecer uma janela cheia de telas de opções. **Na dúvida, clique "Next" em todas** (as opções padrão estão ótimas). Só confira estas duas:
   - **"Choosing the default editor used by Git"**: pode deixar o que estiver (ou escolher "Use Visual Studio Code" se você já tiver).
   - **"Adjusting the name of the initial branch"**: deixe "Let Git decide".
5. No final, clique **Install** e depois **Finish**.
6. **Teste se funcionou:**
   - Aperte a tecla **Windows**, digite `cmd` e abra o **Prompt de Comando**.
   - Digite `git --version` e aperte Enter.
   - Se aparecer algo como `git version 2.4x.x`, deu certo. ✅
7. **Apresente-se ao Git** (só precisa fazer uma vez). No mesmo Prompt de Comando, digite uma linha de cada vez, apertando Enter (troque pelo seu nome e e-mail):

```bash
git config --global user.name "Seu Nome"
git config --global user.email "seuemail@exemplo.com"
```

---

## Parte 2 — Instalar a Unity Hub e a Unity 6000.6.0f1

A **Unity Hub** é a "central" que instala e organiza as versões da Unity. A **Unity** em si (o Editor) é instalada por dentro da Hub.

### 2.1 Baixar e instalar a Unity Hub

1. Entre em **https://unity.com/download**.
2. Clique em **Download Unity Hub** (botão para Windows).
3. Abra o arquivo baixado e instale (Avançar / Next até o fim).
4. Abra a **Unity Hub**. Ela vai pedir para **entrar com uma conta Unity**:
   - Se não tiver, clique em **Create account** e crie (é grátis, só e-mail e senha). Confirme o e-mail que eles mandam.
5. Quando pedir uma **licença**, escolha a **Unity Personal** (gratuita). Ela serve para o nosso caso.

### 2.2 Instalar o Editor da Unity (versão certa)

1. Na Hub, no menu da esquerda, clique em **Installs** (Instalações).
2. Clique em **Install Editor** (Instalar Editor).
3. A lista mostra só as versões mais recentes. A nossa pode não aparecer. Então procure o link **"Archive"** / **"Download archive"** na tela (ou entre em **https://unity.com/releases/editor/archive**).
4. No arquivo, procure por **Unity 6** e depois por **6000.6.0f1**. Ao lado dela, clique em **Install** (ou "Install with Unity Hub"). O navegador vai pedir para abrir a Unity Hub: clique em **Abrir**.
5. A Hub mostra uma tela de **módulos** (coisas extras). **É aqui que se instala o Android. Não pule!** (Detalhe na Parte 3.)

---

## Parte 3 — Instalar o módulo Android (dentro da Unity)

Nesta mesma tela de módulos (que aparece logo depois de escolher a versão), marque:

- ✅ **Android Build Support**
  - ✅ **OpenJDK**
  - ✅ **Android SDK & NDK Tools**

Esses três juntos são "tudo do Android": a Unity traz **o Java e as ferramentas do Android dentro dela**. Por isso **você não precisa instalar o Android Studio** (um programa grande e separado). 🎉

Também pode marcar:

- ✅ **Microsoft Visual Studio Community** (se aparecer; ele instala o editor de código junto. Se marcar aqui, pode pular a Parte 4, mas leia lá a nota sobre "cargas de trabalho").

Depois clique em **Install** / **Continue** e **aceite os termos** quando pedir. Espere terminar (pode levar de 15 a 40 minutos).

### Esqueci de marcar o Android. E agora?

Sem problema, dá para adicionar depois:

1. Unity Hub → **Installs**.
2. Na versão **6000.6.0f1**, clique no ícone de **engrenagem** (⚙) → **Add modules** (Adicionar módulos).
3. Marque os três itens do Android acima e instale.

---

## Parte 4 — Instalar o Visual Studio (editor de código) e o .NET

O Visual Studio é onde o código C# do jogo é lido e editado. **Você não precisa mexer no código só para rodar o jogo**, mas é bom ter.

### 4.1 Instalar

Se você **já marcou** o Visual Studio na Parte 3, ele já foi instalado. Vá para o item 4.2 só para conferir.

Se não:

1. Entre em **https://visualstudio.microsoft.com/downloads/**.
2. Em **Community** (a gratuita), clique em **Free download**.
3. Abra o instalador baixado. Ele abre o **Visual Studio Installer**.
4. Na lista, escolha **Visual Studio Community** e clique em **Install**.

### 4.2 Escolher as "cargas de trabalho" (workloads) — a parte mais importante

Na tela com cartões (cargas de trabalho), marque:

- ✅ **Game development with Unity** (*Desenvolvimento de jogos com Unity*)

É **só essa** que importa. Ela já traz junto tudo o que o Visual Studio precisa para o C# e o .NET.

Se o Visual Studio já estava instalado antes: abra o **Visual Studio Installer** pelo menu Iniciar → clique em **Modify** (Modificar) → marque a carga acima → **Modify**.

### 4.3 E a versão do .NET? (a dúvida mais comum)

Resposta simples: **você não precisa instalar o .NET separadamente.**

- A **Unity** traz o próprio ambiente C# dentro dela e não usa o .NET instalado no Windows para rodar o jogo.
- O **Visual Studio** instala sozinho o que precisa quando você marca "Game development with Unity".

Se algum dia alguma ferramenta pedir um .NET separado, instale o **.NET SDK 8** (versão LTS, de suporte longo) em **https://dotnet.microsoft.com/download** — é a escolha segura e não atrapalha em nada.

### 4.4 Dizer à Unity para usar o Visual Studio

1. Abra o projeto (Parte 6) → menu **Edit → Preferences** (*Editar → Preferências*).
2. Clique em **External Tools** (*Ferramentas externas*).
3. Em **External Script Editor**, escolha **Visual Studio** (a versão que você instalou).
4. Feche a janela. Agora, ao dar **duplo clique em qualquer script**, ele abre no Visual Studio.

---

## Parte 5 — Baixar o projeto (o "clone")

"Clonar" = copiar o projeto do GitHub para uma pasta do seu PC, **com todo o histórico**.

O projeto fica em: **https://github.com/brunoscv/miner-idle**

1. Escolha onde guardar. **Dica:** use um caminho **curto e sem acento nem espaço**, como `C:\Jogos`. (Caminhos compridos e com acento às vezes dão problema.)
   - Crie a pasta: abra o Explorador de Arquivos, vá em `C:\`, clique com o botão direito → **Novo → Pasta** → nome `Jogos`.
2. Abra o **Prompt de Comando** (tecla Windows, digite `cmd`).
3. Digite uma linha de cada vez, apertando Enter:

```bash
cd C:\Jogos
git clone https://github.com/brunoscv/miner-idle.git
```

4. Espere terminar (aparece "Receiving objects... 100%"). Vai criar a pasta `C:\Jogos\miner-idle`.
5. Se o Git pedir login do GitHub, uma janela do navegador abre; entre com sua conta GitHub (ou crie uma, é grátis). O projeto é público, então muitas vezes nem pede.

### Para atualizar depois (quando o Bruno publicar novidades)

```bash
cd C:\Jogos\miner-idle
git pull
```

> ⚠️ **Feche a Unity antes do `git pull`**, para ela não se confundir com arquivos mudando.

---

## Parte 6 — Abrir o projeto e rodar pelo PC (pela Unity)

### 6.1 Adicionar o projeto na Hub

1. Abra a **Unity Hub** → menu **Projects** (*Projetos*).
2. Clique em **Add** (*Adicionar*) → **Add project from disk** (*Adicionar projeto do disco*).
3. Navegue até `C:\Jogos\miner-idle` e clique em **Selecionar pasta**.
4. O projeto aparece na lista. **Confira a coluna da versão: tem que mostrar `6000.6.0f1`.**
   - Se mostrar um ⚠️ dizendo que a versão não está instalada, volte à Parte 2.2.

### 6.2 Abrir

1. Clique no nome do projeto. **A primeira abertura demora bastante (10 a 30 minutos)**, pois a Unity precisa "montar" tudo (a pasta `Library`, que não vem no clone). **Isso é normal. Não feche!** Uma barra azul de progresso aparece embaixo/na tela.
2. Se aparecer uma janela de **"Enter Safe Mode?"** (modo de segurança) por causa de erros: clique em **Ignore** (ou "Continue") se for a primeira vez e olhe a aba **Console**; se persistir, vá à Parte 9.
3. Quando terminar, aparece a tela da Unity cheia de painéis.

### 6.3 Abrir a cena do jogo

1. No painel de baixo (**Project**), abra: **Assets → _Project → Scenes**.
2. Dê **duplo clique** em **Game** (o arquivo `Game.unity`).

### 6.4 Apertar o Play

1. No topo, no meio, tem um botão **▶ (Play)**. Clique nele.
2. O jogo roda na aba **Game**. Clique/toque com o mouse como se fosse o dedo.
3. Clique no **▶ de novo** para parar.

> ⚠️ **Regra de ouro:** tudo que você mudar **enquanto o Play está ligado** é **perdido** ao parar. Mexa só com o Play parado.

### 6.5 Deixar a tela no formato de celular

O jogo é feito para tela em pé (vertical). Na aba **Game**, tem um menu de proporção (escrito "Free Aspect" ou "Full HD"). Clique nele e escolha uma proporção vertical (como **9:16 Portrait**). Assim a tela fica parecida com a do celular.

---

## Parte 7 — Colocar o celular em modo desenvolvedor

Por padrão o celular **não aceita** instalar jogos que vêm do PC. Precisamos liberar isso. **É seguro**, é um modo normal do Android.

> Os nomes dos menus mudam um pouco de marca para marca (Motorola, Samsung, Xiaomi...). Abaixo, o caminho mais comum.

### 7.1 Ligar o "Modo desenvolvedor"

1. No celular, abra **Configurações** (*Settings*).
2. Vá até o final e entre em **Sobre o telefone** (*About phone*).
3. Procure **Número da versão** (*Build number*). Em alguns aparelhos fica dentro de "Informações de software".
4. **Toque 7 vezes seguidas** nele. Na 4ª ou 5ª vez aparece "Faltam X passos para ser desenvolvedor".
5. Se pedir, digite a **senha/PIN** do celular.
6. Vai aparecer "**Você agora é um desenvolvedor!**" 🎉

### 7.2 Ligar a "Depuração USB"

1. Volte às **Configurações** → **Sistema** → **Opções do desenvolvedor** (em alguns aparelhos fica direto em Configurações).
2. Ligue a chave **Opções do desenvolvedor** (no topo) se estiver desligada.
3. Role até **Depuração USB** (*USB debugging*) e **ligue**. Confirme em **OK**.

### 7.3 Conectar ao PC

1. Use um **cabo USB que transfira dados** (alguns cabos baratos só carregam e **não funcionam**!). O cabo que veio com o celular é o mais seguro.
2. Conecte o celular ao PC.
3. No celular, vai aparecer "**Usar USB para...**": escolha **Transferência de arquivos** (*File transfer / MTP*).
4. **Muito importante:** vai aparecer uma janela "**Permitir depuração USB?**" mostrando uma sequência de números. Marque **"Sempre permitir neste computador"** e toque em **Permitir**.
   - Se a janela **não** aparecer: desconecte e conecte de novo; ou em *Opções do desenvolvedor* use **Revogar autorizações de depuração USB** e reconecte.

### 7.4 Conferir se o PC "viu" o celular (opcional, mas ajuda muito)

No Prompt de Comando, rode (é o `adb` que a própria Unity instalou, então basta copiar tudo):

```bash
"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" devices
```

Resultado esperado:

```
List of devices attached
ABC123456789    device
```

- Aparece **`device`** ao lado de um código → ✅ perfeito.
- Aparece **`unauthorized`** → o celular não foi autorizado (volte ao passo 7.3 item 4).
- A lista vem **vazia** → problema de cabo/driver (veja a Parte 9).

---

## Parte 8 — Rodar o jogo no celular

O projeto já tem um menu pronto que **gera o jogo, instala e abre no celular de uma vez só**.

### 8.1 Antes de tudo (uma vez só)

1. Com o projeto aberto na Unity e **o celular conectado e autorizado** (Parte 7).
2. Menu **File → Build Profiles** (*Arquivo → Perfis de Build*).
3. Na lista da esquerda, clique em **Android**. Se aparecer o botão **Switch Platform** (*Trocar plataforma*), clique nele e espere (alguns minutos). Isso diz à Unity "agora vamos fazer para Android".
4. Feche essa janela.

> Se o Android **não aparecer na lista**: o módulo da Parte 3 não foi instalado. Volte lá.

### 8.2 Rodar no celular (o jeito fácil)

1. No menu de cima da Unity: **Tools → Miners Idle → Build → Rodar no celular (Android)**.
2. A Unity vai: aplicar as configurações do projeto → gerar o app (APK) → instalar no celular → abrir.
3. **A primeira vez demora MUITO (15 a 40 minutos)**, pois baixa e prepara várias coisas (Gradle, IL2CPP). As próximas vezes são bem mais rápidas (1 a 5 minutos). Uma barra de progresso aparece. **Não mexa no cabo!**
4. Quando terminar, o jogo **abre sozinho no celular**. ✅

### 8.3 O jeito "padrão" da Unity (alternativa)

Se o menu acima não aparecer por algum motivo:

1. **File → Build Profiles → Android**.
2. Em **Run Device**, escolha o seu celular (se não aparecer, clique em *Refresh*).
3. Clique em **Build And Run** (*Gerar e Rodar*).
4. Se pedir um **nome para o arquivo**, escolha qualquer um (ex.: `miners.apk`) e salve numa pasta fora do projeto, como `C:\Jogos\builds`.

### 8.4 Sobre a versão mínima do Android

O jogo precisa de **Android 8.0 ou mais novo**. Quase todo celular dos últimos anos serve.

### 8.5 Depois que já instalou

- O jogo fica instalado como qualquer app (ícone no celular). Dá para **abrir sem o cabo**.
- Para testar uma **nova versão**, repita o 8.2: ele **substitui** o app antigo (mantendo o seu progresso salvo).

---

## Parte 9 — Problemas comuns (e como resolver)

| Problema | O que fazer |
|---|---|
| **A Unity não abre o projeto / pede outra versão** | Confira que instalou a **6000.6.0f1** exatamente (Parte 2.2). Não aceite "atualizar" o projeto para outra versão. |
| **"Safe Mode" ao abrir** | Abra a aba **Console** (Window → General → Console), veja os erros em vermelho. Se for o primeiro abrir, feche a Unity, apague a pasta `Library` do projeto e abra de novo (ela se refaz). |
| **Janela "Android SDK/NDK/JDK not found"** | O módulo Android não está instalado. Hub → Installs → ⚙ → Add modules (Parte 3). Depois, **Edit → Preferences → External Tools** e deixe marcadas as opções de **"Installed with Unity (recommended)"**. |
| **O celular não aparece em *devices*** | 1) Troque o cabo. 2) Troque a porta USB. 3) Confirme "Depuração USB" ligada. 4) Mude o modo USB do celular para "Transferência de arquivos". 5) Reinicie o celular. 6) Em alguns aparelhos antigos é preciso instalar o driver USB do fabricante. |
| **`unauthorized`** | Olhe a tela do celular e toque em **Permitir** na janela de depuração. |
| **Erro de build falando em "Gradle"** | Quase sempre é internet ruim ou antivírus bloqueando. Tente de novo; desligue VPN; garanta que a pasta do projeto tenha caminho curto (`C:\Jogos`). |
| **Disco cheio durante o build** | Libere espaço (precisa de alguns GB livres). |
| **Aparece erro vermelho "error CS..."** | É erro no código. Confirme que deu `git pull` por completo e que está na versão correta da Unity. Se persistir, mande um print do Console para o Bruno. |
| **O jogo trava ao abrir no celular** | Conecte o cabo e, no Prompt, rode `adb logcat -s Unity` (usando o caminho completo do `adb` da Parte 7.4) e mande as últimas linhas. |
| **Primeiro build "parado" há muito tempo** | Normal: a primeira vez parece congelada, mas está trabalhando. Espere pelo menos 30 minutos antes de se preocupar. |

---

## Resumo rápido (cola de bolso)

1. Instale o **Git** → `git --version`.
2. Instale a **Unity Hub**, entre na conta, licença **Personal**.
3. Instale a Unity **6000.6.0f1** com **Android Build Support + OpenJDK + Android SDK & NDK Tools**.
4. Instale o **Visual Studio Community** com a carga **Game development with Unity** (não precisa de .NET separado).
5. `git clone https://github.com/brunoscv/miner-idle.git` em `C:\Jogos`.
6. Hub → **Add project from disk** → abra → cena **Assets/_Project/Scenes/Game** → **▶ Play**.
7. Celular: toque 7× em **Número da versão** → ligue **Depuração USB** → conecte e **Permita**.
8. **File → Build Profiles → Android → Switch Platform**.
9. **Tools → Miners Idle → Build → Rodar no celular (Android)**.

Dúvidas ou erros? Anote **o que você estava fazendo** e tire um **print da tela de erro** (ou do Console da Unity) para facilitar a ajuda.
