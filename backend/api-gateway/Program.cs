using HRM.Shared;
using HRM.Shared.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "HRM API Gateway")
    .WriteTo.Console());

// The gateway validates the token too, so a bad token is rejected at the edge
// instead of being forwarded to a downstream service.
builder.AddHrmAuth();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddCors(o => o.AddPolicy("hrm", p => p
    .WithOrigins(builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? ["http://localhost:3000"])
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors("hrm");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new
{
    service = "HRM API Gateway",
    routes = new[] { "/api/auth", "/api/users", "/api/employees", "/api/notifications" }
}));

// Forwards X-Correlation-Id downstream automatically because it is set on the request.
app.MapReverseProxy();

app.Run();
