using Microsoft.Extensions.DependencyInjection;
using Xunit;

// Conf.ServiceResolver is static — disable parallelism so tests don't interfere.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MinimalCqrs.Tests;

/// <summary>
/// Reproduces the root-scope accumulation bug: when MinimalCqrs is used without an HTTP context
/// (Azure Functions, Console App), Transient+IDisposable dependencies resolved during handler
/// execution are tracked in the root IServiceProvider's disposables list and never disposed.
///
/// The test asserts the CORRECT behavior (each invocation's disposable is disposed).
/// It FAILS on the current code and should PASS once the per-invocation scope fix is applied
/// to CommandHandlerExecutor and ServiceResolver.CreateScope().
/// </summary>
public class ScopeLeakTests
{
    [Fact]
    public async Task Transient_IDisposable_dependency_is_disposed_after_each_invocation_no_http_context()
    {
        // Simulates Azure Functions / Console App — no IHttpContextAccessor.HttpContext
        TrackableDisposable.Reset();

        var services = new ServiceCollection();
        services.AddTransient<TrackableDisposable>();
        services.AddMinimalCqrsFromAssemblyContaining<LeakTestHandlers.Query>();

        const int invocations = 5;

        for (var i = 0; i < invocations; i++)
        {
            await HandlerExtensions.ExecuteAsync(new LeakTestHandlers.Query(), CancellationToken.None);
        }

        Assert.Equal(invocations, TrackableDisposable.CreatedCount);

        // Before the fix: DisposedCount == 0 (instances leak into root scope, never disposed).
        // After the fix:  DisposedCount == invocations (per-invocation scope disposes them).
        Assert.Equal(invocations, TrackableDisposable.DisposedCount);
    }
}

public sealed class TrackableDisposable : IDisposable
{
    public static int CreatedCount;
    public static int DisposedCount;

    public static void Reset()
    {
        CreatedCount = 0;
        DisposedCount = 0;
    }

    public TrackableDisposable() => Interlocked.Increment(ref CreatedCount);

    public void Dispose() => Interlocked.Increment(ref DisposedCount);
}

public static class LeakTestHandlers
{
    public sealed record Query : IQuery<Response>;

    public sealed record Response;

    public sealed class Handler : MessageHandler<Query, Response>
    {
        private readonly TrackableDisposable _disposable;

        public Handler(TrackableDisposable disposable) => _disposable = disposable;

        public override Task<Response> ExecuteAsync(Query query, CancellationToken ct = default)
            => Task.FromResult(new Response());
    }
}
