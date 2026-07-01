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
        // `code` is a stable, machine-readable identifier that clients (web + mobile)
        // map to their own localized text. `title` stays human-readable for tooling.
        var (status, title, code) = ex switch
        {
            ValidationException => (HttpStatusCode.BadRequest, "Valideringsfel", "validation_error"),
            InvalidImageException => (HttpStatusCode.BadRequest, "Ogiltig bild", "invalid_image"),
            NotFoundException => (HttpStatusCode.NotFound, "Hittades inte", "not_found"),
            EmailNotVerifiedException => (HttpStatusCode.Forbidden, "E-post ej verifierad", "email_not_verified"),
            QuotaExceededException => (HttpStatusCode.Forbidden, "Gräns nådd", "quota_exceeded"),
            ForbiddenException => (HttpStatusCode.Forbidden, "Åtkomst nekad", "forbidden"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Ej autentiserad", "unauthorized"),
            _ => (HttpStatusCode.InternalServerError, "Serverfel", "server_error")
        };

        ctx.Response.StatusCode = (int)status;
        var problem = new ProblemDetails
        {
            Status = (int)status,
            Title = title,
            Detail = ex.Message,
            Extensions = { ["code"] = code }
        };

        // Give the client the details it needs to show a targeted upgrade prompt.
        if (ex is QuotaExceededException quota)
        {
            problem.Extensions["quota"] = quota.Quota;
            problem.Extensions["limit"] = quota.Limit;
            problem.Extensions["plan"] = quota.Plan;
        }

        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
