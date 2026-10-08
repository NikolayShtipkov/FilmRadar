using System.Globalization;
using FilmRadar.Web.Domain;

namespace FilmRadar.Web.Services.Recommendations;

public sealed class MockRecommendationEngine : IRecommendationEngine
{
    public IReadOnlyList<RecommendationResult> Rank(RecommendationContext context, IReadOnlyCollection<RecommendationCandidate> candidates)
    {
        var moodGenres = MoodGenres.For(context.Mood);
        var hasPeriod = context.YearFrom.HasValue || context.YearTo.HasValue;
        // Every candidate uses the same denominator, even when its metadata is missing.
        var denominator = 30 + (context.PreferredGenres.Count > 0 ? 35 : 0) + (context.HasHistory ? 25 : 0) + (hasPeriod ? 10 : 0);
        return candidates.Select(movie =>
        {
            var genres = movie.GenreIds.Distinct().Order().ToArray();
            var components = new List<ScoreComponent>();
            string Names(IEnumerable<int> ids) => string.Join(", ", ids.Select(id => context.GenreNames.GetValueOrDefault(id, $"жанр {id}")));
            double Ratio(IEnumerable<int> matches) => genres.Length == 0 ? 0 : (double)matches.Count() / genres.Length;
            if (context.PreferredGenres.Count > 0)
            {
                var matches = genres.Where(context.PreferredGenres.Contains).ToArray();
                components.Add(new("Жанрове", 35, Ratio(matches), matches.Length > 0 ? $"Сред любимите ви жанрове: {Names(matches)}." : null));
            }
            if (context.HasHistory)
            {
                var affinity = genres.Length == 0 ? 0 : genres.Average(g => context.Affinities.GetValueOrDefault(g, 0.5));
                var liked = genres.Where(g => context.Affinities.TryGetValue(g, out var value) && value > 0.5).ToArray();
                var reason = affinity > 0.5 && liked.Length > 0
                    ? $"Оценявате {Names(liked)} средно с {(liked.Average(g => context.Affinities[g]) * 4).ToString("0.0", CultureInfo.GetCultureInfo("bg-BG"))} от 4."
                    : null;
                components.Add(new("Лични оценки", 25, affinity, reason));
            }
            var moodMatch = genres.Intersect(moodGenres).ToArray();
            components.Add(new("Настроение", 15, Ratio(moodMatch), moodMatch.Length > 0 ? $"Жанровете отговарят на настроение „{Labels.Mood(context.Mood)}“." : null));
            var rating = movie.VoteAverage is double r && double.IsFinite(r) ? Math.Clamp(r, 0, 10) : 0;
            var votes = Math.Max(0, movie.VoteCount);
            var confidence = Math.Min(votes / 200.0, 1.0);
            components.Add(new("TMDB", 15, rating / 10.0 * confidence,
                rating > 0 && votes > 0 ? $"TMDB: {rating.ToString("0.0", CultureInfo.InvariantCulture)}/10 от {votes} гласа{(votes < 200 ? " (ограничена надеждност)" : "")}." : null));
            if (hasPeriod)
            {
                var year = movie.ReleaseDate?.Year;
                var matches = year.HasValue && (!context.YearFrom.HasValue || year >= context.YearFrom) && (!context.YearTo.HasValue || year <= context.YearTo);
                components.Add(new("Период", 10, matches ? 1 : 0, matches ? $"Издаден през {year} г. — в избрания период." : null));
            }
            var score = Math.Clamp(100 * components.Sum(c => c.Weight * c.Value) / denominator, 0, 100);
            var reasons = components.Where(c => c.Value > 0 && c.Reason != null).OrderByDescending(c => c.Weight * c.Value)
                .Take(4).Select(c => c.Reason!).ToArray();
            return new RecommendationResult(movie, score, reasons, components);
        }).OrderByDescending(r => r.Score).ThenByDescending(r => r.Movie.VoteAverage)
          .ThenByDescending(r => r.Movie.VoteCount).ThenBy(r => r.Movie.Id).ToArray();
    }
}
