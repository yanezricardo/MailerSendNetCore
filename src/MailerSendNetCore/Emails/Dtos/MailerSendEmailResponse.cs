using Newtonsoft.Json;
using System.Collections.Generic;

namespace MailerSendNetCore.Emails.Dtos
{
    public class MailerSendEmailResponse
    {
        public string MessageId { get; set; }

        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        [JsonProperty("errors", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string[]> Errors { get; set; }

        [JsonProperty("warnings", NullValueHandling = NullValueHandling.Ignore)]
        public MailerSendEmailWarningResponse Warnings { get; set; }
    }
}
