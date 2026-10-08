using System.ComponentModel.DataAnnotations;
using FilmRadar.Web.Domain;
using FilmRadar.Web.Integrations.Tmdb;
using FilmRadar.Web.Services;
using FilmRadar.Web.Services.Recommendations;
using Microsoft.Extensions.Logging.Abstractions;

namespace FilmRadar.Tests;

public class RecommendationWorkflowTests
{
    private static RecommendationService Service(TestDatabase fixture, FakeTmdb tmdb, IRecommendationEngine? engine = null) => new(
        new ProfileService(fixture.Db, TimeProvider.System), new UserMovieService(fixture.Db, tmdb, TimeProvider.System),
        new GenreCatalogService(fixture.Db, tmdb, TimeProvider.System), tmdb, engine ?? new MockRecommendationEngine(), NullLogger<RecommendationService>.Instance);
    private sealed class CaptureEngine : IRecommendationEngine
    {
        public IReadOnlyCollection<RecommendationCandidate> Candidates { get; private set; } = [];
        public IReadOnlyList<RecommendationResult> Rank(RecommendationContext context, IReadOnlyCollection<RecommendationCandidate> candidates)
        { Candidates = candidates; return new MockRecommendationEngine().Rank(context, candidates); }
    }
    [Fact]
    public async Task RequestInheritsProfilePerFieldAndUsesOnlySelectedGenreFilters()
    {
        await using var fixture = await TestDatabase.CreateAsync(); var tmdb = new FakeTmdb();
        await new ProfileService(fixture.Db, TimeProvider.System).UpdateAsync(new("Tester", [35], 2000, 2025, 7), default);
        var data = await Service(fixture, tmdb).RecommendAsync(new(MovieMood.Cheerful, [18], null, 2020, null), default);
        var query = Assert.Single(tmdb.DiscoverCalls);
        Assert.Equal(2000, query.YearFrom); Assert.Equal(2020, query.YearTo); Assert.Equal(7, query.MinimumRating);
        Assert.Equal([18], query.GenreIds); Assert.Equal(0, tmdb.DetailCalls); Assert.Equal(2020, data.EffectiveRequest.YearTo);
    }
    [Fact]
    public async Task ConflictingEffectiveBoundsAreRejectedBeforeNetwork()
    {
        await using var fixture = await TestDatabase.CreateAsync(); var tmdb = new FakeTmdb();
        await new ProfileService(fixture.Db, TimeProvider.System).UpdateAsync(new("Tester", [], 2020, null, null), default);
        await Assert.ThrowsAsync<ValidationException>(() => Service(fixture, tmdb).RecommendAsync(new(MovieMood.Calm, [], null, 2000, null), default));
        Assert.Empty(tmdb.DiscoverCalls);
    }
    [Fact]
    public async Task WorkflowCapsPagesDeduplicatesExcludesWatchedAndKeepsWatchlist()
    {
        await using var fixture = await TestDatabase.CreateAsync(); var tmdb = new FakeTmdb(); var engine = new CaptureEngine();
        var movies = new UserMovieService(fixture.Db, tmdb, TimeProvider.System);
        var watched = (await movies.AddAsync(101, default)).Id!.Value;
        await movies.UpdateAsync(watched, new(2, 0, false, null, null), default);
        var watchlist = (await movies.AddAsync(102, default)).Id!.Value;
        tmdb.Page = p => new()
        {
            Page = p,
            TotalPages = 50,
            Results = Enumerable.Range(100 + (p - 1) * 10, 20)
            .Select(i => new TmdbMovieSummaryDto { Id = i, Title = "Movie " + i, GenreIds = [35], VoteCount = 200, VoteAverage = 8 }).ToList()
        };
        var result = await Service(fixture, tmdb, engine).RecommendAsync(new(MovieMood.Cheerful, [], null, null, null), default);
        Assert.Equal(3, tmdb.DiscoverCalls.Count); Assert.Equal(39, engine.Candidates.Count);
        Assert.DoesNotContain(engine.Candidates, c => c.Id == 101);
        Assert.Equal(watchlist, engine.Candidates.Single(c => c.Id == 102).SavedId);
        Assert.Equal(10, result.Results.Count); Assert.Equal(2, tmdb.DetailCalls);
    }
    [Fact]
    public async Task WorkflowCapsUniquePoolAt60EvenIfProviderReturnsOversizedPage()
    {
        await using var fixture = await TestDatabase.CreateAsync(); var engine = new CaptureEngine();
        var tmdb = new FakeTmdb { Page = p => new() { Page = p, TotalPages = 100, Results = Enumerable.Range(1, 100).Select(i => new TmdbMovieSummaryDto { Id = i, Title = "Movie" }).ToList() } };
        await Service(fixture, tmdb, engine).RecommendAsync(new(MovieMood.Calm, [], null, null, null), default);
        Assert.Equal(60, engine.Candidates.Count); Assert.Single(tmdb.DiscoverCalls);
    }
}
