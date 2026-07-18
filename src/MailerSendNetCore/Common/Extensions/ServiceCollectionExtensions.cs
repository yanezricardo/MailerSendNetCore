using MailerSendNetCore.Common.Handlers;
using MailerSendNetCore.Common.Interfaces;
using MailerSendNetCore.Emails;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;

namespace MailerSendNetCore.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMailerSendEmailClient(this IServiceCollection services)
    {
        services.AddOptions<MailerSendEmailClientOptions>()
            .Configure(options =>
            {
                options.ApiUrl = "https://api.mailersend.com/v1";
                options.UseRetryPolicy = false;
            });

        AddHttpClient(services);
        return services;
    }

    public static IServiceCollection AddMailerSendEmailClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MailerSendEmailClientOptions>(configuration);
        AddHttpClient(services);
        return services;
    }

    // Legacy compatibility API. Scheduled for redesign in 1.0.
    public static IServiceCollection AddMailerSendEmailClient(
        this IServiceCollection services,
        Action<MailerSendEmailClientOptions> configureOptions,
        Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task> onRetryCallback = default!)
    {
        services.Configure(configureOptions);
        AddHttpClient(services, onRetryCallback);
        return services;
    }

    // Legacy compatibility API. Scheduled for redesign in 1.0.
    public static IServiceCollection AddMailerSendEmailClient(
        this IServiceCollection services,
        MailerSendEmailClientOptions options,
        Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task> onRetryCallback = default!)
    {
        services.AddOptions<MailerSendEmailClientOptions>()
            .Configure(configuredOptions =>
            {
                configuredOptions.ApiUrl = options.ApiUrl;
                configuredOptions.ApiToken = options.ApiToken;
                configuredOptions.UseRetryPolicy = options.UseRetryPolicy;
                configuredOptions.RetryCount = options.RetryCount;
                configuredOptions.RetryDelayInMilliseconds = options.RetryDelayInMilliseconds;
            });

        AddHttpClient(services, onRetryCallback);
        return services;
    }

    private static void AddHttpClient(
        IServiceCollection services,
        Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task>? onRetryAsync = null)
    {
        services
            .AddHttpClient<IMailerSendEmailClient, MailerSendEmailClient>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddHttpMessageHandler(serviceProvider =>
                new MailerSendRetryHandler(
                    serviceProvider.GetRequiredService<IOptions<MailerSendEmailClientOptions>>(),
                    onRetryAsync));
    }
}
