using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using StuffInABox.Application.Common.Interfaces;

namespace StuffInABox.Web.Endpoints;

public static class RecognitionEndpoints
{
    public static IEndpointRouteBuilder MapRecognitionEndpoints(this IEndpointRouteBuilder app)
    {
        // Called when a photo is chosen in the add-item sheet, to pre-fill the name
        // and suggest tags. Returns { name: null, tags: [] } when no provider is configured.
        app.MapPost("/api/recognize", async (IFormFile file, IImageRecognitionService recognizer, CancellationToken ct) =>
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var result = await recognizer.RecognizeAsync(ms.ToArray(), ct);
            return Results.Ok(new
            {
                name = result?.Name,
                tags = result?.Tags ?? [],
            });
        })
        .RequireAuthorization()
        .DisableAntiforgery()
        .WithTags("Recognition")
        .WithSummary("Känn igen föremål på ett foto (för-ifyller namn)");

        return app;
    }
}
