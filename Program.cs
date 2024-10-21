using Microsoft.EntityFrameworkCore;
using TransactionManager.Data;
using TransactionManager.Middleware;
using TransactionManager.Services;
using TransactionManager.Validators;

namespace TransactionManager
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddScoped<TransactionService>();
            builder.Services.AddScoped<TransactionValidator>();
            builder.Services.AddScoped<TransactionRepository>();

            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<TransactionContext>(options =>
                options.UseSqlite(connectionString)
                    /*.EnableSensitiveDataLogging()*/);

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            if (app.Environment.IsProduction())
            {
                //todo - check if we need use swagger on prod.
                app.UseSwagger();
                app.UseSwaggerUI();

                app.UseMiddleware<ExceptionMiddleware>();
            }

            await EnsureDb(app);

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }

        private static async Task EnsureDb(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TransactionContext>();
            await context.Database.OpenConnectionAsync();
            if (await context.Database.EnsureCreatedAsync())
            {
                await context.Database.ExecuteSqlRawAsync(@"CREATE TRIGGER UpdateClientVersion
AFTER UPDATE ON Clients
BEGIN
    UPDATE Clients
    SET Version = Version + 1
    WHERE rowid = NEW.rowid;
END;");
            }
        }
    }
}
