using FilmRadar.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace FilmRadar.Web.Data;

public sealed class DatabaseInitializer(FilmRadarDbContext db, TimeProvider clock)
{
    public static readonly IReadOnlyDictionary<int, string> BaselineGenres = new Dictionary<int, string>
    {
        [28] = "Екшън",
        [12] = "Приключенски",
        [16] = "Анимация",
        [35] = "Комедия",
        [80] = "Криминален",
        [99] = "Документален",
        [18] = "Драма",
        [10751] = "Семеен",
        [14] = "Фентъзи",
        [36] = "Исторически",
        [27] = "Ужаси",
        [10402] = "Музикален",
        [9648] = "Мистерия",
        [10749] = "Романтичен",
        [878] = "Научна фантастика",
        [10770] = "Телевизионен",
        [53] = "Трилър",
        [10752] = "Военен",
        [37] = "Уестърн"
    };

    public async Task InitializeAsync(CancellationToken ct)
    {
        if (!await db.Profiles.AnyAsync(p => p.Id == LocalUser.ProfileId, ct))
            db.Profiles.Add(new UserProfile { Id = LocalUser.ProfileId, CreatedAtUtc = clock.GetUtcNow().UtcDateTime, UpdatedAtUtc = clock.GetUtcNow().UtcDateTime });
        var ids = await db.Genres.Select(g => g.TmdbGenreId).ToListAsync(ct);
        foreach (var (id, name) in BaselineGenres.Where(g => !ids.Contains(g.Key)))
            db.Genres.Add(new Genre { TmdbGenreId = id, Name = name });
        await db.SaveChangesAsync(ct);
    }
}
