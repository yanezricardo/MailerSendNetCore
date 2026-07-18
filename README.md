## MailerSend SDK for .NET

This project provides an easy way to interact with the MailerSend API using C# and .NET. It targets .NET 10 (net10.0) and uses Newtonsoft.Json for JSON serialization and deserialization.

**This is an unofficial SDK for MailerSend and does not claim to be complete.**

## Getting Started

To start using this SDK, you will need to install it via NuGet or cloning and adding a reference in your project.

### Installation
```powershell
Install-Package MailerSendNetCore
```

### Usage

#### Add "MailerSend" section to appsettings.json
```json
  "MailerSend": {
    "ApiUrl": "https://api.mailersend.com/v1",
    "ApiToken": "<your MailerSend api token>",
    "UseRetryPolicy": false,
    "RetryCount": 5,
    "RetryDelayInMilliseconds": 5000
  },
 ```

`UseRetryPolicy=false` is recommended unless your application explicitly accepts the retry
trade-off. When retries are enabled, transient failures can retry `POST` requests. If MailerSend
accepted the first request but the transport failed before the response reached the caller, a retry
can duplicate email delivery.

#### Configure the client using one of the following methods:

```C#
//METHOD #1: Read options from configuration (RECOMMENDED)
builder.Services.AddMailerSendEmailClient(builder.Configuration.GetSection("MailerSend"));

//METHOD #2: Set options from configuration manually
builder.Services.AddMailerSendEmailClient(options =>
{
    options.ApiUrl = builder.Configuration["MailerSend:ApiUrl"];
    options.ApiToken = builder.Configuration["MailerSend:ApiToken"];
});

//METHOD #3: Add custom options instance
builder.Services.AddMailerSendEmailClient(new MailerSendEmailClientOptions
{
    ApiUrl = builder.Configuration["MailerSend:ApiUrl"],
    ApiToken = builder.Configuration["MailerSend:ApiToken"]
});
```

#### Inject the client into your service, controller or handler
```C#
private readonly IMailerSendEmailClient _mailerSendEmailClient;

public EmailService(IMailerSendEmailClient mailerSendEmailClient)
{
    _mailerSendEmailClient = mailerSendEmailClient;
}
```

#### Send Emails

```C#
public async Task<string> SendEmail(string templateId, string senderName, string senderEmail, string[] to, string subject, MailerSendEmailAttachment[] attachments, IDictionary<string, string>? variables, CancellationToken cancellationToken = default)
{
    var parameters = new MailerSendEmailParameters();
    parameters
        .WithTemplateId(templateId)
        .WithFrom(senderEmail, senderName)
        .WithTo(to)
        .WithAttachment(attachments)
        .WithSubject(subject);

    if (variables is { Count: > 0 })
    {
        foreach (var recipient in to)
        {
            parameters.WithPersonalization(recipient, variables);
        }
    }

    var response = await _mailerSendEmailClient.SendEmailAsync(parameters, cancellationToken);
    if (response.HasErrors)
    {
        // Handle the existing 422 validation response.
    }

    return response.MessageId;
}
```

#### Accepted responses and suppressions

MailerSend can return `202 Accepted` with warning details:

```C#
var response = await _mailerSendEmailClient.SendEmailAsync(parameters, cancellationToken);

if (response.IsSuppressed)
{
    // ALL_SUPPRESSED: no message was queued and MessageId is absent.
}
else if (response.WarningItems.Count > 0)
{
    // SOME_SUPPRESSED: inspect all warning details.
}

if (response.IsQueued)
{
    Console.WriteLine(response.MessageId);
}
```

The legacy `Warnings` property remains available and contains the first warning.
`WarningItems` contains the complete response collection. Validation failures continue to return a
`MailerSendEmailResponse` with `Errors`, preserving the existing `422` behavior.

#### Retry compatibility

The existing Polly callback overloads remain available in `0.3.0` for compatibility. They are
legacy compatibility API and are scheduled for redesign in `1.0`.

Applications that enable retries should make their email workflow tolerant of duplicate delivery
and inspect `ApiException.RetryAfter` and `ApiException.IsTransient` when handling failures.

#### Attachment disposition (`inline` / `attachment`)

`MailerSendEmailAttachment` supports an optional `Disposition` value:
- `inline` for CID-based content (for example embedded images),
- `attachment` for regular file attachments.

When using `inline`, provide a non-empty `Id` so it can be referenced from HTML using `cid:<id>`.

```C#
var parameters = new MailerSendEmailParameters()
    .WithFrom("sender@example.com", "Sender")
    .WithTo("recipient@example.com")
    .WithSubject("Inline image example")
    .WithHtmlBody("<p>Logo:</p><img src=\"cid:logo-company\" />")
    .WithAttachment("logo-company", "logo.png", "<base64content>", "inline")
    .WithAttachment("doc-1", "file.pdf", "<base64content>", "attachment");
```

## Additional Resources
* [MailerSend developer site](https://developers.mailersend.com)
* [Newtonsoft.Json documentation](https://www.newtonsoft.com/json/help/html/introduction.htm)
* [.NET documentation](https://learn.microsoft.com/en-us/dotnet/)

## Migrating from 0.2.0

`0.3.0` is intended as a compatible upgrade. Existing registration overloads, interfaces, response
properties, retry options, Polly callbacks, and `422` behavior are preserved.

The package no longer brings `Microsoft.Extensions.Http.Polly` or `Polly.Extensions.Http` into the
consumer dependency graph. Consumers that directly used those transitive packages must add their
own explicit package references.
