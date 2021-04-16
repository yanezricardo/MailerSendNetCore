# Important

This project is an unofficial client for MailerSend and does not claim to be complete, I just added the use cases that I needed for my uses.

## Dependencies

* .NET 5 SDK
* Newtonsoft.Json

## How To Use

* Clone the project and add a referece in for project or Install the [nuget package](https://www.nuget.org/packages/MailerSendNetCore/0.0.1).
* Configure the client in Startup file

```
            services.AddScoped<IMailerSendEmailClient, MailerSendEmailClient>(client =>
            {
                var httpClient = new HttpClient()
                {
                    BaseAddress = new System.Uri(Configuration["MailerSend:ApiUrl"]),
                };
                return new MailerSendEmailClient(httpClient, Configuration["MailerSend:ApiToken"]);
            });
```
* Inject the client and use it

```
            var parameters = new MailerSendEmailParameters();
            parameters
                .WithTemplateId(templateId)
                .WithFrom(senderEmail, senderName)
                .WithTo(to)
                .WithSubject(subject)
                .WithAttachment(attachments);

            var response = await _mailerSendEmailClient.SendEmail(parameters, cancellationToken).ConfigureAwait(false);
```