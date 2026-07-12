// <copyright file="AtyaHttpClientOptions.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using System.Text.Json;
using Atya.Errors.ProblemDetails.Options;

namespace Atya.Http.Client;

/// <summary>
/// Options for Atya HTTP client result handling.
/// </summary>
public sealed class AtyaHttpClientOptions
{
    private string _correlationIdHeaderName = "X-Correlation-ID";
    private Func<string?> _correlationIdAccessor = static () => null;

    /// <summary>
    /// Gets or sets the header name used to propagate correlation identifiers.
    /// </summary>
    /// <exception cref="ArgumentException">The value is empty.</exception>
    public string CorrelationIdHeaderName
    {
        get => _correlationIdHeaderName;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _correlationIdHeaderName = value.Trim();
        }
    }

    /// <summary>
    /// Gets or sets the accessor used to read the current correlation identifier.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public Func<string?> CorrelationIdAccessor
    {
        get => _correlationIdAccessor;
        set => _correlationIdAccessor = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets JSON serializer options used for success bodies and problem details payloads.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; } = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Gets the problem details options used when interpreting upstream problem details payloads.
    /// </summary>
    public AtyaProblemDetailsOptions ProblemDetailsOptions { get; } = new();
}
