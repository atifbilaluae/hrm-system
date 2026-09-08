using FluentValidation;
using HRM.EmployeeService;
using HRM.EmployeeService.Application;
using HRM.EmployeeService.Infrastructure;
using HRM.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddHrmDefaults("HRM Employee Service");
builder.AddHrmAuth();        // validates tokens issued by Identity Service
builder.AddHrmMessaging();   // RabbitMQ publisher

builder.Services.AddControllers(o => o.Filters.Add<ValidationFilter>());
builder.Services.AddValidatorsFromAssemblyContaining<CreateEmployeeRequestValidator>();

builder.Services.AddDbContext<EmployeeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "hrm:";
});

builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IEmployeeService, EmployeeAppService>();
builder.Services.AddHealthChecks().AddDbContextCheck<EmployeeDbContext>("employee-db");

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    for (var attempt = 1; attempt <= 15; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            if (builder.Configuration.GetValue("Seed:Enabled", true))
                await EmployeeSeeder.SeedAsync(db);
            logger.LogInformation("Employee database ready");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Database not ready (attempt {Attempt}): {Message}", attempt, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

app.UseHrmDefaults("HRM Employee Service");
app.Run();
