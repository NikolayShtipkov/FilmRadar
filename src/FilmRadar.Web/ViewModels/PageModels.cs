using FilmRadar.Web.Domain;
using FilmRadar.Web.Services;
using FilmRadar.Web.Services.Recommendations;

namespace FilmRadar.Web.ViewModels;

public record MovieCard(int TmdbId, string Title, string? PosterPath, DateOnly? ReleaseDate, double? TmdbRating,
    int? SavedId = null, WatchStatus? Status = null, PersonalRating? PersonalRating = null, bool IsFavorite = false);
public sealed class BrowsePage
{
    public string Mode { get; set; } = "title";
    public TitleSearchInput TitleInput { get; set; } = new();
    public DiscoverInput DiscoverInput { get; set; } = new();
    public IReadOnlyList<GenreOption> Genres { get; set; } = [];
    public IReadOnlyList<MovieCard> Movies { get; set; } = [];
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public bool Searched { get; set; }
    public string? Error { get; set; }
    public string? PreviousUrl { get; set; }
    public string? NextUrl { get; set; }
}
public sealed class HomePage
{
    public IReadOnlyList<MovieCard> Movies { get; set; } = [];
    public string? Error { get; set; }
}
public record DetailsPage(MovieCard Movie, string? Overview, string? OriginalTitle, int? Runtime, IReadOnlyList<string> Genres);
public record ListPage(IReadOnlyList<MovieCard> Movies, string Filter, string Sort);
public sealed class EditPage
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool WasWatched { get; set; }
    public EditMovieInput Input { get; set; } = new();
}
public sealed class ProfilePage
{
    public ProfileInput Input { get; set; } = new();
    public IReadOnlyList<GenreOption> Genres { get; set; } = [];
}
public sealed class RecommendationPage
{
    public RecommendationInput Input { get; set; } = new();
    public IReadOnlyList<GenreOption> Genres { get; set; } = [];
    public IReadOnlyList<RecommendationResult> Results { get; set; } = [];
    public string? Error { get; set; }
    public string? EffectiveCriteria { get; set; }
}
public record ProblemPage(string Message, int StatusCode = 503);
