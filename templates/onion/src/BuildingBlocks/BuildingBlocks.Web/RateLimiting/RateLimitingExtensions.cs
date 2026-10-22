namespace BuildingBlocks.Web.RateLimiting;

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    /// <summary>Requests allowed per window and per caller (user id, or client IP when anonymous).</summary>
    [Range(1, 1_000_000)]
    public int PermitLimit { get; set; } = 100;

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}

internal static class RateLimitingExtensions
{
    public static WebApplicationBuilder AddRateLimiting(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<RateLimitingSettings>()
            .BindConfiguration(RateLimitingSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddRateLimiter(_ => { });
        builder.Services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitingSettings>>((options, settingsAccessor) =>
            {
                var settings = settingsAccessor.Value;
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = WriteRejectionAsync;

                if (settings.Enabled)
                {
                    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                        RateLimitPartition.GetFixedWindowLimiter(PartitionKey(context), _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.PermitLimit,
                            Window = settings.Window,
                            QueueLimit = 0,
                            AutoReplenishment = true,
                        }));
                }
            });

        return builder;
    }

    /// <summary>Authenticated callers are limited per user, anonymous callers per client IP.</summary>
    private static string PartitionKey(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true
            ? $"user:{context.User.FindFirst("sub")?.Value ?? context.User.Identity.Name}"
            : $"ip:{context.Connection.RemoteIpAddress}";

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetails = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests",
                Detail = "Rate limit exceeded. Retry later.",
            },
        });
    }
}
