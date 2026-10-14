using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HaveTickets.Api.Tests;

/// <summary>
/// A clock the test moves by hand, so sales cut-offs and check-in windows can be
/// crossed deterministically instead of by sleeping.
/// </summary>
internal sealed class MutableTimeProvider(DateTimeOffset start) : TimeProvider
{
    private readonly Lock _gate = new();
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return _now;
        }
    }

    public void SetUtcNow(DateTimeOffset value)
    {
        lock (_gate)
        {
            _now = value;
        }
    }
}

/// <summary>
/// Hosts the real API in memory against its own throw-away SQLite file. Nothing is
/// mocked: routing, validation, rate limiting, EF Core and the database constraints
/// are all the production ones.
/// </summary>
internal sealed class ApiFactory(string environment = "Development") : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"havetickets-tests-{Guid.NewGuid():N}.db");

    public MutableTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:HaveTickets", $"Data Source={_databasePath};Pooling=False");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        foreach (string suffix in (string[])["", "-wal", "-shm"])
        {
            string path = _databasePath + suffix;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
