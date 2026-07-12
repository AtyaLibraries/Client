using System.Net;
using Atya.Foundation.Results;

namespace Atya.Http.Client.UnitTests;

public sealed class AtyaHttpClientErrorsTests
{
    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorKind.Validation)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorKind.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, ErrorKind.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, ErrorKind.NotFound)]
    [InlineData(HttpStatusCode.Conflict, ErrorKind.Conflict)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorKind.Unexpected)]
    [InlineData(HttpStatusCode.TooManyRequests, ErrorKind.Failure)]
    public void HttpFailure_Should_Classify_Status(HttpStatusCode statusCode, ErrorKind expectedKind)
    {
        var error = AtyaHttpClientErrors.HttpFailure(statusCode, requestUri: new Uri("https://example.test/"));

        error.Code.Should().Be(AtyaHttpClientErrors.HttpFailureCode);
        error.Kind.Should().Be(expectedKind);
        error.Target.Should().Be("https://example.test/");
    }

    [Fact]
    public void EmptyJsonResponse_Should_Create_Unexpected_Error()
    {
        var error = AtyaHttpClientErrors.EmptyJsonResponse(new Uri("https://example.test/value"));

        error.Code.Should().Be(AtyaHttpClientErrors.EmptyJsonResponseCode);
        error.Kind.Should().Be(ErrorKind.Unexpected);
        error.Target.Should().Be("https://example.test/value");
    }

    [Fact]
    public void HttpFailure_Should_Create_Message_When_ReasonPhrase_Is_Blank()
    {
        var error = AtyaHttpClientErrors.HttpFailure(HttpStatusCode.ServiceUnavailable, reasonPhrase: " ");

        error.Message.Should().Be("HTTP request failed with status code 503.");
        error.Target.Should().BeNull();
    }

    [Fact]
    public void EmptyJsonResponse_Should_Allow_Missing_RequestUri()
    {
        var error = AtyaHttpClientErrors.EmptyJsonResponse();

        error.Target.Should().BeNull();
    }
}
