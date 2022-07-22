using MailerSendNetCore.Common;
using MailerSendNetCore.Common.Exceptions;
using MailerSendNetCore.Common.Extensions;
using MailerSendNetCore.Common.Interfaces;
using MailerSendNetCore.Emails.Dtos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace MailerSendNetCore.Emails
{
    public class MailerSendEmailClient : IMailerSendEmailClient
    {
        private readonly HttpClient _httpClient;
        private readonly MailerSendEmailClientOptions _options;
        private readonly JsonSerializerSettings _serializerSettings = default!;

        public MailerSendEmailClient(HttpClient httpClient, IOptions<MailerSendEmailClientOptions> options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(_options.ApiToken))
                throw new ArgumentException("Missing apiToken");

            _httpClient.BaseAddress = new Uri(_options.ApiUrl ?? "https://api.mailersend.com");

            _serializerSettings = new JsonSerializerSettings();
        }

        public Task<MailerSendEmailResponse> SendEmailAsync(MailerSendEmailParameters parameters)
        {
            return SendEmailAsync(parameters, CancellationToken.None);
        }

        public async Task<MailerSendEmailResponse> SendEmailAsync(MailerSendEmailParameters parameters, CancellationToken cancellationToken = default)
        {
            using var response = await PostAsync("v1/email", parameters, cancellationToken);

            var headers = response.HeadersToDictionary();
            var xMesageId = response.GetHeaderValueOrDefault("X-Message-Id");

            var status = (int)response.StatusCode;
            if (status == 202)
            {
                return new MailerSendEmailResponse { MessageId = xMesageId };
            }

            if (status == 422)
            {
                var objectResponse = await response.ReadObjectResponseAsync<MailerSendEmailResponse>(headers, _serializerSettings!, cancellationToken: cancellationToken);
                if (objectResponse.Object == null)
                {
                    throw new ApiException("Unexpected response.", status, objectResponse.Text, headers, null);
                }
                objectResponse.Object.MessageId = xMesageId;
                return objectResponse.Object;
            }

            var responseData = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApiException("Unexpected response HTTP status code (" + status + ").", status, responseData, headers, null);
        }


        public Task<MailerSendBulkEmailResponse> SendBulkEmailAsync(MailerSendEmailParameters[] parameters)
        {
            return SendBulkEmailAsync(parameters, CancellationToken.None);
        }

        public async Task<MailerSendBulkEmailResponse> SendBulkEmailAsync(MailerSendEmailParameters[] parameters, CancellationToken cancellationToken)
        {
            using var response = await PostAsync("v1/bulk-email", parameters, cancellationToken);

            var headers = response.HeadersToDictionary();

            var status = (int)response.StatusCode;
            if (status == 202)
            {
                var objectResponse = await response.ReadObjectResponseAsync<MailerSendBulkEmailResponse>(headers, _serializerSettings!, cancellationToken: cancellationToken);
                if (objectResponse.Object == null)
                {
                    throw new ApiException("Unexpected response.", status, objectResponse.Text, headers, null);
                }
                return objectResponse.Object;
            }

            var responseData = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApiException("Unexpected response HTTP status code (" + status + ").", status, responseData, headers, null);
        }


        public Task<MailerSendBulkEmailStatusResponse> GetBulkEmailStatusAsync(string bulkEmailId)
        {
            return GetBulkEmailStatusAsync(bulkEmailId, CancellationToken.None);
        }

        public async Task<MailerSendBulkEmailStatusResponse> GetBulkEmailStatusAsync(string bulkEmailId, CancellationToken cancellationToken)
        {
            using var response = await GetAsync($"v1/bulk-email/{bulkEmailId}", cancellationToken);

            var headers = response.HeadersToDictionary();

            var status = (int)response.StatusCode;
            if (status == 200)
            {
                var objectResponse = await response.ReadObjectResponseAsync<MailerSendBulkEmailStatusResponse>(headers, _serializerSettings!, cancellationToken: cancellationToken);
                if (objectResponse.Object == null)
                {
                    throw new ApiException("Unexpected response.", status, objectResponse.Text, headers, null);
                }
                return objectResponse.Object;
            }

            if (status == 404)
            {
                throw new ApiException($"Not Found ({bulkEmailId})", status, string.Empty, headers, null);
            }

            var responseData = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApiException("Unexpected response HTTP status code (" + status + ").", status, responseData, headers, null);
        }


        private async Task<HttpResponseMessage> PostAsync<TData>(string resource, TData data, CancellationToken cancellationToken = default)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            var content = new StringContent(JsonConvert.SerializeObject(data, _serializerSettings));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            using var request = new HttpRequestMessage(new HttpMethod("POST"), new Uri(resource, UriKind.RelativeOrAbsolute));
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);
            request.Content = content;

            return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> GetAsync(string resource, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(new HttpMethod("GET"), new Uri(resource, UriKind.RelativeOrAbsolute));
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);

            return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }
    }
}
