using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MinimalCqrs.Tests;

/// <summary>
/// Verifies handler execution behaviour in an ASP.NET Core Minimal API context
/// (IHttpContextAccessor.HttpContext is set with a per-request RequestServices scope).
///
/// In the ASP.NET Core path, CreateScope() returns a non-disposing wrapper around
/// HttpContext.RequestServices rather than allocating a child scope. This avoids the
/// per-invocation scope creation overhead while still correctly cleaning up:
///   - Transient+IDisposable services are tracked by the REQUEST scope
///   - They are disposed when the request scope is disposed (request end), not per-invocation
///
/// For Azure Functions / Console App (no HTTP context) see ScopeLeakTests.cs, where
/// a real scope IS created and disposed after each invocation.
/// </summary>
public class MinimalApiScopeTests
{
    [Fact]
    public async Task Transient_IDisposable_dependency_is_disposed_when_request_scope_ends()
    {
        MinimalApiTrackableDisposable.Reset();

        var services = new ServiceCollection();
        services.AddTransient<MinimalApiTrackableDisposable>();
        services.AddMinimalCqrsFromAssemblyContaining<MinimalApiTestHandlers.Query>();

        var appSp = services.BuildServiceProvider();
        var httpContextAccessor = appSp.GetRequiredService<IHttpContextAccessor>();

        const int invocations = 5;

        var requestScope = appSp.CreateScope();

        try
        {
            httpContextAccessor.HttpContext = new DefaultHttpContext
            {
                RequestServices = requestScope.ServiceProvider
            };

            for (var i = 0; i < invocations; i++)
            {
                await HandlerExtensions.ExecuteAsync(new MinimalApiTestHandlers.Query(), CancellationToken.None);
            }

            Assert.Equal(invocations, MinimalApiTrackableDisposable.CreatedCount);

            // In the ASP.NET Core path, CreateScope() wraps the request scope without creating
            // a child scope. Disposables are held by the request scope — not yet disposed here.
            Assert.Equal(0, MinimalApiTrackableDisposable.DisposedCount);
        }
        finally
        {
            httpContextAccessor.HttpContext = null;
            requestScope.Dispose(); // simulates end of HTTP request
        }

        // Request scope ended — all tracked disposables are now cleaned up.
        Assert.Equal(invocations, MinimalApiTrackableDisposable.DisposedCount);
    }
}

public sealed class MinimalApiTrackableDisposable : IDisposable
{
    public static int CreatedCount;
    public static int DisposedCount;

    public static void Reset()
    {
        CreatedCount = 0;
        DisposedCount = 0;
    }

    public MinimalApiTrackableDisposable() => Interlocked.Increment(ref CreatedCount);

    public void Dispose() => Interlocked.Increment(ref DisposedCount);
}

public static class MinimalApiTestHandlers
{
    public sealed record Query : IQuery<Response>;

    public sealed record Response;

    public sealed class Handler(MinimalApiTrackableDisposable _) : MessageHandler<Query, Response>
    {
        public override Task<Response> ExecuteAsync(Query query, CancellationToken ct = default)
            => Task.FromResult(new Response());
    }
}
