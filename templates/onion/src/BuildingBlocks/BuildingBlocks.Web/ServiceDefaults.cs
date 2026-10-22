namespace BuildingBlocks.Web;

using System.Diagnostics;
using System.Text.Json.Serialization;
using BuildingBlocks.Web.ExceptionHandling;
using BuildingBlocks.Web.Health;
using BuildingBlocks.Web.Middleware;
#if (UseOpenTelemetry)
using BuildingBlocks.Web.Observability;
#endif
using BuildingBlocks.Web.OpenApi;
using BuildingBlocks.Web.RateLimiting;
using BuildingBlocks.Web.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Opinionated, production-ready defaults shared by every service:
/// problem details, exception handling, security (JWT, permissions, CORS, headers, Kestrel limits),
/// rate limiting, request timeouts, health checks, OpenAPI and observability.
/// </summary>
public static class ServiceDefaults
{
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Fail fast on broken registrations (captive dependencies, missing services) in every environment.
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var httpContext = context.HttpContext;
            context.ProblemDetails.Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}";
            context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.TraceId.ToHexString() ?? httpContext.TraceIdentifier);
            context.ProblemDetails.Extensions.TryAdd("correlationId", httpContext.TraceIdentifier);
        });
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        builder.AddSecurity();
        builder.AddRateLimiting();

        builder.Services.AddRequestTimeouts(options => options.DefaultPolicy = new RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(30),
            TimeoutStatusCode = StatusCodes.Status504GatewayTimeout,
        });

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), [HealthCheckTags.Live]);

        builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

#if (UseOpenTelemetry)
        builder.AddOpenTelemetry();
#endif

        return builder;
    }

    /// <summary>Middleware pipeline in the correct order.</summary>
    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseCors();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.UseRequestTimeouts();

        return app;
    }

    /// <summary>Health probes and (when enabled) the OpenAPI document.</summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthEndpoints();

        if (app.Configuration.GetValue("OpenApi:Enabled", app.Environment.IsDevelopment()))
        {
            app.MapOpenApi().AllowAnonymous().DisableRateLimiting();
        }

        return app;
    }
}
