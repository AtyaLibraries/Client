// <copyright file="ProblemDetailsErrorMapper.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using System.Net;
using Atya.Errors.ProblemDetails.Constants;
using Atya.Errors.ProblemDetails.Options;
using Atya.Foundation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Atya.Http.Client;

internal sealed class ProblemDetailsErrorMapper
{
    private readonly AtyaProblemDetailsOptions _options;

    public ProblemDetailsErrorMapper(AtyaProblemDetailsOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Error Map(ProblemDetails problemDetails, HttpStatusCode fallbackStatusCode, Uri? requestUri)
    {
        var code = ReadStringExtension(problemDetails, ProblemDetailsExtensionNames.ErrorCode);
        var statusCode = problemDetails.Status ?? (int)fallbackStatusCode;
        var kind = ResolveKind(statusCode);
        var message = string.IsNullOrWhiteSpace(problemDetails.Detail)
            ? problemDetails.Title ?? "The upstream service returned problem details."
            : problemDetails.Detail;

        return new Error(
            string.IsNullOrWhiteSpace(code) ? AtyaHttpClientErrors.HttpFailureCode : code,
            message,
            requestUri?.ToString(),
            kind: kind);
    }

    private static string? ReadStringExtension(ProblemDetails problemDetails, string key)
    {
        return problemDetails.Extensions.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    private ErrorKind ResolveKind(int statusCode)
    {
        var mapping = _options.ErrorMappings.FirstOrDefault(item => item.StatusCode == statusCode);
        if (mapping is not null)
        {
            return mapping.Kind;
        }

        return AtyaHttpClientErrors.MapStatusCode((HttpStatusCode)statusCode);
    }
}
