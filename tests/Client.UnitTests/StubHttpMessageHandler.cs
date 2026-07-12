using System.Net;

namespace Atya.Http.Client.UnitTests;

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public StubHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        _response.RequestMessage = request;
        return Task.FromResult(_response);
    }
}
