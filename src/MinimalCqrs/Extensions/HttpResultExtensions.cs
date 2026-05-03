using Microsoft.AspNetCore.Http;
using MinimalCqrs.Handlers.Responses;

namespace MinimalCqrs;

public static class HttpResultExtensions
{
    public static IResult ToHttpResult(this IHandlerResponse response)
    {
        return response switch
        {
            UnauthorizedResponse
                => Results.Unauthorized(),

            BadRequestResponse badRequest
                => Results.BadRequest(badRequest.Message),

            SuccessResponse
                => Results.Ok(),

            _ => throw new InvalidOperationException($"Unsupported response type: {response.GetType().Name}")
        };
    }
}