using FastEndpoints;

namespace OcelotGateway.WebApi.Endpoints;

public class TestEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/test");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync("Hello World", ct);
    }
} 