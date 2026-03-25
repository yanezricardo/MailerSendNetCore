using Newtonsoft.Json;

namespace MailerSendNetCore.Emails.Dtos;

public class MailerSendEmailAttachment
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("filename")]
    public string FileName { get; set; }

    [JsonProperty("content")]
    public string Content { get; set; }

    [JsonProperty("disposition", NullValueHandling = NullValueHandling.Ignore)]
    public string? Disposition { get; set; }

    public MailerSendEmailAttachment(string id, string filename, string content)
    {
        Id = id;
        FileName = filename;
        Content = content;
    }

    public MailerSendEmailAttachment(string id, string filename, string content, string disposition)
    {
        Id = id;
        FileName = filename;
        Content = content;
        Disposition = disposition;
    }
}
