using System;
using System.Net.Http;

namespace MailserSend.UnitTests.Common.Extensions;

public static class HttpMessageHandlerExtensions
{
    public static THandler GetInnerHandler<THandler>(this HttpMessageHandler httpMessageHandler) where THandler : HttpMessageHandler
    {
        if (httpMessageHandler == null)
        {
            throw new ArgumentNullException(nameof(httpMessageHandler));
        }

        if (httpMessageHandler is DelegatingHandler delegatingHandler)
        {
            var innerHandler = delegatingHandler.InnerHandler;
            if (innerHandler is THandler handler)
            {
                return handler;
            }
            else
            {
                return GetInnerHandler<THandler>(innerHandler!);
            }
        }

        return null;
    }
}
