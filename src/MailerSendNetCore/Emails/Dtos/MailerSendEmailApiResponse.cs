using Newtonsoft.Json;

namespace MailerSendNetCore.Emails.Dtos;

internal sealed class MailerSendEmailApiResponse
{
    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("errors")]
    public IDictionary<string, string[]>? Errors { get; set; }

    [JsonProperty("warnings")]
    public MailerSendEmailWarningApiResponse[]? Warnings { get; set; }
}

internal sealed class MailerSendEmailWarningApiResponse
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("warning")]
    public string? Warning { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("recipients")]
    public MailerSendEmailRecipientWarning[]? Recipients { get; set; }

    public MailerSendEmailWarningResponse ToResponse()
    {
        return new MailerSendEmailWarningResponse
        {
            Type = Type,
            Warning = Warning ?? Message,
            Recipients = Recipients
        };
    }
}
