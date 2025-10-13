using Newtonsoft.Json;

namespace MailerSendNetCore.Emails.Dtos;

public class MailerSendEmailRecipientWarning
{
    [JsonProperty("email")]
    public string? Email { get; set; }

    [JsonProperty("Name")]
    public string? Name { get; set; }

    [JsonProperty("reasons")]
    public string[]? Reasons { get; set; }
}
