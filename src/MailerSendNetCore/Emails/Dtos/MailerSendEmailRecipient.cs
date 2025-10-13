using Newtonsoft.Json;

namespace MailerSendNetCore.Emails.Dtos;

public class MailerSendEmailRecipient
{
    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    public MailerSendEmailRecipient(string email, string name)
    {
        Email = email;
        Name = name;
    }
}
