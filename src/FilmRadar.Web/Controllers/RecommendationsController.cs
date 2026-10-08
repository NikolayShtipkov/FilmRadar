using System.ComponentModel.DataAnnotations;
using FilmRadar.Web.Domain;
using FilmRadar.Web.Integrations.Tmdb;
using FilmRadar.Web.Services;
using FilmRadar.Web.Services.Recommendations;
using FilmRadar.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FilmRadar.Web.Controllers;

public sealed class RecommendationsController(IRecommendationService recommendations, IGenreCatalogService genres, IProfileService profiles) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([Bind(Prefix = "Input")] RecommendationInput input, CancellationToken ct)
    {
        var profile = await profiles.GetAsync(ct);
        var page = new RecommendationPage { Input = input, Genres = await genres.GetAsync(ct) };
        var from = input.YearFrom ?? profile.YearFrom;
        var to = input.YearTo ?? profile.YearTo;
        var min = input.MinimumRating ?? profile.MinimumRating;
        page.EffectiveCriteria = $"Период: {from?.ToString() ?? "без начало"} – {to?.ToString() ?? "без край"} · Минимален TMDB рейтинг: {min?.ToString() ?? "няма"}";
        if (input.Generate && ModelState.IsValid)
        {
            try
            {
                page.Results = (await recommendations.RecommendAsync(new((MovieMood)input.Mood, input.GenreIds,
                    input.YearFrom, input.YearTo, input.MinimumRating), ct)).Results;
            }
            catch (TmdbApiException ex) { page.Error = ex.UserMessage; }
            catch (ValidationException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return View(page);
    }
}
