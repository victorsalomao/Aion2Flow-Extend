# CLAUDE.md — Aion2 Lite (fork do Aion2Flow)

## Contexto
Companion de Aion 2 para Windows. Captura pacotes do jogo (somente leitura) via WinDivert e mostra um overlay de DPS e um painel de progresso diário e semanal.
O plano completo está em `docs/PLANO.md`. Siga as fases em ordem.

## Stack
- .NET 10, `net10.0-windows`, **Native AOT** (`PublishAot=true`). Evite reflection e serialização sem source generators. Use `System.Text.Json` com `JsonSerializerContext`.
- Avalonia + CommunityToolkit.Mvvm (compiled bindings).
- `TreatWarningsAsErrors=true`: o código não pode gerar nenhum warning.

## Estrutura
- `src/Aion2Flow.Capture`: captura e reassembly TCP
- `src/Aion2Flow.Protocol/Packets`: um parser por opcode (`PacketXXXXParser.cs`), usando `PacketSpanReader`
- `src/Aion2Flow.SceneRuntime`: Stores, Journal, Archive, Playback
- `src/Aion2Flow`: app (ViewModels, Views)
- Novos: `Aion2Flow.Persistence`, `Aion2Flow.Domain.Progress`, `Aion2Flow.Module.*`

## Regras
1. **Medir antes de otimizar.** Toda mudança de performance precisa do bench antes e depois, registrado em `docs/perf-baseline.md`.
2. Nunca escrever ou injetar pacotes, nem automatizar ações no jogo.
3. Parsers novos: zero alocação no caminho quente, com teste usando bytes gravados em `tests/`.
4. Módulos novos não referenciam a UI. A comunicação acontece por eventos ou interfaces registradas em DI.
5. Toda feature precisa funcionar em **modo manual** antes de ganhar leitura automática.
6. Rodar `dotnet build -c Release` e `dotnet test` antes de cada commit.
7. Manter a licença GPL-3.0 e os créditos do projeto original.

## Comandos
```
dotnet build -c Release
dotnet test
dotnet publish src/Aion2Flow -c Release -r win-x64
dotnet-counters monitor -n Aion2Flow --counters System.Runtime
dotnet-gcdump collect -n Aion2Flow
```
