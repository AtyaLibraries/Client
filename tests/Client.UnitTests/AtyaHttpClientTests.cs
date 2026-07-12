using System.Net;
using System.Net.Http.Json;
using Atya.Errors.ProblemDetails.Constants;
using Atya.Foundation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Atya.Http.Client.UnitTests;

public sealed class AtyaHttpClientTests
{
    [Fact]
    public async Task SendAsync_Should_Return_Success_For_Success_Status()
    {
        using var client = CreateClient(new HttpResponseMessage(HttpStatusCode.NoContent));
        using var request = new HttpRequestMessage(HttpMethod.Delete, "https://example.test/items/1");

        var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_Should_Return_Failure_For_NonSuccess_Status_Without_ProblemDetails()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            ReasonPhrase = "Missing",
        };
        using var client = CreateClient(response);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/items/42");

        var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(AtyaHttpClientErrors.HttpFailureCode);
        result.Error.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Target.Should().Be("https://example.test/items/42");
        result.Error.Message.Should().Contain("404");
    }

    [Fact]
    public async Task SendAsync_Should_Map_ProblemDetails_Error_Code_And_Status()
    {
        var problemDetails = new ProblemDetails
        {
            Status = 409,
            Title = "Conflict",
            Detail = "The item was changed.",
        };
        problemDetails.Extensions[ProblemDetailsExtensionNames.ErrorCode] = "atya.test.changed";
        using var response = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(problemDetails),
        };
        using var client = CreateClient(response);
        using var request = new HttpRequestMessage(HttpMethod.Put, "https://example.test/items/42");

        var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("atya.test.changed");
        result.Error.Kind.Should().Be(ErrorKind.Conflict);
        result.Error.Message.Should().Be("The item was changed.");
    }

    [Fact]
    public async Task SendAsync_Should_Use_ProblemDetails_Title_When_Detail_Is_Missing()
    {
        var problemDetails = new ProblemDetails
        {
            Status = 403,
            Title = "Forbidden",
        };
        using var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = JsonContent.Create(problemDetails),
        };
        using var client = CreateClient(response);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/reports");

        var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Forbidden);
        result.Error.Message.Should().Be("Forbidden");
    }

    [Fact]
    public async Task SendAsync_Should_Use_Default_ProblemDetails_Message_When_Title_And_Detail_Are_Missing()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = JsonContent.Create(new ProblemDetails { Status = 422 }),
        };
        using var client = CreateClient(response);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://example.test/items");

        var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(AtyaHttpClientErrors.HttpFailureCode);
        result.Error.Kind.Should().Be(ErrorKind.Failure);
        result.Error.Message.Should().Be("The upstream service returned problem details.");
    }

    [Fact]
    public async Task SendAsync_Should_Fallback_When_Response_Body_Is_Not_ProblemDetails_Json()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("not-json"),
        };
        using var client = CreateClient(response);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/failure");

        var result = await client.SendAsync(request, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(AtyaHttpClientErrors.HttpFailureCode);
        result.Error.Kind.Should().Be(ErrorKind.Unexpected);
    }

    [Fact]
    public async Task SendAsync_Should_Propagate_Correlation_Id_When_Request_Does_Not_Contain_Header()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        using var handler = new StubHttpMessageHandler(response);
        using var httpClient = new HttpClient(handler);
        var options = new AtyaHttpClientOptions
        {
            CorrelationIdAccessor = static () => "correlation-123",
        };
        var client = new AtyaHttpClient(httpClient, Options.Create(options));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        handler.LastRequest!.Headers.GetValues("X-Correlation-ID").Should().ContainSingle("correlation-123");
    }

    [Fact]
    public async Task SendAsync_Should_Not_Add_Correlation_Header_When_Accessor_Returns_Blank()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        using var handler = new StubHttpMessageHandler(response);
        using var httpClient = new HttpClient(handler);
        var options = new AtyaHttpClientOptions
        {
            CorrelationIdAccessor = static () => " ",
        };
        using var client = new AtyaHttpClient(httpClient, Options.Create(options));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        handler.LastRequest!.Headers.Contains("X-Correlation-ID").Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_Should_Not_Replace_Existing_Correlation_Header()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        using var handler = new StubHttpMessageHandler(response);
        using var httpClient = new HttpClient(handler);
        var options = new AtyaHttpClientOptions
        {
            CorrelationIdAccessor = static () => "new-id",
        };
        var client = new AtyaHttpClient(httpClient, Options.Create(options));
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/");
        request.Headers.Add("X-Correlation-ID", "existing-id");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        handler.LastRequest!.Headers.GetValues("X-Correlation-ID").Should().ContainSingle("existing-id");
    }

    [Fact]
    public async Task SendJsonAsync_Should_Return_Deserialized_Value_For_Success_Status()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new SampleDto("Ada")),
        };
        using var client = CreateClient(response);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/user");

        var result = await client.SendJsonAsync<SampleDto>(request, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Ada");
    }

    [Fact]
    public async Task SendJsonAsync_Should_Return_Failure_When_Success_Body_Is_Empty()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null"),
        };
        using var client = CreateClient(response);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/user");

        var result = await client.SendJsonAsync<SampleDto>(request, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(AtyaHttpClientErrors.EmptyJsonResponseCode);
        result.Error.Kind.Should().Be(ErrorKind.Unexpected);
    }

    [Fact]
    public async Task SendJsonAsync_Should_Return_Failure_For_ProblemDetails_Status()
    {
        var problemDetails = new ProblemDetails
        {
            Status = 401,
            Title = "Unauthorized",
            Detail = "Token expired.",
        };
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = JsonContent.Create(problemDetails),
        };
        using var client = CreateClient(response);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/user");

        var result = await client.SendJsonAsync<SampleDto>(request, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Error.Kind.Should().Be(ErrorKind.Unauthorized);
        result.Error.Message.Should().Be("Token expired.");
    }

    [Fact]
    public async Task SendAsync_Should_Throw_When_Request_Is_Null()
    {
        using var client = CreateClient(new HttpResponseMessage(HttpStatusCode.OK));

        var act = async () => await client.SendAsync(null!, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendJsonAsync_Should_Throw_When_Request_Is_Null()
    {
        using var client = CreateClient(new HttpResponseMessage(HttpStatusCode.OK));

        var act = async () => await client.SendJsonAsync<SampleDto>(null!, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_Throw_When_HttpClient_Is_Null()
    {
        var act = () => new AtyaHttpClient(null!, Options.Create(new AtyaHttpClientOptions()));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_Should_Throw_When_Options_Is_Null()
    {
        var act = () => new AtyaHttpClient(new HttpClient(new StubHttpMessageHandler(new HttpResponseMessage())), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static AtyaHttpClient CreateClient(HttpResponseMessage response)
    {
        return CreateClient(response, new AtyaHttpClientOptions());
    }

    private static AtyaHttpClient CreateClient(HttpResponseMessage response, AtyaHttpClientOptions options)
    {
        var handler = new StubHttpMessageHandler(response);
        var httpClient = new HttpClient(handler);
        return new AtyaHttpClient(httpClient, Options.Create(options));
    }

    private sealed record SampleDto(string Name);
}
