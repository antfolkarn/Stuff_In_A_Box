using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StuffInABox.Application.Common.Interfaces;
using StuffInABox.Domain.Exceptions;

namespace StuffInABox.Web.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            ValidationException => (HttpStatusCode.BadRequest, "Valideringsfel"),
            InvalidImageException => (HttpStatusCode.BadRequest, "Ogiltig bild"),
            NotFoundException => (HttpStatusCode.NotFound, "Hittades inte"),
            ForbiddenException => (HttpStatusCode.Forbidden, "Åtkomst nekad"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Ej autentiserad"),
            _ => (HttpStatusCode.InternalServerError, "Serverfel")
        };

        ctx.Response.StatusCode = (int)status;
        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Detail = ex.Message
        };

        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
