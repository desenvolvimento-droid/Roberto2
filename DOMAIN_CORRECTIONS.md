# Correções de Domain (priorizadas)

Arquivo: `DOMAIN_CORRECTIONS.md`
Resumo: lista de correções e melhorias no domínio visando atender o Desafio Técnico (MVP com Event Sourcing). Cada item traz: prioridade, arquivos afetados, descrição do problema, impacto e solução recomendada (passos curtos).

---

## 1) Corrigir controle de versão ao reidratar (Critical / Alta)

- Arquivos afetados:
  - `src/CrossCutting.BuildBlocks/Aggregates/AggregateRoot.cs`

- Descrição:
  - O método `LoadFromHistory` aplica eventos do histórico mas não atualiza `OriginalVersao` após a reidratação. Isso faz com que o agregado acredite que sua versão observada ainda é 0 e cause `ConcurrencyException` na primeira gravação posterior.

- Impacto:
  - Quebra o mecanismo de concorrência otimista; todas as operações que persistirem eventos logo após ler o agregado irão falhar.

- Solução recomendada (implementação mínima):
  1. Após aplicar o histórico, setar `OriginalVersao = Versao`.
  2. Manter `_uncommittedEvents.Clear()` e atualizar `AtualizadoEm`.

- Snippet sugerido:

```csharp
// ao final de LoadFromHistory
_uncommittedEvents.Clear();
OriginalVersao = Versao;
AtualizadoEm = DateTime.UtcNow;
```

---

## 2) Evitar nomes de eventos ambíguos (Alta)

- Arquivos afetados:
  - `src/Domain/Entities/Clientes/Cliente.cs`
  - `src/Domain/Entities/Operacao/Operacao.cs`
  - (eventos e handlers correspondentes)

- Descrição:
  - Eventos como `ClienteEvents` e `OperacaoEvents` são pouco descritivos.

- Impacto:
  - Dificulta manutenção, rastreabilidade e integração com handlers/consumers.

- Solução recomendada:
  1. Renomear para nomes explícitos (ex.: `ClienteActivated`, `OperacaoConfirmed`).
  2. Atualizar todos os lugares que os produzem/consomem (When switches, publishers, read-model handlers).

---

## 3) Orquestração entre `Operacao` e `Conta` (Alta)

- Arquivos/áreas afetadas:
  - `src/Domain/Entities/Operacao/Operacao.cs`
  - `src/Domain/Entities/Contas/Conta.cs`
  - Camada de aplicação: handlers/command handlers / sagas

- Descrição:
  - `Operacao` descreve estados (Reserve, Capture, Transfer, Reversal), mas não existe orquestrador que garanta efeitos consistentes entre agregados (ex.: reserva reduz saldo em `Conta` quando operação criada/confirmada).

- Impacto:
  - Inconsistência entre agregados, falta de compensação automatizada em falhas, dificuldade para transfers atômicos.

- Solução recomendada (MVP):
  1. Implementar um `ProcessManager` / Saga para os fluxos críticos:
     - Transfer: reservar no origin -> aplicar crédito no destination -> confirmar; em falha -> reverter.
  2. Usar eventos como gatilho (ex.: `OperacaoProcessed` -> invocar `Conta.Reservar`), com idempotência.
  3. Garantir testes de integração para os fluxos.

---

## 4) Concorrência / Lock por `Conta` (Alta)

- Arquivos/áreas afetadas:
  - Repositório de Event Store
  - Handlers/ProcessManager

- Descrição:
  - Requisito: "operações concorrentes na mesma conta devem ser bloqueadas". O modelo atual usa somente optimistic concurrency via versão.

- Impacto:
  - Otimistic alone pode resultar em muitos retries sob alta concorrência; algumas operações (por exemplo, duas reservas simultâneas) podem competir causando falhas.

- Solução recomendada:
  1. Implementar retries exponenciais na camada de gravação (já suportado pela infra/event store).
  2. Para casos onde é necessário serializar fortemente, adicionar lock distribuído por `ContaId` (ex.: Redis lock ou advisory lock no DB) no handler que realiza modificações.
  3. Alternativa leve: enfileirar comandos por `ContaId` (partitioned queue) para processamento sequencial.

---

## 5) Publicação de eventos, Outbox e Retry (Alta)

- Arquivos/áreas afetadas:
  - `IEventDispatcherRepository` e implementações
  - Infra de persistência (repositório)

- Descrição:
  - O desafio exige publicação assíncrona de eventos, simulação de falhas e retry com backoff exponencial.

- Impacto:
  - Sem Outbox e retry, corre-se o risco de perda de eventos ou inconsistência entre store e broker.

- Solução recomendada:
  1. Implementar padrão Outbox: gravar eventos no mesmo contexto/transaction do event store; uma background worker publica as entradas do outbox.
  2. Publisher com retry exponencial (ex.: Polly) e mecanismos de DLQ/simulação de falhas para testes.
  3. Garantir que cada evento tenha `EventId`, `CorrelationId`, `CausationId` e metadata para idempotência.

---

## 6) Idempotência e _appliedEventIds (Média)

- Arquivos afetados:
  - `src/CrossCutting.BuildBlocks/Aggregates/AggregateRoot.cs`

- Descrição:
  - A proteção `_appliedEventIds` impede reaplicação acidental em memória, mas não persiste entre reidratações (a coleção é limpa em `RestoreFromSnapshotState`).

- Impacto:
  - Consumidores/handlers podem reprocessar eventos se não houver deduplicação ao nível de armazenamento/handlers.

- Solução recomendada:
  1. Persistir metadados de eventos aplicados quando necessário (por exemplo, em read-models ou em uma tabela de deduplicação por consumer).
  2. Garantir que handlers de eventos sejam idempotentes.

---

## 7) Snapshotting para performance (Média)

- Arquivos afetados:
  - `AggregateRoot` (hooks `CreateSnapshot` / `RestoreFromSnapshot`)

- Descrição:
  - Hooks existem, mas não há política de snapshot (quando criar, quando restaurar).

- Impacto:
  - Reidratação lenta com histórico longo; tempo de carga pode violar requisitos de performance.

- Solução recomendada:
  1. Implementar snapshots para `Conta` e `Cliente` com heurística (por exemplo, a cada N eventos ou tempo decorrido).
  2. Repositório deve tentar carregar snapshot + events posteriores.

---

## 8) Testes (Média)

- Arquivos/áreas afetadas:
  - Test projects (criar se inexistente)

- Descrição:
  - Faltam testes que exercitem concorrência, orchestrations e publicação de eventos com retries.

- Solução recomendada:
  1. Unit tests para invariantes do aggregate (`ValidateInvariants`).
  2. Integration tests para flows: `Reserve -> Capture -> Confirm -> Revert`.
  3. Concurrency tests: disparar N requisições concorrentes de reserva na mesma conta e validar comportamento (retries/locks).

---

## 9) Observabilidade e logging (Baixa)

- Arquivos/áreas afetadas:
  - Handlers, repositório, dispatcher

- Descrição:
  - Falta telemetria estruturada em pontos críticos (retries, falhas, latências).

- Solução recomendada:
  1. Adicionar logs contextuais com `CorrelationId` e métricas (contadores/histogramas).
  2. Instrumentar publisher e repositório para métricas de retry/falha.

---

# Checklist de Prioridade (resumo)

- Alta / Crítico: Corrigir `LoadFromHistory` (versão), orquestração entre `Operacao` e `Conta`, locking por `Conta`, outbox + retry.
- Média: Idempotência persistente, snapshotting, testes de integração e concorrência.
- Baixa: Renomeação de eventos (pode ser alta dependendo de time), observabilidade/metrics.

---

Se desejar, eu aplico agora a correção crítica (`LoadFromHistory`) no arquivo `src/CrossCutting.BuildBlocks/Aggregates/AggregateRoot.cs` e adiciono um teste unitário que demonstra o bug corrigido.
