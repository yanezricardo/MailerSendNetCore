using MailerSendNetCore.Common;
using MailerSendNetCore.Common.Exceptions;
using MailerSendNetCore.Common.Interfaces;
using MailerSendNetCore.Emails;
using MailerSendNetCore.Emails.Dtos;
using MailerSendNetCore.UnitTests.Mocks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MailerSendNetCore.UnitTests.Emails;

public class MailerSendEmailResponseHandlingTests
{
    [Fact]
    public async Task SendEmailAsync_AcceptedWithEmptyBody_PreservesHeaderMessageId()
    {
        using var scope = CreateClient(
            _ => CreateResponse(
                HttpStatusCode.Accepted,
                string.Empty,
                ("X-Message-Id", "message-id")));

        var response = await scope.Client.SendEmailAsync(
            CreateParameters(),
            TestContext.Current.CancellationToken);

        Assert.Equal("message-id", response.MessageId);
        Assert.True(response.IsQueued);
        Assert.False(response.IsSuppressed);
        Assert.False(response.HasErrors);
        Assert.Null(response.Message);
        Assert.Null(response.Warnings);
        Assert.Empty(response.WarningItems);
    }

    [Fact]
    public async Task SendEmailAsync_SomeSuppressed_PreservesLegacyAndCompleteWarnings()
    {
        const string body = """
            {
              "message": "Accepted with warnings.",
              "warnings": [
                {
                  "type": "SOME_SUPPRESSED",
                  "warning": "One recipient was suppressed.",
                  "recipients": [
                    {
                      "email": "suppressed@example.test",
                      "name": "Suppressed",
                      "reasons": [ "blocklist" ]
                    }
                  ]
                },
                {
                  "type": "OTHER_WARNING",
                  "message": "A second warning."
                }
              ]
            }
            """;
        using var scope = CreateClient(
            _ => CreateResponse(
                HttpStatusCode.Accepted,
                body,
                ("X-Message-Id", "message-id")));

        var response = await scope.Client.SendEmailAsync(
            CreateParameters(),
            TestContext.Current.CancellationToken);

        Assert.Equal("message-id", response.MessageId);
        Assert.Equal("Accepted with warnings.", response.Message);
        Assert.True(response.IsQueued);
        Assert.False(response.IsSuppressed);
        Assert.False(response.HasErrors);
        Assert.Equal(2, response.WarningItems.Count);

        Assert.NotNull(response.Warnings);
        Assert.Same(response.WarningItems[0], response.Warnings);
        Assert.Equal("SOME_SUPPRESSED", response.Warnings.Type);
        Assert.Equal("One recipient was suppressed.", response.Warnings.Warning);
        var recipient = Assert.Single(response.Warnings.Recipients);
        Assert.Equal("suppressed@example.test", recipient.Email);
        Assert.Equal("Suppressed", recipient.Name);
        Assert.Equal(new[] { "blocklist" }, recipient.Reasons);

        Assert.Equal("OTHER_WARNING", response.WarningItems[1].Type);
        Assert.Equal("A second warning.", response.WarningItems[1].Warning);
    }

    [Theory]
    [InlineData("ALL_SUPPRESSED")]
    [InlineData("all_suppressed")]
    [InlineData("All_Suppressed")]
    public async Task SendEmailAsync_AllSuppressedWithoutMessageId_IsExplicitAndCaseInsensitive(
        string warningType)
    {
        var body = JsonConvert.SerializeObject(
            new
            {
                message = "No emails were queued.",
                warnings = new[]
                {
                    new
                    {
                        type = warningType,
                        warning = "All recipients were suppressed."
                    }
                }
            });
        using var scope = CreateClient(
            _ => CreateResponse(HttpStatusCode.Accepted, body));

        var response = await scope.Client.SendEmailAsync(
            CreateParameters(),
            TestContext.Current.CancellationToken);

        Assert.Null(response.MessageId);
        Assert.False(response.IsQueued);
        Assert.True(response.IsSuppressed);
        Assert.False(response.HasErrors);
        Assert.Equal("No emails were queued.", response.Message);
        Assert.Equal(warningType, Assert.Single(response.WarningItems).Type);
        Assert.Same(response.WarningItems[0], response.Warnings);
    }

    [Fact]
    public async Task SendEmailAsync_UnprocessableEntity_PreservesLegacyResponseContract()
    {
        const string body = """
            {
              "message": "The given data was invalid.",
              "errors": {
                "from.email": [ "The from.email must be verified." ]
              }
            }
            """;
        using var scope = CreateClient(
            _ => CreateResponse(HttpStatusCode.UnprocessableEntity, body));

        var response = await scope.Client.SendEmailAsync(
            CreateParameters(),
            TestContext.Current.CancellationToken);

        Assert.Equal("The given data was invalid.", response.Message);
        Assert.NotNull(response.Errors);
        Assert.Equal(
            new[] { "The from.email must be verified." },
            response.Errors["from.email"]);
        Assert.True(response.HasErrors);
        Assert.False(response.IsQueued);
        Assert.False(response.IsSuppressed);
        Assert.Empty(response.WarningItems);
    }

    [Theory]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task SendEmailAsync_MalformedStructuredBody_ThrowsApiException(
        HttpStatusCode statusCode)
    {
        const string malformedBody = "{ not-valid-json";
        using var scope = CreateClient(
            _ => CreateResponse(statusCode, malformedBody));

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => scope.Client.SendEmailAsync(
                CreateParameters(),
                TestContext.Current.CancellationToken));

        Assert.Equal((int)statusCode, exception.StatusCode);
        Assert.Equal(statusCode, exception.HttpStatusCode);
        Assert.Equal(malformedBody, exception.Response);
        Assert.IsAssignableFrom<JsonException>(exception.InnerException);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    public async Task ApiException_ExposesCompatibleAndAdditiveDiagnostics(
        HttpStatusCode statusCode,
        bool expectedTransient)
    {
        using var scope = CreateClient(
            _ => CreateResponse(
                statusCode,
                "provider response",
                ("Retry-After", "0"),
                ("X-Provider-Trace", "trace-id")));

        var exception = await Assert.ThrowsAsync<ApiException>(
            () => scope.Client.SendEmailAsync(
                CreateParameters(),
                TestContext.Current.CancellationToken));

        Assert.Equal((int)statusCode, exception.StatusCode);
        Assert.Equal(statusCode, exception.HttpStatusCode);
        Assert.Equal("provider response", exception.Response);
        Assert.Equal("trace-id", Assert.Single(exception.Headers["X-Provider-Trace"]));
        Assert.Equal(TimeSpan.Zero, exception.RetryAfter);
        Assert.Equal(expectedTransient, exception.IsTransient);
    }

    [Fact]
    public void ApiException_NullHeaders_PreservesLegacyRuntimeTolerance()
    {
        var exception = new ApiException(
            "provider failure",
            (int)HttpStatusCode.InternalServerError,
            "provider response",
            null!,
            null);

        Assert.Null(exception.Headers);
        Assert.Null(exception.RetryAfter);
        Assert.True(exception.IsTransient);
    }

    private static ClientScope CreateClient(
        Func<int, HttpResponseMessage> responseFactory)
    {
        var handler = new ScriptedHttpMessageHandler(
            (attempt, _, _) => Task.FromResult(responseFactory(attempt)));
        var httpClient = new HttpClient(handler);
        var options = Options.Create(
            new MailerSendEmailClientOptions
            {
                ApiToken = "api-token",
                ApiUrl = "https://example.test/"
            });

        return new ClientScope(
            httpClient,
            new MailerSendEmailClient(httpClient, options));
    }

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        string body,
        params (string Name, string Value)[] headers)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        foreach (var (name, value) in headers)
        {
            response.Headers.TryAddWithoutValidation(name, value);
        }

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

    private sealed class ClientScope : IDisposable
    {
        private readonly HttpClient _httpClient;

        public ClientScope(
            HttpClient httpClient,
            MailerSendEmailClient client)
        {
            _httpClient = httpClient;
            Client = client;
        }

        public IMailerSendEmailClient Client { get; }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
