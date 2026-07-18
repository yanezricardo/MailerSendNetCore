using Newtonsoft.Json;

namespace MailerSendNetCore.Emails.Dtos;

public class MailerSendEmailResponse
{
    private IReadOnlyList<MailerSendEmailWarningResponse> _warningItems =
        Array.Empty<MailerSendEmailWarningResponse>();

    public string? MessageId { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("errors")]
    public IDictionary<string, string[]>? Errors { get; set; }

    [JsonProperty("warnings")]
    public MailerSendEmailWarningResponse? Warnings { get; set; }

    public IReadOnlyList<MailerSendEmailWarningResponse> WarningItems
    {
        get => _warningItems;
        init => _warningItems = Array.AsReadOnly(
            (value ?? Array.Empty<MailerSendEmailWarningResponse>()).ToArray());
    }

    public bool IsQueued => !string.IsNullOrWhiteSpace(MessageId);

    public bool IsSuppressed => WarningItems.Any(warning =>
        string.Equals(
            warning.Type,
            MailerSendWarningTypes.AllSuppressed,
            StringComparison.OrdinalIgnoreCase));

    public bool HasErrors => Errors is { Count: > 0 };
}
