# Minimal CQRS

Formerly known as ```AstroCQRS```. 

[![NuGet](https://img.shields.io/nuget/v/MinimalCqrs.svg)](https://www.nuget.org/packages/MinimalCqrs/) [![Build Status](https://dev.azure.com/isready/minimal-cqrs/_apis/build/status%2Fkedzior-io.minimal-cqrs?branchName=main)](https://dev.azure.com/isready/minimal-cqrs/_build/latest?definitionId=19&branchName=main)


https://www.nuget.org/packages/MinimalCqrs

![Minimal.CQRS](https://github.com/kedzior-io/minimal-cqrs/blob/main/minimal_cqrs_logo_small.png)

A clean, modern implementation of CQRS (Command Query Responsibility Segregation) and Vertical Slice Architecture, designed to simplify and scale application development in .NET.

Quite possibly the easiest and most beginner-friendly implementation out there.

It is designed to be used with:

- .NET 9 & .NET 8
- Minimal API
- Azure Functions (HttpTrigger, ServiceBusTrigger and TimeTrigger)
- Console app
- Blazor (todo)
- MVC (todo)

## Usage

1. Install:

```csharp
dotnet add package MinimalCqrs  
```


2. Configure :

```csharp
builder.Services.AddMinimalCqrs();
```

## Query

1. Create an endpoint:

```csharp
app.MapGetHandler<GetOrderById.Query, GetOrderById.Response>("/orders.getById.{id}");
```

☝️ Simple: we are telling what's the input, the output and the path. 


2. Create a `Query`

```csharp
public static class GetOrderById
{
    public class Query : IQuery<IHandlerResponse<Response>>
    {
        public string Id { get; set; } = "";
    }

    public record Response(OrderModel Order);

    public record OrderModel(string Id, string CustomerName, decimal Total);

    public class Handler : QueryHandler<Query, Response>
    {
        public Handler()
        {
        }

        public override async Task<IHandlerResponse<Response>> ExecuteAsync(Query query, CancellationToken ct)
        {
            // retrive data from data store
            var order = await Task.FromResult(new OrderModel(query.Id, "Gavin Belson", 20));

            if (order is null)
            {
                return Error("Order not found");
            }

            return Success(new Response(order));
        }
    }
}
```

☝️ Simple: We keep the input, the output and executing method in a single file (not mandatory though). 

.... and that's it!

## Command

1. Create an endpoint:

```csharp
app.MapPostHandler<CreateOrder.Command, CreateOrder.Response>("/orders.create");
```

1. Create a `Command`:

```csharp
public static class CreateOrder
{
    public sealed record Command(string CustomerName, decimal Total) : ICommand<IHandlerResponse<Response>>;
    public sealed record Response(Guid OrderId, string SomeValue);

    public sealed class CreateOrderValidator : Validator<Command>
    {
        public CreateOrderValidator()
        {
            RuleFor(x => x.CustomerName)
                .NotNull()
                .NotEmpty();
        }
    }

    public sealed class Handler : CommandHandler<Command, Response>
    {
        public Handler()
        {
        }

        public override async Task<IHandlerResponse<Response>> ExecuteAsync(Command command, CancellationToken ct)
        {
            var orderId = await Task.FromResult(Guid.NewGuid());
            var response = new Response(orderId, $"{command.CustomerName}");

            return Success(response);
        }
    }
}
```

☝️ Simple: Same as above + the command can be flexible and return a response

## Azure Functions

Here are the same query and command used in Azure Functions!

```csharp
services.AddMinimalCqrsFromAssemblyContaining<ListOrders.Query>();
```

☝️ Ah yeah, due to the nature of Azure Functions, we need to point to the assembly where the handlers live


```csharp
public class HttpTriggerFunction
{
    [Function(nameof(HttpTriggerFunction))]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous,"get")] HttpRequestData req)
    {
        return await AzureFunction.ExecuteHttpGetAsync<GetOrderById.Query, GetOrderById.Response>(req);
    }
}
```


```csharp
public class ServiceBusFunction
{
    [Function(nameof(ServiceBusFunction))]
    public async Task Run([ServiceBusTrigger("created-order", Connection = "ConnectionStrings:ServiceBus")] string json, FunctionContext context)
    {
        await AzureFunction.ExecuteServiceBusAsync<CreateOrder.Command, CreateOrder.Response>(json, JsonOptions.Defaults, context);
    }
}
```
## Handlers

The handler always returns three types of responses, which enforce consistency:
- `Success(payload)` - handler executed with success and has response
- `Success()` - handler executed with success but has no response
- `Error("Error message")`- handler has an error

`Error("Order not found")` will return [Problem Details](https://datatracker.ietf.org/doc/html/rfc7807) (a standard way of specifying errors in HTTP API responses)

```
{
    "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    "title": "Order not found",
    "status": 400,
    "errors": {}
}
```

## Sample Code

Check [samples](https://github.com/kedzior-io/astro-cqrs/tree/main/samples) available in this repo. 

Also check a solution template here: [Astro Architecture](https://github.com/kedzior-io/astro-architecture).

## Motives

I'm a big fan of:
- [MediatR](https://github.com/jbogard/MediatR)
- [Wolverine](https://github.com/JasperFx/wolverine)
- [FastEndpoints](https://fast-endpoints.com/)

and used them all in production environment. So why this?

Well, in all of them I was missing something:

- `MediatR` - a bit of setup + always abstracted a lot in my own wrappers.
- `Wolverine` - it covers a lot more that I need, it uses a lot of dependencies, has an odd way to setup query handler. 
- `FastEndpoints` - its command bus is amazing but the whole library enforces REPR Design Pattern (Request-Endpoint-Response) which I'm not a big fan of. It also doesn't work for Azure Functions or Blazor.

I decided to borrow the best features from existing frameworks to create an in-process messaging mechanism that features:

- Easy setup
- Decoupled and reusable handlers
- Enforced consistency
- Built-in validation
- Out-of-the-box compatibility with multiple project types (including Minimal API, Azure Functions, Console, MVC, Blazor)
- Unit testability

It can be seen in production here: [https://shipdotnet.com](https://shipdotnet.com)


## Todo

There are few things to work out here and mainly:

- Blazor example
- MVC example
- Unit test example
- Integration test example
- Benchmarks

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## Questions, feature requests

- Create a [new issue](https://github.com/kedzior-io/astro-cqrs/issues/new)
- [Twitter](https://twitter.com/KedziorArtur)
- [Discord](https://discord.gg/j3vmcaZG)
