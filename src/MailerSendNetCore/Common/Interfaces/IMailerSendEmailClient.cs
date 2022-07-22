using MailerSendNetCore.Emails.Dtos;

namespace MailerSendNetCore.Common.Interfaces
{
    public interface IMailerSendEmailClient
    {
        Task<MailerSendEmailResponse> SendEmailAsync(MailerSendEmailParameters parameters);
        Task<MailerSendEmailResponse> SendEmailAsync(MailerSendEmailParameters parameters, CancellationToken cancellationToken);

        Task<MailerSendBulkEmailResponse> SendBulkEmailAsync(MailerSendEmailParameters[] parameters);
        Task<MailerSendBulkEmailResponse> SendBulkEmailAsync(MailerSendEmailParameters[] parameters, CancellationToken cancellationToken);
        
        Task<MailerSendBulkEmailStatusResponse> GetBulkEmailStatusAsync(string bulkEmailId);
        Task<MailerSendBulkEmailStatusResponse> GetBulkEmailStatusAsync(string bulkEmailId, CancellationToken cancellationToken);
    }
}
