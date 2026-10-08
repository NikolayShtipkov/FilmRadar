using FilmRadar.Web.Data;
using FilmRadar.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace FilmRadar.Web.Services;

public sealed class ProfileService(FilmRadarDbContext db, TimeProvider clock) : IProfileService
{
    public async Task<ProfileData> GetAsync(CancellationToken ct)
    {
        var p = await db.Profiles.AsNoTracking().Include(p => p.PreferredGenres).SingleAsync(p => p.Id == LocalUser.ProfileId, ct);
        return new(p.DisplayName, p.PreferredGenres.Select(g => g.TmdbGenreId).ToArray(), p.PreferredReleaseYearFrom, p.PreferredReleaseYearTo, p.MinimumTmdbRating);
    }

    public async Task<MutationResult> UpdateAsync(ProfileUpdate update, CancellationToken ct)
    {
        var name = update.DisplayName?.Trim() ?? "";
        if (name.Length is < 2 or > 50 || !ValidPeriod(update.YearFrom, update.YearTo) ||
            update.MinimumRating is double r && (!double.IsFinite(r) || r is < 0 or > 10))
            return new(MutationStatus.Invalid, Error: "Проверете името, периода и минималния рейтинг.");
        var known = await db.Genres.Select(g => g.TmdbGenreId).ToListAsync(ct);
        if (update.GenreIds.Except(known).Any()) return new(MutationStatus.Invalid, Error: "Изберете жанрове от списъка.");
        var profile = await db.Profiles.Include(p => p.PreferredGenres).SingleAsync(p => p.Id == LocalUser.ProfileId, ct);
        profile.DisplayName = name;
        profile.PreferredReleaseYearFrom = update.YearFrom;
        profile.PreferredReleaseYearTo = update.YearTo;
        profile.MinimumTmdbRating = update.MinimumRating;
        profile.UpdatedAtUtc = clock.GetUtcNow().UtcDateTime;
        profile.PreferredGenres.RemoveAll(g => !update.GenreIds.Contains(g.TmdbGenreId));
        foreach (var id in update.GenreIds.Distinct().Except(profile.PreferredGenres.Select(g => g.TmdbGenreId)))
            profile.PreferredGenres.Add(new UserPreferredGenre { UserProfileId = LocalUser.ProfileId, TmdbGenreId = id });
        await db.SaveChangesAsync(ct);
        return new(MutationStatus.Success);
    }

    public static bool ValidPeriod(int? from, int? to) =>
        (from is null or >= 1888 and <= 2100) && (to is null or >= 1888 and <= 2100) && !(from > to);
}
