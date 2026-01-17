# Correções específicas por entidade (Cliente, Conta, Operacao)

Objetivo: implementar no documento as mudanças recomendadas para as três entidades:
- `src/Domain/Entities/Clientes/Cliente.cs`
- `src/Domain/Entities/Contas/Conta.cs`
- `src/Domain/Entities/Operacao/Operacao.cs`

As alterações propostas visam: corrigir nomes de eventos ambíguos, preparar os eventos para serem usados por um ProcessManager/Saga (orquestração entre `Operacao` e `Conta`), e melhorar legibilidade e idempotência. Não alteram a lógica de domínio central, apenas clarificam nomes e pontos de integração.

---

## 1) `Cliente.cs` — renomear evento ambíguo

Problema
- O método `Activate` grava `new ClienteEvents(Id)` que é um nome ambíguo.

Impacto
- Dificulta leitura e procura de eventos no event store; consumidores não entendem facilmente o propósito.

Correção recomendada (MVP)
- Renomear o evento produzido para `ClienteActivated`.
- Atualizar o `When` para reconhecer `ClienteActivated`.

Snippet sugerido (substituir em `Cliente.cs`):

```csharp
public void Activate()
{
    if (Status == ClienteStatus.Active)
        throw new InvalidOperationException("Cliente já está ativo.");

    RecordEvent(new ClienteActivated(Id));
}

// ...
case ClienteActivated:
    Status = ClienteStatus.Active;
    break;
```

Notas
- Atualizar a classe de evento correspondente (`ClienteActivated`) no projeto de eventos (se existir) e manter `EventId`, `OcorreuEm`, `CorrelationId`/`CausationId` conforme padrão do projeto.

---

## 2) `Operacao.cs` — nomes claros e preparação à orquestração

Problema
- O método `Confirm` grava `new OperacaoEvents(OperacaoId)` (ambíguo).
- `Operacao` descreve fluxos (Reserve/Capture/Transfer/Reversal) mas não contém integrações para aplicar efeitos em `Conta`.

Impacto
- Ambiguidade nos eventos e falta de gatilhos claros para consumers/ProcessManager.

Correção recomendada (MVP)
- Renomear o evento de confirmação para `OperacaoConfirmed`.
- Garantir que eventos-chave existam e sejam claros: `OperacaoCreated`, `OperacaoValidated`, `OperacaoProcessed`, `OperacaoConfirmed`, `OperacaoFailed`, `OperacaoReverted`.
- Não orquestrar `Conta` dentro do aggregate `Operacao`; ao invés disso, usar handlers na camada de aplicação que escutem `OperacaoProcessed` ou `OperacaoConfirmed` e invocam `Conta` através de repositório (event store) — documentado abaixo.

Snippet sugerido (substituir em `Operacao.cs`):

```csharp
public void Confirm()
{
    if (Status != OperacaoStatus.Processed)
        throw new InvalidOperationException("Operacao precisa estar Processed para Confirm.");

    RecordEvent(new OperacaoConfirmed(OperacaoId));
}

// ...
case OperacaoConfirmed:
    Status = OperacaoStatus.Confirmed;
    break;
```

Integração (ProcessManager / Handler)
- Implementar um handler que responda a `OperacaoProcessed` ou `OperacaoConfirmed` e:
  1. Carregue a `Conta` origem via `IEventStoreRepository<Conta>`.
  2. Execute `conta.Reservar(amount)` ou `conta.ActualizeSaldo(...)` conforme o tipo.
  3. Persista a `Conta` com `AppendAsync` e publique eventos via Outbox/dispatcher.
- Para transferências, aplicar reserva/debit no origin e credit no destination em passos transacionais lógicos com compensação em caso de falha (saga).

---

## 3) `Conta.cs` — confirmar nomes e validar efeitos de reserva

Problema
- `Reservar` gera `ReservationCompleted(Id, valor)` — nome ok porém recomenda-se padronizar prefixo/sufixo e eventos relacionados (ex.: `ContaReservationCompleted` ou `ReservationCompleted` com namespace claro).
- A lógica do `When` aplica `SaldoReservado += e.Valor; SaldoDisponivel -= e.Valor;` mas não existe evento que aumente `SaldoDisponivel` (ex.: crédito). Isso é comportamento esperado mas precisa ficar claro.

Impacto
- Pode confundir ao revisar histórico: onde saldo foi incrementado?

Correção recomendada (MVP)
- Padronizar nome de evento (opcional): `ContaReservationCompleted` ou manter `ReservationCompleted` mas com documentação e namespace adequados.
- Documentar que aumentos de `SaldoDisponivel` devem vir de eventos de crédito (`ContaCredited` / `MoneyCredited`) gerados por fluxos externos (ex.: operações do tipo Credit ou por capture de transferências).

Snippet sugerido (exemplo de padronização - opcional):

```csharp
public void Reservar(decimal valor)
{
    if (SaldoDisponivel + LimiteCredito < valor)
        throw new InvalidOperationException("Limite insuficiente.");

    RecordEvent(new ContaReservationCompleted(ContaId, valor));
}

// When:
case ContaReservationCompleted e:
    SaldoReservado += e.Valor;
    SaldoDisponivel -= e.Valor;
    break;
```

---

## 4) Boas práticas e próximos passos (curto prazo)

1. Atualizar classes de evento (types) conforme renomeações acima.
2. Implementar ProcessManager/Saga na camada de aplicação para coordenar `Operacao` ↔ `Conta`:
   - Ex.: quando `OperacaoProcessed` for publicado, Saga tenta reservar no origin; em sucesso publica evento `ContaReserved` e segue; em falha publica `OperacaoFailed`.
3. Garantir que `AggregateRoot.LoadFromHistory` esteja corrigido (veja `DOMAIN_CORRECTIONS.md`) para ajustar `OriginalVersao = Versao` ao final da reidratação.

