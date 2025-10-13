using Newtonsoft.Json;

namespace MailerSendNetCore.Emails.Dtos;

public class MailerSendBulkEmailStatusData
{
    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("state")]
    public string? State { get; set; }

    [JsonProperty("total_recipients_count")]
    public int TotalRecipientsCount { get; set; }

    [JsonProperty("suppressed_recipients_count")]
    public int SuppressedRecipientsCount { get; set; }

    [JsonProperty("suppressed_recipients")]
    public object? SuppressedRecipients { get; set; }

    [JsonProperty("validation_errors_count")]
    public int ValidationErrorsCount { get; set; }

    [JsonProperty("validation_errors")]
    public object? ValidationErrors { get; set; }

    [JsonProperty("messages_id")]
    public List<string>? MessagesId { get; set; }

    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
