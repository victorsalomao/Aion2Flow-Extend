# AGENTS.md — Aion2 Lite

Time de agentes (definições em `.claude/agents/`). Ponto de entrada: **tech-lead**.

| Agente | Modelo | Complexidade | Responsável por |
|---|---|---|---|
| `tech-lead` | opus | C4 | Planejamento, delegação, review, decisões, métricas |
| `perf-engineer` | sonnet | C3 | Bench, baseline, H1–H7, LiteMode, GC |
| `core-dev` | sonnet | C2–C3 | Persistence, Domain.Progress, ResetClock, módulos |
| `ui-dev` | sonnet | C2 | Avalonia, overlay, abas, telas do MVP |
| `protocol-re` | sonnet | C3 | Diff de opcodes, parsers, tabela versionada (só leitura) |
| `test-engineer` | sonnet | C2 | Testes unitários, replay, paridade |
| `ops-docs` | haiku | C1 | Setup, CI, JSON de dados, docs, coleta de métricas |
| `po` | haiku | C1 | Relatórios de progresso (checkpoint D4, final D7/D14) em `docs/reports/`, burn-up vs alvo |

Fluxo: `tech-lead` planeja em `docs/TASKS.md` → delega → agente entrega diff + verificação → `tech-lead` revisa (checklist) → commit → métricas em `docs/METRICS.md`.

Regras globais: ver `CLAUDE.md`. Plano: `docs/PLANO.md`.
