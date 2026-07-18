using MailerSendNetCore.Emails.Dtos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Xunit;

namespace MailerSendNetCore.UnitTests.Emails;

public class MailerSendEmailResponseTests
{
    [Fact]
    public void WarningItems_ObjectInitializer_CreatesReadOnlyDefensiveCopy()
    {
        var originalWarning = new MailerSendEmailWarningResponse
        {
            Type = "SOME_SUPPRESSED",
            Warning = "First warning"
        };
        var source = new List<MailerSendEmailWarningResponse> { originalWarning };

        var response = new MailerSendEmailResponse { WarningItems = source };
        source.Add(new MailerSendEmailWarningResponse { Warning = "Added later" });

        var warning = Assert.Single(response.WarningItems);
        Assert.Same(originalWarning, warning);
        Assert.Equal("First warning", warning.Warning);
        Assert.IsAssignableFrom<IReadOnlyList<MailerSendEmailWarningResponse>>(
            response.WarningItems);
    }

    [Fact]
    public void WarningItems_NullInitializer_NormalizesToEmptyCollection()
    {
        var response = new MailerSendEmailResponse { WarningItems = null! };

        Assert.NotNull(response.WarningItems);
        Assert.Empty(response.WarningItems);
    }

    [Fact]
    public void WarningItems_NewtonsoftRoundTrip_PreservesItems()
    {
        var source = new MailerSendEmailResponse
        {
            MessageId = "message-id",
            Message = "Accepted with warnings.",
            WarningItems =
            [
                new MailerSendEmailWarningResponse
                {
                    Type = "SOME_SUPPRESSED",
                    Warning = "One recipient was suppressed.",
                    Recipients =
                    [
                        new MailerSendEmailRecipientWarning
                        {
                            Email = "suppressed@example.test",
                            Name = "Suppressed",
                            Reasons = ["blocklist"]
                        }
                    ]
                }
            ]
        };

        var json = JsonConvert.SerializeObject(source);
        var roundTripped = JsonConvert.DeserializeObject<MailerSendEmailResponse>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(source.MessageId, roundTripped.MessageId);
        Assert.Equal(source.Message, roundTripped.Message);
        var warning = Assert.Single(roundTripped.WarningItems);
        Assert.Equal("SOME_SUPPRESSED", warning.Type);
        Assert.Equal("One recipient was suppressed.", warning.Warning);
        var recipient = Assert.Single(warning.Recipients);
        Assert.Equal("suppressed@example.test", recipient.Email);
        Assert.Equal("Suppressed", recipient.Name);
        Assert.Equal(new[] { "blocklist" }, recipient.Reasons);
    }

    [Theory]
    [InlineData("ALL_SUPPRESSED")]
    [InlineData("all_suppressed")]
    [InlineData("All_Suppressed")]
    public void IsSuppressed_AllSuppressedType_IsCaseInsensitive(string warningType)
    {
        var response = new MailerSendEmailResponse
        {
            WarningItems =
            [
                new MailerSendEmailWarningResponse { Type = warningType }
            ]
        };

        Assert.True(response.IsSuppressed);
    }

    [Fact]
    public void CompatibilityFlags_ReflectMessageIdErrorsAndWarningType()
    {
        var response = new MailerSendEmailResponse
        {
            MessageId = "message-id",
            Errors = new Dictionary<string, string[]>
            {
                ["email"] = ["Invalid recipient."]
            },
            WarningItems =
            [
                new MailerSendEmailWarningResponse { Type = "SOME_SUPPRESSED" }
            ]
        };

        Assert.True(response.IsQueued);
        Assert.True(response.HasErrors);
        Assert.False(response.IsSuppressed);
    }

    [Fact]
    public void CompatibilityFlags_EmptyLegacyValues_AreFalse()
    {
        var response = new MailerSendEmailResponse
        {
            MessageId = " ",
            Errors = new Dictionary<string, string[]>()
        };

        Assert.False(response.IsQueued);
        Assert.False(response.HasErrors);
        Assert.False(response.IsSuppressed);
        Assert.Empty(response.WarningItems);
    }
}
