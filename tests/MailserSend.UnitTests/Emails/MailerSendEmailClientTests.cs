using MailerSendNetCore.Common;
using MailerSendNetCore.Common.Exceptions;
using MailerSendNetCore.Common.Interfaces;
using MailerSendNetCore.Emails;
using MailerSendNetCore.Emails.Dtos;
using MailerSendNetCore.UnitTests.Mocks;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace MailerSendNetCore.UnitTests.Emails;

public class MailerSendEmailClientTests
{
    [Fact]
    public async Task Test_SendMail_When_UnprocessableEntity_Then_SetsMessageIdFromHeader()
    {
        var apiToken = "my token";
        var headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "X-Message-Id", new [] { "msg-422" } }
        };
        var handler = new MockHttpMessageHandler(HttpStatusCode.UnprocessableEntity, responseContent: new
        {
            message = "Validation failed.",
            errors = new { field = new[] { "error" } }
        }, responseHeaders: headers);

        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        var parameters = new MailerSendEmailParameters();
        parameters.WithTo("test@test.com").WithSubject("Hi!").WithHtmlBody("this is a test");

        var response = await client.SendEmailAsync(parameters);
        Assert.NotNull(response);
        Assert.Equal("msg-422", response.MessageId);
        Assert.Equal("Validation failed.", response.Message);
    }

    [Fact]
    public async Task Test_SendMail_When_NullParameters_Then_ThrowArgumentNullException()
    {
        var apiToken = "my token";
        var handler = new MockHttpMessageHandler(HttpStatusCode.Accepted, responseContent: null);
        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        Task Act() => client.SendEmailAsync((MailerSendEmailParameters)null);
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task Test_SendBulkMail_When_NullParameters_Then_ThrowArgumentNullException()
    {
        var apiToken = "my token";
        var handler = new MockHttpMessageHandler(HttpStatusCode.Accepted, responseContent: null);
        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        Task Act() => client.SendBulkEmailAsync((MailerSendEmailParameters[])null);
        await Assert.ThrowsAsync<ArgumentNullException>(Act);
    }

    [Fact]
    public async Task Test_GetBulkEmailStatusAsync_When_ServiceUnavailable_Then_ThrowApiExceptionWithDetails()
    {
        var apiToken = "my token";
        var body = new { error = "service unavailable" };
        var handler = new MockHttpMessageHandler(HttpStatusCode.ServiceUnavailable, responseContent: body);

        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        var bulkEmailId = "bulk_id";
        var ex = await Assert.ThrowsAsync<ApiException>(async () => await client.GetBulkEmailStatusAsync(bulkEmailId));
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, ex.StatusCode);
        Assert.Contains("service unavailable", ex.Response);
        Assert.NotNull(ex.Headers);
    }
    [Fact]
    public async Task Test_SendMail_When_ServerError_Then_ThrowApiException()
    {
        var apiToken = "my token";
        var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError);
        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        var parameters = new MailerSendEmailParameters();
        parameters
            .WithFrom("sender@test.com", "Sender")
            .WithTo("test@test.com")
            .WithSubject("Hi!")
            .WithHtmlBody("this is a test");

        Func<Task> action = async () => { await client.SendEmailAsync(parameters); };
        var ex = await Assert.ThrowsAsync<ApiException>(action);
        Assert.Equal("Unexpected response HTTP status code (500).\n\nStatus: 500\nResponse: \n", ex.Message);
    }

    [Fact]
    public async Task Test_SendMail_When_UnprocessableEntityAndUnknownBody_Then_ThrowApiException()
    {
        var apiToken = "my token";
        var handler = new MockHttpMessageHandler(HttpStatusCode.UnprocessableEntity, responseContent: null);
        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        var parameters = new MailerSendEmailParameters();
        parameters
            .WithTo("test@test.com")
            .WithSubject("Hi!")
            .WithHtmlBody("this is a test");

        Func<Task> action = async () => { await client.SendEmailAsync(parameters); };
        var ex = await Assert.ThrowsAsync<ApiException>(action);
        Assert.Equal("Unexpected response.\n\nStatus: 422\nResponse: \n", ex.Message);
    }

    [Fact]
    public async Task Test_SendMail_When_UnprocessableEntity_Then_ReturnMailerSendEmailResponseWithValidationErrorMessage()
    {
        var apiToken = "my token";
        var handler = new MockHttpMessageHandler(HttpStatusCode.UnprocessableEntity, responseContent: new
        {
            message = "The given data was invalid.",
            errors = new
            {
                fromemail = new string[] {
                    "The from.email must be verified.",
                },
            },
        });

        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        var parameters = new MailerSendEmailParameters();
        parameters
            .WithFrom("invalidemail", "Sender")
            .WithTo("test@test.com")
            .WithSubject("Hi!")
            .WithHtmlBody("this is a test");

        var response = await client.SendEmailAsync(parameters);
        Assert.NotNull(response);
        Assert.Equal("The given data was invalid.", response.Message);
        Assert.NotNull(response.Errors);
        Assert.NotEmpty(response.Errors);
        Assert.True(response.Errors.ContainsKey("fromemail"));
        Assert.Contains("The from.email must be verified.", response.Errors["fromemail"]);
    }

    [Fact]
    public async Task Test_SendMail_When_Accepted_Then_ReturnMailerSendEmailResponseWithMessageId()
    {
        IDictionary<string, string[]> headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "X-Message-Id", new string[] { "messageid" } }
        };

        var apiToken = "my token";
        var handler = new MockHttpMessageHandler(HttpStatusCode.Accepted, responseContent: null, responseHeaders: headers);

        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        var parameters = new MailerSendEmailParameters();
        parameters
            .WithFrom("sender@test.com", "Sender")
            .WithTo("test@test.com")
            .WithSubject("Hi!")
            .WithHtmlBody("this is a test");

        var response = await client.SendEmailAsync(parameters);
        Assert.NotNull(response);
        Assert.Equal("messageid", response.MessageId);
        Assert.True(string.IsNullOrEmpty(response.Message));
    }


    [Fact]
    public async Task Test_SendBulkMail_When_ServerError_Then_ThrowApiException()
    {
        var apiToken = "my token";
        var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError);
        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        var bulkParameters = new List<MailerSendEmailParameters>();

        var parameters = new MailerSendEmailParameters();
        parameters
            .WithFrom("sender@test.com", "Sender")
            .WithTo("test@test.com")
            .WithSubject("Hi!")
            .WithHtmlBody("this is a test");

        bulkParameters.Add(parameters);

        parameters = new MailerSendEmailParameters();
        parameters
            .WithFrom("sender@test.com", "Sender")
            .WithTo("test-two@test.com")
            .WithSubject("Hi!")
            .WithHtmlBody("this is a test");

        Func<Task> action = async () =>
        {
            await client.SendBulkEmailAsync(bulkParameters.ToArray());
        };

        var ex = await Assert.ThrowsAsync<ApiException>(action);
        Assert.Equal("Unexpected response HTTP status code (500).\n\nStatus: 500\nResponse: \n", ex.Message);
    }

    [Fact]
    public async Task Test_SendBulkMail_When_Accepted_Then_ReturnMailerSendBulkEmailResponse()
    {
        var responseContent = new
        {
            message = "The bulk email is being processed. Read the Email API to know how you can check the status.",
            bulk_email_id = "614470d1588b866d0454f3e2"
        };

        var apiToken = "my token";
        var handler = new MockHttpMessageHandler(HttpStatusCode.Accepted, responseContent: responseContent);

        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        var bulkParameters = new List<MailerSendEmailParameters>();

        var parameters = new MailerSendEmailParameters();
        parameters
            .WithFrom("sender@test.com", "Sender")
            .WithTo("test@test.com")
            .WithSubject("Hi!")
            .WithHtmlBody("this is a test");

        bulkParameters.Add(parameters);

        parameters = new MailerSendEmailParameters();
        parameters
            .WithFrom("sender@test.com", "Sender")
            .WithTo("test-two@test.com")
            .WithSubject("Hi!")
            .WithHtmlBody("this is a test");

        var response = await client.SendBulkEmailAsync(bulkParameters.ToArray());
        Assert.NotNull(response);
        Assert.Equal(responseContent.bulk_email_id, response.BulkEmailId);
        Assert.Equal(responseContent.message, response.Message);
    }


    [Fact]
    public async Task Test_GetBulkEmailStatusAsync_When_ServerError_Then_ThrowApiException()
    {
        var apiToken = "my token";
        var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError);
        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        var bulkEmailId = "bulk_id";
        Func<Task> action = async () => { await client.GetBulkEmailStatusAsync(bulkEmailId); };
        var ex = await Assert.ThrowsAsync<ApiException>(action);
        Assert.Equal("Unexpected response HTTP status code (500).\n\nStatus: 500\nResponse: \n", ex.Message);
    }


    [Fact]
    public async Task Test_GetBulkEmailStatusAsync_When_OK_Then_ReturnMailerSendEmailResponseWithMessageId()
    {
        var responseContent = new
        {
            data = new
            {
                id = "614470d1588b866d0454f3e2",
                state = "completed",
                total_recipients_count = 1,
                suppressed_recipients_count = 0,
                suppressed_recipients = (object)null,
                validation_errors_count = 0,
                validation_errors = (object)null,
                messages_id = new string[] { "61487a14608b1d0b4d506633" },
                created_at = "2021-09-17T10:41:21.892000Z",
                updated_at = "2021-09-17T10:41:23.684000Z"
            }
        };

        var apiToken = "my token";
        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseContent: responseContent);

        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        var bulkEmailId = "bulk_id";
        var response = await client.GetBulkEmailStatusAsync(bulkEmailId);
        Assert.NotNull(response);
        Assert.Equal(responseContent.data.id, response.Data.Id);
        Assert.Equal(responseContent.data.state, response.Data.State);
        Assert.Equal(responseContent.data.total_recipients_count, response.Data.TotalRecipientsCount);
        Assert.Equal(responseContent.data.suppressed_recipients_count, response.Data.SuppressedRecipientsCount);
        Assert.Equal(responseContent.data.suppressed_recipients, response.Data.SuppressedRecipients);
        Assert.Equal(responseContent.data.validation_errors_count, response.Data.ValidationErrorsCount);
        Assert.Equal(responseContent.data.validation_errors, response.Data.ValidationErrors);
        Assert.Equal(responseContent.data.messages_id, response.Data.MessagesId);
        Assert.Equal(DateTime.Parse(responseContent.data.created_at).Date, response.Data.CreatedAt.Date);
        Assert.Equal(DateTime.Parse(responseContent.data.updated_at).Date, response.Data.UpdatedAt.Date);
    }

    [Fact]
    public async Task Test_GetBulkEmailStatusAsync_When_NotFound_Then_ThrowApiException()
    {
        var apiToken = "my token";
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound, responseContent: null);

        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        var bulkEmailId = "bulk_id";

        Func<Task> action = async () => { await client.GetBulkEmailStatusAsync(bulkEmailId); };
        var exNotFound = await Assert.ThrowsAsync<ApiException>(action);
        Assert.Equal("Not Found (bulk_id)\n\nStatus: 404\nResponse: \n", exNotFound.Message);
    }

    //case: when the response is Too Many Request (429) and the retry-after header is present, the client should retry after the specified time.
    [Fact]
    public async Task Test_GetBulkEmailStatusAsync_When_TooManyRequests_Then_ThrowApiException()
    {
        var apiToken = "my token";
        var handler = new MockHttpMessageHandler(HttpStatusCode.TooManyRequests, responseContent: null);

        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        var bulkEmailId = "bulk_id";
        Func<Task> action = async () => { await client.GetBulkEmailStatusAsync(bulkEmailId); };
        var exTooMany = await Assert.ThrowsAsync<ApiException>(action);
        Assert.Equal("Unexpected response HTTP status code (429).\n\nStatus: 429\nResponse: \n", exTooMany.Message);
    }

    [Fact]
    public async Task Test_SendMail_When_SendTimeIsNull_Then_SendEmailWithoutSendTime()
    {
        var apiToken = "my token";
        var handler = new MockHttpMessageHandler(HttpStatusCode.Accepted, responseContent: null);

        IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

        var parameters = new MailerSendEmailParameters();
        parameters
            .WithFrom("sender@test.com", "Sender")
            .WithTo("test@test.com")
            .WithSubject("Hi!")
            .WithHtmlBody("this is a test");

        var response = await client.SendEmailAsync(parameters);

        // Assert that the request content does not contain SendTime
        var requestContent = handler.GetLastRequestContent<MailerSendEmailParameters>();
        Assert.Null(requestContent.SendTime);
    }

    [Fact]
    public void Test_MailerSendEmailClient_Ctor_NullHttpClient_Throws()
    {
        var options = Options.Create(new MailerSendEmailClientOptions { ApiToken = "token" });
        Assert.Throws<ArgumentNullException>(() => new MailerSendEmailClient(null, options));
    }

    [Fact]
    public void Test_MailerSendEmailClient_Ctor_NullOptions_Throws()
    {
        Assert.Throws<NullReferenceException>(() => new MailerSendEmailClient(new HttpClient(), null));
    }
}
