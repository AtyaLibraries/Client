// <copyright file="AtyaHttpClient.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using System.Net.Http.Json;
using System.Text.Json;
using Atya.Foundation.Guards;
using Atya.Foundation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Atya.Http.Client;

/// <summary>
/// Sends HTTP requests and returns Atya result values instead of throwing for non-success HTTP responses.
/// </summary>
public sealed class AtyaHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly AtyaHttpClientOptions _options;
    private readonly ProblemDetailsErrorMapper _problemDetailsErrorMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtyaHttpClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to send requests.</param>
    /// <param name="options">Options that control result handling.</param>
    public AtyaHttpClient(HttpClient httpClient, IOptions<AtyaHttpClientOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _problemDetailsErrorMapper = new ProblemDetailsErrorMapper(_options.ProblemDetailsOptions);
    }

    /// <summary>
    /// Sends a request and returns success when the response has a successful status code.
    /// </summary>
    /// <param name="request">The request message to send.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A successful result for 2xx responses; otherwise a failure result.</returns>
    public async Task<Result> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(request);
        ApplyCorrelation(request);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        return response.IsSuccessStatusCode
            ? Result.Success()
            : Result.Failure(await CreateErrorAsync(response, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Sends a request and deserializes a successful JSON response body.
    /// </summary>
    /// <typeparam name="TValue">The expected response body type.</typeparam>
    /// <param name="request">The request message to send.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A typed success result for 2xx responses; otherwise a failure result.</returns>
    public async Task<Result<TValue>> SendJsonAsync<TValue>(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(request);
        ApplyCorrelation(request);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<TValue>(await CreateErrorAsync(response, cancellationToken).ConfigureAwait(false));
        }

        var value = await response.Content.ReadFromJsonAsync<TValue>(
            _options.JsonSerializerOptions,
            cancellationToken).ConfigureAwait(false);

        if (value is null)
        {
            return Result.Failure<TValue>(
                AtyaHttpClientErrors.EmptyJsonResponse(response.RequestMessage?.RequestUri));
        }

        return Result.Success(value);
    }

    private async Task<Error> CreateErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problemDetails = await ReadProblemDetailsAsync(response, cancellationToken).ConfigureAwait(false);
        return problemDetails is null
            ? AtyaHttpClientErrors.HttpFailure(response.StatusCode, response.ReasonPhrase, response.RequestMessage?.RequestUri)
            : _problemDetailsErrorMapper.Map(problemDetails, response.StatusCode, response.RequestMessage?.RequestUri);
    }

    private async Task<ProblemDetails?> ReadProblemDetailsAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength == 0)
        {
            return null;
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemDetails>(
                _options.JsonSerializerOptions,
                cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void ApplyCorrelation(HttpRequestMessage request)
    {
        if (request.Headers.Contains(_options.CorrelationIdHeaderName))
        {
            return;
        }

        var correlationId = _options.CorrelationIdAccessor();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.TryAddWithoutValidation(_options.CorrelationIdHeaderName, correlationId);
        }
    }
}
