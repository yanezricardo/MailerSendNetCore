using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MailerSendNetCore.UnitTests.Mocks;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpContent _responseContent = null;
    private readonly HttpStatusCode _responseCode;
    private readonly IDictionary<string, string[]> _responseHeaders;
    private string _requestContent;

    public MockHttpMessageHandler(HttpStatusCode statusCode = HttpStatusCode.OK, object responseContent = null, IDictionary<string, string[]> responseHeaders = null)
    {
        _responseCode = statusCode;
        _responseHeaders = responseHeaders;
        if (responseContent != null)
        {
            _responseContent = new StringContent(JsonConvert.SerializeObject(responseContent));
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request?.Content is not null)
        {
            _requestContent = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        var response = new HttpResponseMessage(_responseCode) { Content = _responseContent };
        if (_responseHeaders != null && _responseHeaders.Count > 0)
        {
            foreach (var item in _responseHeaders)
            {
                response.Headers.TryAddWithoutValidation(item.Key, item.Value);
            }
        }
        return response;
    }

    internal T GetLastRequestContent<T>()
    {
        if (_requestContent is null)
        {
            return default;
        }

        return JsonConvert.DeserializeObject<T>(_requestContent);
    }
}
