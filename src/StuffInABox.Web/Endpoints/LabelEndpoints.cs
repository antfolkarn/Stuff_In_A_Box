using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StuffInABox.Application.Labels.Queries;

namespace StuffInABox.Web.Endpoints;

public static class LabelEndpoints
{
    public static IEndpointRouteBuilder MapLabelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.V1 + "/labels", async (Guid? spaceId, int? boxNumber, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetLabelDataQuery(spaceId, boxNumber), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Labels")
        .WithSummary("Hämta etikett-data för utskrift");

        return app;
    }
}
