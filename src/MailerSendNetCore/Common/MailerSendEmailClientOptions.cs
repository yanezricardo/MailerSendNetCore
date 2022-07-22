namespace MailerSendNetCore.Common
{
    public class MailerSendEmailClientOptions
    {
        public string? ApiToken { get; set; }
        public string? ApiUrl { get; set; }
        public bool UseRetryPolicy { get; set; }
        public int RetryCount { get; set; }
        public int RetryDelayInMilliseconds { get; set; }
    }
}
