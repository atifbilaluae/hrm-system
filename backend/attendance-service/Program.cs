using FluentValidation;
using HRM.AttendanceService;
using HRM.AttendanceService.Application;
using HRM.AttendanceService.Domain;
using HRM.AttendanceService.Infrastructure;
using HRM.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddHrmDefaults("HRM Attendance Service");
builder.AddHrmAuth();
builder.AddHrmMessaging();

builder.Services.AddControllers(o => o.Filters.Add<ValidationFilter>());
builder.Services.AddValidatorsFromAssemblyContaining<CreateCorrectionRequestValidator>();

builder.Services.AddDbContext<AttendanceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

builder.Services.AddScoped<IAttendanceService, AttendanceAppService>();
builder.Services.AddHealthChecks().AddDbContextCheck<AttendanceDbContext>("attendance-db");

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    for (var attempt = 1; attempt <= 15; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            if (!await db.Policies.AnyAsync())
            {
                db.Policies.Add(new AttendancePolicy());   // 09:00-18:00, 15 min grace
                await db.SaveChangesAsync();
            }
            logger.LogInformation("Attendance database ready");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Database not ready (attempt {Attempt}): {Message}", attempt, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

app.UseHrmDefaults("HRM Attendance Service");
app.Run();
