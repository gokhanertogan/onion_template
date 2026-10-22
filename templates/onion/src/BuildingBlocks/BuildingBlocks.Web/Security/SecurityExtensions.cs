namespace BuildingBlocks.Web.Security;

using System.ComponentModel.DataAnnotations;
using BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    /// <summary>Exact origins allowed to call the API from a browser. Empty = no cross-origin access.</summary>
    [Required]
    public string[] AllowedOrigins { get; set; } = [];
}

internal static class SecurityExtensions
{
    public static WebApplicationBuilder AddSecurity(this WebApplicationBuilder builder)
    {
        // JWT bearer settings bind from "Authentication:Schemes:Bearer" (Authority, ValidAudiences, ValidIssuer...).
        // In development `dotnet user-jwts create` writes compatible signing keys into user secrets.
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // keep "sub", "scope" etc. as issued
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "roles";
            });

        // Secure by default: every endpoint requires an authenticated caller unless it opts out.
        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionAuthorizationHandler>());
        builder.Services.AddHttpContextAccessor();
        builder.Services.TryAddScoped<ICurrentUser, CurrentUser>();

        builder.Services.AddOptions<CorsSettings>().BindConfiguration(CorsSettings.SectionName).ValidateDataAnnotations();
        builder.Services.AddCors();
        builder.Services.AddOptions<CorsOptions>()
            .Configure<IOptions<CorsSettings>>((options, settings) => options.AddDefaultPolicy(policy =>
            {
                var origins = settings.Value.AllowedOrigins;
                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins)
                        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                        .WithHeaders("Authorization", "Content-Type", "Idempotency-Key", "X-Correlation-ID")
                        .WithExposedHeaders("Location", "X-Correlation-ID", "Retry-After")
                        .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
                }
            }));

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
            options.Limits.MaxRequestHeadersTotalSize = 32 * 1024;
        });

        return builder;
    }
}
