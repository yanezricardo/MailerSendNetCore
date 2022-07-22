using Newtonsoft.Json;

namespace MailerSendNetCore.Emails.Dtos
{
    public class MailerSendBulkEmailStatusResponse
    {
        [JsonProperty("data")]
        public MailerSendBulkEmailStatusData? Data { get; set; }
    }
}
