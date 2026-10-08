using System.Globalization;
using System.Text.Json.Serialization;

namespace FilmRadar.Web.Integrations.Tmdb;

public sealed class TmdbOptions
{
    public string BaseUrl { get; set; } = "https://api.themoviedb.org/3/";
    public string ImageBaseUrl { get; set; } = "https://image.tmdb.org/t/p/w500";
    public string Language { get; set; } = "bg-BG";
    public string Region { get; set; } = "BG";
    public int TimeoutSeconds { get; set; } = 10;
    public string? ReadAccessToken { get; set; }
}

public record TmdbSearchRequest(string Query, int? Year = null, int Page = 1);
public record TmdbDiscoverRequest(IReadOnlyCollection<int> GenreIds, int? YearFrom = null,
    int? YearTo = null, double? MinimumRating = null, string Sort = "popularity.desc", int Page = 1);
public sealed class TmdbPagedResult<T>
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalResults { get; set; }
    [JsonRequired] public List<T> Results { get; set; } = [];
}
public class TmdbMovieSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? OriginalTitle { get; set; }
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public string? ReleaseDate { get; set; }
    public List<int> GenreIds { get; set; } = [];
    public double? VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public bool Adult { get; set; }
    [JsonIgnore] public DateOnly? ParsedDate => DateOnly.TryParseExact(ReleaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
}
public sealed class TmdbMovieDetailsDto : TmdbMovieSummaryDto
{
    public int? Runtime { get; set; }
    public List<TmdbGenreDto> Genres { get; set; } = [];
}
public sealed record TmdbGenreDto(int Id, string Name);
public sealed class TmdbGenresResponse { [JsonRequired] public List<TmdbGenreDto> Genres { get; set; } = []; }

public enum TmdbErrorKind { Configuration, Unauthorized, RateLimited, Unavailable, InvalidPayload }
public sealed class TmdbApiException(TmdbErrorKind kind) : Exception(kind.ToString())
{
    public TmdbErrorKind Kind { get; } = kind;
    public string UserMessage => Kind switch
    {
        TmdbErrorKind.Configuration => "Каталогът още не е свързан. Добавете TMDB token според README. Личният списък и профилът остават достъпни.",
        TmdbErrorKind.Unauthorized => "Връзката с каталога не е разрешена. Проверете TMDB token.",
        TmdbErrorKind.RateLimited => "Каталогът получава твърде много заявки. Опитайте отново след малко.",
        TmdbErrorKind.InvalidPayload => "Каталогът върна неочакван отговор. Опитайте отново по-късно.",
        _ => "Каталогът временно не е достъпен. Опитайте отново. Личният ви списък е запазен."
    };
}

public interface ITmdbClient
{
    Task<TmdbPagedResult<TmdbMovieSummaryDto>> GetPopularMoviesAsync(int page, CancellationToken ct);
    Task<TmdbPagedResult<TmdbMovieSummaryDto>> SearchMoviesAsync(TmdbSearchRequest request, CancellationToken ct);
    Task<TmdbPagedResult<TmdbMovieSummaryDto>> DiscoverMoviesAsync(TmdbDiscoverRequest request, CancellationToken ct);
    Task<TmdbMovieDetailsDto?> GetMovieDetailsAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<TmdbGenreDto>> GetMovieGenresAsync(CancellationToken ct);
}
