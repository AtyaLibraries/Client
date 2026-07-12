// <copyright file="ProblemDetailsErrorMapper.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using System.Net;
using System.Text.Json;
using Atya.Errors.ProblemDetails.Constants;
using Atya.Errors.ProblemDetails.Models;
using Atya.Errors.ProblemDetails.Options;
using Atya.Foundation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Atya.Http.Client;

internal sealed class ProblemDetailsErrorMapper
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly AtyaProblemDetailsOptions _options;

    public ProblemDetailsErrorMapper(AtyaProblemDetailsOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Error Map(ProblemDetails problemDetails, HttpStatusCode fallbackStatusCode, Uri? requestUri)
    {
        var code = problemDetails.Extensions.TryGetValue(ProblemDetailsExtensionNames.ErrorCode, out var codeValue)
            ? codeValue?.ToString()
            : null;
        var statusCode = problemDetails.Status ?? (int)fallbackStatusCode;
        var kind = ResolveKind(statusCode);
        var message = string.IsNullOrWhiteSpace(problemDetails.Detail)
            ? problemDetails.Title ?? "The upstream service returned problem details."
            : problemDetails.Detail;

        var errorCode = string.IsNullOrWhiteSpace(code) ? AtyaHttpClientErrors.HttpFailureCode : code;
        var details = CreateDetails(problemDetails, errorCode);

        return new Error(
            errorCode,
            message,
            requestUri?.ToString(),
            details,
            kind: kind);

        static Error[] CreateDetails(ProblemDetails problemDetails, string fallbackCode)
        {
            if (!problemDetails.Extensions.TryGetValue(ProblemDetailsExtensionNames.Errors, out var value))
            {
                return Array.Empty<Error>();
            }

            var validationErrors = ReadValidationErrors(value);
            if (validationErrors.Count == 0)
            {
                return Array.Empty<Error>();
            }

            var details = new List<Error>();
            foreach (var pair in validationErrors)
            {
                foreach (var item in pair.Value)
                {
                    var target = string.IsNullOrWhiteSpace(item.PropertyName) ? pair.Key : item.PropertyName;
                    var code = string.IsNullOrWhiteSpace(item.ErrorCode) ? fallbackCode : item.ErrorCode;

                    details.Add(new Error(
                        code,
                        item.Message,
                        target,
                        kind: ErrorKind.Validation));
                }
            }

            return details.ToArray();
        }

        static IReadOnlyDictionary<string, ValidationProblemError[]> ReadValidationErrors(object? value)
        {
            if (value is JsonElement element)
            {
                return element.ValueKind == JsonValueKind.Object
                    ? JsonSerializer.Deserialize<Dictionary<string, ValidationProblemError[]>>(
                        element.GetRawText(),
                        s_jsonSerializerOptions) ?? new Dictionary<string, ValidationProblemError[]>()
                    : new Dictionary<string, ValidationProblemError[]>();
            }

            if (value is IReadOnlyDictionary<string, ValidationProblemError[]> typed)
            {
                return typed;
            }

            return new Dictionary<string, ValidationProblemError[]>();
        }
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
