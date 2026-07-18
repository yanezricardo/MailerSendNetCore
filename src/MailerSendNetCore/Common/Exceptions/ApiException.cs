using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace MailerSendNetCore.Common.Exceptions;

public partial class ApiException : Exception
{
    public int StatusCode { get; private set; }

    public string? Response { get; private set; }

    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; private set; }

    public HttpStatusCode HttpStatusCode { get; }

    public TimeSpan? RetryAfter { get; }

    public bool IsTransient { get; }

    public ApiException(
        string? message,
        int statusCode,
        string? response,
        IReadOnlyDictionary<string, IEnumerable<string>> headers,
        Exception? innerException)
        : base(
            message
            + "\n\nStatus: "
            + statusCode
            + "\nResponse: \n"
            + (response == null
                ? "(null)"
                : response.Substring(0, response.Length >= 512 ? 512 : response.Length)),
            innerException)
    {
        StatusCode = statusCode;
        Response = response;
        Headers = headers;
        HttpStatusCode = (HttpStatusCode)statusCode;
        RetryAfter = GetRetryAfter(headers);
        IsTransient = statusCode == (int)System.Net.HttpStatusCode.RequestTimeout
            || statusCode == (int)System.Net.HttpStatusCode.TooManyRequests
            || statusCode is >= 500 and <= 599;
    }

    public override string ToString()
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "HTTP Response: \n\n{0}\n\n{1}",
            Response,
            base.ToString());
    }

    private static TimeSpan? GetRetryAfter(
        IReadOnlyDictionary<string, IEnumerable<string>>? headers)
    {
        if (headers is null)
        {
            return null;
        }

        var retryAfterHeader = headers.FirstOrDefault(header =>
            string.Equals(header.Key, "Retry-After", StringComparison.OrdinalIgnoreCase));

        if (retryAfterHeader.Key is null)
        {
            return null;
        }

        var value = retryAfterHeader.Value.FirstOrDefault();
        if (!RetryConditionHeaderValue.TryParse(value, out var retryAfter))
        {
            return null;
        }

        return retryAfter.Delta
            ?? retryAfter.Date - DateTimeOffset.UtcNow;
    }
}
