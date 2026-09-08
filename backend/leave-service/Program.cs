using FluentValidation;
using HRM.LeaveService;
using HRM.LeaveService.Application;
using HRM.LeaveService.Infrastructure;
using HRM.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddHrmDefaults("HRM Leave Service");
builder.AddHrmAuth();
builder.AddHrmMessaging();

builder.Services.AddControllers(o => o.Filters.Add<ValidationFilter>());
builder.Services.AddValidatorsFromAssemblyContaining<CreateLeaveRequestValidator>();

builder.Services.AddDbContext<LeaveDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

builder.Services.AddScoped<ILeaveService, LeaveAppService>();
builder.Services.AddHostedService<EmployeeEventConsumer>();
builder.Services.AddHealthChecks().AddDbContextCheck<LeaveDbContext>("leave-db");

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LeaveDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    for (var attempt = 1; attempt <= 15; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            if (builder.Configuration.GetValue("Seed:Enabled", true))
                await LeaveSeeder.SeedAsync(db);
            logger.LogInformation("Leave database ready");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Database not ready (attempt {Attempt}): {Message}", attempt, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

app.UseHrmDefaults("HRM Leave Service");
app.Run();
