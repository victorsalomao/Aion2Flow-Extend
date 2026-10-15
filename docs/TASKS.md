# Aion2 Lite — Backlog e Delegação

Legenda de complexidade: **C1** trivial (haiku) · **C2** padrão (sonnet) · **C3** complexa (sonnet + revisão reforçada) · **C4** decisão (tech-lead/opus)
Status: `todo` · `doing` · `review` · `done` · `blocked`
Pontos (esforço): 1, 2, 3, 5, 8 · Total escopo F0–F5: **95 pts**
Regra de execução: todos os agentes com **effort low**; várias cópias do mesmo agente podem rodar em trilhas paralelas (worktrees separados).

## Sprints (2 × 1 semana — decisão do dono: escopo completo F0–F5)

| Sprint | Datas | Meta | Escopo |
|---|---|---|---|
| S1 | 15/10 – 21/10/2026 | Repo verde + CI + baseline + H1–H7 + LiteMode (T2.1–T2.7) + Fase 3 completa + capturas/diff de opcodes | F0, F1, T2.1–T2.7, F3, T5.1–T5.2 (~62 pts) |
| S2 | 22/10 – 28/10/2026 | −50% RAM (T2.8–T2.9) + MVP manual (F4) + leitura automática (T5.3–T5.5) | T2.8–T2.9, F4, T5.3–T5.5 (~33 pts) |
| stretch | se sobrar em S2 | Insights / LLM | F6 |

Detalhe da S1: `docs/SPRINT-1.md`.

## Trilhas paralelas

| Trilha | Foco | Agentes | Observação |
|---|---|---|---|
| **A** Perf/LiteMode | F1, F2 | perf-engineer ×2, core-dev, ui-dev | Caminho crítico (F0→F1→T1.4 gate→F2) |
| **B** Domínio+Persistence | T3.1–T3.3, T3.5, F4 core | core-dev ×2, test-engineer | **F3 não depende da F2** → começa D1 |
| **C** UI | T3.4, T2.4–T2.5, F4 telas | ui-dev ×2 | Usa interfaces da B; mocks até lá |
| **D** Protocolo/RE | F5 | protocol-re | **Bloqueio externo:** T5.1 exige capturas do Victor até **18/10 (D4)** |
| **E** Ops/Testes | F0, CI, JSON, métricas, relatórios | ops-docs, test-engineer, po | Suporte a todas as trilhas |

## Backlog

| ID | Task | Fase | Trilha | Cx | Agente | Modelo | Pts | Depende de | Sprint | Status |
|---|---|---|---|---|---|---|---|---|---|---|
| T0.1 | Fork, clone, build .NET 10 | 0 | E | C1 | ops-docs | haiku | 1 | — | S1 | done (fork victorsalomao/Aion2Flow-Extend) |
| T0.2 | `dotnet test` e baseline verde | 0 | E | C1 | test-engineer | sonnet | 1 | T0.1 | S1 | done (25 replay + 1046 unit = 1071 verdes, 14/10) |
| T0.3 | CLAUDE.md na raiz | 0 | E | C1 | ops-docs | haiku | 1 | T0.1 | S1 | done |
| T0.4 | CI GitHub Actions (windows-latest) | 0 | E | C1 | ops-docs | haiku | 2 | T0.2 | S1 | done (ci.yml upstream já cobre build+test em windows-latest) |
| T1.1 | `tools/bench.ps1` (replay + dotnet-counters) | 1 | A | C2 | perf-engineer | sonnet | 3 | T0.2 | S1 | done (14/10) |
| T1.2 | `docs/perf-baseline.md` (idle / 5 min / 10 lutas) | 1 | A | C2 | perf-engineer | sonnet | 2 | T1.1 | S1 | done (14/10) |
| T1.3 | gcdump após 10 lutas, top 10 tipos | 1 | A | C2 | perf-engineer | sonnet | 2 | T1.1 | S1 | done (14/10) |
| T1.4 | Confirmar/descartar H1–H7 | 1 | A | C3 | perf-engineer | sonnet | 3 | T1.3 | S1 | done (14/10) |
| G1 | **Gate:** priorizar F2 conforme T1.4 | 1 | A | C4 | tech-lead | opus | 1 | T1.4 | S1 | done: prioridade T2.2+T2.3 > T2.7; T2.4/T2.6 viram verificação (já lazy) |
| T2.1 | Flag `LiteMode` em Settings | 2 | A | C2 | core-dev | sonnet | 1 | T0.2 | S1 | done (15/10, default true) |
| T2.2 | Journal → agregados incrementais + paridade | 2 | A | C3 | perf-engineer | sonnet | 8 | T2.1, G1 | S1 | done (15/10, fights10 heap 187→78 MB, paridade 25 logs) |
| T2.3 | Archive em disco comprimido, resumo em RAM | 2 | A | C3 | perf-engineer #2 | sonnet | 5 | T2.1, G1 | S1 | done (15/10, Brotli em %TEMP%, heap −27% isolado) |
| T2.4 | Playback lazy + liberar ao fechar | 2 | C | C3 | ui-dev | sonnet | 5 | T2.1, G1 | S1 | done (16/10, ScenePlaybackReleaseTests) |
| T2.5 | Sem animações no LiteMode, UI a 4 Hz | 2 | C | C2 | ui-dev | sonnet | 3 | T2.1 | S1 | done (15/10; bench de CPU com UI pendente) |
| T2.6 | Carregar só o pack de idioma ativo | 2 | A | C2 | core-dev | sonnet | 2 | T2.1 | S1 | done (16/10, ResourceCatalogActiveLanguageTests) |
| T2.7 | Change logs em ring buffer | 2 | A | C2 | core-dev | sonnet | 2 | T2.1 | S1 | done (15/10, limite 65536 no LiteMode) |
| T2.8 | Tuning de GC, uma flag por vez | 2 | A | C3 | perf-engineer | sonnet | 3 | T2.2–T2.7 | S2 | todo |
| T2.9 | Bench final vs baseline (meta −50%) | 2 | A | C2 | perf-engineer | sonnet | 2 | T2.8 | S2 | todo |
| T3.1 | `Aion2Flow.Persistence` (SQLite AOT) | 3 | B | C2 | core-dev #2 | sonnet | 5 | T0.2 | S1 | done (15/10, 5 testes; publish AOT pendente: falta MSVC) |
| T3.2 | `Aion2Flow.Domain.Progress` | 3 | B | C2 | core-dev #2 | sonnet | 3 | T0.2 | S1 | done (14/10, 8 testes) |
| T3.3 | `ResetClock` multi-fuso | 3 | B | C3 | core-dev #2 | sonnet | 3 | T3.2 | S1 | done (15/10; reset 05:00 + quarta confirmado pelo Victor) |
| T3.4 | Abas Overlay / Hoje / Semana / Alts / Timers | 3 | C | C2 | ui-dev #2 | sonnet | 3 | T3.2 | S1 | done (16/10, ligado na MainWindow + DI) |
| T3.5 | Testes do ResetClock (dia, semana, DST) | 3 | B | C2 | test-engineer | sonnet | 2 | T3.3 | S1 | done (15/10, 25 testes) |
| T4.1 | `data/tasks.json` + checklist | 4 | C/E | C1/C2 | ops-docs + ui-dev | haiku/sonnet | 3 | T3.4 | S2 | todo |
| T4.2 | Cadastro de alts | 4 | B/C | C2 | core-dev + ui-dev | sonnet | 3 | T3.1 | S2 | parcial (domínio pronto; falta UI) |
| T4.3 | Campos manuais com delta "hoje" | 4 | B/C | C2 | core-dev + ui-dev | sonnet | 3 | T3.1 | S2 | parcial (domínio + migração v2; falta UI) |
| T4.4 | Recarga de resina + notificação | 4 | B | C2 | core-dev | sonnet | 3 | T4.3 | S2 | parcial (domínio + migração v2; falta UI) |
| T4.5 | `data/events.json` + timers | 4 | C/E | C1/C2 | ops-docs + ui-dev | haiku/sonnet | 3 | T3.3 | S2 | todo |
| T4.6 | Calculadora de temporada | 4 | B | C2 | core-dev | sonnet | 2 | T4.3 | S2 | parcial (domínio + migração v2; falta UI) |
| T5.1 | Gravar sessões de pacotes (**capturas do Victor até 18/10**) | 5 | D | C2 | protocol-re | sonnet | 2 | Victor | S1 | blocked — roteiro pronto em docs/ROTEIRO-CAPTURAS.md (build Debug), aguarda Victor até 18/10 |
| T5.2 | Diff de opcodes | 5 | D | C3 | protocol-re | sonnet | 5 | T5.1 | S1 | todo |
| T5.3 | Parsers novos + testes | 5 | D | C3 | protocol-re | sonnet | 5 | T5.2 | S2 | todo |
| T5.4 | Publicar no Module.Progress (fallback manual) | 5 | D | C2 | core-dev | sonnet | 3 | T5.3, T3.2 | S2 | todo |
| T5.5 | Tabela de opcodes versionada | 5 | D | C2 | protocol-re | sonnet | 3 | T5.3 | S2 | todo |
| T6.x | Insights / LLM (opcional) | 6 | — | C4→C2 | tech-lead define | — | — | — | stretch | todo |

## Log de progresso
- **14/10 (D0, adiantado):** Fase 0 ✅ (fork victorsalomao/Aion2Flow-Extend, build 0 warnings, 1071 testes). Fase 1 ✅ (bench core-only: idle 25 MB, 10 lutas **249 MB** vs meta 100 MB; H1/H2/H7 confirmadas, H3/H6 descartadas, H4/H5 inconclusivas). T3.2 ✅. Roteiro de capturas ✅.
- **Pendências técnicas:** medir app completo com UI+WinDivert (admin) para RAM idle real, CPU em luta, H4/H5; refazer gcdump não-lossy. Limpar `D:\tmpbench\` (lixo de build).
- **Próxima sessão:** T2.1 → T2.2 ‖ T2.3 ‖ T2.7 (trilha A), T3.1 ‖ T3.3 (trilha B), T2.5 (trilha C).
- **15/10 (D1):** T2.1, T2.2, T2.3, T3.1, T3.3, T3.5 ✅; T2.7 parcial. Build Release 0 warnings, 1132 testes verdes. Pendências: ligar T2.7 ao LiteMode; bench combinado T2.2+T2.3; instalar MSVC Build Tools para publish AOT; confirmar horário de reset; medir app com UI (admin).
- **15/10 (D1, 2ª leva):** T2.7 ligado, T2.5 ✅, T3.4 casca. Bench combinado fights10 Lite: WS 158 MB (−36%), heap 73,7 MB (−61%) vs baseline. 1132 testes verdes, 0 warnings.
- **Fim da sessão 15/10:** horário de reset confirmado (05:00, semanal na quarta). Ficaram em andamento e **sem commit** no working tree: ligar as abas da T3.4 na MainWindow/DI (lazy, 4 Hz) e verificar T2.4/T2.6 (ScenePlaybackReleaseTests).
- **Próxima sessão:** (1) `git status`, revisar o diff, `dotnet build -c Release` + `dotnet test` e commitar; (2) Victor instala MSVC Build Tools → validar publish AOT; (3) medir o app com UI + WinDivert (admin) e o jogo aberto: RAM idle, CPU em luta, H4/H5, CPU da T2.5; (4) T5.1 capturas → T5.2; (5) Sprint 2: T2.8, T2.9, T4.x.
- **Atualização 15/10:** T2.4/T2.6 verificadas pelo agente em Debug (1057 + 50 verdes). T2.6 corrigiu vazamento de idiomas (~−3 MB). Decisão aberta: incluir o journal no spill do archive para o playback no LiteMode não ficar com a timeline vazia. Tudo isso ainda sem commit; falta o build Release com o app fechado.
- **16/10:** commit abas+T2.4/T2.6; publish AOT OK (31,5 MB; vswhere precisa estar no PATH); app real Lite com jogo: WS 138 MB idle, CPU ~0,5%; +18 MB em 3 min (checar vazamento em sessão longa). T4.2–T4.4 domínio pronto.
