using MinimalCqrs.Handlers.Responses;

namespace MinimalCqrs;

public abstract class Handler<TMessage, TResponse> : IHandler<TMessage, IHandlerResponse<TResponse>> where TMessage : IHandlerMessage<IHandlerResponse<TResponse>>
{
    protected Handler()
    {
    }

    public abstract Task<IHandlerResponse<TResponse>> ExecuteAsync(TMessage command, CancellationToken ct = default);

    protected IHandlerResponse<TResponse> Success(TResponse response)
    {
        return SuccessResponse<TResponse>.CreateSuccess(response);
    }

    protected IHandlerResponse<TResponse> Error(string message)
    {
        return BadRequestResponse<TResponse>.CreateError(message);
    }

    protected IHandlerResponse<TResponse> Unauthorized()
    {
        return UnauthorizedResponse<TResponse>.CreateUnauthorized();
    }
}

public abstract class Handler<TMessage> : IHandler<TMessage, IHandlerResponse> where TMessage : IHandlerMessage<IHandlerResponse>
{
    protected Handler()
    {
    }

    public abstract Task<IHandlerResponse> ExecuteAsync(TMessage command, CancellationToken ct = default);

    protected IHandlerResponse Success()
    {
        return SuccessResponse.CreateEmpty();
    }

    protected IHandlerResponse Error(string message)
    {
        return BadRequestResponse.CreateError(message);
    }

    protected IHandlerResponse Unauthorized(string message)
    {
        return UnauthorizedResponse.CreateUnauthorized();
    }
}

public abstract class EventHandler<TMessage> : IHandler<TMessage> where TMessage : IHandlerMessage
{
    protected EventHandler()
    {
    }

    public abstract Task ExecuteAsync(TMessage command, CancellationToken ct = default);
}