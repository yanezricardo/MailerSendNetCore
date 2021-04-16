using FluentAssertions;
using MailerSendNetCore.Common.Exceptions;
using MailerSendNetCore.Common.Interfaces;
using MailerSendNetCore.Emails;
using MailerSendNetCore.Emails.Dtos;
using MailerSendNetCore.UintTests.Mocks;
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
            IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), apiToken);

            var parameters = new MailerSendEmailParameters();
            parameters
                .WithFrom("sender@test.com", "Sender")
                .WithTo("test@test.com")
                .WithSubject("Hi!")
                .WithHtmlBody("this is a test");

            Func<Task> action = async () => { await client.SendEmailAsync(parameters).ConfigureAwait(false); };
            action.Should()
                .Throw<ApiException>()
                .WithMessage("Unexpected response HTTP status code (500).\n\nStatus: 500\nResponse: \n");
        }

        [Fact]
        public void Test_SendMail_When_UnprocessableEntityAndUnknownBody_Then_ThrowApiException()
        {
            var apiToken = "my token";
            var handler = new MockHttpMessageHandler(HttpStatusCode.UnprocessableEntity, responseContent: null);
            IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), apiToken);

            var parameters = new MailerSendEmailParameters();
            parameters
                .WithTo("test@test.com")
                .WithSubject("Hi!")
                .WithHtmlBody("this is a test");

            Func<Task> action = async () => { await client.SendEmailAsync(parameters).ConfigureAwait(false); };
            action.Should()
                .Throw<ApiException>()
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

            IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), apiToken);

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

            IMailerSendEmailClient client = new MailerSendEmailClient(new MockHttpClient(handler), apiToken);

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
    }
}
