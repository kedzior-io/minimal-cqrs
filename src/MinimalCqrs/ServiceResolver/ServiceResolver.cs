using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace MinimalCqrs;

internal sealed class ServiceResolver : IServiceResolver
{
    private readonly ConcurrentDictionary<Type, ObjectFactory> _factoryCache = new();
    private readonly ConcurrentDictionary<Type, object> _singletonCache = new();
    private readonly IServiceProvider _rootServiceProvider;
    private readonly IHttpContextAccessor _ctxAccessor;

    private readonly bool _isUnitTestMode;

    public ServiceResolver(IServiceProvider provider, IHttpContextAccessor ctxAccessor, bool isUnitTestMode = false)
    {
        _rootServiceProvider = provider;
        _ctxAccessor = ctxAccessor;
        _isUnitTestMode = isUnitTestMode;
    }

    public object CreateInstance(Type type, IServiceProvider? serviceProvider = null)
    {
        var factory = _factoryCache.GetOrAdd(type, static t => ActivatorUtilities.CreateFactory(t, Type.EmptyTypes));

        return factory(serviceProvider ?? _ctxAccessor?.HttpContext?.RequestServices ?? _rootServiceProvider, null);
    }

    public object CreateSingleton(Type type)
        => _singletonCache.GetOrAdd(type, static (t, sp) => ActivatorUtilities.GetServiceOrCreateInstance(sp!, t), _rootServiceProvider);

    public IServiceScope CreateScope()
    {
        if (_isUnitTestMode)
        {
            return _ctxAccessor.HttpContext?.RequestServices.CreateScope() ?? throw new InvalidOperationException("Please follow documentation to configure unit test environment properly!");
        }

        /* 
         * ASP.NET Core wraps the existing request scope without creating a child scope.
         * Transient + IDisposable services are tracked by the request scope and disposed at request end.
         * Avoids allocating a ServiceProviderEngineScope on every handler invocation.
         */
        if (_ctxAccessor?.HttpContext?.RequestServices is { } requestServices)
        {
            return new NonDisposingScope(requestServices);
        }

        /* 
         * Azure Functions & Console App: no ambient request scope so create a real per-invocation
         * scope that IS disposed after the handler returns, preventing root-scope accumulation.
        */
        return _rootServiceProvider.CreateScope();
    }

    private sealed class NonDisposingScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;
        public void Dispose() { }
    }

    public TService Resolve<TService>() where TService : class
        => _ctxAccessor.HttpContext?.RequestServices.GetRequiredService<TService>() ??
           _rootServiceProvider.GetRequiredService<TService>();

    public object Resolve(Type typeOfService)
        => _ctxAccessor.HttpContext?.RequestServices.GetRequiredService(typeOfService) ??
           _rootServiceProvider.GetRequiredService(typeOfService);

    public TService? TryResolve<TService>() where TService : class
        => _ctxAccessor.HttpContext?.RequestServices.GetService<TService>() ??
           _rootServiceProvider.GetService<TService>();

    public object? TryResolve(Type typeOfService)
        => _ctxAccessor.HttpContext?.RequestServices.GetService(typeOfService) ??
           _rootServiceProvider.GetService(typeOfService);
}