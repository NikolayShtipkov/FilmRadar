using System.ComponentModel.DataAnnotations;

namespace FilmRadar.Web.Domain;

public enum PersonalRating
{
    [Display(Name = "За нищо не става")] Terrible = 0,
    [Display(Name = "Зле")] Bad = 1,
    [Display(Name = "Става")] Okay = 2,
    [Display(Name = "Важи")] Good = 3,
    [Display(Name = "Топ")] Top = 4
}

public enum WatchStatus
{
    [Display(Name = "За гледане")] Watchlist = 1,
    [Display(Name = "Гледан")] Watched = 2
}

public enum MovieMood
{
    [Display(Name = "Весело")] Cheerful = 1,
    [Display(Name = "Напрегнато")] Tense,
    [Display(Name = "Романтично")] Romantic,
    [Display(Name = "Приключенско")] Adventurous,
    [Display(Name = "Спокойно")] Calm,
    [Display(Name = "Замислящо")] Thoughtful
}

public static class Labels
{
    public static string Rating(PersonalRating? rating) => rating switch
    {
        PersonalRating.Terrible => "0 — За нищо не става",
        PersonalRating.Bad => "1 — Зле",
        PersonalRating.Okay => "2 — Става",
        PersonalRating.Good => "3 — Важи",
        PersonalRating.Top => "4 — Топ",
        _ => "Не е оценен"
    };
    public static string Status(WatchStatus status) => status == WatchStatus.Watched ? "Гледан" : "За гледане";
    public static string Mood(MovieMood mood) => mood switch
    {
        MovieMood.Cheerful => "Весело",
        MovieMood.Tense => "Напрегнато",
        MovieMood.Romantic => "Романтично",
        MovieMood.Adventurous => "Приключенско",
        MovieMood.Calm => "Спокойно",
        MovieMood.Thoughtful => "Замислящо",
        _ => "Неизвестно"
    };
}
