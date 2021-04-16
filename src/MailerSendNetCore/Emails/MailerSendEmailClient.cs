using MailerSendNetCore.Common.Exceptions;
using MailerSendNetCore.Common.Interfaces;
using MailerSendNetCore.Emails.Dtos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace MailerSendNetCore.Emails
{
    public class MailerSendEmailClient : IMailerSendEmailClient
    {
        protected struct ObjectResponseResult<T>
        {
            public ObjectResponseResult(T responseObject, string responseText)
            {
                this.Object = responseObject;
                this.Text = responseText;
            }
            public T Object { get; }
            public string Text { get; }
        }

        private readonly HttpClient _httpClient;
        private readonly string _apiToken;
        private Lazy<JsonSerializerSettings> _settings;

        public MailerSendEmailClient(HttpClient httpClient, string apiToken)
        {
            if (string.IsNullOrWhiteSpace(apiToken))
                throw new ArgumentNullException(nameof(apiToken));

            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiToken = apiToken;
            _settings = new Lazy<JsonSerializerSettings>(CreateSerializerSettings);
        }

        private JsonSerializerSettings CreateSerializerSettings()
        {
            var settings = new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
            };
            return settings;
        }

        private async Task<ObjectResponseResult<T>> ReadObjectResponseAsync<T>(HttpResponseMessage response, IReadOnlyDictionary<string, IEnumerable<string>> headers, bool readResponseAsString = false, CancellationToken cancellationToken = default)
        {
            if (response == null || response.Content == null)
            {
                return new ObjectResponseResult<T>(default, string.Empty);
            }

            if (readResponseAsString)
            {
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var typedBody = JsonConvert.DeserializeObject<T>(responseText, _settings.Value);
                    return new ObjectResponseResult<T>(typedBody, responseText);
                }
                catch (JsonException exception)
                {
                    var message = "Could not deserialize the response body string as " + typeof(T).FullName + ".";
                    throw new ApiException(message, (int)response.StatusCode, responseText, headers, exception);
                }
            }
            else
            {
                try
                {
                    using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                    using var streamReader = new System.IO.StreamReader(responseStream);
                    using var jsonTextReader = new JsonTextReader(streamReader);
                    var serializer = JsonSerializer.Create(_settings.Value);
                    var typedBody = serializer.Deserialize<T>(jsonTextReader);
                    return new ObjectResponseResult<T>(typedBody, string.Empty);
                }
                catch (JsonException exception)
                {
                    var message = "Could not deserialize the response body stream as " + typeof(T).FullName + ".";
                    throw new ApiException(message, (int)response.StatusCode, string.Empty, headers, exception);
                }
            }
        }

        public Task<MailerSendEmailResponse> SendEmailAsync(MailerSendEmailParameters parameters)
        {
            return SendEmailAsync(parameters, CancellationToken.None);
        }

        public async Task<MailerSendEmailResponse> SendEmailAsync(MailerSendEmailParameters parameters, CancellationToken cancellationToken)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            var resource = "v1/email";
            var json = JsonConvert.SerializeObject(parameters, _settings.Value);
            var content = new StringContent(json);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            using var request = new HttpRequestMessage(new HttpMethod("POST"), new Uri(resource, UriKind.RelativeOrAbsolute));
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
            request.Content = content;

            var client = _httpClient;
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            try
            {
                var headers = Enumerable.ToDictionary(response.Headers, h_ => h_.Key, h_ => h_.Value, StringComparer.OrdinalIgnoreCase);
                if (response.Content != null && response.Content.Headers != null)
                {
                    foreach (var item in response.Content.Headers)
                        headers[item.Key] = item.Value;
                }

                var xMesageId = headers.ContainsKey("X-Message-Id") ? headers["X-Message-Id"].FirstOrDefault() : "";
                var status = (int)response.StatusCode;
                if (status == 202)
                {
                    return new MailerSendEmailResponse { MessageId = xMesageId };
                }

                if (status == 422)
                {
                    var objectResponse = await ReadObjectResponseAsync<MailerSendEmailResponse>(response, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (objectResponse.Object == null)
                    {
                        throw new ApiException("Unexpected response.", status, objectResponse.Text, headers, null);
                    }
                    objectResponse.Object.MessageId = xMesageId;
                    return objectResponse.Object;
                }

                var responseData = response.Content == null ? null : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new ApiException("Unexpected response HTTP status code (" + status + ").", status, responseData, headers, null);
            }
            finally
            {
                response.Dispose();
            }
        }
    }
}
