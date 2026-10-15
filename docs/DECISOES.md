# Decisões do produto

## D-2026-10-15 — Playback no LiteMode: journal em disco só das últimas 5 lutas
- **Decisor:** Victor (15/10/2026).
- **Problema:** no LiteMode (T2.2), o journal das lutas já processadas é descartado. Os totais ficam corretos, mas a timeline bruta do playback fica vazia ou parcial.
- **Decisão:** incluir o journal no spill do archive em disco (T2.3), **só para as 5 lutas mais recentes**. A partir da 6ª, os arquivos da luta mais antiga são apagados. Lutas além das 5 últimas ficam só com o resumo/snapshot, sem timeline bruta.
- **Limpeza obrigatória:** apagar na eviction, limpar pastas órfãs ao iniciar (o mecanismo de 12 h da T2.3 já faz isso) e apagar tudo ao fechar o app. O objetivo é não pesar no disco do usuário.
- **Sem nuvem:** não há armazenamento remoto. Tudo é local e temporário (`%TEMP%/Aion2Flow/archive`).
- **Escopo:** nova task de follow-up da T2.3/T2.4, com teste de round-trip da timeline e da eviction da 6ª luta, e bench de disco/RAM antes e depois em `docs/perf-baseline.md`.
