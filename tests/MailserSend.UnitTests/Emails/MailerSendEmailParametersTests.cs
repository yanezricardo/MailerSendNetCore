using MailerSendNetCore.Emails.Dtos;
using System;
using System.Linq;
using Xunit;

namespace MailerSendNetCore.UnitTests.Emails;

public class MailerSendEmailParametersTests
{
    [Fact]
    public void Test_Constructor_ShouldInitializeCollections()
    {
        var instance = new MailerSendEmailParameters();
        Assert.NotNull(instance.To);
        Assert.NotNull(instance.ReplyTo);
        Assert.NotNull(instance.Variables);
        Assert.NotNull(instance.Attachments);
        Assert.NotNull(instance.Personalizations);
        Assert.NotNull(instance.Tags);
    }

    [Fact]
    public void Test_WithTemplateId_ShouldSetTemplateid()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithTemplateId("1000");
        Assert.Equal("1000", instance.TemplateId);
    }

    [Fact]
    public void Test_WithSendTime_ShouldSetSendTime()
    {
        var instance = new MailerSendEmailParameters();
        DateTime sendDateTime = DateTime.Now;
        long sendUnixTime = ((DateTimeOffset)sendDateTime).ToUnixTimeSeconds();
        instance.WithSendTime(sendDateTime);
        Assert.Equal(sendUnixTime, instance.SendTime);
    }

    [Fact]
    public void Test_SendTime_MustBeNullByDefault()
    {
        var instance = new MailerSendEmailParameters();
        Assert.Null(instance.SendTime);
    }

    [Fact]
    public void Test_WithSendTime_ShouldValidateSendTimeRange()
    {
        var instance = new MailerSendEmailParameters();

        DateTime invalidSendDate = DateTime.Now.AddHours(73);
        instance.WithTo("test@test.com");

        Action action = () => instance.WithSendTime(invalidSendDate);
        var exSendTime = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("The email send time cannot be more than 72 hours in the future", exSendTime.Message);
    }

    [Fact]
    public void Test_WithAttachment1_ShouldReplaceAttachmentCollection()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithAttachment(new MailerSendEmailAttachment("1", "file.pdf", "<base64content>"));
        Assert.NotEmpty(instance.Attachments);
        Assert.Single(instance.Attachments);

        var attachment = instance.Attachments.First();
        Assert.Equal("1", attachment.Id);
        Assert.Equal("file.pdf", attachment.FileName);
        Assert.Equal("<base64content>", attachment.Content);
    }

    [Fact]
    public void Test_WithAttachment2_ShouldAddNewAttachment()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithAttachment(new MailerSendEmailAttachment("1", "file.pdf", "<base64content>"));
        instance.WithAttachment("2", "file2.pdf", "<base64content>");
        Assert.NotEmpty(instance.Attachments);
        Assert.Equal(2, instance.Attachments.Count);

        var attachment = instance.Attachments.Last();
        Assert.Equal("2", attachment.Id);
        Assert.Equal("file2.pdf", attachment.FileName);
        Assert.Equal("<base64content>", attachment.Content);
    }

    [Fact]
    public void Test_WithFrom1_ShouldSetFromObject()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithFrom(new MailerSendEmailRecipient("test@test.com", "Test"));
        Assert.NotNull(instance.From);
        Assert.Equal("test@test.com", instance.From.Email);
        Assert.Equal("Test", instance.From.Name);
    }

    [Fact]
    public void Test_WithFrom2_ShouldSetFromObject()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithFrom("test@test.com", "Test");
        Assert.NotNull(instance.From);
        Assert.Equal("test@test.com", instance.From.Email);
        Assert.Equal("Test", instance.From.Name);
    }

    [Fact]
    public void Test_WithHtmlBody_ShouldSetHtml()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithHtmlBody("<p>email body<p>");
        Assert.False(string.IsNullOrWhiteSpace(instance.Html));
        Assert.Equal("<p>email body<p>", instance.Html);
    }

    [Fact]
    public void Test_WithHtmlBody_ShouldSetTextToPlainText()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithHtmlBody("<p>email body<p>");
        Assert.False(string.IsNullOrWhiteSpace(instance.Text));
        Assert.Equal("\nemail body\n", instance.Text);
    }

    [Fact]
    public void Test_HtmlToPlainText_Should_RemoveScriptsStyles_BrToNewline_AndDecodeEntities()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithHtmlBody("<script>evil()</script><style>.a{}</style><div>a<br>b &amp; c</div>");
        Assert.Equal("\na\nb & c\n", instance.Text);
    }

    [Fact]
    public void Test_WithPersonalization1_ShouldRequireEmailInRecipientCollection()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithTo("test@test.com");

        Action action = () => instance.WithPersonalization(
            new MailerSendEmailPersonalization("test@test.com", new { p1 = "1" }),
            new MailerSendEmailPersonalization("test2@test.com", new { p1 = "2" }));

        var exPers1 = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("The email must be in the list of recipients (to)", exPers1.Message);
    }

    [Fact]
    public void Test_WithPersonalization1_ShouldReplacePersonalizationCollection()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithTo("test@test.com");
        instance.WithPersonalization(new MailerSendEmailPersonalization("test@test.com", new { p1 = "1" }));
        Assert.NotEmpty(instance.Personalizations);
        Assert.Single(instance.Personalizations);

        var item = instance.Personalizations.First();
        Assert.Equal("test@test.com", item.Email);
        Assert.NotNull(item.Data);
        Assert.Equal(new { p1 = "1" }, item.Data);
    }

    [Fact]
    public void Test_WithPersonalization2_ShouldRequireEmailInRecipientCollection()
    {
        var instance = new MailerSendEmailParameters();
        Action action = () => instance.WithPersonalization("test@test.com", new { p1 = "1" });
        var exPers2 = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("The email must be in the list of recipients (to)", exPers2.Message);
    }

    [Fact]
    public void Test_WithPersonalization2_ShouldReplacePersonalizationCollection()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithTo("test@test.com");
        instance.WithTo("test2@test.com");
        instance.WithPersonalization(new MailerSendEmailPersonalization("test@test.com", new { p1 = "1" }));
        instance.WithPersonalization("test2@test.com", new { p1 = "2" });
        Assert.NotEmpty(instance.Personalizations);
        Assert.Equal(2, instance.Personalizations.Count);

        var item = instance.Personalizations.Last();
        Assert.Equal("test2@test.com", item.Email);
        Assert.NotNull(item.Data);
        Assert.Equal(new { p1 = "2" }, item.Data);
    }

    [Fact]
    public void Test_WithTo_StringArray_NullOrEmpty_ShouldNotAdd()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithTo(Array.Empty<string>());
        Assert.Empty(instance.To);

        instance.WithTo((string[])null);
        Assert.Empty(instance.To);
    }

    [Fact]
    public void Test_WithCc_StringArray_NullOrEmpty_ShouldNotAdd()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithCc(Array.Empty<string>());
        Assert.Empty(instance.Cc);

        instance.WithCc((string[])null);
        Assert.Empty(instance.Cc);
    }

    [Fact]
    public void Test_WithBcc_StringArray_NullOrEmpty_ShouldNotAdd()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithBcc(Array.Empty<string>());
        Assert.Empty(instance.Bcc);

        instance.WithBcc((string[])null);
        Assert.Empty(instance.Bcc);
    }

    [Fact]
    public void Test_WithAttachment_ParamsNull_ShouldKeepExisting()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithAttachment("1", "a.pdf", "base64");
        instance.WithAttachment((MailerSendEmailAttachment[])null);
        Assert.Single(instance.Attachments);
        Assert.Equal("1", instance.Attachments.First().Id);
    }

    [Fact]
    public void Test_WithVariable_ParamsNull_ShouldKeepExisting()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithTo("test@test.com");
        instance.WithVariable("test@test.com", new MailerSendEmailVariableSubstitution { Var = "v", Value = "1" });
        instance.WithVariable((MailerSendEmailVariable[])null);
        Assert.Single(instance.Variables);
        Assert.Equal("test@test.com", instance.Variables.First().Email);
    }

    [Fact]
    public void Test_WithPersonalization_ParamsNull_ShouldKeepExisting()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithTo("test@test.com");
        instance.WithPersonalization("test@test.com", new { x = 1 });
        instance.WithPersonalization((MailerSendEmailPersonalization[])null);
        Assert.Single(instance.Personalizations);
        Assert.Equal("test@test.com", instance.Personalizations.First().Email);
    }

    [Fact]
    public void Test_WithSubject_ShouldSetSubject()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithSubject("hi!");
        Assert.False(string.IsNullOrWhiteSpace(instance.Subject));
        Assert.Equal("hi!", instance.Subject);
    }

    [Fact]
    public void Test_WithTags_ShouldReplaceTagsCollection()
    {
        var instance = new MailerSendEmailParameters();
        instance.Tags.Add("first tag");

        instance.WithTags("tag1", "tag2", "tag3");
        Assert.NotNull(instance.Tags);
        Assert.NotEmpty(instance.Tags);
        Assert.Equal(3, instance.Tags.Count);
        Assert.Equal(new string[] { "tag1", "tag2", "tag3" }, instance.Tags);
    }

    [Fact]
    public void Test_WithTo1_ShouldRecplaceToCollection()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithTo(new MailerSendEmailRecipient("test@tests.com", "Test"));
        Assert.NotEmpty(instance.To);
        Assert.Single(instance.To);

        var item = instance.To.First();
        Assert.Equal("test@tests.com", item.Email);
        Assert.Equal("Test", item.Name);
    }

    [Fact]
    public void Test_WithTo2_ShouldAddNewRecipient()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithTo(new MailerSendEmailRecipient("test@tests.com", "Test"));
        instance.WithTo("test2@tests.com");
        Assert.NotEmpty(instance.To);
        Assert.Equal(2, instance.To.Count);

        var item = instance.To.Last();
        Assert.Equal("test2@tests.com", item.Email);
        Assert.Equal("", item.Name);
    }

    [Fact]
    public void Test_WithBcc1_ShouldRecplaceBccCollection()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithBcc(new MailerSendEmailRecipient("test@tests.com", "Test"));
        Assert.NotEmpty(instance.Bcc);
        Assert.Single(instance.Bcc);

        var item = instance.Bcc.First();
        Assert.Equal("test@tests.com", item.Email);
        Assert.Equal("Test", item.Name);
    }

    [Fact]
    public void Test_WithBcc2_ShouldAddNewRecipient()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithBcc(new MailerSendEmailRecipient("test@tests.com", "Test"));
        instance.WithBcc("test2@tests.com");
        Assert.NotEmpty(instance.Bcc);
        Assert.Equal(2, instance.Bcc.Count);

        var item = instance.Bcc.Last();
        Assert.Equal("test2@tests.com", item.Email);
        Assert.Equal("", item.Name);
    }

    [Fact]
    public void Test_WithCc1_ShouldRecplaceCcCollection()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithCc(new MailerSendEmailRecipient("test@tests.com", "Test"));
        Assert.NotEmpty(instance.Cc);
        Assert.Single(instance.Cc);

        var item = instance.Cc.First();
        Assert.Equal("test@tests.com", item.Email);
        Assert.Equal("Test", item.Name);
    }

    [Fact]
    public void Test_WithCc2_ShouldAddNewRecipient()
    {
        var instance = new MailerSendEmailParameters();
        instance.WithCc(new MailerSendEmailRecipient("test@tests.com", "Test"));
        instance.WithCc("test2@tests.com");
        Assert.NotEmpty(instance.Cc);
        Assert.Equal(2, instance.Cc.Count);

        var item = instance.Cc.Last();
        Assert.Equal("test2@tests.com", item.Email);
        Assert.Equal("", item.Name);
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

        Assert.NotEmpty(instance.Variables);
        Assert.Single(instance.Variables);
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

        Assert.NotEmpty(instance.Variables);
        Assert.Equal(2, instance.Variables.Count);
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

        var exVar1 = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("The email must be in the list of recipients (to)", exVar1.Message);
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

        var exVar2 = Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("The email must be in the list of recipients (to)", exVar2.Message);
    }
}

