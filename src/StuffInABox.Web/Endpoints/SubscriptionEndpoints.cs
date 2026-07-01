using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StuffInABox.Application.Subscription.Queries;

namespace StuffInABox.Web.Endpoints;

public static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.V1 + "/subscription").WithTags("Subscription").RequireAuthorization();

        // The user's current plan, usage against its limits, and the tiers to compare/upgrade to.
        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetSubscriptionQuery(), ct)))
            .WithSummary("Min prenumeration och förbrukning");

        return app;
    }
}
