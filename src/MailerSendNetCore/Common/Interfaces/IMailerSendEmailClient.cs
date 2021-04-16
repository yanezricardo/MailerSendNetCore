using MailerSendNetCore.Emails.Dtos;
using System.Threading;
using System.Threading.Tasks;

namespace MailerSendNetCore.Common.Interfaces
{
    public interface IMailerSendEmailClient
    {
        Task<MailerSendEmailResponse> SendEmailAsync(MailerSendEmailParameters parameters);
        Task<MailerSendEmailResponse> SendEmailAsync(MailerSendEmailParameters parameters, CancellationToken cancellationToken);
    }
}
