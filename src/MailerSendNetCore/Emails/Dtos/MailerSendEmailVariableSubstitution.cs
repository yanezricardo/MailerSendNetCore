using Newtonsoft.Json;

namespace MailerSendNetCore.Emails.Dtos;

public class MailerSendEmailVariableSubstitution
{
    [JsonProperty("var")]
    public string? Var { get; set; }

    [JsonProperty("value")]
    public string? Value { get; set; }
}
