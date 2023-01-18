using System;
using System.Net.Http;

namespace MailerSendNetCore.UnitTests.Mocks
{
    public class MockHttpClient : HttpClient
    {
        public MockHttpClient()
        {
            BaseAddress = new Uri("http://www.myserver.com");
        }

        public MockHttpClient(HttpMessageHandler httpMessageHandler) : base(httpMessageHandler)
        {
            BaseAddress = new Uri("http://www.myserver.com");
        }
    }
}
