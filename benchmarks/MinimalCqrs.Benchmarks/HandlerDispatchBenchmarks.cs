using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MinimalCqrs;

namespace MinimalCqrs.Benchmarks;

/// <summary>
/// Measures handler dispatch overhead across three scenarios:
///   - No HTTP context (Azure Functions / Console App / Timer triggers)
///   - With HTTP context (ASP.NET Core Minimal API)
///   - With a heavier dependency graph (closer to real-world handlers)
///
/// Run with: dotnet run -c Release
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class HandlerDispatchBenchmarks
{
    private IServiceProvider _appServiceProvider = null!;
    private IHttpContextAccessor _httpContextAccessor = null!;
    private IServiceScope _simulatedRequestScope = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Registrations used by the "heavier" benchmark variants
        services.AddScoped<ScopedDep>();
        services.AddTransient<TransientDep>();
        services.AddSingleton<SingletonDep>();

        services.AddMinimalCqrsFromAssemblyContaining<NoDepQuery>();

        _appServiceProvider = services.BuildServiceProvider();
        _httpContextAccessor = _appServiceProvider.GetRequiredService<IHttpContextAccessor>();

        // Simulate a long-lived ASP.NET Core request scope for the HTTP context benchmarks.
        // In a real app this would be created per HTTP request by the framework.
        _simulatedRequestScope = _appServiceProvider.CreateScope();
        _httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            RequestServices = _simulatedRequestScope.ServiceProvider
        };
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _httpContextAccessor.HttpContext = null;
        _simulatedRequestScope.Dispose();
        (_appServiceProvider as IDisposable)?.Dispose();
    }

    // -------------------------------------------------------------------------
    // No HTTP context — Azure Functions / Console App
    // Each invocation creates a scope from the root SP.
    // -------------------------------------------------------------------------

    [Benchmark(Baseline = true, Description = "No deps — no HTTP context")]
    public Task NoDeps_NoHttpContext()
    {
        _httpContextAccessor.HttpContext = null;
        return HandlerExtensions.ExecuteAsync(new NoDepQuery(), CancellationToken.None);
    }

    [Benchmark(Description = "Scoped dep — no HTTP context")]
    public Task ScopedDep_NoHttpContext()
    {
        _httpContextAccessor.HttpContext = null;
        return HandlerExtensions.ExecuteAsync(new ScopedDepQuery(), CancellationToken.None);
    }

    [Benchmark(Description = "Transient+IDisposable dep — no HTTP context")]
    public Task TransientDisposableDep_NoHttpContext()
    {
        _httpContextAccessor.HttpContext = null;
        return HandlerExtensions.ExecuteAsync(new TransientDepQuery(), CancellationToken.None);
    }

    [Benchmark(Description = "Mixed deps (Scoped+Transient+Singleton) — no HTTP context")]
    public Task MixedDeps_NoHttpContext()
    {
        _httpContextAccessor.HttpContext = null;
        return HandlerExtensions.ExecuteAsync(new MixedDepQuery(), CancellationToken.None);
    }

    // -------------------------------------------------------------------------
    // With HTTP context — ASP.NET Core Minimal API
    // Each invocation creates a child scope from HttpContext.RequestServices.
    // -------------------------------------------------------------------------

    [Benchmark(Description = "No deps — with HTTP context")]
    public Task NoDeps_WithHttpContext()
    {
        _httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            RequestServices = _simulatedRequestScope.ServiceProvider
        };
        return HandlerExtensions.ExecuteAsync(new NoDepHttpQuery(), CancellationToken.None);
    }

    [Benchmark(Description = "Scoped dep — with HTTP context")]
    public Task ScopedDep_WithHttpContext()
    {
        _httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            RequestServices = _simulatedRequestScope.ServiceProvider
        };
        return HandlerExtensions.ExecuteAsync(new ScopedDepHttpQuery(), CancellationToken.None);
    }

    [Benchmark(Description = "Mixed deps (Scoped+Transient+Singleton) — with HTTP context")]
    public Task MixedDeps_WithHttpContext()
    {
        _httpContextAccessor.HttpContext = new DefaultHttpContext
        {
            RequestServices = _simulatedRequestScope.ServiceProvider
        };
        return HandlerExtensions.ExecuteAsync(new MixedDepHttpQuery(), CancellationToken.None);
    }
}

// ---------------------------------------------------------------------------
// Minimal dependencies — isolates pure dispatch + scope creation overhead
// ---------------------------------------------------------------------------

public sealed record NoDepQuery : IQuery<bool>;
public sealed class NoDepHandler : MessageHandler<NoDepQuery, bool>
{
    public override Task<bool> ExecuteAsync(NoDepQuery query, CancellationToken ct = default)
        => Task.FromResult(true);
}

// ---------------------------------------------------------------------------
// Single Scoped dependency
// ---------------------------------------------------------------------------

public sealed class ScopedDep { }

public sealed record ScopedDepQuery : IQuery<bool>;
public sealed class ScopedDepHandler(ScopedDep _) : MessageHandler<ScopedDepQuery, bool>
{
    public override Task<bool> ExecuteAsync(ScopedDepQuery query, CancellationToken ct = default)
        => Task.FromResult(true);
}

// ---------------------------------------------------------------------------
// Single Transient + IDisposable dependency (the fiz-api leak scenario)
// ---------------------------------------------------------------------------

public sealed class TransientDep : IDisposable
{
    public void Dispose() { }
}

public sealed record TransientDepQuery : IQuery<bool>;
public sealed class TransientDepHandler(TransientDep _) : MessageHandler<TransientDepQuery, bool>
{
    public override Task<bool> ExecuteAsync(TransientDepQuery query, CancellationToken ct = default)
        => Task.FromResult(true);
}

// ---------------------------------------------------------------------------
// Mixed dependency graph — closer to a real-world handler
// ---------------------------------------------------------------------------

public sealed class SingletonDep { }

public sealed record MixedDepQuery : IQuery<bool>;
public sealed class MixedDepHandler(ScopedDep _s, TransientDep _t, SingletonDep _g) : MessageHandler<MixedDepQuery, bool>
{
    public override Task<bool> ExecuteAsync(MixedDepQuery query, CancellationToken ct = default)
        => Task.FromResult(true);
}

// ---------------------------------------------------------------------------
// HTTP context variants (separate query types to avoid registry collision)
// ---------------------------------------------------------------------------

public sealed record NoDepHttpQuery : IQuery<bool>;
public sealed class NoDepHttpHandler : MessageHandler<NoDepHttpQuery, bool>
{
    public override Task<bool> ExecuteAsync(NoDepHttpQuery query, CancellationToken ct = default)
        => Task.FromResult(true);
}

public sealed record ScopedDepHttpQuery : IQuery<bool>;
public sealed class ScopedDepHttpHandler(ScopedDep _) : MessageHandler<ScopedDepHttpQuery, bool>
{
    public override Task<bool> ExecuteAsync(ScopedDepHttpQuery query, CancellationToken ct = default)
        => Task.FromResult(true);
}

public sealed record MixedDepHttpQuery : IQuery<bool>;
public sealed class MixedDepHttpHandler(ScopedDep _s, TransientDep _t, SingletonDep _g) : MessageHandler<MixedDepHttpQuery, bool>
{
    public override Task<bool> ExecuteAsync(MixedDepHttpQuery query, CancellationToken ct = default)
        => Task.FromResult(true);
}
