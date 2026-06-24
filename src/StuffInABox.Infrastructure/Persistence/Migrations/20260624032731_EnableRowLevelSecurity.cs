using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StuffInABox.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Enables Row Level Security on every table. Supabase auto-exposes the public schema
    /// through PostgREST using the public anon key; with no RLS, anyone holding that key
    /// could read/write these tables over REST. Enabling RLS with no policies denies all
    /// data-API access. The app connects as the table owner (postgres), which bypasses
    /// RLS, so it keeps full access — only the public REST path is locked down.
    /// Postgres-only (dev SQLite builds the schema via EnsureCreated, not migrations).
    /// </summary>
    public partial class EnableRowLevelSecurity : Migration
    {
        private const string Npgsql = "Npgsql.EntityFrameworkCore.PostgreSQL";

        private static readonly string[] Tables =
        [
            "Boxes", "Items", "PasswordResetTokens", "RefreshTokens", "SpaceInvites",
            "SpaceMemberships", "Spaces", "UserIdentities", "UserSettings",
            // EF's own bookkeeping table is also in the PostgREST-exposed public schema.
            "__EFMigrationsHistory",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != Npgsql) return;
            foreach (var table in Tables)
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider != Npgsql) return;
            foreach (var table in Tables)
                migrationBuilder.Sql($"ALTER TABLE \"{table}\" DISABLE ROW LEVEL SECURITY;");
        }
    }
}
