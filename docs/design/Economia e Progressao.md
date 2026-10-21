# Economia e Progressão

> Modelo inicial de 2026-10-20. **Todos os números marcados "provisório" são ilustrações para discussão, não valores de balanceamento.**
> As fórmulas do GDD v1.0 estão em [Requisitos e Restrições](Requisitos%20e%20Restricoes.md) (REQ-CRY, REQ-ECO). Este documento é a **fonte de verdade da análise
> econômica**; os valores finais vão para ScriptableObjects (ver [Arquitetura Técnica](../planning/Arquitetura%20Tecnica.md)) e as decisões para o
> [Registro de Decisões](../planning/Decisoes%20e%20Riscos.md).

> **Decisão do Bruno (2026-10-21, D-01):** o jogo **não precisa chegar à caverna 10 agora**, e a progressão será balanceada **ao longo do desenvolvimento**. Portanto a análise das seções 3–4 é uma **observação** (não bloqueia marcos): as fórmulas do GDD v1.0 entram como valores iniciais e o simulador do M2 serve para medir. **Recomendação mantida:** revisitar antes do playtest do M10, para o jogo lançado não ter um teto prático em poucas cavernas.

## 1. Recursos, fontes e ralos

| Elemento | Detalhe [DOC] |
|---|---|
| **Recurso único** | Lasca de cristal (decimal; `double`) |
| **Fontes** | Cristal quebrado pelo **principal** e pelo **estagiário**: `2^(caverna−1) × (1 + Mineração%) × (0,5 se penalidade)` |
| **Ralos** | Picareta, Velocidade, Mineração, Estagiário, Letrado (obrigatório p/ passagem), Picareta Dupla (única) |
| **Secundário** | Boost de AD (×2,5 na velocidade): não é recurso, é multiplicador temporário |
| **Sem** | segunda moeda, segunda fonte, inventário, itens |

**[REC]** Manter assim. Qualquer tentativa de compensar o problema abaixo com uma 2ª moeda deve ser rejeitada.

## 2. Relações matemáticas que precisam ser sintonizadas

| Grandeza | Fórmula do GDD | Crescimento por caverna |
|---|---|---|
| Vida do cristal `H(n)` | `10·10^(n−1)` | **×10** |
| Recompensa básica `R(n)` | `2^(n−1)` | **×2** |
| Dano do golpe `D` | `0,5·2^(p−1)` (Picareta *p* ≤ 20) × 2 (Letrado) × 2 (Dupla) | máx. 1.048.576 (≈ ×2 por compra) |
| Intervalo `T` | `5,0 − 0,1·s`, mínimo 0,5 | ≥ 0,5 s (45 compras para o mínimo) |
| Custo Picareta | `50·4^(k−1)` | ×4 por compra; 19ª compra ≈ 3,4·10¹² |
| Custo Velocidade | `200·3^(k−1)` | ×3 por compra; 45ª compra ≈ 1,8·10²³ |
| Custo Mineração | `50·1,2^(k−1)` | ×1,2; 100ª compra ≈ 3,5·10⁹ |
| Custo Estagiário | `500·5^(k−1)` | ×5; 10ª compra ≈ 9,8·10⁸ |
| Custo Letrado `L(n)` | `10·10^(n−1)` | **×10** |
| Tempo por cristal | `H(n) / dps` | depende de tudo acima |
| Gate de passagem | Letrado *n* comprado | `L(n) / R(n)` = **cristais necessários** |

**Regra de ouro (padrão do gênero):** para a progressão fechar, **a recompensa tem que crescer na mesma ordem de grandeza que o custo do que ela paga**, e
o dano tem que crescer na mesma ordem que a vida. Aqui nenhuma das duas condições é cumprida.

## 3. Verificação do GDD v1.0 (achado C-01)

### 3.1 Aritmética direta (sem simulação) [INT]

- **Cristais necessários para pagar o Letrado:** `L(n)/R(n) = 10^n / 2^(n−1)` → cavernas 1–3: 10, 50, 250; **caverna 5: 6.250; caverna 7: 156.250; caverna 9: ≈ 3,9 M** (sem
  contar Mineração; ≈ 2 M com +100%).
- **Dano necessário vs. disponível:** a vida da caverna 10 é 10¹⁰; o dano máximo de um golpe (Picareta 20, Letrado, Dupla) é ≈ 1,05·10⁶ → **9.537 golpes (≈ 80 min
  a 0,5 s) por cristal** mesmo com tudo no máximo — para ganhar 512 lascas.
- **Velocidade:** chegar a 0,5 s de intervalo exige 45 compras de Velocidade (5,0 → 0,5 em passos de 0,1); a última custa ~10²³, muito além de qualquer renda possível.
- **Tempo do 1º cristal:** 10 de vida ÷ 0,5 de dano = 20 golpes × 5 s = **100 s** sem toque (≈ 95 s com crítico) para **1 lasca**; a primeira melhoria custa 50 (ou 200): ~80 min sem tocar.

### 3.2 Simulação de primeira ordem (descartável, **fora do repositório**)

Script analítico em Python no diretório temporário da sessão (não é código do jogo e **não foi versionado**; o simulador de verdade é uma tarefa do
[marco M2](../planning/Roadmap.md)). Modelo: taxa média contínua de lascas/s = `R × (1+Mineração) × penalidade / (H / dps)`, estagiário contado em separado, crítico como +5% de dano esperado,
toque = 10% do golpe a *t* toques/s; política gananciosa (compra a melhoria com melhor retorno, mas só se o retorno for mais rápido que juntar para o Letrado). **Leitura mais
favorável das ambiguidades:** o *k*-ésimo nível comprado custa `base × fator^(k−1)`. Ignora o tempo de espera do estagiário, a penalidade só aproximada após 100 quebras.

| Cenário | Caverna 1 vencida | Caverna 2 | Caverna 3 | Caverna 4 |
|---|---|---|---|---|
| **GDD literal**, sem toque | 0,3 h | 13,5 h | **245 h** | não alcançada |
| **GDD literal**, 3 toques/s | 0,1 h | 5,4 h | **98 h** | não alcançada |
| **GDD literal**, 6 toques/s | 0,1 h | 3,4 h | **61 h** | não alcançada |

**Conclusão (robusta):** mesmo que a política de compra seja melhorada, o gargalo é estrutural — `L(n)` cresce ×10 por caverna, `R(n)` ×2, e a velocidade de
dano estaciona. **As fórmulas do GDD não permitem concluir o MVP.** Isso conflita com a nota "as fórmulas são requisitos do MVP" (REQ-PRO-06).
**[REC]** Tratar como defeito de especificação; decisão **D-01**.

### 3.3 Sensibilidade (exploração, também provisória)

A mesma simulação com razões **casadas** (vida ×3/caverna, recompensa ×2, Letrado `30·2^(n−1)`, custos de Picareta ×3 e Velocidade ×2) chegou à caverna 10 em ~280 h com 3 toques/s
(8,7 h até a caverna 4; 62,8 h até a 7). Ou seja: **igualar as razões fecha a progressão, mas o tempo total ainda sai longo**, porque as constantes de partida
(100 s para 1 lasca) estão lentas. Calibrar é um problema de **metas de ritmo primeiro**, constantes depois.

## 4. Proposta de método de recalibração [REC]

1. **Definir metas de ritmo (provisórias, a aprovar):**

   | Marco | Meta provisória (jogo ativo) |
   |---|---|
   | 1º cristal quebrado | ≤ 15 s |
   | 1ª compra na loja | ≤ 2–3 min |
   | Caverna 1 → 2 | 10–15 min |
   | Caverna *n* → *n+1* | cresce de 15 a ~60 min até a 10 |
   | MVP inteiro, jogando ativo | ~5–8 h (soma de ≈ 330 min na tabela-guia) |
   | "Algo comprável" | a cada 30–90 s no início; 3–10 min na metade; nunca > 20 min sem decisão |
2. **Escolher um parâmetro *K*** (cristais para pagar o Letrado), p. ex. 30–60, e fazer `L(n) = K · R(n) · (1 − Mineração média esperada)`; ou seja, **o Letrado custa tantos cristais da caverna**.
3. **Casar crescimento:** vida `H(n)` ≈ `h^(n−1)` com `h` pequeno (2–4) e dano escalando pela **Picareta ×2 por compra**, **Letrado ×2** e Estagiário/Dupla como degraus; *h* e o
   número de compras de Picareta (20 níveis) precisam cobrir `H(10)/dano_máx` em poucas dezenas de golpes.
4. **Velocidade:** reduzir o número de compras (45 é excessivo) ou mudar para passos proporcionais; manter o mínimo 0,5 s **alcançável** na caverna ~8–9.
5. **Penalidade:** fixar o limiar de quebras relativo ao `K` (p. ex. 2×K) para que **não seja a regra** a partir da caverna 3 (C-09), ou deixar claro que é a regra e justificar.
6. **Validar no simulador** (M2) com 3 perfis de jogador: *passivo* (0 toque), *casual* (~1–2 toques/s), *ativo* (~4–6 toques/s) + política de compra mais inteligente; gerar curva **tempo × caverna**, tempo ocioso entre compras e "compra mais barata
   disponível".
7. **Preservar a "versão-base" do GDD** (REQ-PRO-06) como arquivo de dados `Balance_GDD_v1.asset` para comparar com a calibrada `Balance_v2.asset`. É exatamente o que o GDD pede em §13.

> **Valores de ilustração (não adotar):** vida `10·3^(n−1)` (cave 10 ≈ 196 k), recompensa `1·2,2^(n−1)` (cave 10 ≈ 1.200), `K ≈ 40`. Servem só para testar a ordem de grandeza no simulador.

## 5. Ritmo, gates e cadência de recompensa

- **Gates [DOC]:** Letrado *n* = única condição para passar da caverna *n*; Dupla (5.000) é compra opcional de grande impacto; Estagiário é o 1º "grande degrau" de automação.
- **Degraus de automação [INT]:** (1) Picareta + toque → (2) Velocidade → (3) Estagiário → (4) Dupla → (5) caverna ~7+ em que o toque praticamente some.
- **Cadência de recompensa [REC]:** a cada Letrado, **celebração de caverna** (transição, nova cor, som); a cada compra, feedback visual no botão e no minerador (brilho da picareta, 2ª picareta).
  Marcos de lascas totais **P1**.
- **Raridade:** não existe no MVP; **P1:** cristal dourado (probabilidade, p. ex. 2% por cristal, 5–10× recompensa) — **provisório**.
- **Prestígio:** **✗ MVP**. Razão: com 10 cavernas e uma economia que ainda não fecha, prestígio esconderia o problema. Reavaliar pós-MVP com "Nova Descida" (reinicia
  cavernas e lascas, mantém um multiplicador permanente).

## 6. Progressão offline (decisão D-04, GDD silencioso)

**[REC]** Se aprovado, modelo **analítico**, não de simulação golpe a golpe: `lascas_offline = taxa_média(estado atual) × min(Δt, teto) × eficiência`.

| Regra provisória | Valor |
|---|---|
| Teto | 2–4 h |
| Eficiência | 50% (toque não conta) |
| Penalidade de 100 quebras | aplica sobre a taxa quando as quebras estimadas passarem de 100 |
| Boost de AD | **não** é aplicado offline (evita lacuna de exploração) |
| Relógio | UTC + proteção simples contra relógio do aparelho voltando no tempo |
| Feedback | modal "enquanto você esteve fora" com valor e botão de coletar |

## 7. Como validar que a progressão "dá gosto"

| Pergunta | Medida | Quando |
|---|---|---|
| Está lento demais? | Tempo até o 1º cristal; até a 1ª compra; tempo parado sem nada para comprar | Simulador (M2) + playtest (M10) |
| Está trivial? | Cavernas vencidas em < meta/2 por jogador ativo | Simulador + playtest |
| O toque vale a pena? | Razão renda ativa/passiva por caverna | Simulador |
| Compra faz diferença sentida? | Δ tempo-por-cristal ao comprar (≥ 15–20% nas primeiras compras) | Painel de debug |
| A penalidade é compreendida? | Playtester descreve o ícone e o que mudou | Playtest |
| Números ficam legíveis? | Abreviações (K, M, B, T) e telas estreitas | Dispositivo |
| Ninguém travou? | Nenhuma sequência sem compra viável (P0) | Teste automatizado: "progressão alcançável" do simulador |

**Telemetria local (sem rede):** *log* em arquivo/console de debug com curva de lascas e eventos de compra, exportável para análise.
