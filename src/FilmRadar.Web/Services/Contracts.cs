using FilmRadar.Web.Domain;

namespace FilmRadar.Web.Services;

public record GenreOption(int Id, string Name);
public record ProfileData(string DisplayName, IReadOnlyList<int> GenreIds, int? YearFrom, int? YearTo, double? MinimumRating);
public record ProfileUpdate(string DisplayName, IReadOnlyList<int> GenreIds, int? YearFrom, int? YearTo, double? MinimumRating);
public record MovieUpdate(int Status, int? Rating, bool IsFavorite, string? Notes, DateOnly? WatchedOn, bool ConfirmReset = false);
public record LocalMovie(int Id, int TmdbId, string Title, string? OriginalTitle, string? PosterPath,
    DateOnly? ReleaseDate, double? TmdbRating, WatchStatus Status, bool IsFavorite,
    PersonalRating? Rating, string? Notes, DateOnly? WatchedOn, IReadOnlyList<int> GenreIds);
public record RatingHistory(int Rating, IReadOnlyList<int> GenreIds);
public enum MutationStatus { Success, Duplicate, NotFound, Invalid }
public record MutationResult(MutationStatus Status, int? Id = null, string? Error = null);

public interface IGenreCatalogService
{
    Task<IReadOnlyList<GenreOption>> GetAsync(CancellationToken ct);
    Task<bool> RefreshAsync(CancellationToken ct);
}
public interface IProfileService
{
    Task<ProfileData> GetAsync(CancellationToken ct);
    Task<MutationResult> UpdateAsync(ProfileUpdate update, CancellationToken ct);
}
public interface IUserMovieService
{
    Task<IReadOnlyList<LocalMovie>> ListAsync(string filter, string sort, CancellationToken ct);
    Task<LocalMovie?> GetAsync(int id, CancellationToken ct);
    Task<IReadOnlyDictionary<int, LocalMovie>> GetStatesAsync(IEnumerable<int> tmdbIds, CancellationToken ct);
    Task<IReadOnlyList<RatingHistory>> GetHistoryAsync(CancellationToken ct);
    Task<MutationResult> AddAsync(int tmdbId, CancellationToken ct);
    Task<MutationResult> AddAsync(int tmdbId, WatchStatus status, CancellationToken ct);
    Task<MutationResult> UpdateAsync(int id, MovieUpdate update, CancellationToken ct);
    Task<MutationResult> DeleteAsync(int id, CancellationToken ct);
}
