using System;
using System.Net.Http;
using System.Net.Http.Headers;
using CheckModsExtended.Configuration;
using CheckModsExtended.Services;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace CheckModsExtended.Extensions;

public static class HttpServiceCollectionExtensions
{
    private const int RateLimiterTokenLimit = 5;
    private const int RateLimiterReplenishmentPeriodMs = 333;
    private const double CircuitBreakerFailureRatioThreshold = 0.99;

    private static void ConfigureDefaultUserAgent(HttpClient client)
    {
        var version = CheckModsExtended.Utils.VersionInfo.SemVer;
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CheckModsExtended", version));
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("(+https://github.com/TerribleTurtle/CheckModsExtended)")
        );
    }

    public static IServiceCollection AddCheckModsHttpClients(this IServiceCollection services)
    {
        // Register the named HttpClient for ForgeApi
        var httpClientBuilder = services.AddHttpClient(
            "ForgeApi",
            (serviceProvider, client) =>
            {
                // Disable HttpClient timeout to allow Polly's TotalRequestTimeout (5 minutes) to control the lifecycle
                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;

                ConfigureDefaultUserAgent(client);
            }
        );

        httpClientBuilder.AddResilienceHandler(
            "pacer",
            builder =>
            {
                var rateLimiter = new System.Threading.RateLimiting.TokenBucketRateLimiter(
                    new System.Threading.RateLimiting.TokenBucketRateLimiterOptions
                    {
                        TokenLimit = RateLimiterTokenLimit,
                        TokensPerPeriod = 1,
                        ReplenishmentPeriod = TimeSpan.FromMilliseconds(RateLimiterReplenishmentPeriodMs),
                        QueueLimit = 10_000,
                        QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true,
                    }
                );

                builder.AddRateLimiter(
                    new Polly.RateLimiting.RateLimiterStrategyOptions
                    {
                        RateLimiter = args => rateLimiter.AcquireAsync(1, args.Context.CancellationToken),
                    }
                );
            }
        );

        httpClientBuilder.AddStandardResilienceHandler(options =>
        {
            // Allow up to 5 minutes total to survive 429 Retry-After delays
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);

            // Prevent the Circuit Breaker from tripping during heavy rate limiting
            options.CircuitBreaker.FailureRatio = CircuitBreakerFailureRatioThreshold;
            options.CircuitBreaker.MinimumThroughput = 1000;
        });

        // Register the remote ignore-list client as a typed HttpClient.
        services
            .AddHttpClient<IRemoteIgnoreFileClient, RemoteIgnoreFileClient>(
                (serviceProvider, client) =>
                {
                    var ignoredOptions = serviceProvider.GetRequiredService<IOptions<IgnoredUpdateOptions>>().Value;
                    client.Timeout = TimeSpan.FromSeconds(ignoredOptions.RemoteTimeoutSeconds);

                    ConfigureDefaultUserAgent(client);
                }
            )
            .AddStandardResilienceHandler();

        services
            .AddHttpClient<IGitHubReleaseClient, GitHubReleaseClient>(
                (serviceProvider, client) =>
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    ConfigureDefaultUserAgent(client);
                }
            )
            .AddStandardResilienceHandler();

        return services;
    }
}
