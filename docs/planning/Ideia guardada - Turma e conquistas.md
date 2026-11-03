# Ideia guardada — Turma de amigos, recados e conquistas

> **Status: FORA DO ESCOPO E DO PLANEJAMENTO.** Registrada em 2026-11-02 a pedido do Bruno só para a ideia não se perder.
> Não entra no [Roadmap](Roadmap.md), não ganha marco, não gera código nem decisão `D-*` enquanto o Bruno não disser o contrário.
> Quando o Bruno perguntar "o que falta fazer no jogo", ela pode ser **lembrada** como ideia guardada — nunca como tarefa pendente.

## 1. A ideia (nas palavras do Bruno)

Um servidor, mesmo que caseiro, para os jogadores conversarem entre si de um jeito simples:

- **Recados prontos para os amigos** (sem texto livre), por exemplo:
  - "Ei, o Nerlock tá dizendo que tu é fraco."
  - "Ei, o Nerlock conseguiu pegar a picareta no nível 4."
  - "Ei, o Nerlock tá na caverna 85, e você? Vamo animar aí?"
- **Turma de amigos** dentro do jogo, trocando conquistas.
- **Conquistas** ganhas jogando, por exemplo:
  - chegou na caverna 10;
  - quebrou 2.323 cristais;
  - renasceu 21 vezes.
- As conquistas ficam **à mostra em algum lugar do jogo**.
- Quando alguém da turma ganha uma conquista, **os outros veem na hora, na tela**: se dois amigos estão jogando e um chega na caverna 80, aparece uma mensagem para o outro.

## 2. Como ficaria no jogo

| Peça | O que é | Onde aparece |
|---|---|---|
| **Conquistas** | Metas com limiar: caverna alcançada, cristais quebrados, renascimentos (runs do GDD v6), item obtido em tal nível | Tela "Conquistas" (ícone na coluna LOJA/AD/AJUSTES) com as ganhas e as próximas |
| **Turma** | Grupo pequeno (até ~10) que se forma por **código de convite** de 6 letras ou link | Tela "Turma": membros, caverna atual de cada um, mural |
| **Mural da turma** | Lista do que a turma conquistou e dos recados recebidos | Dentro da tela "Turma" |
| **Aviso na hora** | Faixa curta no topo ("Nerlock chegou na caverna 80!") que some sozinha e **não interrompe o golpe** nem rouba toque | Por cima do jogo, fila de no máximo 1 aviso por vez |
| **Recados prontos** | Modelos com lacunas (`{amigo}`, `{caverna}`, `{nível}`) preenchidas com dados reais do jogador | Botão "Mandar recado" no perfil do amigo |
| **Notificação** (depois) | Push no celular quando o jogo está fechado | Sistema do Android |

**Por que recados prontos e não chat livre:** sem texto livre não há o que moderar (palavrão, assédio), a Play Store não exige denúncia/bloqueio de conteúdo de usuário, e o tom fica sempre de brincadeira entre amigos. Também dá para traduzir depois, porque o texto mora em dados.

## 3. Como encaixa na arquitetura atual

Segue as regras do [CLAUDE.md](../../CLAUDE.md) e da [Arquitetura Técnica](Arquitetura%20Tecnica.md):

- **Conquistas nascem no Núcleo** (`MinersIdle.Core`, C# puro): um `AchievementTracker` escuta os eventos que o núcleo já emite (cristal quebrado, caverna nova, renascimento) e dispara `AchievementUnlocked`. Definições em ScriptableObject (`AchievementDefinition`: id, tipo, limiar, texto pt-BR); progresso em classe simples, salvo junto com o resto.
- **Conquistas funcionam sem servidor.** O jogo continua 100 % offline; o social é uma camada a mais.
- **Camada social separada** (`MinersIdle.Social`, depende só do Core): interface `ISocialService` com `PublishAchievement`, `SendPreset`, `JoinGroup` e o evento `MessageReceived`. A apresentação só escuta esse evento para mostrar a faixa.
- **Fila de saída local:** sem internet, o que tinha de ser enviado espera e vai quando a conexão voltar. Servidor fora do ar nunca trava nem atrasa a mineração.
- **Trocar o servidor sem mexer no jogo:** como tudo passa por `ISocialService`, dá para começar com um tipo de servidor e mudar depois.

## 4. O servidor — duas opções

| | **A. Serviço pronto (Firebase)** | **B. Caseiro (C# no PC ou Raspberry Pi)** |
|---|---|---|
| Como | Firestore (banco com escuta em tempo real) + login anônimo + Cloud Messaging para push | ASP.NET Core (API mínima) + SignalR (tempo real) + SQLite, exposto com Cloudflare Tunnel (HTTPS sem abrir porta no roteador) |
| Custo | Grátis para um grupo de amigos (cota gratuita sobra) | Grátis, mas a máquina fica ligada 24 h |
| "Na hora" | Sim, o banco avisa o celular quando muda | Sim, via SignalR |
| Manutenção | Quase zero; o Firebase já está no plano dos builds de teste (D-84) | Atualizar, fazer backup, ficar no ar; se o PC desliga, a turma fica sem avisos |
| Vantagem | Confiável e rápido de montar | Mesma linguagem do jogo (dá para dividir os DTOs), controle total, aprendizado |

**Recomendação (quando a ideia sair da gaveta):** começar pela **A**, porque o jogo já vai usar Firebase e ninguém precisa manter máquina ligada. A **B** é ótima se o objetivo for aprender a montar servidor; a interface `ISocialService` deixa trocar de uma para a outra.

### Dados no servidor (qualquer opção)

- `jogadores`: id anônimo, apelido, caverna atual, resumo das estatísticas, visto por último.
- `turmas`: código de convite, membros.
- `eventos`: turma, tipo (conquista/recado), quem, dados, horário — apagados depois de ~30 dias.

## 5. Fases (cada uma termina com algo que roda)

1. **Conquistas locais** — tracker no núcleo, tela "Conquistas", faixa ao ganhar. Nenhum servidor. Atalho no DBG para liberar/zerar conquistas.
2. **Turma** — apelido, criar/entrar por código, ver membros e a caverna de cada um.
3. **Aviso na hora** — conquista de um membro aparece como faixa para quem está jogando; mural guarda o histórico.
4. **Recados prontos** — mandar e receber os modelos com lacunas.
5. **Notificação push** — avisar quem está com o jogo fechado (opcional, com limite para não virar spam).

Teste: dois celulares (o Moto G75 e outro) na mesma turma; DBG com botão "simular amigo" para testar com um aparelho só.

## 6. Riscos e cuidados

- **Privacidade (LGPD) e Play Store:** guardar só apelido e id anônimo, nada de nome real, e-mail ou contatos; atualizar o formulário de "segurança dos dados" da loja.
- **Trapaça:** o celular é quem diz o que conquistou, então dá para falsificar. Entre amigos não importa; ranking público exigiria validação no servidor (fora desta ideia).
- **Perder a conta:** login anônimo some se o jogo for desinstalado. Precisa de um "código de recuperação" ou login Google Play mais tarde.
- **Spam:** limitar recados por amigo por hora.
- **Escopo:** é uma camada inteira (telas, servidor, contas). Só vale depois de o jogo em si estar divertido e publicado — por isso fica guardada.
