using Newtonsoft.Json;

namespace MailerSendNetCore.Emails.Dtos
{
    public class MailerSendBulkEmailResponse
    {
        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("bulk_email_id")]
        public string? BulkEmailId { get; set; }
    }
}
