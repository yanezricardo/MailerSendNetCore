## MailerSend SDK for .NET

This project provides an easy way to interact with the MailerSend API using C# and .NET. It targets .NET 9 (net9.0) and uses Newtonsoft.Json for JSON serialization and deserialization.

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
    "UseRetryPolicy": true,
    "RetryCount": 5,
    "RetryDelayInMilliseconds": 5000
  },
 ```

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
    if (response is { Errors.Count: > 0 })
    {
        //handle errors                
    }

    return response.MessageId;
}
```

## Additional Resources
* [MailerSend developer site](https://developers.mailersend.com)
* [Newtonsoft.Json documentation](https://www.newtonsoft.com/json/help/html/introduction.htm)
* [.NET documentation](https://learn.microsoft.com/en-us/dotnet/)
