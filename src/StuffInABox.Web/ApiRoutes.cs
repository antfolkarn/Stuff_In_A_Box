namespace StuffInABox.Web;

/// <summary>
/// Central API route prefix. Versioned so web and future mobile clients can evolve
/// independently — bumping to a v2 surface is a one-line change here plus new groups.
/// </summary>
internal static class ApiRoutes
{
    public const string V1 = "/api/v1";

    /// <summary>Cookie path for the refresh token — scoped to the auth routes.</summary>
    public const string Auth = V1 + "/auth";
}
