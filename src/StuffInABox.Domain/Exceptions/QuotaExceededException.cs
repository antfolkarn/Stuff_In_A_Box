namespace StuffInABox.Domain.Exceptions;

/// <summary>Thrown when an action would exceed the owner's plan limit. Carries a stable
/// <see cref="Quota"/> code, the <see cref="Limit"/> that was hit and the <see cref="Plan"/>
/// tier, so the client can show a targeted "upgrade" prompt.</summary>
public sealed class QuotaExceededException(string quota, int limit, string plan)
    : Exception($"Gränsen för din plan är nådd ({quota}: {limit}).")
{
    /// <summary>Stable code: "spaces" | "items" | "members".</summary>
    public string Quota { get; } = quota;
    public int Limit { get; } = limit;
    public string Plan { get; } = plan;
}
