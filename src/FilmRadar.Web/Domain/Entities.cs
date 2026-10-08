namespace FilmRadar.Web.Domain;

public static class LocalUser { public const int ProfileId = 1; }

public sealed class UserProfile
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "Киноман";
    public int? PreferredReleaseYearFrom { get; set; }
    public int? PreferredReleaseYearTo { get; set; }
    public double? MinimumTmdbRating { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<UserPreferredGenre> PreferredGenres { get; set; } = [];
}

public sealed class Genre
{
    public int TmdbGenreId { get; set; }
    public string Name { get; set; } = "";
    public DateTime? LastSyncedAtUtc { get; set; }
}

public sealed class UserPreferredGenre
{
    public int UserProfileId { get; set; }
    public int TmdbGenreId { get; set; }
    public Genre Genre { get; set; } = null!;
}

public sealed class SavedMovie
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }
    public int TmdbMovieId { get; set; }
    public string Title { get; set; } = "";
    public string? OriginalTitle { get; set; }
    public string? PosterPath { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public double? TmdbVoteAverage { get; set; }
    public WatchStatus Status { get; set; } = WatchStatus.Watchlist;
    public bool IsFavorite { get; set; }
    public PersonalRating? PersonalRating { get; set; }
    public string? Notes { get; set; }
    public DateTime AddedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateOnly? WatchedOn { get; set; }
    public List<SavedMovieGenre> Genres { get; set; } = [];
}

public sealed class SavedMovieGenre
{
    public int SavedMovieId { get; set; }
    public int TmdbGenreId { get; set; }
    public Genre Genre { get; set; } = null!;
}
