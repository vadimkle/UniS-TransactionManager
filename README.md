# TransactionManager
## Database Creation Notes
This project does not follow the migration approach. Instead, I'm using context.Database.EnsureCreated().

Typically, for database creation, we run:

```pshell
dotnet tool install --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add InitialCreate
dotnet ef database update
```
SQLite is used for testing purposes.

## Exception Handling
`ExceptionMiddleware` is used only when `ASPNETCORE_ENVIRONMENT` is set to `Production`. This means it does not fully meet the requirements (RFC 9457). I decided that during development and debugging, we need the ability to read the call stack.

## Launching from Visual Studio
`https prod` sets the `ASPNETCORE_ENVIRONMENT` variable to `Production`.

`https devd` sets the `ASPNETCORE_ENVIRONMENT` variable to `Development`.
## Business Logic
The `dateTime` property of a transaction should not be earlier than the last one created for the same client. This check was added in addition to provided requirements.

## Technical Notes
The `Clients` table is created to implement optimistic concurrency. When the `Balance` is changed during another database transaction, it triggers a DbUpdateConcurrencyException, causing the current transaction to be rolled back.

### Logging
Not yet implemented.

### Tests
Not yet implemented.