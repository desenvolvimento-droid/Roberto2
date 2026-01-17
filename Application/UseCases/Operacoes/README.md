# UseCases / Operacoes

Este README descreve os use cases do domínio `Operacoes` (financeiras) em `Application/UseCases/Operacoes`.

Objetivo
- Implementar operações financeiras (Reserve, Capture, Refund, Credit, Debit, Transfer) seguindo CQRS e garantindo idempotência, locks por conta e geração de domain events.

Arquivos esperados por use case
- `Reserve`:
  - `ReserveCommand.cs`
  - `ReserveHandler.cs`
  - `ReserveValidation.cs`
  - `ReserveResult.cs`

- `Capture`:
  - `CaptureCommand.cs`
  - `CaptureHandler.cs`
  - `CaptureValidation.cs`
  - `CaptureResult.cs`

- `Refund`:
  - `RefundCommand.cs`
  - `RefundHandler.cs`
  - `RefundValidation.cs`
  - `RefundResult.cs`

- `Credit`:
  - `CreditCommand.cs`
  - `CreditHandler.cs`
  - `CreditValidation.cs`
  - `CreditResult.cs`

- `Debit`:
  - `DebitCommand.cs`
  - `DebitHandler.cs`
  - `DebitValidation.cs`
  - `DebitResult.cs`

- `Transfer`:
  - `TransferCommand.cs`
  - `TransferHandler.cs` (ou Saga/Coordinator)
  - `TransferValidation.cs`
  - `TransferResult.cs`

Conveções e requisitos
- Namespace sugerido: `Application.UseCases.Operacoes.{UseCase}`.
- Todos os comandos incluem `RequestId`/`OperationId` para idempotência.
- Antes de alterar estado, aplicar lock por `AccountId`.
- Persistir eventos no Event Store; delegar publicação ao `CrossCutting.BuildBlocks.Documents.EventDocument`.
- Implementar retry/exponential backoff na publicação e usar dead-letter para falhas persistentes (já implementado no EventDocument).

Testes
- Unit tests para invariantes do aggregate `Conta` e `Operacao`.
- Integration tests para simular concorrência, falhas de publicação e retries.
