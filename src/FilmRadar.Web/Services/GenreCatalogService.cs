using FilmRadar.Web.Data;
using FilmRadar.Web.Domain;
using FilmRadar.Web.Integrations.Tmdb;
using Microsoft.EntityFrameworkCore;

namespace FilmRadar.Web.Services;

public sealed class GenreCatalogService(FilmRadarDbContext db, ITmdbClient tmdb, TimeProvider clock) : IGenreCatalogService
{
    public async Task<IReadOnlyList<GenreOption>> GetAsync(CancellationToken ct) => await db.Genres.AsNoTracking()
        .OrderBy(g => g.Name).Select(g => new GenreOption(g.TmdbGenreId, g.Name)).ToListAsync(ct);

    public async Task<bool> RefreshAsync(CancellationToken ct)
    {
        IReadOnlyList<TmdbGenreDto> remote;
        try { remote = await tmdb.GetMovieGenresAsync(ct); }
        catch (TmdbApiException) { return false; }
        var local = await db.Genres.ToDictionaryAsync(g => g.TmdbGenreId, ct);
        foreach (var item in remote.Where(g => g.Id > 0 && !string.IsNullOrWhiteSpace(g.Name)))
        {
            if (!local.TryGetValue(item.Id, out var genre))
            {
                genre = new Genre { TmdbGenreId = item.Id };
                db.Genres.Add(genre);
                local.Add(item.Id, genre);
            }
            genre.Name = item.Name[..Math.Min(item.Name.Length, 100)];
            genre.LastSyncedAtUtc = clock.GetUtcNow().UtcDateTime;
        }
        await db.SaveChangesAsync(ct);
        return true;
    }
}
