using FluentAssertions;
using MailerSendNetCore.Emails.Dtos;
using System;
using System.Linq;
using Xunit;

namespace MailerSendNetCore.UnitTests.Emails
{
    public class MailerSendEmailParametersTests
    {
        [Fact]
        public void Test_Constructor_ShouldInitializeCollections()
        {
            var instance = new MailerSendEmailParameters();
            instance.To.Should().NotBeNull();
            instance.ReplyTo.Should().NotBeNull();
            instance.Variables.Should().NotBeNull();
            instance.Attachments.Should().NotBeNull();
            instance.Personalizations.Should().NotBeNull();
            instance.Tags.Should().NotBeNull();
        }

        [Fact]
        public void Test_WithTemplateId_ShouldSetTemplateid()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithTemplateId("1000");
            instance.TemplateId.Should().Be("1000");
        }

        [Fact]
        public void Test_WithAttachment1_ShouldReplaceAttachmentCollection()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithAttachment(new MailerSendEmailAttachment("1", "file.pdf", "<base64content>"));
            instance.Attachments.Should().NotBeEmpty();
            instance.Attachments.Should().HaveCount(1);

            var attachment = instance.Attachments.First();
            attachment.Id.Should().Be("1");
            attachment.FileName.Should().Be("file.pdf");
            attachment.Content.Should().Be("<base64content>");
        }

        [Fact]
        public void Test_WithAttachment2_ShouldAddNewAttachment()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithAttachment(new MailerSendEmailAttachment("1", "file.pdf", "<base64content>"));
            instance.WithAttachment("2", "file2.pdf", "<base64content>");
            instance.Attachments.Should().NotBeEmpty();
            instance.Attachments.Should().HaveCount(2);

            var attachment = instance.Attachments.Last();
            attachment.Id.Should().Be("2");
            attachment.FileName.Should().Be("file2.pdf");
            attachment.Content.Should().Be("<base64content>");
        }

        [Fact]
        public void Test_WithFrom1_ShouldSetFromObject()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithFrom(new MailerSendEmailRecipient("test@test.com", "Test"));
            instance.From.Should().NotBeNull();
            instance.From.Email.Should().Be("test@test.com");
            instance.From.Name.Should().Be("Test");
        }

        [Fact]
        public void Test_WithFrom2_ShouldSetFromObject()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithFrom("test@test.com", "Test");
            instance.From.Should().NotBeNull();
            instance.From.Email.Should().Be("test@test.com");
            instance.From.Name.Should().Be("Test");
        }

        [Fact]
        public void Test_WithHtmlBody_ShouldSetHtml()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithHtmlBody("<p>email body<p>");
            instance.Html.Should().NotBeNullOrWhiteSpace();
            instance.Html.Should().Be("<p>email body<p>");
        }

        [Fact]
        public void Test_WithHtmlBody_ShouldSetTextToPlainText()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithHtmlBody("<p>email body<p>");
            instance.Text.Should().NotBeNullOrWhiteSpace();
            instance.Text.Should().Be("\nemail body\n");
        }

        [Fact]
        public void Test_WithPersonalization1_ShouldRequireEmailInRecipientCollection()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithTo("test@test.com");

            Action action = () => instance.WithPersonalization(
                new MailerSendEmailPersonalization("test@test.com", new { p1 = "1" }),
                new MailerSendEmailPersonalization("test2@test.com", new { p1 = "2" }));

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("The email must be in the list of recipients (to)");
        }

        [Fact]
        public void Test_WithPersonalization1_ShouldReplacePersonalizationCollection()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithTo("test@test.com");
            instance.WithPersonalization(new MailerSendEmailPersonalization("test@test.com", new { p1 = "1" }));
            instance.Personalizations.Should().NotBeEmpty();
            instance.Personalizations.Should().HaveCount(1);

            var item = instance.Personalizations.First();
            item.Email.Should().Be("test@test.com");
            item.Data.Should().NotBeNull();
            item.Data.Should().Be(new { p1 = "1" });
        }

        [Fact]
        public void Test_WithPersonalization2_ShouldRequireEmailInRecipientCollection()
        {
            var instance = new MailerSendEmailParameters();
            Action action = () => instance.WithPersonalization("test@test.com", new { p1 = "1" });
            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("The email must be in the list of recipients (to)");
        }

        [Fact]
        public void Test_WithPersonalization2_ShouldReplacePersonalizationCollection()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithTo("test@test.com");
            instance.WithTo("test2@test.com");
            instance.WithPersonalization(new MailerSendEmailPersonalization("test@test.com", new { p1 = "1" }));
            instance.WithPersonalization("test2@test.com", new { p1 = "2" });
            instance.Personalizations.Should().NotBeEmpty();
            instance.Personalizations.Should().HaveCount(2);

            var item = instance.Personalizations.Last();
            item.Email.Should().Be("test2@test.com");
            item.Data.Should().NotBeNull();
            item.Data.Should().Be(new { p1 = "2" });
        }

        [Fact]
        public void Test_WithSubject_ShouldSetSubject()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithSubject("hi!");
            instance.Subject.Should().NotBeNullOrWhiteSpace();
            instance.Subject.Should().Be("hi!");
        }

        [Fact]
        public void Test_WithTags_ShouldReplaceTagsCollection()
        {
            var instance = new MailerSendEmailParameters();
            instance.Tags.Add("first tag");

            instance.WithTags("tag1", "tag2", "tag3");
            instance.Tags.Should().NotBeNullOrEmpty();
            instance.Tags.Should().HaveCount(3);
            instance.Tags.Should().BeEquivalentTo(new string[] { "tag1", "tag2", "tag3" });
        }

        [Fact]
        public void Test_WithTo1_ShouldRecplaceToCollection()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithTo(new MailerSendEmailRecipient("test@tests.com", "Test"));
            instance.To.Should().NotBeEmpty();
            instance.To.Should().HaveCount(1);

            var item = instance.To.First();
            item.Email.Should().Be("test@tests.com");
            item.Name.Should().Be("Test");
        }

        [Fact]
        public void Test_WithTo2_ShouldAddNewRecipient()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithTo(new MailerSendEmailRecipient("test@tests.com", "Test"));
            instance.WithTo("test2@tests.com");
            instance.To.Should().NotBeEmpty();
            instance.To.Should().HaveCount(2);

            var item = instance.To.Last();
            item.Email.Should().Be("test2@tests.com");
            item.Name.Should().Be("");
        }

        [Fact]
        public void Test_WithBcc1_ShouldRecplaceBccCollection()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithBcc(new MailerSendEmailRecipient("test@tests.com", "Test"));
            instance.Bcc.Should().NotBeEmpty();
            instance.Bcc.Should().HaveCount(1);

            var item = instance.Bcc.First();
            item.Email.Should().Be("test@tests.com");
            item.Name.Should().Be("Test");
        }

        [Fact]
        public void Test_WithBcc2_ShouldAddNewRecipient()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithBcc(new MailerSendEmailRecipient("test@tests.com", "Test"));
            instance.WithBcc("test2@tests.com");
            instance.Bcc.Should().NotBeEmpty();
            instance.Bcc.Should().HaveCount(2);

            var item = instance.Bcc.Last();
            item.Email.Should().Be("test2@tests.com");
            item.Name.Should().Be("");
        }

        [Fact]
        public void Test_WithFrom1_ShouldReplaceVariableCollection()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithTo("test@test.com");

            instance.WithVariable(
                    new MailerSendEmailVariable("test@test.com", new MailerSendEmailVariableSubstitution[]
                    {
                        new MailerSendEmailVariableSubstitution
                        {
                            Var = "v1",
                            Value = "1",
                        },
                    }));

            instance.WithVariable(
                    new MailerSendEmailVariable("test@test.com", new MailerSendEmailVariableSubstitution[]
                    {
                        new MailerSendEmailVariableSubstitution
                        {
                            Var = "v2",
                            Value = "2",
                        },
                    }));

            instance.Variables.Should().NotBeEmpty();
            instance.Variables.Should().HaveCount(1);
        }

        [Fact]
        public void Test_WithFrom2_ShouldAdNewVariable()
        {
            var instance = new MailerSendEmailParameters();
            instance.WithTo("test@test.com");
            instance.WithTo("test2@test.com");

            instance.WithVariable("test@test.com", new MailerSendEmailVariableSubstitution[]
                    {
                        new MailerSendEmailVariableSubstitution
                        {
                            Var = "v1",
                            Value = "1",
                        },
                    });

            instance.WithVariable("test2@test.com", new MailerSendEmailVariableSubstitution[]
                    {
                        new MailerSendEmailVariableSubstitution
                        {
                            Var = "v2",
                            Value = "2",
                        },
                    });

            instance.Variables.Should().NotBeEmpty();
            instance.Variables.Should().HaveCount(2);
        }

        [Fact]
        public void Test_WithFrom1_ShouldRequireEmailInRecipientCollection()
        {
            var instance = new MailerSendEmailParameters();
            Action action = () =>
                instance.WithVariable(
                    new MailerSendEmailVariable("test@test.com", new MailerSendEmailVariableSubstitution[]
                    {
                        new MailerSendEmailVariableSubstitution
                        {
                            Var = "v1",
                            Value = "1",
                        },
                    }));

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("The email must be in the list of recipients (to)");
        }

        [Fact]
        public void Test_WithFrom2_ShouldRequireEmailInRecipientCollection()
        {
            var instance = new MailerSendEmailParameters();
            Action action = () =>
                instance.WithVariable("test@test.com", new MailerSendEmailVariableSubstitution[]
                {
                    new MailerSendEmailVariableSubstitution
                    {
                        Var = "v1",
                        Value = "1",
                    },
                });

            action.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("The email must be in the list of recipients (to)");
        }
    }
}
