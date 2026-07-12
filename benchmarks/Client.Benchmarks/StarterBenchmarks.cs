using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Options;

namespace Atya.Http.Client.Benchmarks;

/// <summary>
/// Benchmarks for HTTP result handling.
/// </summary>
[MemoryDiagnoser]
public class StarterBenchmarks : IDisposable
{
    private AtyaHttpClient _client = null!;
    private Uri _requestUri = null!;

    /// <summary>
    /// Creates reusable benchmark fixtures.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _requestUri = new Uri("https://example.test/ping");
        _client = new AtyaHttpClient(
            new HttpClient(new BenchmarkHandler()),
            Options.Create(new AtyaHttpClientOptions()));
    }

    /// <summary>
    /// Sends a successful request through the result-returning client.
    /// </summary>
    /// <returns>The success flag.</returns>
    [Benchmark]
    public async Task<bool> SendAsyncSuccess()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _requestUri);
        var result = await _client.SendAsync(request).ConfigureAwait(false);
        return result.IsSuccess;
    }

    /// <summary>
    /// Releases benchmark resources.
    /// </summary>
    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class BenchmarkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NoContent));
        }
    }
}
