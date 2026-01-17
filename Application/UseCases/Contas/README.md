# UseCases / Contas

This README describes the use cases for the `Contas` context, located in `Application/UseCases/Contas`.

Objetivo
- Implementar use cases do domínio `Contas` seguindo CQRS e Clean Architecture.

Arquivos esperados por use case
- `CreateAccount`:
  - `CreateAccountCommand.cs`
  - `CreateAccountHandler.cs`
  - `CreateAccountValidation.cs`
  - `CreateAccountMap.cs`
  - `CreateAccountResult.cs`

- `DefineCreditLimit`:
  - `DefineCreditLimitCommand.cs`
  - `DefineCreditLimitHandler.cs`
  - `DefineCreditLimitValidation.cs`

- `ActivateAccount`:
  - `ActivateAccountCommand.cs`
  - `ActivateAccountHandler.cs`

- `BlockAccount`:
  - `BlockAccountCommand.cs`
  - `BlockAccountHandler.cs`

Conveções
- Namespace sugerido: `Application.UseCases.Contas.{UseCase}`.
- Pastas e contextos usam o domínio em português (`Contas`) enquanto os `Command` / `Query` podem usar convenção em inglês para compatibilidade.
- Handlers devem aplicar lock por `AccountId` e garantir invariantes do aggregate `Conta`.
- Persistir eventos no Event Store; delegar publicação ao `CrossCutting.BuildBlocks.Documents.EventDocument`.

Testes
- Unit tests para regras do aggregate `Conta`.
- Integration tests com concorrência simulada.
