using FluentValidation;
using HRM.OrganizationService;
using HRM.OrganizationService.Application;
using HRM.OrganizationService.Infrastructure;
using HRM.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddHrmDefaults("HRM Organization Service");
builder.AddHrmAuth();
builder.AddHrmMessaging();

builder.Services.AddControllers(o => o.Filters.Add<ValidationFilter>());
builder.Services.AddValidatorsFromAssemblyContaining<SaveDepartmentRequestValidator>();

builder.Services.AddDbContext<OrganizationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

builder.Services.AddScoped<IOrganizationService, OrganizationAppService>();
builder.Services.AddHealthChecks().AddDbContextCheck<OrganizationDbContext>("organization-db");

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    for (var attempt = 1; attempt <= 15; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            if (builder.Configuration.GetValue("Seed:Enabled", true))
                await OrganizationSeeder.SeedAsync(db);
            logger.LogInformation("Organization database ready");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Database not ready (attempt {Attempt}): {Message}", attempt, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

app.UseHrmDefaults("HRM Organization Service");
app.Run();
