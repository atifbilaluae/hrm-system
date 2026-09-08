using HRM.NotificationService.Application;
using HRM.NotificationService.Infrastructure;
using HRM.Shared;
using HRM.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddHrmDefaults("HRM Notification Service");
builder.AddHrmAuth();
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection(RabbitMqSettings.SectionName));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection(EmailSettings.SectionName));

builder.Services.AddControllers();
builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<HrmEventConsumer>();   // starts listening on boot
builder.Services.AddHealthChecks().AddDbContextCheck<NotificationDbContext>("notification-db");

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    for (var attempt = 1; attempt <= 15; attempt++)
    {
        try { await db.Database.EnsureCreatedAsync(); logger.LogInformation("Notification database ready"); break; }
        catch (Exception ex)
        {
            logger.LogWarning("Database not ready (attempt {Attempt}): {Message}", attempt, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

app.UseHrmDefaults("HRM Notification Service");
app.Run();
