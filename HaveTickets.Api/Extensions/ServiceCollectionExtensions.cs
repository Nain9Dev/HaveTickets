using System.Globalization;
using System.Threading.RateLimiting;
using HaveTickets.Api.Options;
using HaveTickets.Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace HaveTickets.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string ReadPolicy = "read";
    public const string WritePolicy = "write";

    /// <summary>
    /// Allows only the origins listed in configuration. There is no wildcard
    /// fallback: an unconfigured deployment refuses cross-origin calls rather than
    /// accepting every site on the internet.
    /// </summary>
    public static IServiceCollection AddConfiguredCors(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        CorsOptions options = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
        string[] origins = [.. options.AllowedOrigins.Where(origin => !string.IsNullOrWhiteSpace(origin))];

        services.AddCors(cors => cors.AddDefaultPolicy(policy =>
        {
            if (origins.Length == 0)
            {
                return;
            }

            policy.WithOrigins(origins)
                .WithMethods("GET", "POST", "DELETE", "OPTIONS")
                .WithHeaders("Content-Type", "Accept")
                .SetPreflightMaxAge(TimeSpan.FromHours(1));
        }));

        return services;
    }

    /// <summary>
    /// Quotas partitioned by client address, so one noisy caller cannot exhaust the
    /// budget for everybody else. Writes get a tighter budget than reads.
    /// </summary>
    public static IServiceCollection AddConfiguredRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        RateLimitingOptions options =
            configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        var window = TimeSpan.FromSeconds(Math.Max(1, options.WindowSeconds));

        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(ReadPolicy, context =>
                Partition(context, options.ReadPermitsPerWindow, window));

            limiter.AddPolicy(WritePolicy, context =>
                Partition(context, options.WritePermitsPerWindow, window));

            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        title = "Too many requests.",
                        status = StatusCodes.Status429TooManyRequests,
                        detail = "Slow down and try again shortly.",
                        errorCode = "rate_limit.exceeded"
                    },
                    cancellationToken);
            };
        });

        return services;
    }

    /// <summary>Health probes: liveness answers "the process is up", readiness "the database answers".</summary>
    public static IServiceCollection AddConfiguredHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHealthChecks()
            .AddDbContextCheck<HaveTicketsDbContext>("database", tags: ["ready"]);

        return services;
    }

    /// <summary>Trusts the proxy headers Render and similar platforms set in front of the app.</summary>
    public static IServiceCollection AddForwardedHeaders(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // The platform terminates TLS and rewrites these headers itself, so the
            // default "only trust loopback" list would drop them.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    private static RateLimitPartition<string> Partition(HttpContext context, int permitLimit, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
}
