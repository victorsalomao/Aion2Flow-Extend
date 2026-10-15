# Aion2 Lite — Plano de Fork e Implementação

Base: fork de **crazyde21/Aion2Flow-Extend** (licença GPL-3.0, o que obriga o fork a também ser GPL e de código aberto).
Objetivo: um companion **ultra leve** para Aion 2. Ele combina o DPS de party em boss com um painel de progresso diário e semanal do jogador.

---

## 1. Análise do código atual (resumo técnico)

### Stack
| Item | Tecnologia |
|---|---|
| Runtime | .NET 10, `net10.0-windows`, win-x64 |
| UI | Avalonia (Themes.Simple) + CommunityToolkit.Mvvm, compiled bindings |
| Build | **PublishAot=true**: o app já é compilado para nativo |
| Captura | **WinDivert** (driver kernel, modo sniff), não usa Npcap |
| Update | Velopack |
| Código | ~91k linhas de C#, `TreatWarningsAsErrors`, `AllowUnsafeBlocks` |

### Projetos (pipeline de dados)
```
Aion2Flow.WinDivert      → interop nativo com o driver (handles, flags, layers)
Aion2Flow.Capture        → seleção da conexão do jogo, reassembly TCP, PacketCaptureChannel
Aion2Flow.Protocol       → ~40 parsers por opcode (Packet0438DamageParser, Packet4536…)
                           + PacketSpanReader (leitura com Span, sem alocação)
Aion2Flow.Resources      → catálogos binários (skills, NPCs, ícones), 4 idiomas (~1.9 MB)
Aion2Flow.SceneRuntime   → Stores (Combat, Entity, Aura, Resource, Mechanic, BossFocus),
                           Journal (todos os eventos), Archive (10 lutas), Playback
Aion2Flow (app)          → ViewModels, Views .axaml, UiFrameBatchService (UI em lote)
tests/                   → UnitTests + ReplayTests (reproduz pacotes gravados)
```

### O que já está bom (não mexer sem medir)
- Native AOT já está ligado.
- Os parsers usam `Span`, então não fazem cópias desnecessárias.
- `FrameBatchedObservableObject` e `UiFrameBatchService` já agrupam as notificações de UI por frame.
- Os buffers de captura têm limites definidos em `Capture/CaptureBufferLimits.cs`.
- Existem testes de replay. **Essa é a rede de segurança da refatoração.**

### Onde está o peso (hipóteses, precisam ser confirmadas com medição)
| # | Suspeito | Arquivo | Por quê |
|---|---|---|---|
| H1 | Journal guarda **todos** os eventos de cada luta para o playback | `SceneRuntime/Journal/ObservedEventJournal.cs`, `Observation/JournalingRuntimeObservationSink.cs` | Cresce de forma linear com a duração da luta |
| H2 | Histórico de 10 lutas inteiras em RAM | `Archive/EncounterArchiveService.cs` (`MaxHistoryCount = 10`) | Multiplica a H1 por 10 |
| H3 | Playback/timeline (o maior ViewModel, ~2.2k linhas) | `ViewModels/ScenePlaybackViewModel.cs`, `SceneRuntime/Playback/*` | É uma feature "experimental" que fica sempre carregada |
| H4 | `AnimatedItemsView` com barras animadas (~1.4k linhas) | `Controls/AnimatedItemsView.cs` | Animação contínua gasta CPU/GPU |
| H5 | Buffers de 1 MB por stream × 64 candidatos | `CaptureBufferLimits.cs` | Teto de ~4 MB no pior caso, provavelmente ok |
| H6 | 4 packs de idioma | `Resources/Packs/*.bin` | Basta carregar o idioma ativo |
| H7 | Change logs nas Stores | `Stores/CombatStore.cs`, `MechanicStore.cs` | Listas que só crescem durante a luta |

---

## 2. Visão do produto (o que o jogador espera)

| Módulo | Feature | Fonte do dado | Dificuldade |
|---|---|---|---|
| **DPS** (já existe) | DPS da party em boss, rDPS, por skill | Pacotes (já mapeado) | ✅ base pronta |
| **Diárias/Semanais** | Checklist de diárias e semanais por personagem, com reset automático | Manual + timers de reset | Fácil |
| **Resinas/energia** | Quantas gastei, quantas restam, tempo de recarga | Pacotes (opcode ainda **não mapeado**) ou entrada manual | Média/Difícil |
| **Gold do dia** | Saldo no início do dia vs. agora | Pacotes (opcode de inventário/moeda, **não mapeado**) | Difícil |
| **Pontos de Abismo do dia** | Mesmo modelo do gold | Pacotes (**não mapeado**) | Difícil |
| **Alts** | Lista de personagens, com progresso de cada um | Login/seleção de personagem (`Packet4536PcMetadataParser` dá uma pista) | Média |
| **Timers de eventos** | Próximos eventos, bosses de campo, resets | Tabela estática (JSON) + relógio do servidor | Fácil |
| **Temporada** | Pontos da temporada, meta e quanto falta por dia | Manual primeiro, pacote depois | Fácil→Média |
| **Rotação/Builds** | Sugestão de rotação baseada nos logs de luta | Histórico local + LLM opcional | Média |
| **Crafting** | Segundo plano | — | Fase futura |

**Regra de ouro:** toda feature nasce **manual primeiro** (o usuário digita o valor) e só depois ganha leitura automática por pacote. Assim a UI e o domínio ficam prontos sem depender de engenharia reversa.

---

## 3. Arquitetura alvo

```
                ┌───────────────── Core (já existe) ─────────────────┐
WinDivert → Capture → Protocol(parsers) → SceneRuntime(Stores)       │
                └──────────────┬─────────────────────────────────────┘
                               │ eventos de domínio (IObservable / Channel)
              ┌────────────────┼──────────────────────────┐
              ▼                ▼                          ▼
     Module.Combat     Module.Progress (NOVO)     Module.Schedule (NOVO)
     (DPS/boss)        gold, abismo, resinas,     timers, resets,
                       diárias, alts, temporada   eventos
              │                │                          │
              └────── Persistence (NOVO): SQLite ou JSON por personagem
                               │
                        UI (Avalonia): Overlay compacto + Painel
                               │
                     Module.Insights (NOVO, opcional): rotação/LLM
```

**Padrões que você vai praticar:**
- **Strategy:** um parser por opcode. Já existe e você vai seguir o mesmo modelo para os opcodes novos.
- **Observer/Pub-Sub:** os módulos assinam os eventos do Core e o Core não conhece os módulos.
- **Repository:** a camada `Persistence` esconde se o armazenamento é SQLite ou JSON.
- **Plugin/Module:** cada módulo é um projeto separado (`Aion2Flow.Module.Progress`) e registrado por DI.
- **State machine:** reset diário e semanal (`Pendente → Feito → Resetado`).

**Decisão de performance:** o overlay compacto mostra só números. O painel completo abre sob demanda (lazy) e é **descarregado** quando fecha.

---

## 4. Roadmap e tarefas

### Fase 0 — Setup (1–2 dias)
- [ ] T0.1 Fazer fork no GitHub, clonar e compilar com o .NET 10 SDK no Windows
- [ ] T0.2 Rodar `dotnet test` (Unit + Replay) e registrar a baseline verde
- [ ] T0.3 Criar `CLAUDE.md` na raiz (modelo em `CLAUDE.md` deste pacote)
- [ ] T0.4 Configurar CI no GitHub Actions: build + testes no `windows-latest`

### Fase 1 — Medir antes de otimizar (2–3 dias)
- [ ] T1.1 Criar o script `tools/bench.ps1`: abre o app, roda um replay de luta e coleta o Working Set, o Private Bytes e a CPU média com `dotnet-counters`
- [ ] T1.2 Registrar a baseline em `docs/perf-baseline.md`: idle, luta de 5 min e após 10 lutas
- [ ] T1.3 Gerar um heap snapshot com `dotnet-gcdump` depois de 10 lutas e anotar os top 10 tipos por tamanho
- [ ] T1.4 Confirmar ou descartar as hipóteses H1–H7

### Fase 2 — Modo ULTRA leve (1–2 semanas)
- [ ] T2.1 Adicionar a flag `LiteMode` (padrão **ligado**) em Settings
- [ ] T2.2 (H1) Com LiteMode, trocar o Journal completo por **agregados incrementais**. O DPS e os totais por skill não precisam da lista de eventos.
- [ ] T2.3 (H2) Serializar as lutas arquivadas para disco (arquivo comprimido) e manter na RAM só o resumo
- [ ] T2.4 (H3) Carregar o playback só quando o usuário abrir a janela (lazy loading) e liberar a memória ao fechar
- [ ] T2.5 (H4) Desligar as animações no LiteMode e limitar o refresh da UI a 4 Hz
- [ ] T2.6 (H6) Carregar só o pack do idioma ativo
- [ ] T2.7 (H7) Limitar o tamanho dos change logs (ring buffer)
- [ ] T2.8 Testar o ajuste de GC: `<ServerGarbageCollector>false</ServerGarbageCollector>`, `<ConcurrentGarbageCollection>false</ConcurrentGarbageCollection>`, `<TieredPGO>` e `GCHeapHardLimit`. Medir cada opção isoladamente.
- [ ] T2.9 Rodar o bench de novo e comparar com a baseline. **Meta: −50% de RAM depois de 10 lutas.**

### Fase 3 — Fundação dos novos módulos (1 semana)
- [ ] T3.1 Criar o projeto `Aion2Flow.Persistence`: SQLite (`Microsoft.Data.Sqlite`, compatível com AOT) com as tabelas `Character`, `DailySnapshot`, `TaskState` e `SeasonProgress`
- [ ] T3.2 Criar o projeto `Aion2Flow.Domain.Progress`: modelos puros, sem dependência de UI
- [ ] T3.3 Criar o `ResetClock`: calcula os resets diário e semanal pelo fuso do servidor, que é configurável (KR/TW/NA/EU)
- [ ] T3.4 Fazer a UI de navegação: abas Overlay | Hoje | Semana | Alts | Timers
- [ ] T3.5 Escrever testes unitários do `ResetClock`, incluindo virada de dia, de semana e horário de verão

### Fase 4 — Diárias, Semanais, Alts e Timers (MVP manual) (1–2 semanas)
- [ ] T4.1 Checklist de diárias e semanais definido em JSON (`data/tasks.json`), editável pelo usuário
- [ ] T4.2 Cadastro de personagens (alts) com o progresso de cada um
- [ ] T4.3 Campos manuais: resinas, gold, pontos de abismo e pontos de temporada, com o delta "hoje" calculado
- [ ] T4.4 Recarga de resina: informar o valor atual e a taxa, e o app calcula quando enche e mostra uma notificação
- [ ] T4.5 Timers de eventos lidos de `data/events.json`, com contagem regressiva e notificação
- [ ] T4.6 Calculadora de temporada: meta, dias restantes e **pontos por dia necessários**

### Fase 5 — Leitura automática por pacotes (contínuo)
- [ ] T5.1 Usar a ferramenta de diagnóstico que já existe (`Capture/Diagnostics/PacketLogReplayService`) para gravar sessões: antes e depois de gastar resina, ganhar gold e ganhar abismo
- [ ] T5.2 Fazer diff de opcodes para achar os pacotes de moeda, energia e personagem
- [ ] T5.3 Criar um parser para cada opcode novo seguindo o padrão de `Packet*Parser.cs` + teste com bytes gravados
- [ ] T5.4 Publicar os valores lidos no `Module.Progress`. O manual continua como fallback.
- [ ] T5.5 Montar uma tabela de opcodes versionada em JSON, para sobreviver a patches do jogo sem precisar lançar uma versão nova

### Fase 6 — Insights / IA (opcional)
- [ ] T6.1 Gerar o resumo de cada luta em JSON (skills, uso, uptime de buffs, DPS por fase)
- [ ] T6.2 Regras locais: "skill X ficou N segundos fora de cooldown", "buff Y caiu"
- [ ] T6.3 Integração opcional com um LLM (API key do usuário, desligado por padrão): enviar o resumo e receber sugestões de rotação
- [ ] T6.4 Builds: importar e exportar no formato do BAHION (aion2-builder) ou criar um link para ele

### Fase 7 — Crafting (backlog)

---

## 5. Metas de performance (Definition of Done)

| Métrica | Meta |
|---|---|
| RAM idle | < 60 MB |
| RAM após 10 lutas | < 100 MB e sem crescimento contínuo |
| CPU em luta | < 2% médio |
| Startup | < 1 s |

## 6. Riscos
- **ToS da NCSOFT:** o app só lê pacotes, nunca escreve nem injeta. Não automatize ações no jogo.
- **Patches do jogo** mudam os opcodes. Mitigação: tabela versionada (T5.5) e testes de replay.
- **Criptografia de pacotes** pode chegar no futuro. Mitigação: todo módulo funciona em modo manual.
- **GPL-3.0:** manter os créditos e a licença, e publicar o código do fork.
