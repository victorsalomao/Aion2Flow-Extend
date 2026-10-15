# Aion2 Lite — S1 kickoff (14/10/2026, D0)

**Status geral:** 🟢 no prazo e adiantado. Já há 19 de 95 pontos concluídos antes do D1 (15/10), acima da curva do burn-up (meta de 12% no D3).

## Entregue
- **Base do projeto pronta:** fork criado, build com 0 warnings e 1071 testes verdes (25 de replay + 1046 unitários).
- **Medição de desempenho feita:** um harness de console mede o núcleo do app sem interface. Os resultados estão em `docs/perf-baseline.md`.
- **Próximo passo técnico definido:** o tech-lead decidiu a prioridade da Fase 2. Journal e arquivamento de lutas vêm antes do resto; animações e carregamento de idioma foram rebaixados porque já são preguiçosos.
- **Domínio de progresso (T3.2):** a lógica de progresso diário e semanal está pronta, com 8 testes.
- **Roteiro de capturas de pacotes (para T5.1):** escrito e aguardando a sua gravação.

## Em andamento / atrasado
- **Fase 2 (modo Lite, T2.1 a T2.7):** 23 pontos, ainda não iniciada. É o caminho crítico de RAM. Começa no D1 com a flag `LiteMode`.
- **Fase 3 (persistência e fuso horário, T3.1, T3.3 a T3.5):** 16 pontos. Pode andar em paralelo com a Fase 2.
- **Diff de opcodes (T5.2):** depende da sua captura (T5.1), por isso fica bloqueado até lá.
- **Pendência de ambiente:** o app completo com interface e WinDivert ainda não foi executado. Não há medição de RAM em uso real, CPU em luta, startup nem tamanho do executável AOT.

## Números
| Indicador | Atual | Meta | Situação |
|---|---|---|---|
| Escopo concluído (F0–F5) | 19 / 95 pts (20%) | 12% no D3 | Adiantado |
| S1 concluída (planejado 62 pts) | 19 pts (31%) | — | Em dia |
| RAM idle | 25,5 MB (só núcleo, sem interface) | < 60 MB | Inconclusivo |
| RAM após 10 lutas | 249 MB (só núcleo) | < 100 MB | Estouro de cerca de 2,5x, mesmo sem interface |
| CPU em luta | sem dado | < 2% | Sem dado |
| Startup | 77–90 ms até o Main (não é a primeira janela) | < 1 s | Sem dado no app real |
| Tamanho do AOT | sem dado | não regredir mais de 10% | Sem dado |

Os pontos concluídos são: T0.2, T0.3, T0.4, T1.1 a T1.4, G1 e T3.2. O T0.1 segue em `doing`, com 1 ponto.

## Riscos e bloqueios (ação sua)
1. **Capturas de pacotes até 18/10 (T5.1):** sem elas, a Fase 5 perde a janela da S1 e os 7 pontos de T5.1 e T5.2 viram carry-over. Roteiro pronto em `docs/ROTEIRO-CAPTURAS.md`, com build Debug.
2. **Medição no app completo:** é preciso rodar o app com WinDivert e privilégio de administrador, com o jogo aberto, para obter RAM real, CPU em luta e H4/H5. Sem isso, a meta de RAM não pode ser confirmada.
3. **Fork no GitHub:** a tabela do backlog diz que o fork ainda está pendente, mas o log de progresso diz que ele já existe. Confirme qual é o estado correto.
4. **Limpeza:** remover `D:\tmpbench\` (lixo de build).

## Próximos passos
- D1 (15/10): iniciar T2.1 (`LiteMode`) e T3.1 em paralelo, e fechar T0.1.
- Até 18/10: receber suas capturas e começar T5.2 assim que chegarem.
- Antes da Fase 2 fechar: rodar a medição do app completo para ter RAM idle, CPU em luta e startup reais.
