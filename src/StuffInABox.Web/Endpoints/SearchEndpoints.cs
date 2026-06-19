using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StuffInABox.Application.Search.Queries;

namespace StuffInABox.Web.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/search", async (string q, IMediator mediator, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest(new { error = "Sökfras krävs." });
            var result = await mediator.Send(new SearchQuery(q), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Search")
        .WithSummary("Sök föremål, lådor och utrymmen");

        return app;
    }
}
