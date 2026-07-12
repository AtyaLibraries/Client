using System.Net;
using System.Net.Http.Json;
using Atya.Http.Client;
using Microsoft.Extensions.Options;

using var httpClient = new HttpClient(new SampleHandler())
{
    BaseAddress = new Uri("https://api.example.test/"),
};

var client = new AtyaHttpClient(
    httpClient,
    Options.Create(new AtyaHttpClientOptions
    {
        CorrelationIdAccessor = static () => "sample-correlation-id",
    }));

using var request = new HttpRequestMessage(HttpMethod.Get, "customers/42");
var result = await client.SendJsonAsync<CustomerDto>(request);

Console.WriteLine(result.IsSuccess
    ? $"Loaded customer {result.Value.Name}."
    : $"Request failed: {result.Error.Code}");

internal sealed record CustomerDto(string Name);

internal sealed class SampleHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.TryGetValues("X-Correlation-ID", out var correlationValues);
        var correlationId = correlationValues?.SingleOrDefault() ?? "missing";

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new CustomerDto($"Ada ({correlationId})")),
        };

        return Task.FromResult(response);
    }
}
