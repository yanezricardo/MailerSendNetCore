using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace MailerSendNetCore.Emails.Dtos
{
    public class MailerSendEmailParameters
    {
        [JsonProperty("from")]
        public MailerSendEmailRecipient From { get; set; }

        [JsonProperty("to")]
        public ICollection<MailerSendEmailRecipient> To { get; set; }

        [JsonProperty("reply_to")]
        public ICollection<MailerSendEmailRecipient> ReplyTo { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("html")]
        public string Html { get; set; }

        [JsonProperty("template_id")]
        public string TemplateId { get; set; }

        [JsonProperty("variables")]
        public ICollection<MailerSendEmailVariable> Variables { get; set; }

        [JsonProperty("attachments")]
        public ICollection<MailerSendEmailAttachment> Attachments { get; set; }

        [JsonProperty("personalization")]
        public ICollection<MailerSendEmailPersonalization> Personalizations { get; set; }

        [JsonProperty("tags")]
        public ICollection<string> Tags { get; set; }

        public MailerSendEmailParameters()
        {
            To = new List<MailerSendEmailRecipient>();
            ReplyTo = new List<MailerSendEmailRecipient>();
            Variables = new List<MailerSendEmailVariable>();
            Attachments = new List<MailerSendEmailAttachment>();
            Personalizations = new List<MailerSendEmailPersonalization>();
            Tags = new List<string>();
        }

        public MailerSendEmailParameters WithTemplateId(string templateId)
        {
            TemplateId = templateId;
            return this;
        }

        public MailerSendEmailParameters WithSubject(string subject)
        {
            Subject = subject;
            return this;
        }

        public MailerSendEmailParameters WithHtmlBody(string html)
        {
            Html = html;
            Text = HtmlToPlainText(html);
            return this;
        }

        public MailerSendEmailParameters WithFrom(string email, string name)
        {
            return WithFrom(new MailerSendEmailRecipient(email, name));
        }

        public MailerSendEmailParameters WithFrom(MailerSendEmailRecipient from)
        {
            From = from;
            return this;
        }

        public MailerSendEmailParameters WithTo(params string[] to)
        {
            if (to == null || to.Length == 0)
                return this;

            foreach (var email in to)
            {
                To.Add(new MailerSendEmailRecipient(email, ""));
            }
            return this;
        }

        public MailerSendEmailParameters WithTo(params MailerSendEmailRecipient[] to)
        {
            To = new List<MailerSendEmailRecipient>(to);
            return this;
        }

        public MailerSendEmailParameters WithVariable(string email, params MailerSendEmailVariableSubstitution[] substitutions)
        {
            if (!To.ToList().Any(r => string.Equals(r.Email, email, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("The email must be in the list of recipients (to)");

            Variables.Add(new MailerSendEmailVariable(email, substitutions));
            return this;
        }

        public MailerSendEmailParameters WithVariable(params MailerSendEmailVariable[] variable)
        {
            Variables = new List<MailerSendEmailVariable>();
            foreach (var item in variable)
            {
                if (!To.ToList().Any(r => string.Equals(r.Email, item.Email, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("The email must be in the list of recipients (to)");

                Variables.Add(item);
            }
            return this;
        }

        public MailerSendEmailParameters WithAttachment(string id, string filename, string content)
        {
            Attachments.Add(new MailerSendEmailAttachment(id, filename, content));
            return this;
        }

        public MailerSendEmailParameters WithAttachment(params MailerSendEmailAttachment[] attachments)
        {
            Attachments = new List<MailerSendEmailAttachment>(attachments);
            return this;
        }

        public MailerSendEmailParameters WithTags(params string[] tags)
        {
            Tags = new List<string>(tags);
            return this;
        }

        public MailerSendEmailParameters WithPersonalization(string email, object data)
        {
            if (!To.ToList().Any(r => string.Equals(r.Email, email, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("The email must be in the list of recipients (to)");

            Personalizations.Add(new MailerSendEmailPersonalization(email, data));
            return this;
        }

        public MailerSendEmailParameters WithPersonalization(params MailerSendEmailPersonalization[] personalizations)
        {
            Personalizations = new List<MailerSendEmailPersonalization>();
            foreach (var item in personalizations)
            {
                if (!To.ToList().Any(r => string.Equals(r.Email, item.Email, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("The email must be in the list of recipients (to)");

                Personalizations.Add(item);
            }
            return this;
        }

        private static string HtmlToPlainText(string html)
        {
            string block = "address|article|aside|blockquote|canvas|dd|div|dl|dt|" +
              "fieldset|figcaption|figure|footer|form|h\\d|header|hr|li|main|nav|" +
              "noscript|ol|output|p|pre|section|table|tfoot|ul|video";

            var plainText = Regex.Replace(html, $"(\\s*?</?({block})[^>]*?>)+\\s*", "\n", RegexOptions.IgnoreCase);

            // Replace br tag to newline.
            plainText = Regex.Replace(plainText, @"<(br)[^>]*>", "\n", RegexOptions.IgnoreCase);

            // (Optional) remove styles and scripts.
            plainText = Regex.Replace(plainText, @"<(script|style)[^>]*?>.*?</\1>", "", RegexOptions.Singleline);

            // Remove all tags.
            plainText = Regex.Replace(plainText, @"<[^>]*(>|$)", "", RegexOptions.Multiline);

            // Replace HTML entities.
            return WebUtility.HtmlDecode(plainText);
        }
    }
}
