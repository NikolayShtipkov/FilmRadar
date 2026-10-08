using FilmRadar.Web.Domain;
using FilmRadar.Web.Services.Recommendations;

namespace FilmRadar.Tests;

public class RecommendationEngineTests
{
    private readonly MockRecommendationEngine engine = new();
    private static RecommendationContext Context(bool all = false) => new(
        all ? new HashSet<int> { 35 } : new HashSet<int>(), all ? new Dictionary<int, double> { [35] = 1 } : new Dictionary<int, double>(),
        all, MovieMood.Cheerful, all ? 2000 : null, all ? 2030 : null, new Dictionary<int, string> { [35] = "Комедия" });
    private static RecommendationCandidate Movie(int id = 1, int[]? genres = null, double? rating = 10, int votes = 200) =>
        new(id, "Film", null, new DateOnly(2020, 1, 1), genres ?? [35], rating, votes);

    [Fact] public void PerfectMatchScores100() => Assert.Equal(100, engine.Rank(Context(true), [Movie()]).Single().Score);
    [Fact]
    public void ColdStartNormalizesOnlyActiveComponents()
    {
        var result = engine.Rank(Context(), [Movie()]).Single();
        Assert.Equal(100, result.Score); Assert.Equal(2, result.Components.Count);
    }
    [Fact]
    public void MissingMetadataDoesNotShrinkDenominator()
    {
        var result = engine.Rank(Context(true), [Movie(genres: [], rating: null) with { ReleaseDate = null }]).Single();
        Assert.Equal(0, result.Score); Assert.Empty(result.Reasons); Assert.NotNull(result.Information);
    }
    [Fact]
    public void GenreRatioUsesCandidateGenresNotPreferenceCount()
    {
        var result = engine.Rank(Context(true), [Movie(genres: [35, 18])]).Single();
        Assert.Equal(0.5, result.Components.Single(c => c.Name == "Жанрове").Value);
    }
    [Fact]
    public void DuplicateGenresDoNotChangeScore() =>
        Assert.Equal(engine.Rank(Context(true), [Movie()])[0].Score, engine.Rank(Context(true), [Movie(genres: [35, 35])])[0].Score);
    [Fact]
    public void UnknownHistoryGenreIsNeutralWithoutPositiveReason()
    {
        var result = engine.Rank(Context(true), [Movie(genres: [18])]).Single();
        var history = result.Components.Single(c => c.Name == "Лични оценки");
        Assert.Equal(0.5, history.Value); Assert.Null(history.Reason);
    }
    [Fact]
    public void ZeroRatingHistoryIsNegativeNotMissing()
    {
        var context = Context(true) with { Affinities = new Dictionary<int, double> { [35] = 0 } };
        var result = engine.Rank(context, [Movie()]).Single();
        Assert.Equal(75, result.Score); Assert.Null(result.Components.Single(c => c.Name == "Лични оценки").Reason);
    }
    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 0.5)]
    [InlineData(200, 1)]
    [InlineData(1000, 1)]
    public void TmdbConfidenceUsesVoteCount(int votes, double expected) =>
        Assert.Equal(expected, engine.Rank(Context(), [Movie(votes: votes)])[0].Components.Single(c => c.Name == "TMDB").Value);
    [Fact]
    public void ReasonsAreLimitedToFourAndNotPadded()
    {
        Assert.Equal(4, engine.Rank(Context(true), [Movie()])[0].Reasons.Count);
        Assert.Single(engine.Rank(Context(), [Movie(genres: [], rating: 7)])[0].Reasons);
    }
    [Fact]
    public void TiesUseIdRegardlessOfInputOrder() =>
        Assert.Equal([1, 2, 3], engine.Rank(Context(), [Movie(3), Movie(1), Movie(2)]).Select(r => r.Movie.Id));
    [Fact]
    public void WatchlistBadgeDoesNotChangeScore() =>
        Assert.Equal(engine.Rank(Context(), [Movie()])[0].Score, engine.Rank(Context(), [Movie() with { SavedId = 42 }])[0].Score);
    [Fact]
    public void OneSidedPeriodIsActiveAndInclusive()
    {
        var context = Context() with { YearFrom = 2020 };
        Assert.Equal(100, engine.Rank(context, [Movie()])[0].Score);
        Assert.Equal(75, engine.Rank(context, [Movie() with { ReleaseDate = new(2019, 1, 1) }])[0].Score);
    }
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void EveryMoodHasGenres(int mood) => Assert.NotEmpty(MoodGenres.For((MovieMood)mood));

    [Fact]
    public void MoodSwitchChangesTopCandidate()
    {
        var candidates = new[] { Movie(1, [35]), Movie(2, [53]) };
        Assert.Equal(1, engine.Rank(Context(), candidates)[0].Movie.Id);
        Assert.Equal(2, engine.Rank(Context() with { Mood = MovieMood.Tense }, candidates)[0].Movie.Id);
    }
    [Fact]
    public void PreferredGenreOutranksOtherwiseEqualNonMatch()
    {
        var context = Context() with { PreferredGenres = new HashSet<int> { 878 } };
        Assert.Equal(2, engine.Rank(context, [Movie(1, [18]), Movie(2, [878])])[0].Movie.Id);
    }
    [Fact]
    public void HistoryTopOutranksHistoryZero()
    {
        var context = Context() with { HasHistory = true, Affinities = new Dictionary<int, double> { [18] = 0, [878] = 1 } };
        Assert.Equal(2, engine.Rank(context, [Movie(1, [18]), Movie(2, [878])])[0].Movie.Id);
    }
    [Fact]
    public void ReliableRatingOutranksSparseHighRating() =>
        Assert.Equal(2, engine.Rank(Context(), [Movie(1, rating: 9, votes: 2), Movie(2, rating: 8, votes: 500)])[0].Movie.Id);
    [Fact]
    public void EqualScoreUsesRatingThenVotesThenId()
    {
        var result = engine.Rank(Context(), [Movie(4, rating: 8, votes: 200), Movie(3, rating: 8, votes: 400), Movie(2, rating: 10, votes: 160)]);
        Assert.Equal([2, 3, 4], result.Select(r => r.Movie.Id));
    }
}
