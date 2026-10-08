using FilmRadar.Web.Services;
using FilmRadar.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FilmRadar.Web.Controllers;

public sealed class ProfileController(IProfileService profiles, IGenreCatalogService genres) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Edit(CancellationToken ct)
    {
        var profile = await profiles.GetAsync(ct);
        return View(new ProfilePage
        {
            Genres = await genres.GetAsync(ct),
            Input = new ProfileInput
            {
                DisplayName = profile.DisplayName,
                GenreIds = profile.GenreIds.ToList(),
                YearFrom = profile.YearFrom,
                YearTo = profile.YearTo,
                MinimumRating = (int?)profile.MinimumRating
            }
        });
    }
    [HttpPost]
    public async Task<IActionResult> Edit([Bind(Prefix = "Input")] ProfileInput input, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await profiles.UpdateAsync(new(input.DisplayName, input.GenreIds, input.YearFrom, input.YearTo, input.MinimumRating), ct);
            if (result.Status == MutationStatus.Success) { TempData["Message"] = "Предпочитанията са запазени."; return RedirectToAction(nameof(Edit)); }
            ModelState.AddModelError("", result.Error!);
        }
        return View(new ProfilePage { Input = input, Genres = await genres.GetAsync(ct) });
    }
    [HttpPost]
    public async Task<IActionResult> RefreshGenres(CancellationToken ct)
    {
        var success = await genres.RefreshAsync(ct);
        TempData[success ? "Message" : "Error"] = success ? "Жанровете са обновени от TMDB." : "Каталогът е недостъпен. Запазени са локалните жанрове.";
        return RedirectToAction(nameof(Edit));
    }
}
