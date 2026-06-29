using System.Reflection;

namespace StuffInABox.Web.Endpoints;

/// <summary>
/// Exposes which build is actually running, so a stale deploy can be spotted at a
/// glance instead of by guessing. Anonymous and cheap on purpose — it must be
/// callable right after a deploy (e.g. <c>curl /version</c>) to confirm the new
/// code landed.
///
/// <para>The commit id comes from the assembly's informational version: .NET appends
/// <c>+&lt;SourceRevisionId&gt;</c> to it, so publishing with
/// <c>-p:SourceRevisionId=$(git rev-parse HEAD)</c> bakes the commit in. The build
/// time is the running assembly's file timestamp — it reflects when this exact DLL
/// was produced.</para>
/// </summary>
public static class VersionEndpoints
{
    public static void MapVersionEndpoints(this WebApplication app)
    {
        var asm = Assembly.GetEntryAssembly()!;
        var informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? asm.GetName().Version?.ToString()
            ?? "unknown";

        // InformationalVersion looks like "1.0.0+<sha>" once SourceRevisionId is set.
        var plus = informational.IndexOf('+');
        var commit = plus >= 0 ? informational[(plus + 1)..] : "unknown";
        var version = plus >= 0 ? informational[..plus] : informational;

        DateTime? buildTimeUtc = null;
        try
        {
            var location = asm.Location;
            if (!string.IsNullOrEmpty(location) && File.Exists(location))
                buildTimeUtc = File.GetLastWriteTimeUtc(location);
        }
        catch { /* best-effort — never let /version throw */ }

        app.MapGet("/version", () => Results.Ok(new
        {
            version,
            commit,
            buildTimeUtc,
        }));
    }
}
