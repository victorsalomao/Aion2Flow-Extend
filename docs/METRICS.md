# Aion2 Lite — Métricas de Evolução por Sprint

Preenchido pelo **tech-lead** no último dia de cada sprint (números coletados pelo `ops-docs` e `perf-engineer`).

## 1. Métricas de produto (performance — DoD do PLANO.md §5)

| Métrica | Meta | Como medir |
|---|---|---|
| RAM idle | < 60 MB | `tools/bench.ps1` (Working Set) |
| RAM após 10 lutas | < 100 MB, sem crescimento contínuo | bench + 2ª rodada para ver tendência |
| CPU em luta | < 2% médio | `dotnet-counters` durante replay |
| Startup | < 1 s | bench (tempo até primeira janela) |
| Tamanho do executável AOT | acompanhar (não regredir > 10%) | `dotnet publish` |

## 2. Métricas de entrega (fluxo)

| Métrica | Definição |
|---|---|
| Velocidade | Pontos `done` na sprint |
| Previsibilidade | Pontos `done` ÷ pontos planejados (meta ≥ 80%) |
| Lead time médio | Dias de `doing` → `done` por task |
| Tasks bloqueadas | Quantidade e motivo |
| Carry-over | Tasks que passaram para a sprint seguinte |

## 3. Métricas de qualidade

| Métrica | Meta |
|---|---|
| Build Release sem warnings | 100% dos commits |
| `dotnet test` verde no CI | 100% |
| Nº de testes (Unit + Replay) | crescente a cada sprint |
| Cobertura dos projetos novos (Persistence, Domain, Modules) | ≥ 80% |
| Bugs/regressões encontrados na review | tendência de queda |
| Retrabalho | tasks devolvidas pelo tech-lead ÷ tasks entregues (meta ≤ 20%) |

## 4. Métricas de delegação (eficiência dos agentes)

| Métrica | Definição | Meta |
|---|---|---|
| % tasks por modelo | haiku / sonnet / opus | opus ≤ 15% |
| Taxa de escalonamento | tasks que subiram de nível ÷ total | ≤ 15% |
| Aprovação de primeira | entregas aprovadas sem retrabalho | ≥ 75% |

### Burn-up alvo (escopo F0–F5 = 95 pts; S1 = D1–D7 15–21/10, S2 = D8–D14 22–28/10)

| Marco | D3 (17/10) | D5 (19/10) | D7 (21/10) | D10 (24/10) | D14 (28/10) |
|---|---|---|---|---|---|
| % escopo `done` alvo | 12% | 35% | 65% | 82% | 100% |

Desvio > 10 p.p. abaixo do alvo → `po` sinaliza no relatório e tech-lead re-prioriza.

---

## Template por sprint (copiar a cada sprint)

### Sprint SX — DD/MM a DD/MM/2026
**Meta:** …
**Resultado:** atingida / parcial / não atingida

| Produto | Baseline | Sprint anterior | Atual | Δ | Meta |
|---|---|---|---|---|---|
| RAM idle (MB) | | | | | < 60 |
| RAM 10 lutas (MB) | | | | | < 100 |
| CPU luta (%) | | | | | < 2 |
| Startup (s) | | | | | < 1 |

| Entrega/Qualidade | Valor |
|---|---|
| Pontos planejados / entregues | / |
| Previsibilidade | % |
| Testes (antes → depois) | → |
| Build sem warnings | sim/não |
| Retrabalho | % |
| Tasks haiku / sonnet / opus | / / |
| Escalonamentos | |

**O que funcionou:** …
**O que melhorar:** …
**Ações para a próxima sprint (1–3):** …
**Decisões tomadas (ADR curto):** …

---

## Histórico
_(sprints concluídas entram aqui, mais recente primeiro)_
