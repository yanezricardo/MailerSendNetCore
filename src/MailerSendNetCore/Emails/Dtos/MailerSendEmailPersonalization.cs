using Newtonsoft.Json;

namespace MailerSendNetCore.Emails.Dtos;

public class MailerSendEmailPersonalization
{
    public MailerSendEmailPersonalization(string email, object data)
    {
        Email = email;
        Data = data;
    }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("data")]
    public object Data { get; set; }
}
