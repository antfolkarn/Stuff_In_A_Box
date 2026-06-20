using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StuffInABox.Infrastructure.Persistence;

namespace StuffInABox.Web.HealthChecks;

/// <summary>Readiness check: verifies the database is reachable.</summary>
public sealed class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default) =>
        await db.Database.CanConnectAsync(ct)
            ? HealthCheckResult.Healthy("Databasen är nåbar.")
            : HealthCheckResult.Unhealthy("Kan inte nå databasen.");
}
