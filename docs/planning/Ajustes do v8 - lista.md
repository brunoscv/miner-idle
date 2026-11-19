# Ajustes do v8 — lista

> Criada em 2026-11-18 pela **D-298**. Durante a implementação dos blocos do GDD v8, as melhorias notadas nos testes não interrompem o marco da vez: entram aqui e viram o **marco de ajustes**, no fim, depois de todos os blocos e antes dos testes longos.

## Como usar

- Uma linha por ajuste, com a data, quem sugeriu (Bruno ou Claude), onde aparece no jogo e o que muda.
- Ajuste que mexe em **valor de regra** (fórmula, custo, tempo, balanceamento) precisa de decisão registrada antes de entrar, como sempre.
- Erro que quebra o jogo **não** espera: é corrigido no marco da vez.
- No começo do marco de ajustes, a lista é ordenada por prioridade com o Bruno e vira as etapas do plano.

## Lista

| # | Data | De | Onde | Ajuste | Estado |
|---|---|---|---|---|---|
| 1 | 2026-11-18 | Claude | Bomba (M9g) | Arte própria do tier 2 (Dinamite, Pacote, Caixa, Barril); até lá, o `icon_bomb` recolorido e maior (D-315) | espera o desenho |
| 2 | 2026-11-18 | Claude | Câmera de 60° (M9h) | Os esteios somem de uma vez 0,6 m atrás do anão: na câmera nova o arco some no meio da tela. Sumir aos poucos (transparência) ou só deixar passar os postes | M9i E6 |
| 3 | 2026-11-18 | Claude | Câmera de 60° (M9h) | A metade de cima da tela mostra ~9 m de corredor atrás do anão, já minerado e vazio. Algo ali: marca das quebras, cacos, poeira | M9i E6 |
| 4 | 2026-11-18 | Claude | Transição de andar (M9h, D-327) | Os cantos de cima escurecem quando a câmera sobe na transição B; paredes mais altas só na transição, se incomodar | M9i E6 |
| 5 | 2026-11-18 | Claude | DBG → Câmera (M9h) | O "Recuo" é da F1 (D-148): na câmera do v8 mostra "Recuo ?" e leva a uma câmera misturada. Tirar o botão ou fazer o recuo contar da câmera em uso | M9i E6 |
| 6 | 2026-11-19 | Bruno (1) | Barra do andar | Barra **~40% menor** com a borda `cave_meter` e a seta `cave_meter_arrow` do Apellon no lugar das nossas; as cores dos andares por cima do resto de fundo amarelo dentro da borda (o Apellon conserta a borda depois) | M9i E2 ✅ |
| 7 | 2026-11-19 | Bruno (2) | Coluna dos bônus | Os bônus saem da esquerda (disputam com a barra do andar) e vão para a **direita, abaixo do botão Ajustes** | M9i E1 ✅ |
| 8 | 2026-11-19 | Bruno (3) | Fila de cristais | "Cristais distantes no começo": **é do GDD** — 3 m entre cristais, e o Atalho (no descanso) tira 0,5 m por nível (D-252, D-277, D-325). Desconsiderado, como pedido | respondido |
| 9 | 2026-11-19 | Bruno (4) | HUD e coluna da direita | LASCAS e VERMELHAS com mais altura (o texto encosta na borda de dentro); o AD com mais espaço embaixo; LOJA e AJUSTES com o fundo dos outros (hoje preto): testar o das lascas e o do AD | M9i E1 ✅ tile do Apellon (D-334) |
| 10 | 2026-11-19 | Bruno (5) | Save | Conferir se **Apagar save** apaga tudo. **Versão do save** para os testes de agora: save de uma versão mais velha do app → aviso de versão nova com a sugestão de recomeçar (ou recomeço). Para produção, depois: atualizar o app, ver as novidades e manter o save consistente | M9i E4 |
| 11 | 2026-11-19 | Bruno (6) | Transição de andar | O texto do andar no escuro é pequeno: maior e numa moldura (placa, modal); mockup com opções | M9i E2 ✅ placa A (D-335) |
| 12 | 2026-11-19 | Bruno (7) | Ícones | **De-para** dos ícones atuais com os do Apellon e trocar todos: `icon_settings`, `icon_boost`, `icon_shop` na coluna da direita, a **escada** (`stair`) no Próximo Andar no lugar da seta, a marca dos andares (`cave_meter_arrow`) e o resto da pasta | M9i E1 (coluna ✅), E2 (escada, seta ✅), E6 (resto) |
| 13 | 2026-11-19 | Bruno (8) | Modais | Títulos com mais altura e o X de fechar maior: hoje tudo pequeno e espremido | M9i E3 🔶 feito (D-336), espera o teste |
| 14 | 2026-11-19 | Bruno (9) | HUD | "A Velocidade saiu do alto da tela": **é do GDD v8** — o HUD mostra só lascas e vermelhas (§7); Dano e Velocidade foram para a LOJA (D-281) | respondido |
| 15 | 2026-11-19 | Bruno (10) | Barra de baixo | O **Descansar** sempre na tela, apagado ("indisponível") antes do andar 10, para o jogador saber o que existe; o mesmo vale para os outros elementos (escada, barra, bomba) | M9i E1 ✅ |
| 16 | 2026-11-19 | Bruno (11) | Bomba | **Ideia, não ajuste:** um bônus para quem dá o **último golpe** no cristal com a bomba — uma disputa entre o anão e o jogador; em andares de muita vida, puxa a compra das bombas maiores. Para o Bruno conversar com o Apellon | ideia |
| 17 | 2026-11-19 | Bruno (12) | Android | **Voltar duas vezes** para sair: modal "Sair do jogo?" com o que o estagiário rende offline | M9i E5 |
| 18 | 2026-11-19 | Bruno (13) | Ajustes | Opção do lançamento da bomba: **deslizar** (como hoje) ou **botão** | M9i E5 |
