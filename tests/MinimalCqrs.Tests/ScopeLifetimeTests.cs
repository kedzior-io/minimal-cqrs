using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MinimalCqrs.Tests;

/// <summary>
/// Tests that verify service lifetime semantics are correct after the per-invocation scope fix.
/// All tests use no HTTP context (Azure Functions / Console App scenario).
/// </summary>
public class ScopeLifetimeTests
{
    // -------------------------------------------------------------------------
    // 1. Scoped service is a NEW instance on each invocation.
    //    Critical for fiz-api: IHandlerContext as Transient must be isolated per call.
    //    Before fix: resolved from root SP → effectively Singleton (same instance forever).
    //    After fix:  resolved from a new scope per invocation → new instance each time.
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Scoped_service_is_new_instance_per_invocation()
    {
        ScopedPerInvocation.Reset();

        var services = new ServiceCollection();
        services.AddScoped<ScopedPerInvocation>();
        services.AddMinimalCqrsFromAssemblyContaining<ScopedIsolationHandlers.Query>();

        await HandlerExtensions.ExecuteAsync(new ScopedIsolationHandlers.Query(), CancellationToken.None);
        await HandlerExtensions.ExecuteAsync(new ScopedIsolationHandlers.Query(), CancellationToken.None);

        Assert.Equal(2, ScopedPerInvocation.Instances.Count);
        Assert.NotSame(ScopedPerInvocation.Instances[0], ScopedPerInvocation.Instances[1]);
    }

    // -------------------------------------------------------------------------
    // 2. Within a single invocation, two dependencies that both take a common
    //    Scoped service must receive the SAME instance of that service.
    //    This is how DbContext sharing works: HandlerContext and Repository
    //    both need the same DbContext within one handler execution.
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Scoped_service_is_shared_across_dependencies_within_same_invocation()
    {
        SharedScopedDep.Reset();

        var services = new ServiceCollection();
        services.AddScoped<SharedScopedDep>();
        services.AddTransient<DependencyA>();
        services.AddTransient<DependencyB>();
        services.AddMinimalCqrsFromAssemblyContaining<ScopedSharingHandlers.Query>();

        await HandlerExtensions.ExecuteAsync(new ScopedSharingHandlers.Query(), CancellationToken.None);

        // Both DependencyA and DependencyB received the same SharedScopedDep instance.
        Assert.Single(SharedScopedDep.Instances);
        Assert.Same(ScopedSharingHandlers.Handler.SeenFromA, ScopedSharingHandlers.Handler.SeenFromB);
    }

    // -------------------------------------------------------------------------
    // 3. Singleton services must remain the same instance across every invocation.
    //    The per-invocation scope resolves Singletons from its parent (root SP),
    //    so they should never be duplicated.
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Singleton_service_is_same_instance_across_invocations()
    {
        SingletonService.Reset();

        var services = new ServiceCollection();
        services.AddSingleton<SingletonService>();
        services.AddMinimalCqrsFromAssemblyContaining<SingletonHandlers.Query>();

        await HandlerExtensions.ExecuteAsync(new SingletonHandlers.Query(), CancellationToken.None);
        await HandlerExtensions.ExecuteAsync(new SingletonHandlers.Query(), CancellationToken.None);
        await HandlerExtensions.ExecuteAsync(new SingletonHandlers.Query(), CancellationToken.None);

        Assert.Single(SingletonService.Instances);
        Assert.Same(SingletonHandlers.Handler.SeenOnFirstCall, SingletonHandlers.Handler.SeenOnLastCall);
    }

    // -------------------------------------------------------------------------
    // 4. If the handler throws, the exception must propagate to the caller and
    //    the per-invocation scope must still be disposed (no scope leak on failure).
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Exception_propagates_and_scope_is_disposed()
    {
        DisposeOnThrow.Reset();

        var services = new ServiceCollection();
        services.AddTransient<DisposeOnThrow>();
        services.AddMinimalCqrsFromAssemblyContaining<ThrowingHandlers.Query>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => HandlerExtensions.ExecuteAsync(new ThrowingHandlers.Query(), CancellationToken.None));

        Assert.Equal(1, DisposeOnThrow.CreatedCount);
        Assert.Equal(1, DisposeOnThrow.DisposedCount);
    }

    // -------------------------------------------------------------------------
    // 5. The return value from the handler must flow back to the caller unchanged.
    //    The async/await wrapping in the executor must not swallow or alter results.
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Return_value_is_propagated_correctly()
    {
        var services = new ServiceCollection();
        services.AddMinimalCqrsFromAssemblyContaining<ReturnValueHandlers.Query>();

        var result = await HandlerExtensions.ExecuteAsync(
            new ReturnValueHandlers.Query("hello"), CancellationToken.None);

        Assert.Equal("HELLO", result.Value);
    }
}

// ---------------------------------------------------------------------------
// Supporting types — one set per test, to avoid cross-test interference
// ---------------------------------------------------------------------------

public sealed class ScopedPerInvocation
{
    public static readonly List<ScopedPerInvocation> Instances = new();
    public static void Reset() => Instances.Clear();
    public ScopedPerInvocation() => Instances.Add(this);
}

public static class ScopedIsolationHandlers
{
    public sealed record Query : IQuery<bool>;

    public sealed class Handler(ScopedPerInvocation _) : MessageHandler<Query, bool>
    {
        public override Task<bool> ExecuteAsync(Query query, CancellationToken ct = default)
            => Task.FromResult(true);
    }
}

public sealed class SharedScopedDep
{
    public static readonly List<SharedScopedDep> Instances = new();
    public static void Reset() => Instances.Clear();
    public SharedScopedDep() => Instances.Add(this);
}

public sealed class DependencyA(SharedScopedDep dep) { public SharedScopedDep Dep => dep; }
public sealed class DependencyB(SharedScopedDep dep) { public SharedScopedDep Dep => dep; }

public static class ScopedSharingHandlers
{
    public sealed record Query : IQuery<bool>;

    public sealed class Handler(DependencyA a, DependencyB b) : MessageHandler<Query, bool>
    {
        public static SharedScopedDep? SeenFromA;
        public static SharedScopedDep? SeenFromB;

        public override Task<bool> ExecuteAsync(Query query, CancellationToken ct = default)
        {
            SeenFromA = a.Dep;
            SeenFromB = b.Dep;
            return Task.FromResult(true);
        }
    }
}

public sealed class SingletonService
{
    public static readonly List<SingletonService> Instances = new();
    public static void Reset() => Instances.Clear();
    public SingletonService() => Instances.Add(this);
}

public static class SingletonHandlers
{
    public sealed record Query : IQuery<bool>;

    public sealed class Handler(SingletonService service) : MessageHandler<Query, bool>
    {
        public static SingletonService? SeenOnFirstCall;
        public static SingletonService? SeenOnLastCall;

        public override Task<bool> ExecuteAsync(Query query, CancellationToken ct = default)
        {
            SeenOnFirstCall ??= service;
            SeenOnLastCall = service;
            return Task.FromResult(true);
        }
    }
}

public sealed class DisposeOnThrow : IDisposable
{
    public static int CreatedCount;
    public static int DisposedCount;
    public static void Reset() { CreatedCount = 0; DisposedCount = 0; }
    public DisposeOnThrow() => Interlocked.Increment(ref CreatedCount);
    public void Dispose() => Interlocked.Increment(ref DisposedCount);
}

public static class ThrowingHandlers
{
    public sealed record Query : IQuery<bool>;

    public sealed class Handler(DisposeOnThrow _) : MessageHandler<Query, bool>
    {
        public override Task<bool> ExecuteAsync(Query query, CancellationToken ct = default)
            => throw new InvalidOperationException("handler failure");
    }
}

public static class ReturnValueHandlers
{
    public sealed record Query(string Input) : IQuery<Response>;
    public sealed record Response(string Value);

    public sealed class Handler : MessageHandler<Query, Response>
    {
        public override Task<Response> ExecuteAsync(Query query, CancellationToken ct = default)
            => Task.FromResult(new Response(query.Input.ToUpper()));
    }
}
