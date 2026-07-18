using MailerSendNetCore.Common;
using MailerSendNetCore.Common.Extensions;
using MailerSendNetCore.Common.Interfaces;
using MailerSendNetCore.Emails.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MailerSendNetCore.UnitTests.Common.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMailerSendEmailClient_DoesNotBuildAnIntermediateServiceProvider()
    {
        var configureInvocationCount = 0;
        var services = new ServiceCollection();

        services.AddMailerSendEmailClient(options =>
        {
            configureInvocationCount++;
            options.ApiToken = "api-token";
        });

        Assert.Equal(0, configureInvocationCount);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<MailerSendEmailClientOptions>>().Value;

        Assert.Equal(1, configureInvocationCount);
        Assert.Equal("api-token", options.ApiToken);
    }

    [Fact]
    public void AddMailerSendEmailClient_WithoutApiToken_ThrowsWhenClientIsResolved()
    {
        var services = new ServiceCollection();
        services.AddMailerSendEmailClient();

        using var serviceProvider = services.BuildServiceProvider();

        var exception = Assert.Throws<ArgumentException>(
            serviceProvider.GetRequiredService<IMailerSendEmailClient>);

        Assert.Equal("Missing apiToken", exception.Message);
    }

    [Fact]
    public void AddMailerSendEmailClient_OptionsInstance_CopiesEveryLegacyOption()
    {
        var services = new ServiceCollection();
        var source = new MailerSendEmailClientOptions
        {
            ApiToken = "api-token",
            ApiUrl = "https://example.test/v1",
            UseRetryPolicy = true,
            RetryCount = 4,
            RetryDelayInMilliseconds = 75
        };

        services.AddMailerSendEmailClient(source);

        using var serviceProvider = services.BuildServiceProvider();
        var configured = serviceProvider
            .GetRequiredService<IOptions<MailerSendEmailClientOptions>>()
            .Value;

        Assert.NotSame(source, configured);
        Assert.Equal(source.ApiToken, configured.ApiToken);
        Assert.Equal(source.ApiUrl, configured.ApiUrl);
        Assert.Equal(source.UseRetryPolicy, configured.UseRetryPolicy);
        Assert.Equal(source.RetryCount, configured.RetryCount);
        Assert.Equal(source.RetryDelayInMilliseconds, configured.RetryDelayInMilliseconds);
        Assert.NotNull(serviceProvider.GetRequiredService<IMailerSendEmailClient>());
    }

    [Fact]
    public void AddMailerSendEmailClient_Configuration_BindsEveryLegacyOption()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ApiToken"] = "configured-token",
                ["ApiUrl"] = "https://example.test/v2",
                ["UseRetryPolicy"] = "true",
                ["RetryCount"] = "3",
                ["RetryDelayInMilliseconds"] = "25"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddMailerSendEmailClient(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider
            .GetRequiredService<IOptions<MailerSendEmailClientOptions>>()
            .Value;

        Assert.Equal("configured-token", options.ApiToken);
        Assert.Equal("https://example.test/v2", options.ApiUrl);
        Assert.True(options.UseRetryPolicy);
        Assert.Equal(3, options.RetryCount);
        Assert.Equal(25, options.RetryDelayInMilliseconds);
        Assert.NotNull(serviceProvider.GetRequiredService<IMailerSendEmailClient>());
    }

    [Fact]
    public void AddMailerSendEmailClient_ConfigureAction_BindsEveryLegacyOption()
    {
        var services = new ServiceCollection();

        services.AddMailerSendEmailClient(options =>
        {
            options.ApiToken = "action-token";
            options.ApiUrl = "https://example.test/v3";
            options.UseRetryPolicy = false;
            options.RetryCount = 2;
            options.RetryDelayInMilliseconds = 10;
        });

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider
            .GetRequiredService<IOptions<MailerSendEmailClientOptions>>()
            .Value;

        Assert.Equal("action-token", options.ApiToken);
        Assert.Equal("https://example.test/v3", options.ApiUrl);
        Assert.False(options.UseRetryPolicy);
        Assert.Equal(2, options.RetryCount);
        Assert.Equal(10, options.RetryDelayInMilliseconds);
        Assert.NotNull(serviceProvider.GetRequiredService<IMailerSendEmailClient>());
    }

    [Fact]
    public void AddMailerSendEmailClient_OptionsWithoutApiUrl_UsesLegacyDefaultBaseAddress()
    {
        var services = new ServiceCollection();
        services.AddMailerSendEmailClient(new MailerSendEmailClientOptions
        {
            ApiToken = "api-token",
            ApiUrl = null
        });

        using var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IMailerSendEmailClient>();
        var httpClient = GetHttpClient(client);

        Assert.Equal("https://api.mailersend.com/", httpClient.BaseAddress?.ToString());
    }

    [Fact]
    public void PublicRegistrationOverloads_PreserveVersionZeroPointTwoSignatures()
    {
        var methods = typeof(ServiceCollectionExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == nameof(ServiceCollectionExtensions.AddMailerSendEmailClient))
            .ToArray();

        Assert.Equal(4, methods.Length);
        Assert.Contains(methods, method => HasParameters(method, typeof(IServiceCollection)));
        Assert.Contains(
            methods,
            method => HasParameters(
                method,
                typeof(IServiceCollection),
                typeof(IConfiguration)));
        Assert.Contains(
            methods,
            method => HasParameters(
                method,
                typeof(IServiceCollection),
                typeof(Action<MailerSendEmailClientOptions>),
                typeof(Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task>)));
        Assert.Contains(
            methods,
            method => HasParameters(
                method,
                typeof(IServiceCollection),
                typeof(MailerSendEmailClientOptions),
                typeof(Func<DelegateResult<HttpResponseMessage>, TimeSpan, int, Context, Task>)));

        var callbackOverloads = methods
            .Where(method => method.GetParameters().Length == 3)
            .ToArray();
        Assert.All(
            callbackOverloads,
            method =>
            {
                var callback = method.GetParameters()[2];
                Assert.True(callback.HasDefaultValue);
                Assert.Null(callback.DefaultValue);
            });
    }

    [Fact]
    public void PublicClientInterface_PreservesVersionZeroPointTwoOverloads()
    {
        var methods = typeof(IMailerSendEmailClient).GetMethods();

        Assert.Equal(6, methods.Length);
        Assert.Contains(
            methods,
            method => HasParameters(method, typeof(MailerSendEmailParameters)));
        Assert.Contains(
            methods,
            method => HasParameters(
                method,
                typeof(MailerSendEmailParameters),
                typeof(CancellationToken)));
        Assert.Contains(
            methods,
            method => HasParameters(method, typeof(MailerSendEmailParameters[])));
        Assert.Contains(
            methods,
            method => HasParameters(
                method,
                typeof(MailerSendEmailParameters[]),
                typeof(CancellationToken)));
        Assert.Contains(methods, method => HasParameters(method, typeof(string)));
        Assert.Contains(
            methods,
            method => HasParameters(method, typeof(string), typeof(CancellationToken)));
    }

    private static bool HasParameters(MethodInfo method, params Type[] parameterTypes)
    {
        return method.GetParameters()
            .Select(parameter => parameter.ParameterType)
            .SequenceEqual(parameterTypes);
    }

    private static HttpClient GetHttpClient(IMailerSendEmailClient client)
    {
        var field = client.GetType()
            .GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance);

        return Assert.IsType<HttpClient>(field?.GetValue(client));
    }
}
