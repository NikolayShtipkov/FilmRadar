using System.ComponentModel.DataAnnotations;
using FilmRadar.Web.Services;

namespace FilmRadar.Web.ViewModels;

public sealed class TitleSearchInput
{
    [Display(Name = "Заглавие"), StringLength(150, ErrorMessage = "Заглавието трябва да е до 150 символа.")]
    public string? Query { get; set; }
    [Display(Name = "Година"), Range(1888, 2100, ErrorMessage = "Годината трябва да е между 1888 и 2100.")]
    public int? Year { get; set; }
    [Range(1, 500, ErrorMessage = "Невалиден номер на страница.")] public int Page { get; set; } = 1;
}
public class PeriodInput : IValidatableObject
{
    [Display(Name = "От година"), Range(1888, 2100, ErrorMessage = "Невалидна начална година.")] public int? YearFrom { get; set; }
    [Display(Name = "До година"), Range(1888, 2100, ErrorMessage = "Невалидна крайна година.")] public int? YearTo { get; set; }
    [Display(Name = "Минимален TMDB рейтинг"), Range(0, 10, ErrorMessage = "Рейтингът трябва да е между 0 и 10.")] public int? MinimumRating { get; set; }
    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (YearFrom > YearTo) yield return new("Началната година трябва да предхожда крайната.", [nameof(YearTo)]);
    }
}
public sealed class DiscoverInput : PeriodInput
{
    [Display(Name = "Жанр"), Range(1, int.MaxValue)] public int? GenreId { get; set; }
    [Range(1, 500, ErrorMessage = "Невалиден номер на страница.")] public int Page { get; set; } = 1;
    [RegularExpression("^(popularity.desc|vote_average.desc|primary_release_date.desc)$", ErrorMessage = "Невалидно сортиране.")]
    public string Sort { get; set; } = "popularity.desc";
}
public sealed class ProfileInput : PeriodInput
{
    [Display(Name = "Име"), Required(ErrorMessage = "Въведете име."), StringLength(50, MinimumLength = 2, ErrorMessage = "Името трябва да е между 2 и 50 символа.")]
    public string DisplayName { get; set; } = "";
    public List<int> GenreIds { get; set; } = [];
}
public sealed class RecommendationInput : PeriodInput
{
    [Display(Name = "Настроение"), Range(1, 6, ErrorMessage = "Изберете настроение от списъка.")]
    public int Mood { get; set; } = 1;
    public List<int> GenreIds { get; set; } = [];
    public bool Generate { get; set; }
}
public sealed class EditMovieInput
{
    [Display(Name = "Статус"), Required, Range(1, 2, ErrorMessage = "Изберете валиден статус.")] public int? StatusValue { get; set; }
    [Display(Name = "Моята оценка"), Range(0, 4, ErrorMessage = "Оценката трябва да е от 0 до 4.")] public int? PersonalRatingValue { get; set; }
    [Display(Name = "Любим")] public bool IsFavorite { get; set; }
    [Display(Name = "Бележка"), StringLength(1000, ErrorMessage = "Бележката трябва да е до 1000 символа.")] public string? Notes { get; set; }
    [Display(Name = "Дата на гледане"), DataType(DataType.Date)] public DateOnly? WatchedOn { get; set; }
    [Display(Name = "Потвърждавам изчистването на оценката и датата при връщане в „За гледане“.")]
    public bool ConfirmReset { get; set; }
}
