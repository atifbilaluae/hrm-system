using System.Text;
using HRM.Shared.Auth;
using HRM.Shared.Messaging;
using HRM.Shared.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

namespace HRM.Shared;

/// <summary>
/// Cross-cutting wiring every microservice needs. Written once here instead of
/// copy-pasted into nine Program.cs files (DRY).
/// </summary>
public static class ServiceDefaults
{
    public static WebApplicationBuilder AddHrmDefaults(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Host.UseSerilog((ctx, cfg) => cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo { Title = serviceName, Version = "v1" });
            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization", Type = SecuritySchemeType.Http,
                Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header,
                Description = "Paste the access token (without the 'Bearer ' prefix)."
            });
            o.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { new OpenApiSecurityScheme { Reference = new OpenApiReference
                    { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
            });
        });

        builder.Services.AddCors(o => o.AddPolicy("hrm", p => p
            .WithOrigins(builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? ["http://localhost:3000"])
            .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

        builder.Services.AddHealthChecks();
        return builder;
    }

    public static WebApplicationBuilder AddHrmAuth(this WebApplicationBuilder builder)
    {
        var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Jwt settings missing");
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        // Permission-based authorization, available in every service.
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        builder.Services.AddAuthorization();
        return builder;
    }

    public static WebApplicationBuilder AddHrmMessaging(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection(RabbitMqSettings.SectionName));
        builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        return builder;
    }

    public static WebApplication UseHrmDefaults(this WebApplication app, string serviceName)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseSwagger();
        app.UseSwaggerUI(o => o.SwaggerEndpoint("v1/swagger.json", serviceName));
        app.UseCors("hrm");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health");
        app.MapControllers();
        return app;
    }
}
