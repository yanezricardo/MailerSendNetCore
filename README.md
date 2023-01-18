# Important

This project is an unofficial client for MailerSend and does not claim to be complete, I just added the use cases that I needed for my uses.

## Dependencies

* .NET 7 SDK
* Newtonsoft.Json

## How To Use

* Clone the project and add a referece in for project or Install the [nuget package](https://www.nuget.org/packages/MailerSendNetCore/0.0.4).
* Configure the client using one of the following methods>

```
            //Default options without ApiToken
            services.AddMailerSendEmailClient();

            //Read options from configuration
            services.AddMailerSendEmailClient(configuration.GetSection("MailerSend"));

            //Set options from configuration manually
            services.AddMailerSendEmailClient(options =>
            {
                options.ApiUrl = configuration["MailerSend:ApiUrl"];
                options.ApiToken = configuration["MailerSend:ApiToken"];
            });

            //Add custom options
            services.AddMailerSendEmailClient(new MailerSendEmailClientOptions
            {
                ApiUrl = configuration["MailerSend:ApiUrl"],
                ApiToken = configuration["MailerSend:ApiToken"]
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