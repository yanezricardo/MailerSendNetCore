using Microsoft.Extensions.Options;
using Polly;
using System.Net;

namespace MailerSendNetCore.Common.Handlers;

internal sealed class MailerSendRetryHandler : DelegatingHandler
{
    private readonly IAsyncPolicy<HttpResponseMessage>? _retryPolicy;

    public MailerSendRetryHandler(
        IOptions<MailerSendEmailClientOptions> options,
        Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task>? onRetryAsync)
    {
        ArgumentNullException.ThrowIfNull(options);

        var retryOptions = options.Value;
        if (retryOptions.UseRetryPolicy)
        {
            _retryPolicy = CreateRetryPolicy(
                retryOptions.RetryCount,
                retryOptions.RetryDelayInMilliseconds,
                onRetryAsync);
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_retryPolicy is null)
        {
            return base.SendAsync(request, cancellationToken);
        }

        return _retryPolicy.ExecuteAsync(
            (_, retryCancellationToken) => base.SendAsync(request, retryCancellationToken),
            new Context(),
            cancellationToken,
            continueOnCapturedContext: false);
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(
        int retryCount,
        int delayInMilliseconds,
        Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task>? onRetryAsync)
    {
        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(IsTransientResponse)
            .WaitAndRetryAsync(
                retryCount,
                (_, response, _) => GetRetryDelay(response, delayInMilliseconds),
                async (response, retryDelay, currentRetryCount, context) =>
                {
                    try
                    {
                        if (onRetryAsync is not null)
                        {
                            await onRetryAsync(
                                    response,
                                    retryDelay,
                                    currentRetryCount,
                                    context)
                                .ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        response.Result?.Dispose();
                    }
                });
    }

    private static bool IsTransientResponse(HttpResponseMessage response)
    {
        var statusCode = (int)response.StatusCode;
        return response.StatusCode == HttpStatusCode.RequestTimeout
            || response.StatusCode == HttpStatusCode.TooManyRequests
            || statusCode is >= 500 and <= 599;
    }

    private static TimeSpan GetRetryDelay(
        DelegateResult<HttpResponseMessage> response,
        int delayInMilliseconds)
    {
        if (response.Result is not null
            && response.Result.Headers.TryGetValues("Retry-After", out var values)
            && int.TryParse(values.FirstOrDefault(), out var delayInSeconds))
        {
            return TimeSpan.FromSeconds(delayInSeconds);
        }

        return TimeSpan.FromMilliseconds(delayInMilliseconds);
    }
}
