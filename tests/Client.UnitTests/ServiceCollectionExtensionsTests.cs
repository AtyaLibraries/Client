using Microsoft.Extensions.DependencyInjection;

namespace Atya.Http.Client.UnitTests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAtyaHttpClient_Should_Register_Typed_Client_And_Options()
    {
        var services = new ServiceCollection();

        services.AddAtyaHttpClient(options => options.CorrelationIdHeaderName = "X-Request-ID");
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<AtyaHttpClient>();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AtyaHttpClientOptions>>();

        client.Should().NotBeNull();
        options.Value.CorrelationIdHeaderName.Should().Be("X-Request-ID");
    }

    [Fact]
    public void AddAtyaHttpClient_Should_Throw_When_Services_Is_Null()
    {
        var act = () => ServiceCollectionExtensions.AddAtyaHttpClient(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddAtyaHttpClient_Should_Register_With_Default_Options_When_Configure_Is_Null()
    {
        var services = new ServiceCollection();

        services.AddAtyaHttpClient();
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AtyaHttpClientOptions>>();

        options.Value.CorrelationIdHeaderName.Should().Be("X-Correlation-ID");
    }
}
