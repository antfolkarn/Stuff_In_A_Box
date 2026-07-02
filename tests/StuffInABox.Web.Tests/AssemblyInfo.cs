using Xunit;

// Several integration tests use the app's real on-disk SQLite database (they don't override the
// DbContext). Running test collections in parallel makes them hammer that single file at once,
// which surfaces as intermittent SQLite lock errors (500s) — a test-host artifact, not an app
// bug (in production one process owns the connection). Serialize the collections to remove it.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
