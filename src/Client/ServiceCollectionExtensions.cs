// <copyright file="ServiceCollectionExtensions.cs" company="Atya">
// Copyright (c) Atya. All rights reserved.
// </copyright>

using Atya.Foundation.Guards;
using Microsoft.Extensions.DependencyInjection;

namespace Atya.Http.Client;

/// <summary>
/// Service collection extensions for Atya HTTP client helpers.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AtyaHttpClient"/> as a typed HTTP client.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configure">An optional callback for customizing client options.</param>
    /// <returns>A builder for configuring the underlying typed HTTP client.</returns>
    public static IHttpClientBuilder AddAtyaHttpClient(
        this IServiceCollection services,
        Action<AtyaHttpClientOptions>? configure = null)
    {
        Guard.AgainstNull(services);

        services.AddOptions<AtyaHttpClientOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services.AddHttpClient<AtyaHttpClient>();
    }
}
