namespace MinimalCqrs.Handlers.Responses;

internal class UnauthorizedResponse : IHandlerResponse
{
    public bool IsSuccess => false;
    public bool IsFailure => true;
    public string Message { get; protected init; } = string.Empty;

    protected UnauthorizedResponse()
    {
    }

    internal static IHandlerResponse CreateUnauthorized()
    {
        return new UnauthorizedResponse();
    }
}

internal sealed class UnauthorizedResponse<TResponse> : UnauthorizedResponse, IHandlerResponse<TResponse>
{
    private UnauthorizedResponse()
    {
    }

    public TResponse? Payload { get; }

    internal new static IHandlerResponse<TResponse> CreateUnauthorized()
    {
        return new UnauthorizedResponse<TResponse>();
    }
}