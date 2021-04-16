using Newtonsoft.Json;

namespace MailerSendNetCore.Emails.Dtos
{
    public class MailerSendEmailVariable
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("substitutions")]
        public MailerSendEmailVariableSubstitution[] Substitutions { get; set; }

        public MailerSendEmailVariable(string email, MailerSendEmailVariableSubstitution[] substitutions)
        {
            Email = email;
            Substitutions = substitutions;
        }
    }
}
