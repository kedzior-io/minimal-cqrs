namespace MinimalCqrs;

internal abstract class HandlerExecutorBase
{
    internal abstract Task Execute(IHandlerMessage handlerMessage, Type handlerType, CancellationToken ct);
}

internal sealed class CommandHandlerExecutor<TCommand> : HandlerExecutorBase where TCommand : IHandlerMessage
{
    internal override async Task Execute(IHandlerMessage command, Type tCommandHandler, CancellationToken ct)
    {
        using var scope = Conf.ServiceResolver.CreateScope();
        await ((IHandler<TCommand>)Conf.ServiceResolver.CreateInstance(tCommandHandler, scope.ServiceProvider))
            .ExecuteAsync((TCommand)command, ct);
    }
}

internal abstract class HandlerExecutorBase<TResult>
{
    internal abstract Task<TResult> Execute(IHandlerMessage<TResult> command, Type handlerType, CancellationToken ct);
}

internal sealed class CommandHandlerExecutor<TCommand, TResult> : HandlerExecutorBase<TResult> where TCommand : IHandlerMessage<TResult>
{
    internal override async Task<TResult> Execute(IHandlerMessage<TResult> command, Type tCommandHandler, CancellationToken ct)
    {
        using var scope = Conf.ServiceResolver.CreateScope();
        return await ((IHandler<TCommand, TResult>)Conf.ServiceResolver.CreateInstance(tCommandHandler, scope.ServiceProvider))
            .ExecuteAsync((TCommand)command, ct);
    }
}