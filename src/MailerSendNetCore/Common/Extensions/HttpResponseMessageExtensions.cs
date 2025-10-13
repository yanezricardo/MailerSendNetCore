using MailerSendNetCore.Common.Exceptions;
using Newtonsoft.Json;

namespace MailerSendNetCore.Common.Extensions;

internal static class HttpResponseMessageExtensions
{
    public static async Task<ObjectResponseResult<T>> ReadObjectResponseAsync<T>(this HttpResponseMessage response, IReadOnlyDictionary<string, IEnumerable<string>> headers, JsonSerializerSettings settings, CancellationToken cancellationToken = default)
    {
        if (response == null || response.Content == null)
        {
            return new ObjectResponseResult<T>(default!, string.Empty);
        }

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var typedBody = JsonConvert.DeserializeObject<T>(responseText, settings);
            return new ObjectResponseResult<T>(typedBody!, responseText);
        }
        catch (JsonException ex)
        {
            var message = "Could not deserialize the response body string as " + typeof(T).FullName + ".";
            throw new ApiException(message, (int)response.StatusCode, responseText, headers, ex);
        }
    }

    public static IReadOnlyDictionary<string, IEnumerable<string>> HeadersToDictionary(this HttpResponseMessage response)
    {
        var headers = Enumerable.ToDictionary(response.Headers, h => h.Key, h => h.Value, StringComparer.OrdinalIgnoreCase);
        if (response.Content != null && response.Content.Headers != null)
        {
            foreach (var item in response.Content.Headers)
                headers[item.Key] = item.Value;
        }

        return headers;
    }

    public static string? GetHeaderValueOrDefault(this HttpResponseMessage response, string headerName)
    {
        var headers = response.HeadersToDictionary();
        return headers.ContainsKey(headerName) ? headers[headerName].FirstOrDefault() : default;
    }
}
