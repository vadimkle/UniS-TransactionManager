# TransactionManager
## DB create notes
Normally, for database creation we run
```pshell
dotnet tool install --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add InitialCreate
dotnet ef database update
```
This project does not suppose migrations approach, I'm using context.Database.EnsureCreated();

SqlIt is used as test approach.

## Launch from Visual Studio
`https prod` set "ASPNETCORE_ENVIRONMENT" = "Production" env variable

`https devd` set "ASPNETCORE_ENVIRONMENT" = "Development" env variable

## Business logic
I suppose transaction's "dateTime" property must be not earlier then the last one created (for the same client). 

## Technical notes
Table `Clients` is created in order to implement optimistic concurrency. When `Balance` is changed during another 
db transaction it causes DbUpdateConcurrencyException and current transaction is rolled back

### Logging
Not implemented

### Tests
Not implemented