# Perf baseline (Sprint 1, T1.1-T1.4)

Data: 2026-10-14 · branch aion2-lite · .NET 10.0.402 · Release (JIT, nao AOT) · sem alteracao de codigo de producao.

## Metodo e LIMITACAO (leia antes dos numeros)
- O app real (Avalonia + WinDivert) exige driver/admin e jogo ativo: **nao foi executado**. Sem dado de: RAM do app completo, CPU em luta com UI, startup ate primeira janela, tamanho do exe AOT.
- Substituto: `tools/BenchHarness` (console, so referencia Capture + SceneRuntime) reproduz os logs de `tests/Aion2Flow.ReplayTests/Fixtures/logs` via `PacketLogReplayService` (pipeline real: reassembly, parsers, Stores, Journal). Mede o **core sem UI**. "10 lutas" = os 10 maiores logs em sequencia (137 470 linhas), resultados mantidos na RAM (superestima: o app guarda 10 arquivos + 1 cena viva).
- Rodar: `pwsh tools/bench.ps1 -Scenario idle|session|fights [-Fights N] [-Runs 2]` -> `tools/bench-out/*.csv`.
- `startup_ms` = tempo de processo ate Main (nao e "primeira janela").

## Baseline (2 runs cada; variacao entre runs < 10% em todas as metricas de memoria)
| Cenario | WS pos-GC (MB) | WS pico (MB) | Heap gerenciado pos-GC (MB) | CPU medio durante replay | Startup harness (ms) |
|---|---|---|---|---|---|
| idle (runtime + cena vazia) | 25,5 | 25,5 | 0,3 | n/a | 77-90 |
| session (5 logs, ~94k linhas, proxy "5 min") | 194,8 / 194,8 | 194,4 / 194,5 | 138,2 | ~100% de 1 core (replay a velocidade maxima, nao comparavel a luta real) | 79-80 |
| fights10 (10 logs, 137k linhas) | 249,5 / 248,7 | 252,0 / 252,2 | 187,2 | ~100% (idem) · 553 MB alocados, GC 55/21/11 (g0/g1/g2) | 81-85 |

Metas METRICS §1: RAM idle < 60 MB (harness 25,5 MB, sem UI - inconclusivo), RAM apos 10 lutas < 100 MB (**249 MB no core, estouro de ~2,5x mesmo sem UI**), CPU em luta < 2% (sem dado confiavel), startup < 1 s (sem dado). Tendencia: 5 logs -> 195 MB, 10 logs -> 249 MB (crescimento sublinear por logs de tamanhos diferentes, mas sem plato).

## gcdump apos 10 lutas (T1.3)
Arquivo reprocessavel: `docs/perf-10fights.gcdump` (processo mantido vivo apos o replay; `dotnet-gcdump report`). **Aviso:** o runtime .NET 10 so oferece buffering lossy; o dump soma 196 MB de heap mas o `report` lista ~25 MB por tipo, entao a tabela abaixo e parcial (so grandes arrays).

| Tipo | Contagem | Bytes (report) |
|---|---|---|
| `CombatMechanicEventRecord[]` (MechanicStore._events) | 1 + 4 + 4 | 13,6 MB + 1,1 MB + 0,2 MB |
| `CombatSnapshotChange[]` (change logs Combat/Mechanic) | 3 + 11 + 1 | 3,1 MB + 0,3 MB + 0,1 MB |
| `SkillSemanticRuntimeNode[]` (catalogo de recursos) | 1 | 1,9 MB |
| `Entry<int,SkillDisplayEntry>[]` / `SkillSemanticRuntimeSlot[]` / `Entry<int,SkillDefinition>[]` | 1 cada | 0,56 / 0,41 / 0,35 MB |
| `CombatEventRecord[]` (segmentos de eventos) | 136 | 0,21 MB |
| `RawPacketReference[]` / `StoredObservedEventHeader[]` (Journal) | 621 / 621 segmentos | 0,10 / 0,02 MB |
| Instancias | 10 `ObservedEventJournal`, 621 `ObservedEventStorageSegment` (512 entradas cada, ~318k eventos) | |

Top 10 por bytes: os 3 primeiros acima + SkillSemanticRuntimeNode[], Entry<SkillDisplayEntry>[], System.Int32[] (0,5 MB), SkillSemanticRuntimeSlot[], Entry<SkillDefinition>[], CombatSnapshotChange[] (0,29 MB), SkillDisplayEntry[].

## Hipoteses H1-H7 (T1.4) - ordenadas por ganho estimado
Ganho = estimativa a partir do heap pos-GC (187 MB/10 lutas, ~19 MB por luta no core); o gcdump lossy nao fecha 100% da atribuicao.

| # | Hipotese | Veredito | Evidencia | Ganho estimado |
|---|---|---|---|---|
| H1 | Journal guarda todos os eventos | **Confirmada (inconclusiva em bytes)** | `ObservedEventJournal.cs:6-12,202` segmentos de 512 sem limite nem trim; gcdump: 10 journals, 621 segmentos (~318k eventos retidos) | Maior fatia do residual nao listado (~100+ MB das 10 lutas); validar com gcdump nao-lossy no Fase 2 |
| H2 | 10 lutas inteiras em RAM | **Confirmada** | `EncounterArchiveService.cs:9` `MaxHistoryCount = 10`; `ArchivedEncounterRecord` retem `ScenePayload` completo (`SceneArchivePayload.cs:21` inclui `mechanicEvents`) | multiplica H1/H7 por 10: ate ~150-180 MB |
| H7 | Change logs/eventos nas Stores | **Confirmada** | `MechanicStore.cs:10,15` e `CombatStore.cs:16` `List<>` sem teto; gcdump: `CombatMechanicEventRecord[]` 13,6 MB (maior objeto) + `CombatSnapshotChange[]` ~3,5 MB | ~15-20 MB nas 10 lutas (ate ~2 MB/luta com ring buffer) |
| H3 | ScenePlaybackViewModel sempre carregado | **Descartada (como descrita)** | VM so e criado ao abrir a janela: `MainWindow.axaml.cs:878` (`new ScenePlaybackViewModel` sob demanda, `TryActivate` reaproveita). O custo real do playback e o Journal (H1) | ~0 MB direto |
| H6 | 4 packs de idioma | **Descartada** | `ResourceCatalog.cs:19` `Load(language)` carrega shared + 1 locale sob demanda (cache por idioma); packs: shared 1,3 MB + locale ~0,2 MB. Catalogo ocupa ~4-5 MB no gcdump | <= 1 MB (so se usuario trocar idioma) |
| H5 | Buffers 1 MB x 64 candidatos | **Inconclusiva (provavelmente irrelevante)** | `CaptureBufferLimits.cs:5-13` teto total `CandidateStreamsTotalByteLimit` = 4 MB, vida 2 s; replay nao exercita captura WinDivert | <= 4 MB pior caso |
| H4 | AnimatedItemsView animada | **Inconclusiva** | Sem UI no bench. Codigo: duracoes 180-220 ms e animacao so em move/add/remove (`AnimatedItemsView.cs:27-28,174,304`), sem timer continuo evidente; CPU de render precisa de medicao no app real | Sem dado (CPU, nao MB) |

## Prioridade recomendada para Fase 2
1. T2.2/H1 (Journal -> agregados no LiteMode) e T2.3/H2 (arquivar em disco, manter resumo) - juntos respondem pela quase totalidade do estouro de RAM.
2. T2.7/H7 (ring buffer nos change logs/mechanic events) - ganho pequeno e barato.
3. Descartar/rebaixar T2.4 (H3) e T2.6 (H6): ja sao lazy; manter apenas se medir ganho no app real.
4. Antes: obter medicao do app completo (RAM idle, CPU em luta, H4/H5) em maquina com WinDivert/admin, e gcdump de um .NET 11 ou via `DOTNET_gcServer=0` com heap menor para validar a atribuicao de H1.

## T2.3 - Archive em disco comprimido (2026-10-14)
Mudanca: `EncounterArchiveService` (LiteMode) grava os arrays de eventos de cada luta (`CombatEventRecord`, `CombatMechanicEventRecord`, `CombatResourceEventRecord`) em arquivo Brotli (Fastest) em `%TEMP%/Aion2Flow/archive/<guid>/` e os remove da RAM; `SceneArchivePayload.CreateDetailDelta` recarrega sob demanda (sem cache). Arquivo e apagado ao sair do historico (10 lutas); pastas > 12 h sao limpas na inicializacao. Fica em RAM: snapshot, resumo (pares/combatentes), indice de detalhe, `TimelineSegment` (Journal = T2.2). Round-trip: `Archive_Spill_RoundTrips_Detail_And_Deletes_File_On_Eviction`.
Bench: `tools/bench.ps1 -Scenario archive-ram|archive-spill -Fights 10 -Runs 2` (10 maiores logs, so o historico de arquivos e retido; resultados do replay descartados). Variacao entre runs < 1% (memoria).

| Cenario (10 lutas) | WS pos-GC (MB) | WS pico (MB) | Heap pos-GC (MB) | CPU medio (replay max. velocidade) | Disco |
|---|---|---|---|---|---|
| archive-ram (antes, LiteMode off) | 256,0 | 261,6 | 134,7 | 74-82% | 0 |
| archive-spill (depois, LiteMode on) | 217,2 | 241,4 | 97,7 | 84-89% (gravacao Brotli + variacao entre runs ~10%) | ~12 MB |

Ganho: -37 MB de heap (-27%) e -39 MB de WS; por luta ~3,7 MB, mas a RAM por luta ainda cresce ~9,8 MB: o residual e o Journal retido via `TimelineSegment` (T2.2) e o indice de ordinais por combatente (~2x8 bytes por evento, candidato a spill tambem). Private Bytes e idle/5 min nao medidos (harness sem UI). Custo de CPU: compressao ocorre uma vez por luta arquivada (fora do caminho de pacotes); carga so ao abrir detalhes.

## T2.2 - Journal -> agregados incrementais (LiteMode)
Mudanca: `ObservedEventJournal.DiscardConsumed` + `TrimBefore(ordinal)` (libera segmentos ja projetados); `SceneReadModelOwner.RefreshCore` chama o trim; `SceneLiveReadModel(..., liteMode)` liga a flag (ligada ao `AppSettings.LiteMode` em `Program.cs`/`WinDivertCaptureService`). Limitacao: playback bruto de faixas ja descartadas fica indisponivel em LiteMode (escopo de T2.4/T2.3). Bench com refresh simulado a cada 1024 linhas (cadencia da UI) nos dois modos; `tools/bench.ps1 -Lite`.
Teste de paridade: `LiteModeJournalParityTests` (25 logs, DPS/dano/cura/hits identicos full x lite, journal menor em lite).

| Cenario (core sem UI, 2 runs) | WS pos-GC MB | Heap pos-GC MB | CPU ms (wall ms) |
|---|---|---|---|
| idle | 25,3 -> 25,3 | 0,3 -> 0,3 | n/a |
| fights10 antes (journal completo) | 249,5 | 187,0 | 4969-5281 (6627-6963) |
| fights10 depois (LiteMode) | 170,6 | 78,3 | 4672-5188 (5145-5713) |

Ganho: -79 MB WS (-32%), -109 MB heap (-58%), CPU nao subiu (wall -20%). Residual 78 MB = archives (T2.3) + change logs (T2.7). Private Bytes e "5 min"/CPU de UI nao medidos (harness sem UI).
