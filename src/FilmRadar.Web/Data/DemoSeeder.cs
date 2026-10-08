using FilmRadar.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace FilmRadar.Web.Data;

public sealed class DemoSeeder(FilmRadarDbContext db, TimeProvider clock)
{
    public async Task SeedAsync(CancellationToken ct)
    {
        // Synthetic titles keep the offline fixture independent of licensed API snapshots.
        var samples = new (int Id, string Title, int Genre, PersonalRating? Rating)[]
        {
            (900000001,"Демо: Отвъд орбитата",878,PersonalRating.Top),
            (900000002,"Демо: Последният сигнал",878,PersonalRating.Top),
            (900000003,"Демо: Случайна среща",10749,PersonalRating.Terrible),
            (900000004,"Демо: Нощен влак",53,PersonalRating.Good),
            (900000005,"Демо: Почивен ден",35,PersonalRating.Okay),
            (900000006,"Демо: Нови хоризонти",12,null)
        };
        foreach (var sample in samples)
        {
            if (await db.SavedMovies.AnyAsync(m => m.TmdbMovieId == sample.Id, ct)) continue;
            db.SavedMovies.Add(new SavedMovie
            {
                UserProfileId = LocalUser.ProfileId,
                TmdbMovieId = sample.Id,
                Title = sample.Title,
                Status = sample.Rating.HasValue ? WatchStatus.Watched : WatchStatus.Watchlist,
                PersonalRating = sample.Rating,
                IsFavorite = sample.Rating == PersonalRating.Top,
                WatchedOn = sample.Rating.HasValue ? DateOnly.FromDateTime(clock.GetLocalNow().DateTime) : null,
                Notes = "Измислен демонстрационен запис; няма съответстваща страница в TMDB.",
                AddedAtUtc = clock.GetUtcNow().UtcDateTime,
                UpdatedAtUtc = clock.GetUtcNow().UtcDateTime,
                Genres = [new SavedMovieGenre { TmdbGenreId = sample.Genre }]
            });
        }
        await db.SaveChangesAsync(ct);
    }
}
