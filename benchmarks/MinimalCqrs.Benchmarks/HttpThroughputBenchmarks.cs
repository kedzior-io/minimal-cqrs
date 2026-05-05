using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalCqrs;

namespace MinimalCqrs.Benchmarks;

/// <summary>
/// Compares MinimalCqrs handler dispatch overhead against vanilla ASP.NET Core Minimal API.
/// Uses an in-process TestServer so results reflect pure framework overhead, not network stack.
///
/// Answers the question: "what does MinimalCqrs cost on top of plain ASP.NET Core?"
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class HttpThroughputBenchmarks
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    private static readonly StringContent PostBody =
        new("""{"name":"benchmark-item"}""", Encoding.UTF8, "application/json");

    [GlobalSetup]
    public async Task Setup()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = Array.Empty<string>()
        });

        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        builder.Services.AddMinimalCqrsFromAssemblyContaining<HttpPingQuery>();

        _app = builder.Build();

        // MinimalCqrs mapped endpoints
        _app.MapGetHandler<HttpPingQuery, HttpPingResponse>("/cqrs/ping");
        _app.MapPostHandler<HttpCreateCommand, HttpCreateResponse>("/cqrs/create");

        // Vanilla Minimal API endpoints (baseline — same logic, no MinimalCqrs)
        _app.MapGet("/vanilla/ping", () => Results.Ok(new HttpPingResponse("pong")));
        _app.MapPost("/vanilla/create", (HttpCreateRequest req) =>
            Results.Ok(new HttpCreateResponse(Guid.NewGuid(), req.Name)));

        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    // -------------------------------------------------------------------------
    // GET — no request body, no dependencies
    // -------------------------------------------------------------------------

    [Benchmark(Baseline = true, Description = "Vanilla Minimal API — GET")]
    public Task Vanilla_Get() => _client.GetAsync("/vanilla/ping");

    [Benchmark(Description = "MinimalCqrs — GET")]
    public Task Cqrs_Get() => _client.GetAsync("/cqrs/ping");

    // -------------------------------------------------------------------------
    // POST — JSON body deserialization + response serialization
    // -------------------------------------------------------------------------

    [Benchmark(Description = "Vanilla Minimal API — POST")]
    public Task Vanilla_Post()
        => _client.PostAsync("/vanilla/create",
            new StringContent("""{"name":"benchmark-item"}""", Encoding.UTF8, "application/json"));

    [Benchmark(Description = "MinimalCqrs — POST")]
    public Task Cqrs_Post()
        => _client.PostAsync("/cqrs/create",
            new StringContent("""{"name":"benchmark-item"}""", Encoding.UTF8, "application/json"));
}

// ---------------------------------------------------------------------------
// MinimalCqrs handlers
// ---------------------------------------------------------------------------

public sealed record HttpPingQuery : IQuery<IHandlerResponse<HttpPingResponse>>;
public sealed record HttpPingResponse(string Message);

public sealed class HttpPingHandler : Handler<HttpPingQuery, HttpPingResponse>
{
    public override Task<IHandlerResponse<HttpPingResponse>> ExecuteAsync(
        HttpPingQuery query, CancellationToken ct = default)
        => Task.FromResult(Success(new HttpPingResponse("pong")));
}

public sealed record HttpCreateCommand(string Name) : ICommand<IHandlerResponse<HttpCreateResponse>>;
public sealed record HttpCreateResponse(Guid Id, string Name);

public sealed class HttpCreateHandler : Handler<HttpCreateCommand, HttpCreateResponse>
{
    public override Task<IHandlerResponse<HttpCreateResponse>> ExecuteAsync(
        HttpCreateCommand cmd, CancellationToken ct = default)
        => Task.FromResult(Success(new HttpCreateResponse(Guid.NewGuid(), cmd.Name)));
}

// ---------------------------------------------------------------------------
// Vanilla baseline types (no MinimalCqrs)
// ---------------------------------------------------------------------------

public sealed record HttpCreateRequest(string Name);
