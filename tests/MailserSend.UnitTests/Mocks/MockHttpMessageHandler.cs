using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MailerSendNetCore.UintTests.Mocks
{
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpContent _responseContent = null;
        private readonly HttpStatusCode _responseCode;
        private readonly IDictionary<string, string[]> _responseHeaders;

        public MockHttpMessageHandler(HttpStatusCode statusCode = HttpStatusCode.OK, object responseContent = null, IDictionary<string, string[]> responseHeaders = null)
        {
            _responseCode = statusCode;
            _responseHeaders = responseHeaders;
            if (responseContent != null)
            {
                _responseContent = new StringContent(JsonConvert.SerializeObject(responseContent));
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_responseCode) { Content = _responseContent };
            if (_responseHeaders != null && _responseHeaders.Count > 0)
            {
                foreach (var item in _responseHeaders)
                {
                    response.Headers.TryAddWithoutValidation(item.Key, item.Value);
                }
            }
            return Task.FromResult(response);
        }
    }
}
