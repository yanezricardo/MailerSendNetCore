using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MailerSendNetCore.UnitTests.Mocks;

internal sealed class ScriptedHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<int, HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>
        _sendAsync;

    public ScriptedHttpMessageHandler(
        Func<int, HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
    {
        _sendAsync = sendAsync;
    }

    public int AttemptCount { get; private set; }

    public IList<byte[]> RequestBodies { get; } = new List<byte[]>();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        AttemptCount++;
        if (request.Content is not null)
        {
            RequestBodies.Add(
                await request.Content
                    .ReadAsByteArrayAsync(cancellationToken)
                    .ConfigureAwait(false));
        }

        return await _sendAsync(AttemptCount, request, cancellationToken)
            .ConfigureAwait(false);
    }
}
