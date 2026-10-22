namespace BuildingBlocks.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Applies pending migrations at startup (creates the schema when no migrations exist).
    /// Recommended for development only; use migration bundles in production pipelines.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
