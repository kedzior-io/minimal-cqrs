namespace MinimalCqrs.Handlers.Responses;

internal class BadRequestResponse : IHandlerResponse
{
    public bool IsSuccess => false;
    public bool IsFailure => true;
    public string Message { get; protected init; } = string.Empty;

    internal BadRequestResponse()
    {
    }

    private BadRequestResponse(string message)
    {
        Message = message;
    }

    internal static IHandlerResponse CreateError(string message)
    {
        return new BadRequestResponse(message);
    }
}

internal sealed class BadRequestResponse<TResponse> : BadRequestResponse, IHandlerResponse<TResponse>
{
    private BadRequestResponse(string message)
    {
        Message = message;
    }

    public TResponse? Payload { get; }

    internal new static IHandlerResponse<TResponse> CreateError(string message)
    {
        return new BadRequestResponse<TResponse>(message);
    }
}