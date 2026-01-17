# Application — Estrutura de UseCases por contexto

Este README descreve a estrutura do projeto `Application` separada por contexto: `Clients`, `Accounts` e `Operations`.

Conveções:
- Padrão CQRS: `Command` / `Query` + `Handler` (MediatR)
- Validação com `FluentValidation`
- Mapeamento com `Mapster`
- Resultados com `FluentResults`
- Persistência de eventos via `IEventStoreRepository<TAggregate>` e publicação via `IEventDispatcherRepository`.
  A publicação de eventos já está implementada no projeto `CrossCutting.BuildBlocks` em `Documents/EventDocument.cs`, que provê retry com backoff, fallback e dead-letter para falhas na publicação.
- Todos os handlers devem ser assíncronos (`async`) e garantir idempotência via `RequestId`/`OperationId`.

Estrutura geral (por contexto)

UseCases
  - Clientes
    - Create
      - `CreateClientCommand.cs` (implements `IRequest<Result<CreateClientResult>>`)
      - `CreateClientHandler.cs` (implements `IRequestHandler<CreateClientCommand, Result<CreateClientResult>>`)
      - `CreateClientValidation.cs` (FluentValidation)
      - `CreateClientMap.cs` (Mapster)
      - `CreateClientResult.cs`
    - Get
      - `GetClienteQuery.cs` (implements `IRequest<Result<GetClientDto>>`)
      - `GetClienteHandler.cs`
      - `GetClienteMap.cs`

  - Contas
    - CreateAccount
      - `CreateAccountCommand.cs`
      - `CreateAccountHandler.cs`
      - `CreateAccountValidation.cs`
      - `CreateAccountMap.cs`
      - `CreateAccountResult.cs`
    - DefineCreditLimit
      - `DefineCreditLimitCommand.cs`
      - `DefineCreditLimitHandler.cs`
      - `DefineCreditLimitValidation.cs`
    - ActivateAccount
      - `ActivateAccountCommand.cs`
      - `ActivateAccountHandler.cs`
    - BlockAccount
      - `BlockAccountCommand.cs`
      - `BlockAccountHandler.cs`

  - Operacoes (financeiras)
    - Reserve
      - `ReserveCommand.cs`
      - `ReserveHandler.cs`
      - `ReserveValidation.cs`
      - `ReserveResult.cs`
    - Capture
      - `CaptureCommand.cs`
      - `CaptureHandler.cs`
      - `CaptureValidation.cs`
      - `CaptureResult.cs`
    - Refund
      - `RefundCommand.cs`
      - `RefundHandler.cs`
      - `RefundValidation.cs`
      - `RefundResult.cs`
    - Credit
      - `CreditCommand.cs`
      - `CreditHandler.cs`
      - `CreditValidation.cs`
      - `CreditResult.cs`
    - Debit
      - `DebitCommand.cs`
      - `DebitHandler.cs`
      - `DebitValidation.cs`
      - `DebitResult.cs`
    - Transfer
      - `TransferCommand.cs`
      - `TransferHandler.cs` (ou Saga/Coordinator)
      - `TransferValidation.cs`
      - `TransferResult.cs`

Regras transversais
- Idempotência: cada Command inclui `RequestId`/`OperationId` e deve ser registrado para evitar processamento duplicado.
- Concorrência: aplicar lock/mutex por `AccountId` ao executar operações que mutam estado.
- Eventos: alterações no aggregate sempre geram domain events que são persistidos e publicados assincronamente. Note que a lógica de publicação (retry, fallback, dead-letter) já está centralizada em `CrossCutting.BuildBlocks.Documents.EventDocument` — handlers devem apenas gravar no event store e delegar a publicação.
- Resiliência: publicação de eventos com retry exponencial e fallback (dead-letter) para falhas persistentes.
- Auditoria: event store como histórico imutável com metadados (origin, userId, requestId, timestamp).

Namespaces sugeridos
- `Application.UseCases.Clientes.Create`
- `Application.UseCases.Clientes.Get`
- `Application.UseCases.Contas.CreateAccount`
- `Application.UseCases.Operacoes.Reserve`

Posso gerar templates (Command/Handler/Validation/Map/Result) em cada pasta e renomear handlers existentes para os nomes em inglês seguindo esta convenção. Deseja que eu gere os templates agora?