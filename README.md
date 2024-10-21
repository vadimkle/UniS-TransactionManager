# TransactionManager
## DB create notes
Normally, for database creation we run

dotnet tool install --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add InitialCreate
dotnet ef database update

This project does not suppose migrations approach, I'm using context.Database.EnsureCreated();

## 