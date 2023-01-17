using FluentAssertions;
using MailerSendNetCore.Common;
using MailerSendNetCore.Common.Exceptions;
using MailerSendNetCore.Common.Interfaces;
using MailerSendNetCore.Emails;
using MailerSendNetCore.Emails.Dtos;
using MailerSendNetCore.UintTests.Mocks;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace MailerSendNetCore.UintTests.Emails
{
    public class MailerSendEmailClientTests
    {
        [Fact]
        public void Test_SendMail_When_ServerError_Then_ThrowApiException()
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

            Func<Task> action = async () => { await client.SendEmailAsync(parameters).ConfigureAwait(false); };
            action.Should()
                .ThrowAsync<ApiException>()
                .WithMessage("Unexpected response HTTP status code (500).\n\nStatus: 500\nResponse: \n");
        }

        [Fact]
        public void Test_SendMail_When_UnprocessableEntityAndUnknownBody_Then_ThrowApiException()
        {
            var apiToken = "my token";
            var handler = new MockHttpMessageHandler(HttpStatusCode.UnprocessableEntity, responseContent: null);
            IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

            var parameters = new MailerSendEmailParameters();
            parameters
                .WithTo("test@test.com")
                .WithSubject("Hi!")
                .WithHtmlBody("this is a test");

            Func<Task> action = async () => { await client.SendEmailAsync(parameters).ConfigureAwait(false); };
            action.Should()
                .ThrowAsync<ApiException>()
                .WithMessage("Unexpected response.\n\nStatus: 422\nResponse: \n");
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

            var response = await client.SendEmailAsync(parameters).ConfigureAwait(false);
            response.Should().NotBeNull();
            response.Message.Should().Be("The given data was invalid.");
            response.Errors.Should().NotBeEmpty();
            response.Errors.Should().ContainKey("fromemail");
            response.Errors["fromemail"].Should().Contain("The from.email must be verified.");
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

            var response = await client.SendEmailAsync(parameters).ConfigureAwait(false);
            response.Should().NotBeNull();
            response.MessageId.Should().Be("messageid");
            response.Message.Should().BeNullOrEmpty();
        }


        [Fact]
        public void Test_SendBulkMail_When_ServerError_Then_ThrowApiException()
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

            action.Should()
                .ThrowAsync<ApiException>()
                .WithMessage("Unexpected response HTTP status code (500).\n\nStatus: 500\nResponse: \n");
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
            response.Should().NotBeNull();
            response.BulkEmailId.Should().Be(responseContent.bulk_email_id);
            response.Message.Should().Be(responseContent.message);
        }


        [Fact]
        public void Test_GetBulkEmailStatusAsync_When_ServerError_Then_ThrowApiException()
        {
            var apiToken = "my token";
            var handler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError);
            IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

            var bulkEmailId = "bulk_id";
            Func<Task> action = async () => { await client.GetBulkEmailStatusAsync(bulkEmailId).ConfigureAwait(false); };
            action.Should()
                .ThrowAsync<ApiException>()
                .WithMessage("Unexpected response HTTP status code (500).\n\nStatus: 500\nResponse: \n");
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
                    messages_id = "['61487a14608b1d0b4d506633']",
                    created_at = "2021-09-17T10:41:21.892000Z",
                    updated_at = "2021-09-17T10:41:23.684000Z"
                }
            };

            var apiToken = "my token";
            var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseContent: responseContent);

            IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

            var bulkEmailId = "bulk_id";
            var response = await client.GetBulkEmailStatusAsync(bulkEmailId).ConfigureAwait(false);
            response.Should().NotBeNull();
            response.Data.Id.Should().Be(responseContent.data.id);
            response.Data.State.Should().Be(responseContent.data.state);
            response.Data.TotalRecipientsCount.Should().Be(responseContent.data.total_recipients_count);
            response.Data.SuppressedRecipientsCount.Should().Be(responseContent.data.suppressed_recipients_count);
            response.Data.SuppressedRecipients.Should().Be(responseContent.data.suppressed_recipients);
            response.Data.ValidationErrorsCount.Should().Be(responseContent.data.validation_errors_count);
            response.Data.ValidationErrors.Should().Be(responseContent.data.validation_errors);
            response.Data.MessagesId.Should().Be(responseContent.data.messages_id);
            response.Data.CreatedAt.Should().BeSameDateAs(DateTime.Parse(responseContent.data.created_at));
            response.Data.UpdatedAt.Should().BeSameDateAs(DateTime.Parse(responseContent.data.updated_at));
        }

        [Fact]
        public async Task Test_GetBulkEmailStatusAsync_When_NotFound_Then_ThrowApiException()
        {
            var apiToken = "my token";
            var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound, responseContent: null);

            IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

            var bulkEmailId = "bulk_id";
           
            Func<Task> action = async () => { await client.GetBulkEmailStatusAsync(bulkEmailId).ConfigureAwait(false); };
            await action.Should()
                .ThrowAsync<ApiException>()
                .WithMessage("Not Found (bulk_id)\n\nStatus: 404\nResponse: \n");
        }

        //case: when the response is Too Many Request (429) and the retry-after header is present, the client should retry after the specified time.
        [Fact]
        public async Task Test_GetBulkEmailStatusAsync_When_TooManyRequests_Then_ThrowApiException()
        {
            var apiToken = "my token";
            var handler = new MockHttpMessageHandler(HttpStatusCode.TooManyRequests, responseContent: null);

            IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), Options.Create(new MailerSendEmailClientOptions { ApiToken = apiToken }));

            var bulkEmailId = "bulk_id";
            Func<Task> action = async () => { await client.GetBulkEmailStatusAsync(bulkEmailId).ConfigureAwait(false); };
            await action.Should()
                .ThrowAsync<ApiException>()
                .WithMessage("Unexpected response HTTP status code (429).\n\nStatus: 429\nResponse: \n");
        }

    }
}
