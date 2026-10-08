using FilmRadar.Web.Domain;

namespace FilmRadar.Web.Services.Recommendations;

public record RecommendationCandidate(int Id, string Title, string? PosterPath, DateOnly? ReleaseDate,
    IReadOnlyList<int> GenreIds, double? VoteAverage, int VoteCount, int? SavedId = null);
public record RecommendationContext(IReadOnlySet<int> PreferredGenres, IReadOnlyDictionary<int, double> Affinities,
    bool HasHistory, MovieMood Mood, int? YearFrom, int? YearTo, IReadOnlyDictionary<int, string> GenreNames);
public record ScoreComponent(string Name, double Weight, double Value, string? Reason);
public record RecommendationResult(RecommendationCandidate Movie, double Score, IReadOnlyList<string> Reasons,
    IReadOnlyList<ScoreComponent> Components)
{
    public string? Information => Reasons.Count == 0 ? "Няма достатъчно данни за силно съвпадение с предпочитанията ви." : null;
}
public record RecommendationRequest(MovieMood Mood, IReadOnlyList<int> GenreIds, int? YearFrom, int? YearTo, double? MinimumRating);
public record RecommendationPageData(IReadOnlyList<RecommendationResult> Results, RecommendationRequest EffectiveRequest);
public interface IRecommendationEngine
{
    IReadOnlyList<RecommendationResult> Rank(RecommendationContext context, IReadOnlyCollection<RecommendationCandidate> candidates);
}
public interface IRecommendationService
{
    Task<RecommendationPageData> RecommendAsync(RecommendationRequest request, CancellationToken ct);
}

public static class MoodGenres
{
    public static IReadOnlyList<int> For(MovieMood mood) => mood switch
    {
        MovieMood.Cheerful => [35, 16, 10751],
        MovieMood.Tense => [53, 9648, 80],
        MovieMood.Romantic => [10749, 18],
        MovieMood.Adventurous => [12, 14, 28],
        MovieMood.Calm => [18, 99, 10751],
        MovieMood.Thoughtful => [878, 18, 99, 9648],
        _ => throw new ArgumentOutOfRangeException(nameof(mood))
    };
}
