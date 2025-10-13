using MailerSendNetCore.Common;
using MailerSendNetCore.Common.Extensions;
using MailerSendNetCore.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace MailserSend.UnitTests.Common.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMailerSend_WhenApiTokenNotSet_ThenThrowException()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddMailerSendEmailClient();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        Action action = () => { serviceProvider.GetService<IMailerSendEmailClient>(); };
        var ex = Assert.Throws<ArgumentException>(action);
        Assert.Equal("Missing apiToken", ex.Message);
    }

    [Fact]
    public void AddMailerSend_WhenApiUrlNotSet_ThenSetDefaultValue()
    {
        var serviceCollection = new ServiceCollection();

        var options = new MailerSendEmailClientOptions
        {
            ApiToken = "apiToken",
            ApiUrl = null
        };
        serviceCollection.AddMailerSendEmailClient(options);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var mailerSendEmailClient = serviceProvider.GetService<IMailerSendEmailClient>();

        var httpClient = mailerSendEmailClient.GetType()
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(mailerSendEmailClient) as HttpClient;

        Assert.NotNull(httpClient);
        Assert.Equal("https://api.mailersend.com/", httpClient.BaseAddress?.ToString());
    }

    [Fact]
    public void AddMailerSendEmailClient_When_IConfigurationParameterSet_Then_RegisterOptionsFromConfiguration()
    {
        var builder = new ConfigurationBuilder();

        builder.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("ApiToken", "api_token"),
            new KeyValuePair<string, string>("ApiUrl", "https://api.mailersend.com/v3"),
            new KeyValuePair<string, string>("UseRetryPolicy", "true"),
            new KeyValuePair<string, string>("RetryCount", "2"),
            new KeyValuePair<string, string>("RetryDelayInMilliseconds", "5000")
        });

        var configuration = builder.Build();

        var services = new ServiceCollection();

        services.AddMailerSendEmailClient(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var mailerSendEmailClient = serviceProvider.GetService<IMailerSendEmailClient>();
        Assert.NotNull(mailerSendEmailClient);

        var options = serviceProvider.GetService<IOptions<MailerSendEmailClientOptions>>()?.Value;
        Assert.NotNull(options);
        Assert.Equal("api_token", options.ApiToken);
        Assert.Equal("https://api.mailersend.com/v3", options.ApiUrl);
        Assert.True(options.UseRetryPolicy);
        Assert.Equal(2, options.RetryCount);
        Assert.Equal(5000, options.RetryDelayInMilliseconds);
    }

    [Fact]
    public void AddMailerSendEmailClient_When_ConfigureOptionsParameterSet_Then_RegisterOptionsFromConfigureOptionsAction()
    {
        var services = new ServiceCollection();

        services.AddMailerSendEmailClient(options =>
        {
            options.ApiToken = "api_token_1";
            options.ApiUrl = "https://api.mailersend_extra.com";
            options.UseRetryPolicy = false;
            options.RetryCount = 20;
            options.RetryDelayInMilliseconds = 50000;
        });

        var serviceProvider = services.BuildServiceProvider();

        var mailerSendEmailClient = serviceProvider.GetService<IMailerSendEmailClient>();
        Assert.NotNull(mailerSendEmailClient);

        var options = serviceProvider.GetService<IOptions<MailerSendEmailClientOptions>>()?.Value;
        Assert.NotNull(options);
        Assert.Equal("api_token_1", options.ApiToken);
        Assert.Equal("https://api.mailersend_extra.com", options.ApiUrl);
        Assert.False(options.UseRetryPolicy);
        Assert.Equal(20, options.RetryCount);
        Assert.Equal(50000, options.RetryDelayInMilliseconds);
    }

    [Fact]
    public void AddMailerSend_WhenUseRetryPolicyIsFalse_ThenDoNotConfigureRetryPolicy()
    {
        var serviceCollection = new ServiceCollection();

        var options = new MailerSendEmailClientOptions
        {
            ApiToken = "apiToken",
            ApiUrl = "https://api.mailersend.com/",
            UseRetryPolicy = true,
            RetryCount = 3
        };
        serviceCollection.AddMailerSendEmailClient(options);

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var mailerSendEmailClient = serviceProvider.GetService<IMailerSendEmailClient>();

        var httpClient = mailerSendEmailClient.GetType()
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(mailerSendEmailClient) as HttpClient;

        var handlerField = typeof(HttpMessageInvoker).GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance);
        var handlerValue = handlerField?.GetValue(httpClient);

        Assert.IsAssignableFrom<HttpMessageHandler>(handlerValue);

        var httpMessageHandler = handlerValue as HttpMessageHandler;

        var policyHandler = httpMessageHandler.GetInnerHandler<PolicyHttpMessageHandler>();

        Assert.NotNull(policyHandler);

        Assert.NotNull(httpClient);
        Assert.Equal("https://api.mailersend.com/", httpClient.BaseAddress?.ToString());
    }

    [Fact]
    public void AddMailerSendEmailClient_When_UseRetryIsSet_Then_SetRetryPolicy()
    {
        var getRetryPolicyMathodInfo = typeof(ServiceCollectionExtensions)
            .GetMethod("GetRetryPolicy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var retryPolicy = (IAsyncPolicy<HttpResponseMessage>)getRetryPolicyMathodInfo
            .Invoke(null, new object[] { 2, 5000, null });

        Assert.NotNull(retryPolicy);
    }



    [Theory]
    [InlineData(2, 5000, 2 * 5000)]
    [InlineData(4, 5000, 4 * 5000)]
    public async Task AddMailerSendEmailClient_When_RetryDelayIsSetAndRetryAfterHeaderIsSet_Then_TheAccumulatedDurationAndRetryCountAreAsExpected(int retryCount, int retryDelayInMilliseconds, int expectedDuration)
    {
        int realRetryCount = 0;
        double accumulatedDuration = 0;

        var getRetryPolicyMathodInfo = typeof(ServiceCollectionExtensions)
            .GetMethod("GetRetryPolicy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var onRetryCallback = new Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task>((result, timeSpan, retryCount, context) =>
        {
            realRetryCount++;
            accumulatedDuration += retryDelayInMilliseconds;
            return Task.CompletedTask;
        });

        var retryPolicy = (IAsyncPolicy<HttpResponseMessage>)getRetryPolicyMathodInfo
            .Invoke(null, new object[] { retryCount, retryDelayInMilliseconds, onRetryCallback });

        await retryPolicy.ExecuteAsync(() =>
        {
            var response = new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.TooManyRequests
            };
            response.Headers.Add("Retry-After", "1");
            return Task.FromResult(response);
        });

        Assert.Equal(retryCount, realRetryCount);
        Assert.Equal(expectedDuration, accumulatedDuration);
    }

    [Theory]
    [InlineData(2, 1000, 2 * 1000)]
    [InlineData(4, 500, 4 * 500)]
    public async Task AddMailerSendEmailClient_When_RetryDelayIsSetAndRetryAfterHeaderIsNotSet_Then_UseGivenDelayInMilliseconds(int retryCount, int delayInMilliseconds, int expectedDuration)
    {
        int realRetryCount = 0;
        double totalDelay = 0;

        var getRetryPolicyMathodInfo = typeof(ServiceCollectionExtensions)
            .GetMethod("GetRetryPolicy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var onRetryCallback = new Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task>((result, timeSpan, retryCount, context) =>
        {
            realRetryCount++;
            totalDelay += timeSpan.TotalMilliseconds;
            return Task.CompletedTask;
        });

        var retryPolicy = (IAsyncPolicy<HttpResponseMessage>)getRetryPolicyMathodInfo
            .Invoke(null, new object[] { retryCount, delayInMilliseconds, onRetryCallback });

        await retryPolicy.ExecuteAsync(() =>
        {
            var response = new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.TooManyRequests
            };
            return Task.FromResult(response);
        });

        Assert.Equal(retryCount, realRetryCount);
        Assert.Equal(expectedDuration, totalDelay);
    }

    // Note: default values of the parameterless AddMailerSendEmailClient()
    // are covered indirectly via functional tests elsewhere.

    [Fact]
    public void AddMailerSendEmailClient_When_UseRetryPolicyFalse_Then_NoPolicyHandler()
    {
        var services = new ServiceCollection();
        services.AddMailerSendEmailClient(options =>
        {
            options.ApiToken = "token";
            options.ApiUrl = "https://api.mailersend.com/";
            options.UseRetryPolicy = false;
        });

        var sp = services.BuildServiceProvider();
        var client = sp.GetService<IMailerSendEmailClient>();
        Assert.NotNull(client);

        var httpClient = client.GetType()
            .GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(client) as HttpClient;
        Assert.NotNull(httpClient);

        var handlerField = typeof(HttpMessageInvoker).GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance);
        var handlerValue = handlerField?.GetValue(httpClient) as HttpMessageHandler;
        var policyHandler = handlerValue.GetInnerHandler<PolicyHttpMessageHandler>();
        Assert.Null(policyHandler);
    }
}

