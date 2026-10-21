# Decisões, questões em aberto e riscos

> Registro vivo iniciado em 2026-10-20. Convenção herdada do Underworld Towers: cada decisão tem **opções, uma recomendação e o marco que ela bloqueia**; quando o
> Bruno decide, a linha vai para a tabela "Decididas", **com a data**. Valores de regra não mudam sem decisão registrada aqui.

## 1. Já decididas (documentadas nos arquivos originais)

| # | Fonte | Decisão |
|---|---|---|
| DEC-01 | GDD, RM | Mobile, **vertical**, Android primeiro, uma tela principal |
| DEC-02 | RM §2 | Unity LTS + C# + URP 2D; Unity UI + TextMeshPro; ScriptableObjects; JSON versionado em `persistentDataPath`; Unity Test Framework; Git + Git LFS; AudioSource + AudioMixer; `double` para números |
| DEC-03 | GDD | **Uma moeda** (lasca); 10 cavernas; 5 cristais por lote; mineração automática + toque (10%); crítico 5%×2; combo; penalidade após 100 quebras (−50%); boost AD ×2,5/30 s (simulado); estagiário; Letrado libera a passagem; Picareta Dupla |
| DEC-04 | GDD §12 | Fora do MVP: prestígio, dinheiro real/anúncios reais, multiplayer/rankings/missões diárias/eventos, inventário/múltiplas moedas, narrativa/chefes, servidor/nuvem |
| DEC-05 | GDD §8 | Pixel art; paleta-âncora; anão frontal, bípede, barba; picareta de madeira/aço |
| DEC-06 | RM §5 | Arte de IA é **matéria-prima**; licença e origem de todo asset são registradas; Point filter |
| DEC-07 | RM §7.1 | **Não reduzir:** testes de fórmulas, persistência, matriz do estagiário, penalidade, progressão até a cave 10, mineração durante loja/modal, testes em aparelho. **Reduzir primeiro:** animações extra, partículas, fundos únicos, brilho de 20 níveis, sons extra, transições |
| DEC-08 | GDD §13 | Balanceamento só muda após telemetria/playtest, com uma versão-base para comparação **(entra em conflito com D-01)** |
| DEC-09 | Repositório | Documentos e conversa em **pt-BR** (padrão do Bruno nos outros projetos) |

### 1.1 Decididas pelo Bruno em 2026-10-21

| # | Decisão |
|---|---|
| **D-01** | **Não recalibrar a economia agora.** O jogo **não precisa chegar à caverna 10 neste momento**; as conquistas/progressão serão balanceadas ao longo do desenvolvimento. O achado C-01 (fórmulas do GDD não fecham) fica **registrado como observação**, não como bloqueio. Mantêm-se as fórmulas do GDD v1.0 como valores iniciais em `BalanceConfig`; o simulador do M2 continua útil para medir, mas não condiciona o avanço dos marcos. |
| **D-05** | **Estilo = o do mockup** (fantasia, mina de madeira), exatamente como em `Designer (2).png`. O texto "industrial/sci-fi" do GDD §8.1 fica **superado** nesse ponto; as cores funcionais (âmbar = ação, vermelho = alerta) e a paleta de cristais permanecem. |
| **D-12** | **Criar o projeto Unity** na 6000.6.0f1 + instalar Android Build Support (recomendação aceita). *A decisão está tomada; a criação em si só começa quando o Bruno autorizar a próxima fase.* |
| **D-02 / D-03** | **Só pt-BR no MVP** (textos em dados) e nome **Miners Idle**. |
| **D-20** | Aparelho de teste: **Motorola Moto G75**. **Não há ambiente Android instalado** no PC (SDK/módulo do Unity/depuração USB a configurar no M1). |
| **D-17** | **~20 horas por semana**; o Bruno é o desenvolvedor (e o responsável pelo produto). |
| **D-18** | O jogo será **comercializado e terá anúncios reais** (antes tratado como pós-MVP). O **MVP continua com AD simulado**; a integração de rede de anúncios entra como marco próprio antes da publicação (ver [Roadmap](Roadmap.md) M12). |
| **D-21** | **Anúncios fora do MVP.** O time ainda vai definir como serão os ads. Por ora o botão AD só dá o **boost de velocidade** (×2,5 por 30 s), **sem integração de rede de anúncios**. M12 deixa de incluir o SDK de anúncios (volta a ser só publicação); a integração fica como item **pós-MVP** quando os ads forem definidos (D-18 continua: o jogo será comercializado e deverá ter ads no futuro). O código mantém o boost atrás de uma interface para trocar depois. |
| **D-23** | **PixelLab:** aprovado seguir com o teste do anão (verificação de créditos e termos comerciais continua no M1b). |
| **D-24** | **Moto G75 é o aparelho-alvo mínimo de desempenho.** |
| **D-25** | **M1 aprovado** (fundação técnica; APK rodando no Moto G75 a 60 fps). |
| **D-26** | **Mineração no mapa.** O anão **anda** por um mapa visto de cima e minera **grupos de cristais** que **nascem continuamente** (até um máximo na tela). Substitui o lote fixo de 5 cristais com o minerador parado acima do alvo (REQ-LOOP-02 e REQ-LOOP-03 ficam **superados**). Cada grupo de cristais é **um alvo** (uma vida, uma recompensa). Referência de layout da tela: [Referencia de tela - mapa.jpg](../design/Referencia%20de%20tela%20-%20mapa.jpg) (HUD de 4 indicadores em cima, Loja/AD à direita, "Passar de caverna" embaixo). Cada caverna terá um **bioma** (gelo, fogo, lava, pântano…); o detalhe visual fica para o Art Spike. Os elementos do print sem regra definida (barras laterais, timers, "Modifiers", botões subir/descer/Auto) **não** estão decididos. |
| **D-27** | **Regra de prioridade de alvo: "lote, depois mais próximo".** Cada cristal nasce com um **número de lote**: os que estão na tela no início são o lote 1; cada nascimento posterior recebe o próximo número. O anão escolhe **o cristal de menor lote; entre os do mesmo lote, o mais próximo dele** no momento da escolha (empate exato: o que nasceu primeiro). O alvo **não muda** até quebrar. Sem cristal na tela, o anão fica parado onde está. Efeito: os cristais iniciais são quebrados de vizinho em vizinho; depois a fila segue a ordem de nascimento, mesmo que um cristal novo surja ao lado do anão. |
| **D-28** | **Roadmap reordenado: jogável antes do núcleo de economia.** O novo **M2 é o "Protótipo do loop no mapa"** (graybox jogável: andar, minerar, quebrar, juntar lascas, HUD e debug), com só as regras que esse loop pede. O antigo M2 (calculadores de melhoria, `BalanceConfig` v1, simulador econômico, spec do estagiário) passa a ser o **M3 "Regras e economia"**, adaptado ao mapa. O antigo M3 (vertical slice) fica absorvido pelo M2 e pelo M3. M4 em diante não muda de número. Referências a "M2" como marco de economia/simulador em documentos anteriores a esta data valem para o **M3**. |
| **D-29** | **Valores de protótipo no M2.** O M2 usa um conjunto de valores marcado **"protótipo"** (vida, recompensa, dano, intervalo, velocidade de caminhada, nascimento de cristais), num ScriptableObject separado. Esses números servem para sentir o loop, **não são balanceamento**, e podem ser ajustados durante o M2 sem nova decisão. Os valores de regra do GDD v1 (D-01) continuam valendo como referência do M3. |
| **Ferramenta de arte** | Testar o **PixelLab** (IA que gera pixel art e animações por MCP) para o anão; viabilidade de créditos gratuitos **a confirmar** (ver [Arte](../design/Direcao%20de%20Arte%20e%20Assets.md) §5). |

## 2. Aguardando aprovação do Bruno (ordenadas pelo que bloqueiam)

Legenda: **Bloq.** = bloqueia a partir de qual marco · **Rec.** = recomendação.

> **Atualização 2026-10-21 (tarde):** D-25…D-29 decididas (M1 aprovado, mineração no mapa, regra de alvo, roadmap reordenado, valores de protótipo); D-30 e D-31 abertas em §2.3.
>
> **Atualização 2026-10-21:** D-01, D-02, D-03, D-05, D-12, D-17, D-18 e D-20 foram **decididas** (tabela §1.1). A tabela abaixo é o histórico das perguntas; restam abertas as D-04, D-06…D-11, D-13…D-16 e D-19.

### 2.1 Bloqueantes (decidir antes de M1/M2) — *todas respondidas em 2026-10-21*

| # | Pergunta | Opções | Rec. | Bloq. |
|---|---|---|---|---|
| **D-01** | **Economia:** as fórmulas do GDD v1.0 não fecham (C-01). Aceitar a **recalibração orientada a metas de ritmo** (Economia §4) mantendo a *intenção* do GDD e guardando o GDD v1 como `Balance_GDD_v1` para comparação? | (a) Recalibrar [rec.] · (b) Manter literal e aceitar o jogo inalcançável · (c) Reescrever as regras do Letrado/penalidade | **(a)** | M2 |
| **D-02** | Idioma do jogo e dos documentos | pt-BR só (UI e docs) · pt-BR + en na UI desde já · outro | **pt-BR só para o MVP**, com **todos os textos em dados** para en depois | M1 |
| **D-03** | Nome do produto (e do pacote Android) | **Miners Idle** (repositório) · Idle Miner (documentos) · outro | **Miners Idle** (a confirmar disponibilidade na Play Store) | M1 |
| **D-05** | **Direção visual:** texto do GDD (industrial/sci-fi) × mockup (fantasia/mina de madeira) | (a) **Mockup** (fantasia de mina) com funções de cor do texto [rec.] · (b) Industrial puro · (c) Híbrido (madeira + metal de segurança) | **(a)** | M1b |
| **D-12** | **Criar o projeto Unity** em `miners-idle`: versão (**6000.6.0f1**, a dos outros jogos; confirmar se é LTS), instalar **Android Build Support**, template 2D URP | (a) 6000.6.0f1 + módulo Android [rec.] · (b) LTS mais recente | **(a)** | M1 |
| **D-20** | **Aparelho Android** de teste (modelo, versão) e depuração USB | — | Obter antes do M1 | M1 |

### 2.2 Importantes (decidir até o marco indicado)

| # | Pergunta | Opções / Rec. | Bloq. |
|---|---|---|---|
| D-04 | **Progressão offline** no MVP? | Sim, analítica: teto 2–4 h, 50% (Economia §6) **[rec.]** · Não, só pós-MVP | M8 |
| D-06 | **Linhas de melhoria:** 6 (sem *Crítico*) ou 7? "Letrado" ou "Pesquisador"? | **6 linhas, nome "Letrado"**, crítico fixo 5% **[rec.]**; ou adicionar *Crítico* (custo/limite a definir) | M2 |
| D-07 | **"Quebrar a parede"** para passar de caverna, ou só o botão? | **Botão + animação de parede quebrando como efeito** (sem vida/estado) **[rec.]** · Parede com vida (novo sistema) · Só botão | M7 |
| D-08 | **Regras do toque:** área (qualquer ponto × só o cristal), taxa máxima, crítico no toque? | **Qualquer ponto**, sem limite rígido, **crítico vale no toque**, tremor reduzido em alta taxa **[rec.]**; decidir após ver o simulador | M3 |
| D-09 | **Boost:** o mínimo de 0,5 s vale com boost? Recarga igual à duração é intencional? | **Boost pode passar do mínimo (piso 0,2 s)**; recarga **60 s** após o fim **[rec.]** (ou manter 30 s se for proposital) | M8 |
| D-10 | **Ferramenta de pixel art** e origem da arte | Aseprite (pago, ~US$ 20 [verificar]) **[rec.]** · LibreSprite/Pixelorama (grátis) · só IA+retoque; **fallback**: pacotes CC0 de ambiente | M1b |
| D-11 | Formato numérico (`1.234,00` pt-BR × `1,234.00` do mockup) e abreviações | **pt-BR** com abreviações K/M/B/T acima de 1 M **[rec.]** | M4 |
| D-13 | **UI:** uGUI+TMP [DOC] × UI Toolkit (usado no Underworld Towers) | **uGUI+TMP** **[rec.]** (mundo em pixel art, números flutuantes) | M4 |
| D-14 | **Resolução de referência e PPU** | **270×480, ×4, PPU 16** **[rec., a validar no Art Spike]**; plano B 360×640 ×3 | M1b |
| D-15 | **Rastreador de tarefas** | Checklist nos planos + GitHub Issues/Projects (mostrar a lista antes de criar) **[rec.]** · só docs | M1 |
| D-16 | **Git LFS e ramos:** LFS só para áudio/binários grandes (não para PNG pixel art, cenas ou prefabs); *main* + ramos curtos | **[rec.]** como em Qualidade §2–3 | M1 |
| D-17 | **Horas por semana** disponíveis e calendário | Informar; converte as faixas do Roadmap em semanas | M2 |
| D-19 | **Justificativa da penalidade** (−50% após 100 quebras): "anti-grind/empurrar ao Letrado" × "pena ao jogador parado" | Documentar a intenção e ajustar o limiar relativo ao *K* (Economia §4) **[rec.]** | M2 |

### 2.3 Novas perguntas geradas pelas respostas

| # | Pergunta | Rec. | Bloq. |
|---|---|---|---|
| ~~D-21~~ | ~~Rede de anúncios real~~ → **decidido (2026-10-21): fora do MVP**, só o boost de velocidade (ver §1.1) | — | — |
| **D-22** | **Conformidade de anúncios** (quando houver ads): política de privacidade, consentimento, classificação etária. A política de privacidade e a conta do Google Play ainda são necessárias para publicar. | Planejar no M12; verificar regras vigentes na época | M12 |
| ~~D-23~~ | ~~PixelLab~~ → **aprovado** o teste; falta confirmar créditos grátis e **termos de uso comercial** no M1b | — | M1b |
| ~~D-24~~ | ~~Moto G75 aparelho-alvo~~ → **decidido** | — | — |
| **D-30** | **"Contar picaretas"** (pedido de 2026-10-21): é um **contador de golpes** (estatística) ou um **segundo recurso**? | **Contador de golpes**, exibido no painel de debug no M2; um segundo recurso contraria a "uma moeda" (DEC-03) e pediria decisão própria **[rec.]** | M2 (não bloqueia o início) |
| **D-31** | **Caminho do anão:** linha reta até o alvo (cristais não bloqueiam) ou desviar de cristais/paredes? | **Linha reta no M2** **[rec.]**; desvio só se o mapa ganhar obstáculos | M2 |

### 2.4 Para depois do MVP
Prestígio ("Nova Descida") · música · iOS · idiomas adicionais · missões.

## 3. Questões abertas rápidas (para responder quando convier)

1. ~~`Designer (2).png`~~ **encontrado em `docs/`** (é o mockup do GDD). O `GDD.txt` original continua **não localizado**; se existir, pode trazer detalhes que o GDD consolidado omitiu.
2. ~~Quem é o desenvolvedor?~~ **Bruno** (também o responsável pelo produto).
3. ~~Publicar/monetizar?~~ **Sim**, com anúncios reais (D-18). Consequência: **licenças de assets e termos do PixelLab precisam permitir uso comercial**; evitar CC-BY-NC/SA.
4. ~~AD simulado?~~ **Simulado no MVP; real antes da publicação** (M12).
5. ~~Tema musical~~: sem preferência por enquanto (música segue pós-MVP).
6. ~~Jogos de referência de ritmo~~: sem preferência por enquanto; as metas de ritmo ficam para o balanceamento ao longo do desenvolvimento.

## 4. Registro de riscos

Probabilidade/Impacto: B(aixo) · M(édio) · A(lto). Dono: Bruno, salvo indicação.

| ID | Risco | Prob. | Imp. | Mitigação | Gatilho / acompanhamento |
|---|---|---|---|---|---|
| R-01 | **Economia do GDD v1 não fecha** (C-01) → progressão travada nas primeiras cavernas. **Rebaixado em 2026-10-21**: aceito como observação; balanceamento ao longo do desenvolvimento (D-01) | A (confirmado) | **M** (não bloqueia marcos) | Medir no simulador (M2) e **revisitar antes do playtest do M10**; não deixar o jogo "inalcançável" chegar a ser lançado | Cave 2 > ~1 h de jogo ativo |
| R-02 | Calibração demora várias rodadas | M | M | Metas de ritmo primeiro; 3 perfis de jogador; `Balance_v2.x` versionado | M2 passa de ~55 h |
| R-03 | Ambiguidade do estagiário gera estados travados | A | A | Spec escrita (M2) + matriz de testes (M6) | Qualquer P0 do estagiário |
| R-04 | **Arte de IA inconsistente** / pixel de tamanhos mistos | A | A | Art Spike; redesenhar à mão sobre a referência; paleta indexada; auditoria | Folha de estilo reprovada no aparelho |
| R-05 | Custo de arte (10 cavernas × estados) estoura | M | M | Estrutura reutilizável + troca de rampa; cortes de RM §7.1; pacotes CC0 de ambiente | M9 > 90 h |
| R-06 | Falta de ferramenta/versão (módulo Android, JDK, Aseprite) | M | M | Instalar no M1; checar no Hub; plano B de ferramenta grátis | APK não gera em M1 |
| R-07 | ~~Falta de aparelho Android~~ → **Moto G75 disponível**; falta o **ambiente Android** (módulo Unity, SDK, depuração USB, driver) | B | M | Configurar no M1 (T-M1-01/08) | APK não instala no Moto G75 |
| R-08 | Horas: o RM supõe 40 h/sem; o Bruno tem **~20 h/sem** (D-17) → o calendário **dobra** (≈ 22–33 semanas, 5–8 meses, para 450–660 h) | A | M | Não prometer datas; reestimar após M1 e M3; cortar escopo pelo RM §7.1 | M3 > 55 h |
| R-21 | **PixelLab:** créditos grátis insuficientes, inconsistência entre quadros, termos sem uso comercial (jogo será vendido) | M | A | D-23: testar 1 personagem; ler os termos; retoque manual em editor de pixel; plano B de arte (Art doc §5) | Anão sem consistência em 4+ quadros |
| R-22 | **Anúncios reais + venda**: políticas de loja, privacidade/LGPD, consentimento, taxas de conta; rejeição na publicação | M | A | M12 dedicado; D-21/D-22; manter anúncio só premiado e opcional | Início do M12 |
| R-09 | Overengineering (DI, event bus, ECS) | B | M | Lista "fora de escopo" (Arquitetura §5); regra "jogável antes de sistema" | Revisão por marco |
| R-10 | Tap domina cedo e some tarde → percepção de "jogo se joga sozinho" | M | M | D-08; simular perfis; ensinar automação por feedback | Playtest M5/M10 |
| R-11 | Penalidade percebida como punição injusta | M | M | Ícone + aviso + explicação (S10); revisar limiar | Playtester não entende a seta |
| R-12 | Números grandes ilegíveis em tela estreita | M | B | Formatador único (K/M/B/T), testes de largura | Texto transborda no aparelho |
| R-13 | Perda de save / duplicação de moeda | B | **A (P0)** | Escrita atômica, backup, migrações testadas; idempotência da compra | Qualquer ocorrência |
| R-14 | Relógio do aparelho manipulado (boost/offline) | M | B | UTC + "não ganhar se o relógio voltou"; teto offline | Playtester abusa |
| R-15 | Licença de asset mal registrada (CC-BY-NC, EULA) | M | A | `CREDITS.md` desde o 1º asset; evitar CC-BY-NC/SA; registrar *prompt*/ferramenta/data da IA | Asset sem linha na tabela |
| R-16 | Keystore/senha vazados ou perdidos | B | **A** | Nunca no git; backup offline do keystore | Antes do M12 |
| R-17 | Git LFS mal configurado trava o repositório | B | M | Escopo mínimo do LFS (D-16); testar clone limpo | Falha no `git push` |
| R-18 | Requisitos de loja/política mudam até M12 | M | B | Checar no momento; M12 é separado do MVP | Início do M12 |
| R-19 | Jogo "parece genérico" no mercado de idle mining | M | M | Investir em *game feel* e arte (M5), identidade da descida, polimento | Playtest sem "uau" |
| R-20 | Dois outros jogos em andamento disputam as horas do Bruno | A | M | Trabalhar 1 marco por vez; estimar com horas reais (D-17) | Marco atrasado 2× |
| R-23 | **Tempo de caminhada** (D-26/D-27) vira parte da economia: andar longe pela ordem do lote reduz lascas/min e pode parecer "burro" ao jogador | M | M | Caminhada no núcleo (medível no simulador); velocidade de caminhada como valor ajustável; avaliar o *feel* no M2 com o debug de ordem ligado | Bruno acha o vaivém irritante no M2 |
| R-24 | Mapa com nascimento contínuo + estagiário (M6) aumenta a chance de alvo duplicado ou espera infinita | M | A | Seletor de alvo isolado e testado desde o M2; reserva de alvo entra junto com o estagiário | Qualquer P0 do estagiário |

## 5. Premissas a validar

Lista em [Descoberta](Descoberta%20-%20Relatorio.md) §9. As três mais críticas: **D-01** (a economia pode ser recalibrada), **D-05** (o mockup manda), **D-17** (horas/semana).
