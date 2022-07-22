using MailerSendNetCore.Common.Interfaces;
using MailerSendNetCore.Emails;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace MailerSendNetCore.Common.Extensions
{
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

            services.AddHttpClient<IMailerSendEmailClient, MailerSendEmailClient>();
            return services;
        }

        public static IServiceCollection AddMailerSendEmailClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MailerSendEmailClientOptions>(configuration);

            var options = services.BuildServiceProvider().GetService<IOptions<MailerSendEmailClientOptions>>()!.Value;

            AddHttpClient(services, options.UseRetryPolicy, options.RetryCount, options.RetryDelayInMilliseconds);

            return services;
        }

        public static IServiceCollection AddMailerSendEmailClient(this IServiceCollection services, Action<MailerSendEmailClientOptions> configureOptions, Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task> onRetryCallback = default!)
        {
            services.Configure(configureOptions);

            var options = services.BuildServiceProvider().GetService<IOptions<MailerSendEmailClientOptions>>()!.Value;

            AddHttpClient(services, options.UseRetryPolicy, options.RetryCount, options.RetryDelayInMilliseconds, onRetryCallback);

            return services;
        }

        public static IServiceCollection AddMailerSendEmailClient(this IServiceCollection services, MailerSendEmailClientOptions options, Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task> onRetryCallback = default!)
        {
            services.AddOptions<MailerSendEmailClientOptions>()
                .Configure(o =>
                {
                    o.ApiUrl = options.ApiUrl;
                    o.ApiToken = options.ApiToken;
                    o.UseRetryPolicy = options.UseRetryPolicy;
                    o.RetryCount = options.RetryCount;
                    o.RetryDelayInMilliseconds = options.RetryDelayInMilliseconds;
                });

            AddHttpClient(services, options.UseRetryPolicy, options.RetryCount, options.RetryDelayInMilliseconds, onRetryCallback);

            return services;
        }

        private static void AddHttpClient(IServiceCollection services, bool useRetryPolicy, int retryCount = 10, int delayInMilliseconds = 10000, Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task> onRetryAsync = default!)
        {
            var clientBuilder = services.AddHttpClient<IMailerSendEmailClient, MailerSendEmailClient>();
            if (useRetryPolicy)
            {
                clientBuilder
                    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                    .AddPolicyHandler(GetRetryPolicy(retryCount, delayInMilliseconds, onRetryAsync));
            }
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount, int delayInMilliseconds, Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task> onRetryAsync)
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests || response.StatusCode == HttpStatusCode.ServiceUnavailable)
                .WaitAndRetryAsync(retryCount,
                    sleepDurationProvider: (retryCount, response, context) =>
                    {
                        var delay = TimeSpan.FromMilliseconds(delayInMilliseconds);

                        if (response.Result.Headers.TryGetValues("retry-after", out IEnumerable<string>? values) && int.TryParse(values.First(), out int delayInSeconds))
                            delay = TimeSpan.FromSeconds(delayInSeconds);

                        return delay;
                    },
                    onRetryAsync: async (response, timespan, retryCount, context) =>
                    {
                        if (onRetryAsync != null)
                            await onRetryAsync(response, timespan, retryCount, context);
                    }
                );

            return retryPolicy;
        }
    }
}
