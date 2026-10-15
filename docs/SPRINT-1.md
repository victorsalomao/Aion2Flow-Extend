# Sprint 1 — 15/10 a 21/10/2026

## Meta
- Repo verde + CI; baseline medida; H1–H7 decididas (gate G1)
- LiteMode T2.1–T2.7 em `review`/`done`
- Fase 3 completa (Persistence, Domain.Progress, ResetClock, abas)
- T5.1–T5.2: capturas gravadas + diff de opcodes
- Burn-up alvo D7: 65% de 95 pts (ver `docs/METRICS.md`)

## Regras gerais (todas as delegações)
- effort low; resposta do agente = diff + saída da verificação, sem prosa
- Verificação padrão: `dotnet build -c Release` (0 warnings) + `dotnet test`
- Trilhas paralelas em worktrees separados; merge só após review do tech-lead
- Proibido: reflection (AOT), alocação em parser quente, módulo referenciar UI, escrever/injetar pacote, remover GPL/créditos

## Trilhas
| Trilha | Agentes (cópias) | Tasks S1 |
|---|---|---|
| A Perf/LiteMode | perf-engineer ×2, core-dev #1 | T1.1–T1.4, T2.1, T2.2, T2.3, T2.6, T2.7 |
| B Domínio+Persistence | core-dev #2, test-engineer | T3.1, T3.2, T3.3, T3.5 |
| C UI | ui-dev #1, ui-dev #2 | T2.4, T2.5, T3.4 |
| D Protocolo/RE | protocol-re | T5.1, T5.2 |
| E Ops/Testes | ops-docs, test-engineer, po | T0.1–T0.4, relatórios |

## Cronograma
| Dia | Data | A | B | C | D | E / TL |
|---|---|---|---|---|---|---|
| D1 | qua 15 | T1.1 (após T0.2) | — (aguarda T0.2) | — | prepara ferramenta de captura | T0.1, T0.2, T0.3; TL: planning |
| D2 | qui 16 | T1.2, T1.3; T2.1 | T3.2, T3.1 início | T2.5 | aguarda capturas | T0.4 CI |
| D3 | sex 17 | T1.4 → **G1** | T3.1, T3.3 | T3.4 (sobre T3.2) | aguarda capturas | TL: gate G1 |
| D4 | sáb 18 | T2.2, T2.3, T2.6 | T3.3, T3.5 | T2.4, T3.4 | **limite capturas** → T5.1 | **po: checkpoint** |
| D5 | dom 19 | T2.2, T2.3, T2.7 | T3.1 fim, T3.5 | T2.4 | T5.2 | TL: review lote 1 |
| D6 | seg 20 | T2.2 paridade | buffer/ajustes review | ajustes review | T5.2 | TL: review lote 2 |
| D7 | ter 21 | bench parcial pós-LiteMode | — | — | T5.2 fim | **po: final**; TL: METRICS + retro |

## Caminho crítico
T0.1 → T0.2 → T1.1 → T1.3 → T1.4 → G1 → T2.2 (8 pts) → [S2] T2.8 → T2.9
- Secundário: capturas do Victor (18/10) → T5.1 → T5.2 → [S2] T5.3 → T5.4/T5.5

## Gates do tech-lead
- **G0 (D1 fim):** build+test verdes no fork → libera trilhas A e B
- **G1 (D3):** H1–H7 confirmadas/descartadas → ordem/escopo de T2.2–T2.7
- **G2 (D4):** capturas recebidas? Não → T5.1 `blocked`, escalar ao Victor, D replaneja
- **G3 (D5/D6):** reviews em lote (checklist bloqueante do tech-lead.md)
- **G4 (D7):** METRICS.md preenchido, carry-over definido para S2

## Riscos
| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Capturas do Victor atrasam | média | F5 inteira escorrega | Data limite 18/10; ferramenta pronta D1; TL cobra D3 |
| T2.2 (8 pts) estoura | alta | S2 sem T2.8/T2.9 | Começa D4; teste de paridade primeiro; escalar ao TL na 2ª falha |
| Conflito de merge entre trilhas paralelas | média | retrabalho | Worktrees; arquivos disjuntos por task; merge diário pelo TL |
| H1–H7 refutam ganhos esperados | baixa | replanejar F2 | G1 decide; trocar tasks sem perder pontos |
| Gargalo de review no TL | média | fila em `review` | Reviews em lote D5/D6; test-engineer pré-valida |

## Relatórios do PO (`po`, haiku)
- **D4 (18/10) checkpoint:** `docs/reports/S1-checkpoint-2026-10-18.md` — entregue, atrasado, bloqueios (capturas), burn-up real vs alvo D3/D5
- **D7 (21/10) final:** `docs/reports/S1-final-2026-10-21.md` — entregue vs meta, burn-up vs 65%, riscos e carry-over para S2
- Entrada: TASKS.md, METRICS.md, SPRINT-1.md. Não edita código nem TASKS.md.

---

## Pacotes de delegação

### T0.1 — Fork, clone, build
- Agente/modelo: ops-docs / haiku · Trilha E · D1
- Objetivo: fork GPL-3.0 do Aion2Flow-Extend clonado e compilando em .NET 10
- Arquivos: raiz do repo, `*.sln`, `global.json`
- Passos: fork → clone → `dotnet --version` (10.x) → `dotnet restore` → build Release
- Aceite: build Release ok, LICENSE e créditos intactos
- Verificação: `dotnet build -c Release`
- Não tocar: código-fonte, LICENSE
- Entregável: repo clonado + log do build

### T0.2 — Testes baseline verdes
- Agente/modelo: test-engineer / sonnet · E · D1
- Objetivo: `dotnet test` verde; registrar nº de testes inicial
- Arquivos: projetos `*.Tests`
- Passos: rodar testes → corrigir só falhas de ambiente → anotar contagem
- Aceite: 100% verde; contagem anotada em METRICS (baseline)
- Verificação: `dotnet test -c Release`
- Não tocar: lógica de produção
- Entregável: saída do test + contagem

### T0.3 — CLAUDE.md na raiz do fork
- Agente/modelo: ops-docs / haiku · E · D1
- Objetivo: copiar/adaptar CLAUDE.md do planejamento para a raiz do fork
- Arquivos: `CLAUDE.md`, `docs/`, `.claude/agents/`
- Passos: copiar docs e agentes → ajustar caminhos
- Aceite: caminhos válidos; sem perda de regras
- Verificação: revisão TL
- Não tocar: código
- Entregável: commit de docs

### T0.4 — CI GitHub Actions
- Agente/modelo: ops-docs / haiku · E · D2
- Objetivo: workflow build+test em `windows-latest`
- Arquivos: `.github/workflows/ci.yml`
- Passos: setup-dotnet 10 → restore → build Release (warnings=erro) → test
- Aceite: pipeline verde no push/PR
- Verificação: run do Actions verde
- Não tocar: código
- Entregável: `ci.yml` + link do run

### T1.1 — tools/bench.ps1
- Agente/modelo: perf-engineer #1 / sonnet · A · D1–D2
- Objetivo: bench reproduzível (replay + dotnet-counters)
- Arquivos: `tools/bench.ps1`, replays existentes de teste
- Passos: subir app em replay → coletar Working Set, CPU, GC, startup → saída CSV/MD
- Aceite: 2 execuções seguidas com variação < 10%
- Verificação: `pwsh tools/bench.ps1 -Scenario idle`
- Não tocar: código de produção
- Entregável: script + saída exemplo

### T1.2 — perf-baseline.md
- Agente/modelo: perf-engineer #1 / sonnet · A · D2
- Objetivo: baseline idle / 5 min / 10 lutas
- Arquivos: `docs/perf-baseline.md`
- Passos: rodar 3 cenários ×2 → tabela
- Aceite: 4 métricas do METRICS §1 preenchidas
- Verificação: TL confere números vs saída do bench
- Não tocar: código
- Entregável: `docs/perf-baseline.md`

### T1.3 — gcdump após 10 lutas
- Agente/modelo: perf-engineer #2 / sonnet · A · D2
- Objetivo: top 10 tipos por bytes retidos
- Arquivos: `docs/perf-baseline.md` (seção gcdump)
- Passos: replay 10 lutas → `dotnet-gcdump collect` → `report` → top 10
- Aceite: tabela tipo/contagem/bytes
- Verificação: arquivo `.gcdump` reprocessável
- Não tocar: código
- Entregável: seção no baseline

### T1.4 — H1–H7
- Agente/modelo: perf-engineer #1 / sonnet (C3) · A · D3
- Objetivo: confirmar/descartar cada hipótese do PLANO.md com evidência
- Arquivos: `docs/perf-baseline.md` (seção hipóteses)
- Passos: cruzar gcdump + código → veredito + ganho estimado (MB) por hipótese
- Aceite: 7 linhas com veredito e evidência
- Verificação: revisão TL (gate G1)
- Não tocar: código
- Entregável: tabela H1–H7 ordenada por ganho

### T2.1 — Flag LiteMode
- Agente/modelo: core-dev #1 / sonnet · A · D2
- Objetivo: `LiteMode` em Settings, default on, persistido via `JsonSerializerContext`
- Arquivos: classe Settings + contexto JSON + testes
- Passos: propriedade → source-gen JSON → teste round-trip
- Aceite: flag lida/gravada; sem reflection
- Verificação: build + test
- Não tocar: UI, Journal
- Entregável: diff + teste

### T2.2 — Journal → agregados incrementais
- Agente/modelo: perf-engineer #1 / sonnet (C3, revisão reforçada) · A · D4–D6
- Objetivo: em LiteMode, manter só agregados incrementais em vez do journal bruto
- Arquivos: SceneRuntime/Journal, agregadores, `*.Tests`
- Passos: escrever teste de paridade (replay → totais iguais) **antes** → implementar agregador → ligar por flag
- Aceite: paridade 100% nos replays; queda de RAM 10 lutas medida
- Verificação: build + test + `tools/bench.ps1 -Scenario fights10`
- Não tocar: parsers, UI, modo não-Lite
- Entregável: diff + teste de paridade + bench antes/depois em perf-baseline

### T2.3 — Archive em disco comprimido
- Agente/modelo: perf-engineer #2 / sonnet (C3) · A · D4–D5
- Objetivo: lutas encerradas vão para disco comprimido; RAM guarda só resumo
- Arquivos: archive/histórico de lutas, testes
- Passos: serializador source-gen + compressão → resumo em RAM → carga sob demanda
- Aceite: round-trip íntegro; RAM não cresce por luta
- Verificação: build + test + bench fights10
- Não tocar: Journal (T2.2), UI
- Entregável: diff + teste round-trip + bench

### T2.4 — Playback lazy
- Agente/modelo: ui-dev #1 / sonnet (C3) · C · D4–D5
- Objetivo: playback carrega dados só ao abrir e libera ao fechar
- Arquivos: views/viewmodels de playback
- Passos: carregar via archive (T2.3; mock se não pronto) → dispose ao fechar
- Aceite: RAM volta ao patamar após fechar (bench)
- Verificação: build + test + bench
- Não tocar: módulos core
- Entregável: diff + bench

### T2.5 — Sem animações, UI 4 Hz
- Agente/modelo: ui-dev #1 / sonnet · C · D2–D3
- Objetivo: LiteMode desliga animações e limita refresh a 4 Hz
- Arquivos: estilos Avalonia, timer de refresh
- Passos: condicionar animações à flag → throttle 250 ms
- Aceite: CPU em luta menor (bench)
- Verificação: build + test + bench
- Não tocar: lógica de domínio
- Entregável: diff + CPU antes/depois

### T2.6 — Só pack de idioma ativo
- Agente/modelo: core-dev #1 / sonnet · A · D4
- Objetivo: carregar apenas o idioma ativo
- Arquivos: carregador de localização
- Passos: lazy load por cultura → troca descarrega anterior
- Aceite: teste troca de idioma; RAM idle menor
- Verificação: build + test
- Não tocar: textos
- Entregável: diff + teste

### T2.7 — Change logs em ring buffer
- Agente/modelo: core-dev #1 / sonnet · A · D5
- Objetivo: logs de mudança com capacidade fixa
- Arquivos: change log + testes
- Passos: ring buffer genérico sem alocação → substituir lista
- Aceite: teste de overflow; zero alocação no add
- Verificação: build + test
- Não tocar: UI
- Entregável: diff + teste

### T3.1 — Aion2Flow.Persistence
- Agente/modelo: core-dev #2 / sonnet · B · D2–D5
- Objetivo: projeto SQLite compatível com AOT
- Arquivos: novo `Aion2Flow.Persistence` + `.Tests`
- Passos: SQLite sem ORM com reflection → migrations simples → repositórios das entidades de T3.2
- Aceite: CRUD testado; `dotnet publish -p:PublishAot=true` sem warnings de trim
- Verificação: build + test + publish AOT
- Não tocar: projetos de meter/LiteMode
- Entregável: projeto + testes (cobertura ≥ 80%)

### T3.2 — Aion2Flow.Domain.Progress
- Agente/modelo: core-dev #2 / sonnet · B · D2
- Objetivo: entidades e interfaces (alt, tarefa, evento, recurso)
- Arquivos: novo `Aion2Flow.Domain.Progress`
- Passos: records imutáveis → interfaces de repositório → eventos de domínio
- Aceite: sem dependência de UI/Persistence
- Verificação: build + test
- Não tocar: projetos existentes
- Entregável: projeto + testes

### T3.3 — ResetClock multi-fuso
- Agente/modelo: core-dev #2 / sonnet (C3) · B · D3–D4
- Objetivo: próximos resets diário/semanal por região/fuso
- Arquivos: `Domain.Progress/ResetClock`
- Passos: `TimeProvider` injetável → regras por região → API NextDaily/NextWeekly
- Aceite: determinístico com TimeProvider fake
- Verificação: build + test
- Não tocar: UI
- Entregável: diff

### T3.4 — Abas Overlay/Hoje/Semana/Alts/Timers
- Agente/modelo: ui-dev #2 / sonnet · C · D3–D4
- Objetivo: casca das 5 abas consumindo interfaces de T3.2
- Arquivos: views/viewmodels novos
- Passos: TabControl → viewmodels com dados fake → binding compilado
- Aceite: navega nas 5 abas; respeita LiteMode
- Verificação: build + test
- Não tocar: Domain/Persistence
- Entregável: diff + screenshot

### T3.5 — Testes do ResetClock
- Agente/modelo: test-engineer / sonnet · B · D4–D5
- Objetivo: casos dia, semana, virada de mês, DST
- Arquivos: `Domain.Progress.Tests`
- Passos: tabela de casos com TimeProvider fake
- Aceite: ≥ 10 casos verdes incl. DST
- Verificação: `dotnet test`
- Não tocar: código de produção
- Entregável: testes

### T5.1 — Gravar sessões de pacotes
- Agente/modelo: protocol-re / sonnet · D · D1 prep, D4 execução
- **Bloqueio externo:** Victor grava capturas no jogo até **18/10**
- Objetivo: sessões gravadas e organizadas (só leitura, nada é enviado ao jogo)
- Arquivos: `captures/` (fora do git se sensível), roteiro de captura
- Passos: D1 entregar roteiro ao Victor (ações a fazer no jogo) → receber arquivos → indexar
- Aceite: ≥ 1 sessão por ação do roteiro, com anotação de tempo
- Verificação: arquivos reproduzíveis pelo replay
- Não tocar: código de captura que injete/escreva pacotes
- Entregável: roteiro + índice das capturas

### T5.2 — Diff de opcodes
- Agente/modelo: protocol-re / sonnet (C3) · D · D5–D7
- Objetivo: identificar opcodes de progresso (tarefas, recursos, timers)
- Arquivos: `docs/opcodes-diff.md`
- Passos: diff entre sessões → correlacionar com ações anotadas → hipóteses por opcode
- Aceite: lista opcode/campo/confiança
- Verificação: revisão TL
- Não tocar: parsers existentes
- Entregável: `docs/opcodes-diff.md`
