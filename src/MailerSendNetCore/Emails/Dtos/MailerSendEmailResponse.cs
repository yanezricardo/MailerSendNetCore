using Newtonsoft.Json;

namespace MailerSendNetCore.Emails.Dtos
{
    public class MailerSendEmailResponse
    {
        public string? MessageId { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("errors")]
        public IDictionary<string, string[]>? Errors { get; set; }

        [JsonProperty("warnings")]
        public MailerSendEmailWarningResponse? Warnings { get; set; }
    }
}
