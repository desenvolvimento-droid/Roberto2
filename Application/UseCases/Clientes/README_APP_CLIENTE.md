# UseCases / Clientes

Este README descreve os casos de uso que devem residir na pasta `Application/UseCases/Clientes`.

Objetivo
- Implementar os use cases relacionados ao domínio `Clientes` seguindo CQRS (Commands / Queries) e a arquitetura definida no `Application/README.md`.

- 
Arquivos esperados por use case
- `Create`:
  - `CreateClientCommand.cs` (IRequest<Result<CreateClientResult>>) 
  - `CreateClientHandler.cs` (IRequestHandler<CreateClientCommand, Result<CreateClientResult>>)
  - `CreateClientValidation.cs` (FluentValidation)
  - `CreateClientMap.cs` (Mapster)
  - `CreateClientResult.cs`

- `Get`:
  - `GetClientQuery.cs` (IRequest<Result<GetClientDto>>)
  - `GetClientHandler.cs`
  - `GetClientMap.cs`

	- Interfaces para escrita: IEventStoreRepository<Cliente> e leitura: IReadOnlyEventStoreRepository<Cliente> devem ser injetadas via construtor nos handlers.
	- Interfaces para busca: IReadOnlyRepository<Cliente> deve ser injetada via construtor nos handlers de queries.
	- Result<> do pacote FluentResults deve ser usado para encapsular respostas.

Conveções e notas
- Namespace sugerido: `Application.UseCases.Clientes.{UseCase}` (ex.: `Application.UseCases.Clientes.Create`).
- Names dos `Command` / `Query` seguem a convenção CQRS em inglês (ex.: `CreateClientCommand`, `GetClientQuery`) para facilitar integração; pastas e contexto mantêm o nome de domínio em português (`Clientes`).
- Todos os handlers devem ser `async`, validar idempotência via `RequestId` / `OperationId`, e persistir domain events no `EventStore`.
- A publicação de eventos (retry/backoff/fallback/dead-letter) já está centralizada em `CrossCutting.BuildBlocks/Documents/EventDocument.cs`. Os handlers devem delegar a publicação ao event document ou ao `IEventDispatcherRepository`.

Auditoria
- Cada evento salvo deve incluir metadados: `origin`, `userId`, `requestId`, `timestamp`.
