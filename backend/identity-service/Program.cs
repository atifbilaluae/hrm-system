using FluentValidation;
using HRM.IdentityService.Application;
using HRM.IdentityService.Infrastructure;
using HRM.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddHrmDefaults("HRM Identity Service");
builder.AddHrmAuth();

builder.Services.AddControllers(o => o.Filters.Add<HRM.IdentityService.ValidationFilter>());
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHealthChecks().AddDbContextCheck<IdentityDbContext>("identity-db");

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // SQL Server container may still be starting up.
    for (var attempt = 1; attempt <= 15; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            await DbSeeder.SeedAsync(db,
                builder.Configuration["Seed:AdminEmail"] ?? "admin@hrm.local",
                builder.Configuration["Seed:AdminPassword"] ?? "Admin@12345");
            logger.LogInformation("Identity database ready");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Database not ready (attempt {Attempt}): {Message}", attempt, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

app.UseHrmDefaults("HRM Identity Service");
app.Run();
