using MailerSendNetCore.Common;
using MailerSendNetCore.Common.Exceptions;
using MailerSendNetCore.Common.Extensions;
using MailerSendNetCore.Common.Interfaces;
using MailerSendNetCore.Emails.Dtos;
using MailerSendNetCore.UnitTests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MailerSendNetCore.UnitTests.Common;

public class RetryBehaviorTests
{
    [Fact]
    public async Task RetryDisabled_TransientResponse_IsSentOnce()
    {
        var handler = CreateStatusSequenceHandler(HttpStatusCode.InternalServerError);
        using var serviceProvider = CreateServiceProvider(handler, useRetryPolicy: false);
        var client = serviceProvider.GetRequiredService<IMailerSendEmailClient>();

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => client.SendEmailAsync(CreateParameters(), TestContext.Current.CancellationToken));

        Assert.Equal((int)HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Equal(1, handler.AttemptCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task RetryEnabled_TransientResponse_RetriesPost(HttpStatusCode transientStatus)
    {
        var handler = CreateStatusSequenceHandler(transientStatus, HttpStatusCode.Accepted);
        using var serviceProvider = CreateServiceProvider(handler, useRetryPolicy: true);
        var client = serviceProvider.GetRequiredService<IMailerSendEmailClient>();

        var response = await client.SendEmailAsync(
            CreateParameters(),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, handler.AttemptCount);
        Assert.Equal("message-id", response.MessageId);
    }

    [Fact]
    public async Task RetryEnabled_HttpRequestException_RetriesPost()
    {
        var handler = new ScriptedHttpMessageHandler(
            (attempt, _, _) =>
            {
                if (attempt == 1)
                {
                    throw new HttpRequestException("transient transport failure");
                }

                return Task.FromResult(CreateAcceptedResponse());
            });
        using var serviceProvider = CreateServiceProvider(handler, useRetryPolicy: true);
        var client = serviceProvider.GetRequiredService<IMailerSendEmailClient>();

        var response = await client.SendEmailAsync(
            CreateParameters(),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, handler.AttemptCount);
        Assert.Equal("message-id", response.MessageId);
    }

    [Fact]
    public async Task RetryEnabled_NonTransientResponse_DoesNotRetry()
    {
        var handler = CreateStatusSequenceHandler(
            HttpStatusCode.BadRequest,
            HttpStatusCode.Accepted);
        using var serviceProvider = CreateServiceProvider(handler, useRetryPolicy: true);
        var client = serviceProvider.GetRequiredService<IMailerSendEmailClient>();

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => client.SendEmailAsync(CreateParameters(), TestContext.Current.CancellationToken));

        Assert.Equal((int)HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal(1, handler.AttemptCount);
    }

    [Fact]
    public async Task RetryCallback_PreservesOrderCountDelayResultAndContext()
    {
        var events = new List<string>();
        var callbackContexts = new List<Context>();
        var callbackDelays = new List<TimeSpan>();
        var callbackStatuses = new List<HttpStatusCode>();
        var handler = new ScriptedHttpMessageHandler(
            (attempt, _, _) =>
            {
                events.Add($"send-{attempt}");
                return Task.FromResult(
                    attempt < 3
                        ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                        : CreateAcceptedResponse());
            });
        Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task> callback =
            (result, delay, retryCount, context) =>
            {
                events.Add($"retry-{retryCount}");
                callbackContexts.Add(context);
                callbackDelays.Add(delay);
                callbackStatuses.Add(result.Result.StatusCode);
                return Task.CompletedTask;
            };
        using var serviceProvider = CreateServiceProvider(
            handler,
            useRetryPolicy: true,
            retryCount: 2,
            retryDelayInMilliseconds: 0,
            callback);
        var client = serviceProvider.GetRequiredService<IMailerSendEmailClient>();

        await client.SendEmailAsync(CreateParameters(), TestContext.Current.CancellationToken);

        Assert.Equal(
            new[] { "send-1", "retry-1", "send-2", "retry-2", "send-3" },
            events);
        Assert.Equal(new[] { TimeSpan.Zero, TimeSpan.Zero }, callbackDelays);
        Assert.Equal(
            new[] { HttpStatusCode.ServiceUnavailable, HttpStatusCode.ServiceUnavailable },
            callbackStatuses);
        Assert.Equal(2, callbackContexts.Count);
        Assert.Same(callbackContexts[0], callbackContexts[1]);
    }

    [Fact]
    public async Task RetryAfter_DeltaSeconds_OverridesConfiguredDelay()
    {
        TimeSpan? observedDelay = null;
        var handler = new ScriptedHttpMessageHandler(
            (attempt, _, _) =>
            {
                if (attempt == 1)
                {
                    var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                    response.Headers.TryAddWithoutValidation("Retry-After", "0");
                    return Task.FromResult(response);
                }

                return Task.FromResult(CreateAcceptedResponse());
            });
        Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task> callback =
            (_, delay, _, _) =>
            {
                observedDelay = delay;
                return Task.CompletedTask;
            };
        using var serviceProvider = CreateServiceProvider(
            handler,
            useRetryPolicy: true,
            retryCount: 1,
            retryDelayInMilliseconds: 500,
            callback);
        var client = serviceProvider.GetRequiredService<IMailerSendEmailClient>();

        await client.SendEmailAsync(CreateParameters(), TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.Zero, observedDelay);
        Assert.Equal(2, handler.AttemptCount);
    }

    [Fact]
    public async Task Cancellation_DuringAttempt_IsPropagatedWithoutRetry()
    {
        var callbackCount = 0;
        var attemptStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new ScriptedHttpMessageHandler(
            async (_, _, cancellationToken) =>
            {
                attemptStarted.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("Unreachable");
            });
        Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task> callback =
            (_, _, _, _) =>
            {
                callbackCount++;
                return Task.CompletedTask;
            };
        using var serviceProvider = CreateServiceProvider(
            handler,
            useRetryPolicy: true,
            retryCount: 2,
            retryDelayInMilliseconds: 0,
            callback);
        var client = serviceProvider.GetRequiredService<IMailerSendEmailClient>();
        using var cancellationSource = new CancellationTokenSource();

        var sendTask = client.SendEmailAsync(CreateParameters(), cancellationSource.Token);
        await attemptStarted.Task;
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sendTask);

        Assert.Equal(1, handler.AttemptCount);
        Assert.Equal(0, callbackCount);
    }

    [Fact]
    public async Task RetriedPost_HasByteEquivalentContentAcrossAttempts()
    {
        var handler = CreateStatusSequenceHandler(
            HttpStatusCode.InternalServerError,
            HttpStatusCode.Accepted);
        using var serviceProvider = CreateServiceProvider(handler, useRetryPolicy: true);
        var client = serviceProvider.GetRequiredService<IMailerSendEmailClient>();

        await client.SendEmailAsync(CreateParameters(), TestContext.Current.CancellationToken);

        Assert.Equal(2, handler.RequestBodies.Count);
        Assert.Equal(handler.RequestBodies[0], handler.RequestBodies[1]);
        Assert.NotEmpty(handler.RequestBodies[0]);
    }

    [Fact]
    public async Task RetriedFailedResponse_IsDisposedAfterCallback()
    {
        var callbackObservedUndisposedContent = false;
        var failedContent = new TrackingContent("transient");
        var handler = new ScriptedHttpMessageHandler(
            (attempt, _, _) =>
            {
                if (attempt == 1)
                {
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                        {
                            Content = failedContent
                        });
                }

                return Task.FromResult(CreateAcceptedResponse());
            });
        Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task> callback =
            (_, _, _, _) =>
            {
                callbackObservedUndisposedContent = !failedContent.IsDisposed;
                return Task.CompletedTask;
            };
        using var serviceProvider = CreateServiceProvider(
            handler,
            useRetryPolicy: true,
            callback: callback);
        var client = serviceProvider.GetRequiredService<IMailerSendEmailClient>();

        await client.SendEmailAsync(CreateParameters(), TestContext.Current.CancellationToken);

        Assert.True(callbackObservedUndisposedContent);
        Assert.True(failedContent.IsDisposed);
    }

    private static ServiceProvider CreateServiceProvider(
        ScriptedHttpMessageHandler primaryHandler,
        bool useRetryPolicy,
        int retryCount = 1,
        int retryDelayInMilliseconds = 0,
        Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task> callback = null)
    {
        var services = new ServiceCollection();
        services.AddMailerSendEmailClient(
            new MailerSendEmailClientOptions
            {
                ApiToken = "api-token",
                ApiUrl = "https://example.test/",
                UseRetryPolicy = useRetryPolicy,
                RetryCount = retryCount,
                RetryDelayInMilliseconds = retryDelayInMilliseconds
            },
            callback!);
        services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
            new PrimaryHandlerFilter(primaryHandler));

        return services.BuildServiceProvider();
    }

    private static ScriptedHttpMessageHandler CreateStatusSequenceHandler(
        params HttpStatusCode[] statuses)
    {
        return new ScriptedHttpMessageHandler(
            (attempt, _, _) =>
            {
                var status = statuses[Math.Min(attempt - 1, statuses.Length - 1)];
                return Task.FromResult(
                    status == HttpStatusCode.Accepted
                        ? CreateAcceptedResponse()
                        : new HttpResponseMessage(status));
            });
    }

    private static HttpResponseMessage CreateAcceptedResponse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent(string.Empty)
        };
        response.Headers.TryAddWithoutValidation("X-Message-Id", "message-id");
        return response;
    }

    private static MailerSendEmailParameters CreateParameters()
    {
        return new MailerSendEmailParameters()
            .WithFrom("sender@example.test", "Sender")
            .WithTo("recipient@example.test")
            .WithSubject("Subject")
            .WithHtmlBody("<p>Body</p>");
    }

    private sealed class TrackingContent : StringContent
    {
        public TrackingContent(string content)
            : base(content, Encoding.UTF8, "text/plain")
        {
        }

        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class PrimaryHandlerFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly HttpMessageHandler _primaryHandler;

        public PrimaryHandlerFilter(HttpMessageHandler primaryHandler)
        {
            _primaryHandler = primaryHandler;
        }

        public Action<HttpMessageHandlerBuilder> Configure(
            Action<HttpMessageHandlerBuilder> next)
        {
            return builder =>
            {
                next(builder);
                builder.PrimaryHandler = _primaryHandler;
            };
        }
    }
}
