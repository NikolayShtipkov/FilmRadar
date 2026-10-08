using FilmRadar.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace FilmRadar.Web.Data;

public sealed class FilmRadarDbContext(DbContextOptions<FilmRadarDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<SavedMovie> SavedMovies => Set<SavedMovie>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var profile = model.Entity<UserProfile>();
        profile.ToTable("Profiles", t =>
        {
            t.HasCheckConstraint("CK_Profile_Id", "Id = 1");
            t.HasCheckConstraint("CK_Profile_Name", "length(trim(DisplayName)) BETWEEN 2 AND 50");
            t.HasCheckConstraint("CK_Profile_Rating", "MinimumTmdbRating IS NULL OR MinimumTmdbRating BETWEEN 0 AND 10");
            t.HasCheckConstraint("CK_Profile_Years", "(PreferredReleaseYearFrom IS NULL OR PreferredReleaseYearFrom BETWEEN 1888 AND 2100) AND (PreferredReleaseYearTo IS NULL OR PreferredReleaseYearTo BETWEEN 1888 AND 2100) AND (PreferredReleaseYearFrom IS NULL OR PreferredReleaseYearTo IS NULL OR PreferredReleaseYearFrom <= PreferredReleaseYearTo)");
        });
        profile.Property(p => p.Id).ValueGeneratedNever();
        profile.Property(p => p.DisplayName).HasMaxLength(50);
        var genre = model.Entity<Genre>();
        genre.HasKey(g => g.TmdbGenreId);
        genre.Property(g => g.TmdbGenreId).ValueGeneratedNever();
        genre.Property(g => g.Name).HasMaxLength(100);
        genre.ToTable("Genres", t => t.HasCheckConstraint("CK_Genre_Id", "TmdbGenreId > 0"));

        var preference = model.Entity<UserPreferredGenre>();
        preference.HasKey(p => new { p.UserProfileId, p.TmdbGenreId });
        preference.HasOne<UserProfile>().WithMany(p => p.PreferredGenres).HasForeignKey(p => p.UserProfileId).OnDelete(DeleteBehavior.Cascade);
        preference.HasOne(p => p.Genre).WithMany().HasForeignKey(p => p.TmdbGenreId).OnDelete(DeleteBehavior.Restrict);

        var movie = model.Entity<SavedMovie>();
        movie.HasIndex(m => new { m.UserProfileId, m.TmdbMovieId }).IsUnique();
        movie.HasOne<UserProfile>().WithMany().HasForeignKey(m => m.UserProfileId).OnDelete(DeleteBehavior.Cascade);
        movie.Property(m => m.Title).HasMaxLength(500);
        movie.Property(m => m.OriginalTitle).HasMaxLength(500);
        movie.Property(m => m.PosterPath).HasMaxLength(250);
        movie.Property(m => m.Notes).HasMaxLength(1000);
        movie.ToTable("SavedMovies", t =>
        {
            t.HasCheckConstraint("CK_Movie_Id", "TmdbMovieId > 0");
            t.HasCheckConstraint("CK_Movie_Status", "Status IN (1, 2)");
            t.HasCheckConstraint("CK_Movie_Rating", "PersonalRating IS NULL OR PersonalRating BETWEEN 0 AND 4");
            t.HasCheckConstraint("CK_Movie_Tmdb", "TmdbVoteAverage IS NULL OR TmdbVoteAverage BETWEEN 0 AND 10");
            t.HasCheckConstraint("CK_Movie_State", "(Status = 1 AND PersonalRating IS NULL AND WatchedOn IS NULL) OR (Status = 2 AND WatchedOn IS NOT NULL)");
            t.HasCheckConstraint("CK_Movie_Notes", "Notes IS NULL OR length(Notes) <= 1000");
        });
        var relation = model.Entity<SavedMovieGenre>();
        relation.HasKey(g => new { g.SavedMovieId, g.TmdbGenreId });
        relation.HasOne<SavedMovie>().WithMany(m => m.Genres).HasForeignKey(g => g.SavedMovieId).OnDelete(DeleteBehavior.Cascade);
        relation.HasOne(g => g.Genre).WithMany().HasForeignKey(g => g.TmdbGenreId).OnDelete(DeleteBehavior.Restrict);
    }
}
