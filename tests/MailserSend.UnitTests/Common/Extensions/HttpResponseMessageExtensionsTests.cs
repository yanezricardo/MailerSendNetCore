using MailerSendNetCore.Emails;
using MailerSendNetCore.Emails.Dtos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MailserSend.UnitTests.Common.Extensions;

public class HttpResponseMessageExtensionsTests
{
    [Fact]
    public async Task ReadObjectResponseAsync_When_NullContent_Returns_Default()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = null };
        var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
        var settings = new JsonSerializerSettings();

        var asm = typeof(MailerSendEmailClient).Assembly;
        var extType = asm.GetType("MailerSendNetCore.Common.Extensions.HttpResponseMessageExtensions");
        var method = extType.GetMethod("ReadObjectResponseAsync", BindingFlags.Public | BindingFlags.Static)
            .MakeGenericMethod(typeof(MailerSendEmailResponse));

        var task = (Task)method.Invoke(null, new object[] { response, headers, settings, CancellationToken.None });
        await task;

        var result = task.GetType().GetProperty("Result")!.GetValue(task);
        var obj = result.GetType().GetProperty("Object")!.GetValue(result);
        var text = (string)result.GetType().GetProperty("Text")!.GetValue(result);

        Assert.Null(obj);
        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public async Task ReadObjectResponseAsync_When_InvalidJson_Throws_ApiException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not json") };
        var headers = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
        var settings = new JsonSerializerSettings();

        var asm = typeof(MailerSendEmailClient).Assembly;
        var extType = asm.GetType("MailerSendNetCore.Common.Extensions.HttpResponseMessageExtensions");
        var method = extType.GetMethod("ReadObjectResponseAsync", BindingFlags.Public | BindingFlags.Static)
            .MakeGenericMethod(typeof(MailerSendEmailResponse));

        async Task Act()
        {
            var task = (Task)method.Invoke(null, new object[] { response, headers, settings, CancellationToken.None });
            await task;
        }

        var ex = await Assert.ThrowsAsync<MailerSendNetCore.Common.Exceptions.ApiException>(Act);
        Assert.Contains("Could not deserialize the response body string as", ex.Message);
        Assert.Equal((int)HttpStatusCode.OK, ex.StatusCode);
        Assert.Equal("not json", ex.Response);
    }

    [Fact]
    public void HeadersAndGetHeaderValueOrDefault_Merge_And_CaseInsensitive()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("") };
        response.Headers.TryAddWithoutValidation("X-Test", new[] { "A" });
        response.Content.Headers.Add("X-Test", "B");

        var asm = typeof(MailerSendEmailClient).Assembly;
        var extType = asm.GetType("MailerSendNetCore.Common.Extensions.HttpResponseMessageExtensions");

        var getHeaderMethod = extType.GetMethod("GetHeaderValueOrDefault", BindingFlags.Public | BindingFlags.Static);
        var value = (string)getHeaderMethod.Invoke(null, new object[] { response, "x-test" });

        // content header overrides and case-insensitive lookup
        Assert.Equal("B", value);
    }
}
