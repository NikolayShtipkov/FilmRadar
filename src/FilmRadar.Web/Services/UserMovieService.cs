using FilmRadar.Web.Data;
using FilmRadar.Web.Domain;
using FilmRadar.Web.Integrations.Tmdb;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FilmRadar.Web.Services;

public sealed class UserMovieService(FilmRadarDbContext db, ITmdbClient tmdb, TimeProvider clock) : IUserMovieService
{
    private IQueryable<SavedMovie> Read() => db.SavedMovies.AsNoTracking().Include(m => m.Genres).Where(m => m.UserProfileId == LocalUser.ProfileId);
    private static LocalMovie Map(SavedMovie m) => new(m.Id, m.TmdbMovieId, m.Title, m.OriginalTitle, m.PosterPath,
        m.ReleaseDate, m.TmdbVoteAverage, m.Status, m.IsFavorite, m.PersonalRating, m.Notes, m.WatchedOn, m.Genres.Select(g => g.TmdbGenreId).ToArray());

    public async Task<IReadOnlyList<LocalMovie>> ListAsync(string filter, string sort, CancellationToken ct)
    {
        var query = Read();
        query = filter switch { "watchlist" => query.Where(m => m.Status == WatchStatus.Watchlist), "watched" => query.Where(m => m.Status == WatchStatus.Watched), "favorites" => query.Where(m => m.IsFavorite), _ => query };
        query = sort switch { "title" => query.OrderBy(m => m.Title).ThenBy(m => m.Id), "rating" => query.OrderByDescending(m => m.PersonalRating).ThenBy(m => m.Id), _ => query.OrderByDescending(m => m.AddedAtUtc).ThenBy(m => m.Id) };
        return (await query.ToListAsync(ct)).Select(Map).ToArray();
    }
    public async Task<LocalMovie?> GetAsync(int id, CancellationToken ct)
    {
        var movie = await Read().SingleOrDefaultAsync(m => m.Id == id, ct);
        return movie is null ? null : Map(movie);
    }
    public async Task<IReadOnlyDictionary<int, LocalMovie>> GetStatesAsync(IEnumerable<int> tmdbIds, CancellationToken ct)
    {
        var ids = tmdbIds.Distinct().ToArray();
        return (await Read().Where(m => ids.Contains(m.TmdbMovieId)).ToListAsync(ct)).ToDictionary(m => m.TmdbMovieId, Map);
    }
    public async Task<IReadOnlyList<RatingHistory>> GetHistoryAsync(CancellationToken ct) =>
        (await Read().Where(m => m.Status == WatchStatus.Watched && m.PersonalRating != null).ToListAsync(ct))
            .Select(m => new RatingHistory((int)m.PersonalRating!.Value, m.Genres.Select(g => g.TmdbGenreId).ToArray())).ToArray();

    public async Task<MutationResult> AddAsync(int tmdbId, CancellationToken ct)
    {
        if (tmdbId <= 0) return new(MutationStatus.Invalid, Error: "Невалиден филм.");
        var existing = await db.SavedMovies.Where(m => m.UserProfileId == LocalUser.ProfileId && m.TmdbMovieId == tmdbId).Select(m => (int?)m.Id).SingleOrDefaultAsync(ct);
        if (existing.HasValue) return new(MutationStatus.Duplicate, existing);
        var details = await tmdb.GetMovieDetailsAsync(tmdbId, ct);
        if (details is null || details.Adult) return new(MutationStatus.NotFound);
        if (details.Id != tmdbId || string.IsNullOrWhiteSpace(details.Title)) throw new TmdbApiException(TmdbErrorKind.InvalidPayload);
        var known = await db.Genres.Select(g => g.TmdbGenreId).ToListAsync(ct);
        var genres = details.Genres.Where(g => g.Id > 0 && !string.IsNullOrWhiteSpace(g.Name)).DistinctBy(g => g.Id).ToArray();
        foreach (var g in genres.Where(g => !known.Contains(g.Id))) db.Genres.Add(new Genre { TmdbGenreId = g.Id, Name = g.Name[..Math.Min(100, g.Name.Length)] });
        var movie = new SavedMovie
        {
            UserProfileId = LocalUser.ProfileId,
            TmdbMovieId = tmdbId,
            Title = details.Title[..Math.Min(500, details.Title.Length)],
            OriginalTitle = details.OriginalTitle,
            PosterPath = details.PosterPath,
            ReleaseDate = details.ParsedDate,
            TmdbVoteAverage = details.VoteAverage is >= 0 and <= 10 ? details.VoteAverage : null,
            AddedAtUtc = clock.GetUtcNow().UtcDateTime,
            UpdatedAtUtc = clock.GetUtcNow().UtcDateTime,
            Genres = genres.Select(g => new SavedMovieGenre { TmdbGenreId = g.Id }).ToList()
        };
        db.SavedMovies.Add(movie);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
        {
            // The unique index remains authoritative when two requests pass the pre-check.
            db.ChangeTracker.Clear();
            var duplicate = await db.SavedMovies.SingleOrDefaultAsync(m => m.UserProfileId == LocalUser.ProfileId && m.TmdbMovieId == tmdbId, ct);
            if (duplicate is null) throw;
            return new(MutationStatus.Duplicate, duplicate.Id);
        }
        return new(MutationStatus.Success, movie.Id);
    }

    public async Task<MutationResult> UpdateAsync(int id, MovieUpdate update, CancellationToken ct)
    {
        if (!Enum.IsDefined((WatchStatus)update.Status) || update.Rating is < 0 or > 4 || update.Notes?.Length > 1000)
            return new(MutationStatus.Invalid, Error: "Проверете статуса, оценката и дължината на бележката.");
        var movie = await db.SavedMovies.SingleOrDefaultAsync(m => m.Id == id && m.UserProfileId == LocalUser.ProfileId, ct);
        if (movie is null) return new(MutationStatus.NotFound);
        var status = (WatchStatus)update.Status;
        if (movie.Status == WatchStatus.Watched && status == WatchStatus.Watchlist && !update.ConfirmReset)
            return new(MutationStatus.Invalid, Error: "Потвърдете изчистването на оценката и датата при връщане в „За гледане“.");
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        if (status == WatchStatus.Watched && update.WatchedOn > today)
            return new(MutationStatus.Invalid, Error: "Датата на гледане не може да бъде в бъдещето.");
        movie.Status = status;
        movie.PersonalRating = status == WatchStatus.Watched ? (PersonalRating?)update.Rating : null;
        movie.WatchedOn = status == WatchStatus.Watched ? update.WatchedOn ?? movie.WatchedOn ?? today : null;
        movie.IsFavorite = update.IsFavorite;
        movie.Notes = string.IsNullOrWhiteSpace(update.Notes) ? null : update.Notes.Trim();
        movie.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(ct);
        return new(MutationStatus.Success, movie.Id);
    }

    public async Task<MutationResult> DeleteAsync(int id, CancellationToken ct)
    {
        var movie = await db.SavedMovies.SingleOrDefaultAsync(m => m.Id == id && m.UserProfileId == LocalUser.ProfileId, ct);
        if (movie is null) return new(MutationStatus.NotFound);
        db.SavedMovies.Remove(movie);
        await db.SaveChangesAsync(ct);
        return new(MutationStatus.Success);
    }
}
