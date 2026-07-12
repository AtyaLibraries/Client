<h1 align="center">Atya.Http.Client</h1>

<p align="center"><i>Typed HttpClient helpers that return Atya Result values, understand ProblemDetails failures, and propagate correlation identifiers.</i></p>

<p align="center">
  <a href="https://www.nuget.org/packages/Atya.Http.Client"><img src="https://img.shields.io/nuget/v/Atya.Http.Client?style=for-the-badge&logo=nuget&logoColor=white&label=NuGet&color=512BD4" alt="NuGet Version"></a>
  <a href="https://www.nuget.org/packages/Atya.Http.Client"><img src="https://img.shields.io/nuget/dt/Atya.Http.Client?style=for-the-badge&logo=nuget&logoColor=white&label=Downloads&color=512BD4" alt="NuGet Downloads"></a>
  <img src="https://img.shields.io/badge/.NET_10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="Target Framework">
  <a href="LICENSE"><img src="https://img.shields.io/github/license/AtyaLibraries/Client?style=for-the-badge&color=512BD4" alt="License"></a>
  <a href="https://github.com/AtyaLibraries/Client/actions"><img src="https://img.shields.io/github/actions/workflow/status/AtyaLibraries/Client/ci.yml?branch=development&style=for-the-badge&logo=githubactions&logoColor=white&label=Build" alt="Build"></a>
</p>

## Overview

`Atya.Http.Client` wraps `HttpClient` calls in `Result` and `Result<T>` so callers handle expected upstream failures without exception control flow. It reads RFC ProblemDetails responses, aligns status-to-error-kind behavior with `Atya.Errors.ProblemDetails`, and forwards correlation identifiers on outbound requests.

## Features

- Result-returning send methods for command-style and JSON response calls.
- ProblemDetails-aware failure mapping with Atya error codes and error kinds.
- Correlation propagation through the `X-Correlation-ID` header by default.
- Typed-client registration through `IServiceCollection`.

## Installation

```bash
dotnet add package Atya.Http.Client
```

```xml
<PackageReference Include="Atya.Http.Client" Version="<latest-stable>" />
```

## Quick Start

```csharp
using Atya.Http.Client;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddAtyaHttpClient(options =>
{
    options.CorrelationIdAccessor = () => "current-correlation-id";
});

using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<AtyaHttpClient>();

using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.test/customers/42");
var result = await client.SendJsonAsync<CustomerDto>(request);

if (result.IsSuccess)
{
    Console.WriteLine(result.Value.Name);
}
else
{
    Console.WriteLine(result.Error.Code);
}

public sealed record CustomerDto(string Name);
```

## Feature Tour

### Command-style calls

Use `SendAsync` when the response body is not part of the success contract:

```csharp
using var request = new HttpRequestMessage(HttpMethod.Delete, "https://api.example.test/customers/42");
var result = await client.SendAsync(request);
```

### JSON calls

Use `SendJsonAsync<TValue>` when a successful response contains JSON:

```csharp
using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.test/customers/42");
var result = await client.SendJsonAsync<CustomerDto>(request);
```

### Correlation

`AtyaHttpClientOptions.CorrelationIdAccessor` provides the current correlation id. The client forwards it through `X-Correlation-ID` unless the request already has that header.

```csharp
services.AddAtyaHttpClient(options =>
{
    options.CorrelationIdAccessor = () => Activity.Current?.TraceId.ToString();
});
```

## Error Codes

| Code | Meaning |
|---|---|
| `atya.http.client.http_failure` | The upstream response was not successful and did not provide a specific Atya error code in ProblemDetails. |
| `atya.http.client.empty_json_response` | The upstream response succeeded but the JSON body deserialized to `null`. |

When an upstream ProblemDetails payload contains the `errorCode` extension from `Atya.Errors.ProblemDetails`, that code is returned instead of `atya.http.client.http_failure`.

## Why These Dependencies

`Atya.Foundation.Results` provides the `Result` and `Error` model. `Atya.Errors.ProblemDetails` owns the ProblemDetails extension names and error-kind mapping options consumed when interpreting upstream HTTP failures. `Microsoft.Extensions.Http` provides the standard typed-client registration surface.

## Compatibility

Targets `net10.0`.

## Testing

```bash
dotnet test
```

## Benchmarks

```bash
dotnet run -c Release --project benchmarks/Client.Benchmarks/Client.Benchmarks.csproj -- --list flat
```

## Links

- Repository: https://github.com/AtyaLibraries/Client
- NuGet: https://www.nuget.org/packages/Atya.Http.Client
- License: https://github.com/AtyaLibraries/Client/blob/development/LICENSE

## License

Released under the MIT license. See [LICENSE](LICENSE) for details.
