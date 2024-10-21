using Microsoft.EntityFrameworkCore;
using TransactionManager.Middleware;
using TransactionManager.Services;
using TransactionManager.Storage;
using TransactionManager.Validators;

namespace TransactionManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddScoped<TransactionService>();
            builder.Services.AddScoped<TransactionValidator>();
            builder.Services.AddScoped<TransactionRepository>();

            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());


            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<TransactionContext>(options =>
                options.UseSqlite(connectionString));

            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<TransactionContext>();
                context.Database.OpenConnection();
                context.Database.EnsureCreated();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
