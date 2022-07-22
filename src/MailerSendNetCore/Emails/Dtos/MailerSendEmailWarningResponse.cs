using Newtonsoft.Json;

namespace MailerSendNetCore.Emails.Dtos
{
    public class MailerSendEmailWarningResponse
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("warning")]
        public string? Warning { get; set; }

        [JsonProperty("recipients")]
        public MailerSendEmailRecipientWarning[]? Recipients { get; set; }
    }
}