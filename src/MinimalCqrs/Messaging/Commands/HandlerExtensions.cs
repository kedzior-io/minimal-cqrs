﻿using System.Collections.Concurrent;

namespace MinimalCqrs;

internal class HandlerRegistry : ConcurrentDictionary<Type, HandlerDefinition>
{ }

public static class HandlerExtensions
{
    public static Task<TResponse> ExecuteAsync<TResponse>(IHandlerMessage<TResponse> message, CancellationToken ct)
    {
        var messageType = message.GetType();

        var registry = Conf.ServiceResolver.Resolve<HandlerRegistry>();

        if (!registry.TryGetValue(messageType, out var handlerDefinition))
        {
            throw new InvalidOperationException($"Unable to create an instance of the handler for [{messageType.FullName}]");
        }

        handlerDefinition.HandlerExecutor ??= CreateHandlerExecutor(messageType);

        return ((HandlerExecutorBase<TResponse>)handlerDefinition.HandlerExecutor).Execute(message, handlerDefinition.HandlerType, ct);

        static HandlerExecutorBase<TResponse> CreateHandlerExecutor(Type tCommand)
            => (HandlerExecutorBase<TResponse>)
                Conf.ServiceResolver.CreateSingleton(Types.CommandHandlerExecutorOf2.MakeGenericType(tCommand, typeof(TResponse)));
    }

    public static Task<TResponse> ExecuteWithEmptyMessageAsync<TMessage, TResponse>(CancellationToken ct) where TMessage : IHandlerMessage<TResponse>
    {
        var message = Activator.CreateInstance<TMessage>();
        return ExecuteAsync(message, ct);
    }
}