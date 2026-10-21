# Interface — Inventário de telas e UX

> Proposta de 2026-10-20 (**[REC]**, exceto o que cita **[DOC]**). Baseada em GDD §7, §9 e no mockup. **Nada foi implementado.** Estilo visual em
> [Direção de Arte](Direcao%20de%20Arte%20e%20Assets.md); requisitos REQ-UI-xx em [Requisitos](Requisitos%20e%20Restricoes.md).

## 1. Princípios

1. **Uma tela principal; tudo o mais é sobreposição** (loja, modais) — a mineração nunca para (REQ-PLT-04, REQ-UI-04).
2. **Toda ação tem resposta em < 100 ms** (visual + som); toda compra tem resposta visível no minerador, não só no HUD.
3. **Zona do polegar:** ações frequentes na metade inferior/direita (Loja, "Passar de Caverna"); informação na metade superior. Alvo de toque ≥ **48 dp** (recomendação do Android) — o GDD pede "alvos grandes" sem número.
4. **Hierarquia:** cristal e minerador (mundo) > barras de vida > botões de ação > HUD > decoração.
5. **Números:** `double` por baixo; abreviações (K, M, B, T) acima de 1 milhão para caber em tela estreita; HUD "duas casas decimais" [DOC] aplica-se **ao que cabe**; idioma/formato (`1.234,00` pt-BR × `1,234.00` do mockup) é a decisão **D-11**.
6. **Texto em dados desde o primeiro texto** (lição herdada): nada de string de interface escrita em código.
7. **Safe Area** aplicada ao *canvas* inteiro (REQ-PLT-05).

## 2. Inventário

> **Nota de 2026-10-21 (D-26/D-28):** S01 passa a ser o **mapa** com o anão andando e grupos de cristais nascendo continuamente (não mais "5 cristais"); layout de referência em [Referencia de tela - mapa.jpg](Referencia%20de%20tela%20-%20mapa.jpg). O primeiro graybox de S01 e S02 é no **M2**.

| ID | Tela / elemento | Propósito | Elementos | Interações | Estados | Dependências | Quando prototipar |
|---|---|---|---|---|---|---|---|
| **S01** | **Tela principal de mineração** (mundo + HUD) | O jogo | HUD (S02), minerador(es), 5 cristais + barras de vida, lasca voando, `xN`, "CRÍTICO", selos de penalidade/boost, botões laterais, "Passar de Caverna" | Toque (mundo), abrir Loja, abrir AD, abrir Config, Passar de Caverna | Mineração · penalidade ativa · boost ativo · loja/modal aberto (mundo escurecido e sem toque, mas rodando) · transição de caverna | Núcleo de regras (M2) | **M3 (graybox)**, M5 (polimento) |
| **S02** | **HUD superior** | Dar leitura imediata | 4 indicadores: Lascas, Dano, Caverna/Nível, Velocidade; engrenagem | Ler; engrenagem abre S06 | Normal · valor mudou (pulso curto) · valor máximo/limite | Fórmulas, formatação | M3 |
| **S03** | **Loja** (tela cheia sobre S01) | Gastar lascas | Linha por melhoria (6): ícone, nome, nível atual/limite, efeito (de→para), custo, botão Comprar; saldo de lascas; botão Voltar | Comprar (uma vez por toque); rolagem se necessário; Voltar | Comprável · sem lascas · no limite · compra única já feita · Letrado "desta caverna" × "já comprado" · selo "novo" no botão Loja | Compras (M4); estagiário/Letrado/Dupla | **M4** (funcional), M9 (skin) |
| **S04** | **Modal de AD (simulado)** | Boost opcional | Contagem de 5 s, botão "Aprimorar" desabilitado até zerar, "Cancelar/Fechar" | Esperar; confirmar; fechar sem bônus | Contando · pronto · recarga ativa (botão AD mostra 30 s) · boost ativo | AD (M8) | M8 |
| **S05** | **Botão "Passar de Caverna"** | Descer | Livro + texto | Tocar (quando habilitado) | Desabilitado (cinza) · **habilitado (âmbar, pulsante)** | Letrado (M7) | M7 |
| **S06** | **Transição de caverna** | Recompensa e clima | Quebra da "parede" **[se D-07 mantiver a parede]**, nova paleta, texto "Caverna N — nome", novos cristais | Nenhuma (automática, ~1,5–2 s) | Em curso · concluída | Dados de caverna (M7), arte (M9) | M7 (provisória), M9 |
| **S07** | **Configurações** | Conforto | Volume mestre, volume SFX, tremor de tela on/off, vibração on/off, **Apagar progresso** (com confirmação), créditos, versão do build | Mover sliders, alternar, abrir S08 | Normal | Áudio, save (M8) | M8 |
| **S08** | **Confirmação de apagar save** | Evitar perda acidental | Texto claro + "Apagar" (vermelho) + "Cancelar"; opcional: digitar/segurar para confirmar | Confirmar/cancelar | — | Save | M8 |
| **S09** | **Retorno offline** *(se D-04 = sim)* | Mostrar o que rendeu | "Você esteve fora X h", lascas ganhas, botão Coletar | Coletar | — | Offline (M8) | M8 |
| **S10** | **Dicas contextuais (tutorial mínimo)** | Evitar abandono no 1º minuto | Mão/ícone apontando o toque; seta para "Loja" quando comprar; aviso da penalidade (1ª vez); explicação do Letrado | Tocar para dispensar | Mostrar uma vez; flag no save | S01–S05 | M5 (toque), M7 (Letrado/penalidade) |
| **S11** | **Notificações e celebrações** | Marcos | "Nova caverna!", "Estagiário contratado!", "Picareta Dupla!", "Penalidade ativa" (selo), "Boost ativo" | Nenhuma | Enfileiradas, não bloqueiam toque | Eventos | M5–M7 |
| **S12** | **Créditos / licenças** | Obrigação de licença | Lista de assets e autores | Rolar, voltar | — | `CREDITS.md` | M9–M11 |
| **S13** | **Fim da descida** *(MVP)* | Fechar o MVP | Mensagem de conclusão, estatísticas simples (tempo, lascas totais), botão "Continuar minerando" (sem prestígio) | Continuar | — | Caverna 10 vencida | M8–M9 |

**Dependência cruzada:** o sistema de overlays (loja/modal) precisa de **uma camada única de bloqueio de toque** com "mundo continua rodando" (teste P0 da semana 8).

## 3. Composição da tela principal (porta-retrato)

```
┌───────────────────────────┐
│ Lascas │ Dano │ Cav. │ Vel.│ ⚙  ← HUD (toda a largura, Safe Area)
│                       [Loja]│
│            ☻ minerador   [AD]│
│                             │
│     ◆ ← cristal 1 (barra)   │  ← 5 cristais: posições aleatórias,
│          ◆ ← cristal 2 ☻    │    leitura vertical; estagiário
│     ◆ ← 3                   │    um à frente
│           ◆ ← 4             │
│     ◆ ← 5                   │
│     [ PASSAR DE CAVERNA ]   │  ← âmbar quando habilitado
└───────────────────────────┘
```

## 4. Ordem de prototipação

| Cedo (valida o loop) | Depois (já com o loop aprovado) |
|---|---|
| S01 (mundo + barras), S02 (HUD simples), toque + feedback básico, S03 mínima (2 itens) | Skin da UI, S06 elaborada, S10, S11, S12, S13 |
| Botão "Passar de Caverna" (estado habilitado/desabilitado) | Animação de entrada/saída de painéis, selos animados |

## 5. Regras de feedback (REQ-UI-05)

| Evento | Cor | Forma | Movimento |
|---|---|---|---|
| Crítico | amarelo/branco quente | explosão radial + texto grande | estoura e sobe, posição aleatória |
| Combo `xN` | cor da caverna | número com contorno | pulsa e desaparece em 1 s |
| Combo ×10 | âmbar | anel/estrela + faixa | "pop" maior e tremor de HUD |
| Penalidade | vermelho `#D84235` | **seta para baixo** abaixo do minerador | pulsa devagar enquanto ativa |
| Boost | âmbar/amarelo | **raio** acima do minerador | pulsa rápido 30 s; contagem regressiva visível no botão |

## 6. Acessibilidade e conforto [REC]

- Não depender só de cor: crítico/combo/penalidade/boost já diferem por **forma e movimento** (REQ-UI-05); caverna diferenciada também por **nome e padrão do piso**.
- Opção de **tremor de tela off** e **vibração off** desde o MVP; contraste de texto ≥ 4,5:1 (#F5F7F8 sobre #20262A atende).
- Fonte pixel **com diacríticos** (ã, ç, é, í, õ).
- Botões grandes e com espaço; sem gesto obrigatório de duas mãos.
