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

## Launch from Visual Studio
`https prod` set "ASPNETCORE_ENVIRONMENT" = "Production" env variable

`https devd` set "ASPNETCORE_ENVIRONMENT" = "Development" env variable

## Business logic
I suppose transaction's "dateTime" property must be not earlier then the last one created (for the same client). 