namespace Atya.Http.Client.UnitTests;

public sealed class AtyaHttpClientOptionsTests
{
    [Fact]
    public void CorrelationIdHeaderName_Should_Default_To_Atya_Web_Header()
    {
        var options = new AtyaHttpClientOptions();

        options.CorrelationIdHeaderName.Should().Be("X-Correlation-ID");
    }

    [Fact]
    public void CorrelationIdHeaderName_Should_Trim_Value()
    {
        var options = new AtyaHttpClientOptions
        {
            CorrelationIdHeaderName = " X-Request-ID ",
        };

        options.CorrelationIdHeaderName.Should().Be("X-Request-ID");
    }

    [Fact]
    public void CorrelationIdHeaderName_Should_Reject_Blank_Value()
    {
        var options = new AtyaHttpClientOptions();

        var act = () => options.CorrelationIdHeaderName = " ";

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CorrelationIdAccessor_Should_Reject_Null_Value()
    {
        var options = new AtyaHttpClientOptions();

        var act = () => options.CorrelationIdAccessor = null!;

        act.Should().Throw<ArgumentNullException>();
    }
}
