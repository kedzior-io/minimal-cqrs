namespace MinimalCqrs.Handlers.Responses;

internal class SuccessResponse : IHandlerResponse
{
    public bool IsSuccess => true;
    public bool IsFailure => false;
    public string Message { get; protected init; } = string.Empty;

    protected SuccessResponse()
    {
    }

    internal static IHandlerResponse CreateEmpty()
    {
        return new SuccessResponse();
    }
}

internal sealed class SuccessResponse<TResponse> : SuccessResponse, IHandlerResponse<TResponse>
{
    private SuccessResponse(TResponse payload)
    {
        Payload = payload;
    }

    public TResponse? Payload { get; }

    internal static IHandlerResponse<TResponse> CreateSuccess(TResponse payload)
    {
        return new SuccessResponse<TResponse>(payload);
    }
}