using FilmRadar.Web.Data;
using FilmRadar.Web.Integrations.Tmdb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FilmRadar.Tests;

internal sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:;Foreign Keys=True");
    public FilmRadarDbContext Db { get; private set; } = null!;
    public static async Task<TestDatabase> CreateAsync()
    {
        var fixture = new TestDatabase();
        await fixture.connection.OpenAsync();
        fixture.Db = new(new DbContextOptionsBuilder<FilmRadarDbContext>().UseSqlite(fixture.connection).Options);
        await fixture.Db.Database.MigrateAsync();
        await new DatabaseInitializer(fixture.Db, TimeProvider.System).InitializeAsync(default);
        return fixture;
    }
    public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
}

internal sealed class FakeTmdb : ITmdbClient
{
    public List<TmdbDiscoverRequest> DiscoverCalls { get; } = [];
    public int DetailCalls { get; private set; }
    public TmdbApiException? Failure { get; set; }
    public Func<int, TmdbPagedResult<TmdbMovieSummaryDto>> Page { get; set; } = p => new()
    {
        Page = p,
        TotalPages = 1,
        TotalResults = 2,
        Results = [new() { Id = 101, Title = "Test comedy", GenreIds = [35], VoteAverage = 8, VoteCount = 300 },
                   new() { Id = 102, Title = "Test drama", GenreIds = [18], VoteAverage = 7, VoteCount = 250 }]
    };
    public Task<TmdbPagedResult<TmdbMovieSummaryDto>> DiscoverMoviesAsync(TmdbDiscoverRequest request, CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); if (Failure != null) throw Failure; DiscoverCalls.Add(request); return Task.FromResult(Page(request.Page)); }
    public Task<TmdbPagedResult<TmdbMovieSummaryDto>> GetPopularMoviesAsync(int page, CancellationToken ct) => DiscoverMoviesAsync(new([], Page: page), ct);
    public Task<TmdbPagedResult<TmdbMovieSummaryDto>> SearchMoviesAsync(TmdbSearchRequest request, CancellationToken ct) => DiscoverMoviesAsync(new([], Page: request.Page), ct);
    public Task<TmdbMovieDetailsDto?> GetMovieDetailsAsync(int id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested(); if (Failure != null) throw Failure; DetailCalls++;
        return Task.FromResult<TmdbMovieDetailsDto?>(id == 404 ? null : new()
        {
            Id = id,
            Title = "Test movie " + id,
            Overview = "A test overview.",
            Genres = [new(35, "Комедия"), new(18, "Драма")],
            ReleaseDate = "2020-01-02",
            VoteAverage = 8.1,
            VoteCount = 300,
            Runtime = 100
        });
    }
    public Task<IReadOnlyList<TmdbGenreDto>> GetMovieGenresAsync(CancellationToken ct)
    { if (Failure != null) throw Failure; return Task.FromResult<IReadOnlyList<TmdbGenreDto>>([new(35, "Комедия"), new(12345, "New genre")]); }
}
