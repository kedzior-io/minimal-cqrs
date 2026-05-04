using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MinimalCqrs.Tests;

/// <summary>
/// Verifies that handler execution works correctly in an ASP.NET Core Minimal API context
/// (IHttpContextAccessor.HttpContext is set with a per-request RequestServices scope).
///
/// HttpContextAccessor stores the context in a static AsyncLocal, so all instances —
/// including the one inside ServiceResolver's inner service provider — share the same value.
///
/// Expected behavior after the scope fix:
///   - CreateScope() uses HttpContext.RequestServices.CreateScope() (child of request scope)
///   - Handler's Transient+IDisposable dependencies are disposed when the child scope exits
///   - This happens per-invocation, before the request scope itself is disposed
/// </summary>
public class MinimalApiScopeTests
{
    [Fact]
    public async Task Transient_IDisposable_dependency_is_disposed_after_each_invocation_with_http_context()
    {
        // Simulates ASP.NET Core Minimal API — IHttpContextAccessor.HttpContext is populated.
        MinimalApiTrackableDisposable.Reset();

        var services = new ServiceCollection();
        services.AddTransient<MinimalApiTrackableDisposable>();
        services.AddMinimalCqrsFromAssemblyContaining<MinimalApiTestHandlers.Query>();

        // Build an outer SP to own the request scope and the IHttpContextAccessor.
        // HttpContextAccessor uses a static AsyncLocal, so the inner SP's accessor
        // (inside ServiceResolver) will see whatever we set here.
        var appSp = services.BuildServiceProvider();
        var httpContextAccessor = appSp.GetRequiredService<IHttpContextAccessor>();

        const int invocations = 5;

        using var requestScope = appSp.CreateScope();

        try
        {
            httpContextAccessor.HttpContext = new DefaultHttpContext
            {
                RequestServices = requestScope.ServiceProvider
            };

            for (var i = 0; i < invocations; i++)
            {
                await HandlerExtensions.ExecuteAsync(new MinimalApiTestHandlers.Query(), CancellationToken.None);

                // After the fix: each invocation's child scope is disposed immediately,
                // so DisposedCount increments after every call.
                // Before the fix: instances are held by the request scope and DisposedCount stays 0
                // until requestScope.Dispose() is called at the end.
                Assert.Equal(i + 1, MinimalApiTrackableDisposable.DisposedCount);
            }

            Assert.Equal(invocations, MinimalApiTrackableDisposable.CreatedCount);
            Assert.Equal(invocations, MinimalApiTrackableDisposable.DisposedCount);
        }
        finally
        {
            httpContextAccessor.HttpContext = null;
        }
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

    public sealed class Handler : MessageHandler<Query, Response>
    {
        private readonly MinimalApiTrackableDisposable _disposable;

        public Handler(MinimalApiTrackableDisposable disposable) => _disposable = disposable;

        public override Task<Response> ExecuteAsync(Query query, CancellationToken ct = default)
            => Task.FromResult(new Response());
    }
}
