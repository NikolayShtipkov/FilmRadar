using System.ComponentModel.DataAnnotations;
using FilmRadar.Web.Domain;
using FilmRadar.Web.Integrations.Tmdb;

namespace FilmRadar.Web.Services.Recommendations;

public sealed class RecommendationService(IProfileService profiles, IUserMovieService movies,
    IGenreCatalogService genres, ITmdbClient tmdb, IRecommendationEngine engine,
    ILogger<RecommendationService> logger) : IRecommendationService
{
    public async Task<RecommendationPageData> RecommendAsync(RecommendationRequest request, CancellationToken ct)
    {
        var profile = await profiles.GetAsync(ct);
        var catalog = await genres.GetAsync(ct);
        var effective = request with
        {
            YearFrom = request.YearFrom ?? profile.YearFrom,
            YearTo = request.YearTo ?? profile.YearTo,
            MinimumRating = request.MinimumRating ?? profile.MinimumRating
        };
        if (!Enum.IsDefined(effective.Mood) || !ProfileService.ValidPeriod(effective.YearFrom, effective.YearTo) ||
            effective.GenreIds.Except(catalog.Select(g => g.Id)).Any() ||
            effective.MinimumRating is double r && (!double.IsFinite(r) || r is < 0 or > 10))
            throw new ValidationException("Проверете жанровете, настроението и ефективния период от профила.");
        var pool = new Dictionary<int, TmdbMovieSummaryDto>();
        for (var page = 1; page <= 3 && pool.Count < 60; page++)
        {
            var response = await tmdb.DiscoverMoviesAsync(new(effective.GenreIds, effective.YearFrom,
                effective.YearTo, effective.MinimumRating, Page: page), ct);
            foreach (var movie in response.Results.Where(m => m.Id > 0 && !m.Adult))
            {
                pool.TryAdd(movie.Id, movie);
                if (pool.Count == 60) break;
            }
            if (page >= response.TotalPages) break;
        }
        var states = await movies.GetStatesAsync(pool.Keys, ct);
        var history = await movies.GetHistoryAsync(ct);
        var affinities = history.SelectMany(h => h.GenreIds.Distinct().Select(g => (Genre: g, Rating: h.Rating)))
            .GroupBy(x => x.Genre).ToDictionary(g => g.Key, g => g.Average(x => x.Rating) / 4.0);
        var context = new RecommendationContext(profile.GenreIds.ToHashSet(), affinities, history.Count > 0,
            effective.Mood, effective.YearFrom, effective.YearTo, catalog.ToDictionary(g => g.Id, g => g.Name));
        var candidates = pool.Values.Where(m => !states.TryGetValue(m.Id, out var state) || state.Status != WatchStatus.Watched)
            .Select(m => new RecommendationCandidate(m.Id, m.Title, m.PosterPath, m.ParsedDate, m.GenreIds,
                m.VoteAverage, m.VoteCount, states.GetValueOrDefault(m.Id)?.Id)).ToArray();
        logger.LogInformation("Ranking {CandidateCount} candidates", candidates.Length);
        return new(engine.Rank(context, candidates).Take(10).ToArray(), effective);
    }
}
