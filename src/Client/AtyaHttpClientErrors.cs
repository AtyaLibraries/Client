// <copyright file="AtyaHttpClientErrors.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using System.Net;
using Atya.Foundation.Results;

namespace Atya.Http.Client;

/// <summary>
/// Factory methods for errors returned by <see cref="AtyaHttpClient"/>.
/// </summary>
public static class AtyaHttpClientErrors
{
    /// <summary>
    /// The error code returned when a non-success response has no usable problem details payload.
    /// </summary>
    public const string HttpFailureCode = "atya.http.client.http_failure";

    /// <summary>
    /// The error code returned when a successful JSON response has an empty body.
    /// </summary>
    public const string EmptyJsonResponseCode = "atya.http.client.empty_json_response";

    /// <summary>
    /// Creates an error for a non-success HTTP response.
    /// </summary>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="reasonPhrase">The optional response reason phrase.</param>
    /// <param name="requestUri">The optional request URI.</param>
    /// <returns>An error classified from the HTTP status code.</returns>
    public static Error HttpFailure(HttpStatusCode statusCode, string? reasonPhrase = null, Uri? requestUri = null)
    {
        var status = (int)statusCode;
        var message = string.IsNullOrWhiteSpace(reasonPhrase)
            ? $"HTTP request failed with status code {status}."
            : $"HTTP request failed with status code {status}: {reasonPhrase}.";

        return new Error(
            HttpFailureCode,
            message,
            requestUri?.ToString(),
            kind: MapStatusCode(statusCode));
    }

    /// <summary>
    /// Creates an error for a successful JSON response with no deserialized value.
    /// </summary>
    /// <param name="requestUri">The optional request URI.</param>
    /// <returns>An unexpected error describing the empty response body.</returns>
    public static Error EmptyJsonResponse(Uri? requestUri = null)
    {
        return new Error(
            EmptyJsonResponseCode,
            "HTTP response succeeded but did not contain a JSON value.",
            requestUri?.ToString(),
            kind: ErrorKind.Unexpected);
    }

    internal static ErrorKind MapStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => ErrorKind.Validation,
            HttpStatusCode.Unauthorized => ErrorKind.Unauthorized,
            HttpStatusCode.Forbidden => ErrorKind.Forbidden,
            HttpStatusCode.NotFound => ErrorKind.NotFound,
            HttpStatusCode.Conflict => ErrorKind.Conflict,
            >= HttpStatusCode.InternalServerError => ErrorKind.Unexpected,
            _ => ErrorKind.Failure,
        };
    }
}
