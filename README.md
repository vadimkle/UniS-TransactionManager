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
The `dateTime` property of a transaction should not be earlier than the last one created for the same client. 
This check was added in addition to provided requirements.

Another issue is an ability to make balance **negative**.
I decided `transaction revert` has more priority than positive balance.
But it's subject to discuss.

## Technical Notes
The `Clients` table is created to implement optimistic concurrency. 
When the `Balance` is changed during another database transaction, it triggers a `DbUpdateConcurrencyException`, 
causing the current transaction to be rolled back.

It's probably not the best approach to use datetime as a row version, since SQLite does not keep fractional seconds, but for MS SQL it would work.
For SQLite I would suggest use `long` and autoincrement in trigger.

### Logging
Not yet implemented.

### Tests
Test cover most business logic implemented in `TransactionService`